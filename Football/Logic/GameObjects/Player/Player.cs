using Football.EventArguments;
using Football.Logic;
using Football.Logic.Conflicts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.Logic.GameObjects.Player
{
    public enum PlayerPosition
    {
        Goalkeeper,
        CentralDefender,
        LeftBack,
        RightBack,
        LeftMidfielder,
        RightMidfielder,
        CentralMidfielder,
        Striker
    }

    class Player : GameObject
    {
        public Player(int teamNumber, int id, PlayerPosition position, int height, int weight, Team playerTeam)
            : base()
        {
            TeamId = teamNumber;
            Id = id;

            if (teamNumber == 0)
            {
                ImageWithoutBallLeft = Properties.Resources.Player0Left;
                ImageWithoutBallRight = Properties.Resources.Player0Right;
                ImageWithBallLeft = Properties.Resources.Player0BallLeft;
                ImageWithBallRight = Properties.Resources.Player0BallRight;
                ImageOnGround = Properties.Resources.Player0OnGround;
            }
            else
            {
                ImageWithoutBallLeft = Properties.Resources.Player1Left;
                ImageWithoutBallRight = Properties.Resources.Player1Right;
                ImageWithBallLeft = Properties.Resources.Player1BallLeft;
                ImageWithBallRight = Properties.Resources.Player1BallRight;
                ImageOnGround = Properties.Resources.Player1OnGround;
            }

            IsMarkedUp = false;
            IsAllowedToTakeBall = true;

            this.Weight = weight;
            this.Height = height;
            this.Position = position;

            SetCharacteristics();
            MaxSpeed = 1;
            PlayerActions = new Queue<PlayerAction>();
            rnd = new Random();
            CheckPlayerImage();
        }

        public PlayerPosition Position { get; set; }
        public int Id { get; set; }
        public int TeamId { get; set; }
        public double TackleStrength { get; set; }
        public double TacklePrecision { get; set; }
        public double ShootPrecision { get; set; }
        public int ShootSpeed { get; set; }
        public int MaxSpeed { get; set; }
        public double BallControl { get; set; }
        public int ThrowStrength { get; set; }
        public int Weight { get; set; }
        public int Height { get; set; }

        private bool hasBall;
        public bool HasBall
        {
            get { return hasBall; }
            set
            {
                hasBall = value;
                CheckPlayerImage();
            }
        }
        public bool IsPassTarget { get; set; }
        public bool IsMarkedUp { get; set; }
        public bool NeedsIntelligence { get; set; }
        public bool LiesOnTheGround { get; set; }
        public bool IsAllowedToTakeBall { get; set; }

        public Image ImageWithBallLeft;
        public Image ImageWithBallRight;
        public Image ImageWithoutBallLeft;
        public Image ImageWithoutBallRight;
        public Image ImageOnGround;

        public Point StartLocation { get; set; }
        public int LeftSteps { get; set; }
        public int LeftActions { get; set; }
        public Queue<PlayerAction> PlayerActions { get; set; }
        public PlayerAction TemporaryAction { get; set; }//Shows a temporary direction run-line in realtime when the users move the mouse

        public event EventHandler BallContact;
        public event EventHandler LosesBall;
        public event EventHandler<PointReceivedArgs> MovesBall;
        public event EventHandler<FoulEventArgs> FouledAPlayer;

        private Random rnd;

        /// <summary>
        /// The player will move to to his next waypoints.
        /// </summary>
        /// <param name="gameBall"></param>
        /// <param name="isLastPointConflict"></param>
        /// <param name="reachedLastPoint"></param>
        public void Move(Ball gameBall, bool isLastPointConflict, ref bool reachedLastPoint)
        {
            CalculateWaysToTarget(false);
            while (LeftSteps > 0)
            {
                if (PlayerActions.Count > 0 && PlayerActions.Peek().WayToTarget.Count > 0)
                {

                    SetDirectionToNextWayPoint();
                    int newXCoordinate = XCoordinate + this.XDirection;
                    int newYCoordinate = YCoordinate + this.YDirection;
                    
                    //breaks the method if there is a conflict at the last point & the new point would be the last point.
                    if (IsLastPoint(new Point(newXCoordinate, newYCoordinate)) && isLastPointConflict)
                    {
                        reachedLastPoint = true;
                        break;
                    }

                    SetCoordinates(newXCoordinate, newYCoordinate);
                    
                    //move the ball to the new position if the player currently controls the ball
                    if (HasBall)
                    {
                        MoveBall(gameBall);
                    }
                    else
                    {
                        HasBall = TryToGetBall(gameBall);
                    }

                    CheckPlayerImage();
                    
                    //dequeues the way-queue if the new location equals the top waypoint
                    if (PlayerActions.Count > 0 && Location.Equals(PlayerActions.Peek().WayToTarget.Peek()))
                    {
                        if (PlayerActions.Peek().IsActionToGetBall && !HasBall)
                        {
                            IsPassTarget = true;
                        }
                        PlayerActions.Peek().WayToTarget.Dequeue();
                    }

                    //breaks if the player reached his target point
                    if (Location.Equals(PlayerActions.Peek().TargetPoint))
                    {
                        LeftSteps--;
                        break;
                    }
                }
                LeftSteps--;
            }
        }

        /// <summary>
        /// Moves the ball to the own new position.
        /// </summary>
        /// <param name="gameBall"></param>
        private void MoveBall(Ball gameBall)
        {
            Pathfinding pathfinding = new Pathfinding();

            gameBall.TargetPoint = null;
            gameBall.Speed = 0;
            gameBall.Location = Location;
            gameBall.ExactLocation = pathfinding.GetExactLocation(gameBall.Location);

            PointReceivedArgs args = new PointReceivedArgs(gameBall.Location, gameBall.ExactLocation);
            MovesBall(this, args);
        }

        /// <summary>
        /// Checks whether the player has the ball and raises the BallContact-/LosesBall-event if the state changes.
        /// </summary>
        /// <param name="ball"></param>
        /// <returns></returns>
        public bool TryToGetBall(Ball ball)
        {
            bool contact = Location.X == ball.XCoordinate && Location.Y == ball.YCoordinate;
            if (contact && ball.Speed > 0 && !hasBall && ball.TargetPoint.HasValue && !ball.TargetPoint.Value.Equals(Location))
            {
                //tries to get the ball
                double shootDistance = Pathfinding.DistanceBetweenPoints(ball.RootPoint, ball.TargetPoint.Value);
                double distanceToRoot = Pathfinding.DistanceBetweenPoints(ball.RootPoint, Location);
                double distanceToTarget = Pathfinding.DistanceBetweenPoints(ball.TargetPoint.Value, Location);
                double shortestDistance = (distanceToRoot < distanceToTarget) ? distanceToRoot : distanceToTarget;

                contact = shortestDistance < 5 * (BallControl + rnd.Next(0, 1));
            }

            if (contact && ball.LastPlayer == this && ball.TargetPoint.HasValue && !ball.TargetPoint.Value.Equals(Location))
            {
                contact = false;
            }

            if (!IsAllowedToTakeBall)
            {
                contact = false;
            }

            if (contact && !hasBall)
            {
                //releases the BallContact-event if the player has ballcontact now and if he did not had it before
                BallContact(this, EventArgs.Empty);
            }
            //releases the LosesBall-event if the player doesn't have ball contact at the moment, but he had it in the last round.
            if (!contact && hasBall)
            {
                LosesBall(this, EventArgs.Empty);
            }

            return contact;
        }

        /// <summary>
        /// Sets the correct image depending on hasBall-boolean.
        /// </summary>
        public void CheckPlayerImage()
        {
            if (LiesOnTheGround)
            {
                this.ObjectImage = ImageOnGround;
            }
            else
            {
                bool leftDirection = XDirection == -1 || (XDirection == 0 && TeamId == 1);

                if (leftDirection)
                {
                    if (Position == PlayerPosition.Goalkeeper)
                    {
                        this.ObjectImage = (hasBall) ? Properties.Resources.GoalKeeperLeftBall : Properties.Resources.GoalKeeperLeft;
                    }
                    else
                    {
                        this.ObjectImage = (hasBall) ? ImageWithBallLeft : ImageWithoutBallLeft;
                    }
                }
                else
                {
                    if (Position == PlayerPosition.Goalkeeper)
                    {
                        this.ObjectImage = (hasBall) ? Properties.Resources.GoalKeeperRightBall : Properties.Resources.GoalKeeperRight;
                    }
                    else
                    {
                        this.ObjectImage = (hasBall) ? ImageWithBallRight : ImageWithoutBallRight;
                    }
                }
            }
        }

        /// <summary>
        /// Sets player's direction to his next waypoint.
        /// </summary>
        public void SetDirectionToNextWayPoint()
        {
            int xDirection = 0;
            int yDirection = 0;
            /* Sets object's direction towards it's target.
            * If xDirection or yDirection is '0', the object won't move horizontally/vertically.
            * If xDirection is '-1' or '1' the object will move in left/right direction.
            * If yDirection is '-1' or '1' the object will move in up/down direction. */
            if (PlayerActions.Count > 0 && PlayerActions.Peek().WayToTarget.Count > 0)
            {
                Pathfinding.TargetPointDirection(ref xDirection, ref yDirection, new Point(XCoordinate, YCoordinate), PlayerActions.Peek().WayToTarget.Peek());
            }
            XDirection = xDirection;
            YDirection = yDirection;
        }

        /// <summary>
        /// Calculates the way to all PlayerAction's targets.
        /// </summary>
        /// <param name="forceCalculation"></param>
        public void CalculateWaysToTarget(bool forceCalculation)
        {
            Pathfinding pathfinding = new Pathfinding();

            if (PlayerActions.Count > 0)
            {
                Point? lastAimPoint = null;
                lock (PlayerActions)
                {
                    List<PlayerAction> playerActionsList = PlayerActions.ToList();
                    foreach (PlayerAction playerAction in playerActionsList)
                    {
                        int invalidPoints;
                        if (forceCalculation || playerAction.WayToTarget.Count == 0 || pathfinding.InvalidWayPoints(playerAction.WayToTarget.ToList()).Count > 0)
                        {
                            invalidPoints = pathfinding.InvalidWayPoints(playerAction.WayToTarget.ToList()).Count;

                            playerAction.UpdateTargetPoint(Location, MaxSpeed);
                            Point targetPoint = playerAction.TargetPoint;
                            Point startLocation;
                            if (lastAimPoint.HasValue)
                            {
                                startLocation = lastAimPoint.Value;
                            }
                            else
                            {
                                startLocation = Location;
                            }
                            int shortestWayLength = -1;
                            int triesToBeatPath = -1;
                            int maximumTriesToBeatPath = 0;
                            int recursionStep = 0;

                            List<Point> way = pathfinding.CalculateShortestWayToTarget(targetPoint, startLocation, Location, new List<Point>(), ref shortestWayLength, ref triesToBeatPath, maximumTriesToBeatPath, ref recursionStep);
                            way = pathfinding.ReduceWay(way);
                            if (playerAction.WayToTarget.Count == 0 || invalidPoints > 0 || playerAction.TargetPointChanged || playerAction.WayToTarget.Count > way.Count)
                                playerAction.WayToTarget = new Queue<Point>(way);

                            //save the current targetpoint as the lastAimPoint, if the current action is a run or tackle
                            if (playerAction.Type == ActionType.Run || playerAction.Type == ActionType.Tackle)
                            {
                                if (playerAction.WayToTarget.Count == 0)
                                {
                                    lastAimPoint = playerAction.TargetPoint;
                                }
                                else
                                {
                                    lastAimPoint = playerAction.WayToTarget.ElementAt(playerAction.WayToTarget.Count - 1);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the PlayerAction where IsPreviewAction-bool is true.
        /// Returns null if there is no such action.
        /// </summary>
        /// <returns></returns>
        public PlayerAction SearchPreviewAction()
        {
            List<PlayerAction> actions = PlayerActions.ToList();
            PlayerAction foundAction = actions.Find(action => action.IsPreviewAction);

            return foundAction;
        }

        /// <summary>
        /// Removes all PlayerActions from the queue where IsPreviewAction is true.
        /// </summary>
        public void RemovePreviewActions()
        {
            List<PlayerAction> actions = PlayerActions.ToList();
            actions.RemoveAll(x => x.IsPreviewAction);
            PlayerActions = new Queue<PlayerAction>(actions);
        }

        /// <summary>
        /// Checks whether this point is the last point where the player will go to in this round.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool IsLastPoint(Point point)
        {
            //return the last point
            List<Point> lastWay = PlayerActions.ElementAt(PlayerActions.Count - 1).WayToTarget.ToList();
            if (lastWay.Count == 0)
            {
                return false;
            }
            return (LeftSteps == 1) || lastWay.ElementAt(lastWay.Count - 1).Equals(point);
        }

        /// <summary>
        /// Informs the pass target that he will get the ball.
        /// </summary>
        public void WaitForBall()
        {
            IsPassTarget = true;
            PlayerActions.Clear();
        }

        /// <summary>
        /// Instructs the player to run to a point where the ball will be in x rounds.
        /// </summary>
        /// <param name="targetPoint"></param>
        public void WaitForBallAtPoint(Point targetPoint)
        {
            PlayerActions.Clear();
            PlayerAction action = new PlayerAction(null, targetPoint, ActionType.Run);
            action.IsActionToGetBall = true;

            PlayerActions.Enqueue(action);
            CalculateWaysToTarget(true);
        }

        /// <summary>
        /// Returns whether the PlayerActions-queue contains an action where IsActionToGetBall is true.
        /// </summary>
        /// <returns></returns>
        public bool HasPlayerActionToGetBall()
        {
            return !PlayerActions.ToList().TrueForAll(x => x.IsActionToGetBall);
        }

        /// <summary>
        /// Executes the next actions (Shoot, Pass, Move etc.)
        /// </summary>
        /// <param name="gameBall"></param>
        /// <param name="isLastPointConflict"></param>
        /// <param name="gameStatus"></param>
        public void ExecuteAction(Ball gameBall, bool isLastPointConflict, Status gameStatus)
        {
            bool execute = false;

            //the player can't execute any action if he lies on the ground
            if (LiesOnTheGround)
            {
                LiesOnTheGround = false;
            }
            else
            {
                execute = true;
            }

            if (execute)
            {
                HasBall = TryToGetBall(gameBall);
                if (PlayerActions.Count > 0)
                {
                    Pathfinding pathfinding = new Pathfinding();
                    bool reachedLastPoint = false;

                    while (LeftActions > 0 && !reachedLastPoint)
                    {
                        if (PlayerActions.Count > 0)
                        {
                            if (gameStatus != Status.Normal && HasBall)
                            {
                                if (PlayerActions.Peek().Type != ActionType.Pass && PlayerActions.Peek().Type != ActionType.Shoot && PlayerActions.Peek().Type != ActionType.Throw)
                                {
                                    PlayerActions.Dequeue();
                                }
                            }
                        }

                        if (PlayerActions.Count > 0)
                        {
                            HasBall = TryToGetBall(gameBall);

                            #region ActionTypeSwitch
                            switch (PlayerActions.Peek().Type)
                            {
                                case ActionType.Tackle:
                                    Player affectedPlayer = PlayerActions.Peek().AffectedPlayer;
                                    if (affectedPlayer.HasBall)
                                    {
                                        CalculateWaysToTarget(true);
                                        Move(gameBall, isLastPointConflict, ref reachedLastPoint);
                                        if (LeftSteps == 0)
                                        {
                                            LeftActions--;
                                        }
                                        if (pathfinding.ArePointsNeighbors(Location, affectedPlayer.Location))
                                        {
                                            Tackle(gameBall);
                                            PlayerActions.Dequeue();
                                        }
                                    }
                                    else
                                    {
                                        PlayerActions.Dequeue();
                                    }
                                    break;
                                case ActionType.Run:
                                    Move(gameBall, isLastPointConflict, ref reachedLastPoint);
                                    if (LeftSteps == 0)
                                    {
                                        LeftActions--;
                                    }
                                    if (Location.Equals(PlayerActions.Peek().TargetPoint))
                                    {
                                        if (PlayerActions.Peek().IsActionToGetBall && !HasBall)
                                        {
                                            IsPassTarget = true;
                                        }
                                        PlayerActions.Dequeue();
                                    }
                                    break;
                                case ActionType.Shoot:
                                    if (HasBall)
                                    {
                                        Shoot(gameBall);
                                        LeftActions--;
                                    }
                                    PlayerActions.Dequeue();
                                    break;
                                case ActionType.Pass:
                                    if (HasBall)
                                    {
                                        Pass(gameBall, 3);
                                        LeftActions--;
                                    }
                                    PlayerActions.Dequeue();
                                    break;
                                case ActionType.Throw:
                                    if (HasBall)
                                    {
                                        Pass(gameBall, 1);
                                        LeftActions--;
                                    }
                                    PlayerActions.Dequeue();
                                    break;
                                default:
                                    LeftActions--;
                                    break;
                            }
                            #endregion
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            HasBall = TryToGetBall(gameBall);
        }

        /// <summary>
        /// Shoots the ball to the next target point
        /// </summary>
        /// <param name="gameBall">The ball which the player has to use</param>
        private void Shoot(Ball gameBall)
        {
            Pathfinding pathfinding = new Pathfinding();
            Point targetPoint = PlayerActions.Peek().TargetPoint;
            HasBall = false;

            int horizontalDistance = Math.Abs(targetPoint.X - Location.X);
            int verticalDistance = Math.Abs(targetPoint.Y - Location.Y);
            int hVariance = verticalDistance / 3, hVarianceRnd = rnd.Next(-hVariance, hVariance);
            int vVariance = horizontalDistance / 3, vVarianceRnd = rnd.Next(-vVariance, vVariance);

            //calculate the end point
            Point exactTargetLocation = pathfinding.GetExactLocation(PlayerActions.Peek().TargetPoint);
            exactTargetLocation.Y += (int) ((vVarianceRnd * (1 - ShootPrecision)) * 40);
            exactTargetLocation.X += (int)((hVarianceRnd * (1 - ShootPrecision)) * 40);
            //HasBall = false;

            LosesBall(this, EventArgs.Empty);

            gameBall.Speed = ShootSpeed;
            gameBall.TargetPoint = pathfinding.GetGridLocation(exactTargetLocation);
            gameBall.ExactTargetLocation = exactTargetLocation;
            gameBall.SetDirectionToTargetPoint();
            gameBall.RootPoint = gameBall.Location;
            gameBall.IsInShootState = true;
            gameBall.HasReachedTargetPoint = false;
            gameBall.Move();
        }

        /// <summary>
        /// Passes the ball to the next target point
        /// </summary>
        /// <param name="gameBall">The ball which the player has to use</param>
        private void Pass(Ball gameBall, int speed)
        {
            Pathfinding pathfinding = new Pathfinding();

            gameBall.Speed = speed;
            gameBall.TargetPoint = PlayerActions.Peek().TargetPoint;
            gameBall.ExactTargetLocation = pathfinding.GetExactLocation(gameBall.TargetPoint.Value);
            gameBall.SetDirectionToTargetPoint();

            if (PlayerActions.Peek().AffectedPlayer != null)
            {
                PlayerActions.Peek().AffectedPlayer.WaitForBall();
            }

            //HasBall = false;
            LosesBall(this, EventArgs.Empty);

            gameBall.RootPoint = gameBall.Location;
            gameBall.IsInShootState = false;
            gameBall.HasReachedTargetPoint = false;
            gameBall.Move();
        }

        /// <summary>
        /// Creates a new ConflictHandler-object and uses it to get the winner of the tackle.
        /// If this player is the winner, the ball will be moved to the player's current position.
        /// </summary>
        /// <param name="gameBall"></param>
        private void Tackle(Ball gameBall)
        {
            TackleConflictHandler tackleHander = new TackleConflictHandler();
            Player targetPlayer = PlayerActions.Peek().AffectedPlayer;
            Player winner = tackleHander.TackleConflictWinner(this, targetPlayer);
            
            if (winner.Equals(this))
            {
                MoveBall(gameBall);
                HasBall = true;
                CheckPlayerImage();
                targetPlayer.HasBall = false;

                double precision = rnd.NextDouble() * rnd.NextDouble() * 2 * (TacklePrecision + rnd.NextDouble());
                if (precision < 0.6)
                {
                    targetPlayer.LiesOnTheGround = true;
                    targetPlayer.CheckPlayerImage();
                    if (precision < 0.3)
                    {
                        FoulEventArgs args = new FoulEventArgs(this, targetPlayer);
                        FouledAPlayer(this, args);
                    }               
                }
            }
        }

        /// <summary>
        /// Sets the position's characteristics.
        /// </summary>
        private void SetCharacteristics()
        {
            TacklePrecision = 0.5;
            ThrowStrength = 6;

            switch (Position)
            {
                case PlayerPosition.Striker:
                    MaxSpeed = 2;
                    ShootSpeed = 5;
                    TackleStrength = 0.4;
                    ShootPrecision = 0.5;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.Goalkeeper:
                    MaxSpeed = 2;
                    ShootSpeed = 3;
                    TackleStrength = 0.8;
                    ShootPrecision = 0.5;
                    BallControl = 1.0;
                    break;
                case PlayerPosition.CentralDefender:
                    MaxSpeed = 2;
                    ShootSpeed = 3;
                    ShootPrecision = 0.3;
                    TackleStrength = 0.7;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.CentralMidfielder:
                    MaxSpeed = 2;
                    ShootSpeed = 4;
                    TackleStrength = 0.4;
                    ShootPrecision = 0.7;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.LeftBack:
                    MaxSpeed = 2;
                    ShootSpeed = 3;
                    TackleStrength = 0.7;
                    ShootPrecision = 0.3;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.LeftMidfielder:
                    MaxSpeed = 2;
                    ShootSpeed = 4;
                    TackleStrength = 0.4;
                    ShootPrecision = 0.7;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.RightBack:
                    MaxSpeed = 2;
                    ShootSpeed = 3;
                    TackleStrength = 0.7;
                    ShootPrecision = 0.3;
                    BallControl = 0.5;
                    break;
                case PlayerPosition.RightMidfielder:
                    MaxSpeed = 2;
                    ShootSpeed = 4;
                    TackleStrength = 0.4;
                    ShootPrecision = 0.7;
                    BallControl = 0.5;
                    break;
            }

            TackleStrength = 2;
        }

        /// <summary>
        /// Returns the conflict strength in this conflict. The value depends on the ActionType.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public double ConflictStrength(ActionType type)
        {
            double random = rnd.NextDouble();
            switch (type)
            {
                case ActionType.Run:
                    return random * TackleStrength;
                case ActionType.Tackle:
                    return random * TackleStrength * Weight;
                default:
                    return 0.0;
            }
        }
    }
}
