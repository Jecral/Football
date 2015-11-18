using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence
{
    abstract class PlayerAI
    {
        public PlayerAI(AI gameAI)
        {
            this.GameAI = gameAI;
            Rectangle runRoom = new Rectangle(new Point(1, 1), new Size(GameAI.FieldCell.GetLength(0) - 3, GameAI.FieldCell.GetLength(1) - 3));
            LeftSideRunRoom = runRoom;
            RightSideRunRoom = runRoom;
            DistanceToEnemy = 3;
        }

        public AI GameAI { get; set; }

        public virtual Rectangle LeftSideRunRoom { get; set; }
        public virtual Rectangle RightSideRunRoom { get; set; }
        public virtual int DefenceBallDistance { get; set; }
        public virtual int AttackLine { get; set; } //distance to the own goal - if an enemy player is closer to the goal than this distance, the player will isAttack him
        public virtual int DistanceToEnemy { get; set; }
        public int RoundsWithBall { get; set; }

        /// <summary>
        /// If the player controls the ball, the method ActionWithBall() gets raised.
        /// If the player does not control the ball, the method ActionWithoutBall() gets raised.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="gameAction"></param>
        /// <returns></returns>
        public PlayerAction CalculateRoundAction(Player player, GameAction gameAction)
        {
            if (player.HasBall)
            {
                return ActionWithBall(player, gameAction);
            }
            else
            {
                return ActionWithoutBall(player, gameAction);
            }
        }

        #region Actions - Player controls ball
        public virtual PlayerAction ActionWithBall(Player player, GameAction gameAction)
        {
            if (gameAction.GameStatus == Status.CornerKick)
            {
                return CornerKick(player);
            }
            else if (gameAction.GameStatus == Status.ThrowIn)
            {
                return ThrowIn(player, gameAction.ThrowRoom);
            }
            else if (gameAction.GameStatus == Status.FreeKick)
            {
                return FreeKick(player);
            }
            else if(gameAction.GameStatus == Status.KickOff)
            {
                return Pass(player);
            }
            else if (IsSurrounded(player, 1))
            {
                if (IsDeepInOwnSide(player))
                {
                    RoundsWithBall = 0;
                    return PassDeepIntoField(player);
                }
                else if (IsDeepInEnemySide(player))
                {
                    if (IsInShootPosition(player))
                    {
                        //Shoot into the goal if the player stands close to it
                        RoundsWithBall = 0;
                        return Shoot(GameAI.PlayersTeam(player));
                    }
                    else
                    {
                        RoundsWithBall = 0;
                        return PassCloseToGoal(player);
                    }
                }
                else
                {
                    RoundsWithBall = 0;
                    return Pass(player);
                }
            }
            else
            {
                return ActionWithBallNotSurrounded(player);
            }

        }

        public PlayerAction FreeKick(Player player)
        {
            if (GameAI.IsEnemyInBlockedRoom && (RoundsWithBall < 5 && !IsDeepInEnemySide(player)))
            {
                RoundsWithBall++;
                return null;
            }
            else
            {
                RoundsWithBall = 0;
                return PassCloseToGoal(player);
            }
        }

        public PlayerAction CornerKick(Player player)
        {
            if (GameAI.IsEnemyInBlockedRoom && RoundsWithBall < 5)
            {
                RoundsWithBall++;
                return null;
            }
            else
            {
                RoundsWithBall = 0;
                return PassCloseToGoal(player);
            }
        }

        public PlayerAction ThrowIn(Player player, Rectangle throwRoom)
        {
            if (GameAI.IsEnemyInBlockedRoom || RoundsWithBall < 5)
            {
                RoundsWithBall++;
                return null;
            }
            else
            {
                List<Player> goodPlayers = GameAI.PlayerInRectangle(GameAI.PlayersTeam(player).Players, throwRoom, 0);
                Random rnd = new Random();
                int rndInt = rnd.Next(0, (goodPlayers.Count <= 6) ? goodPlayers.Count : 5);

                Player goodPlayer = goodPlayers.ElementAt(rnd.Next(0, rndInt));

                goodPlayer.WaitForBall();
                RoundsWithBall = 0;
                return new PlayerAction(goodPlayer, goodPlayer.Location, ActionType.Pass);
            }
        }

        /// <summary>
        /// Shoots into the goal.
        /// </summary>
        /// <param name="playerTeam"></param>
        /// <returns></returns>
        public PlayerAction Shoot(Team playerTeam)
        {
            Goal enemyGoal = GameAI.GetEnemyTeam(playerTeam).TeamGoal;
            Point goalMiddle = enemyGoal.MiddlePoint;

            Point keeperPosition = GameAI.KeeperPosition(GameAI.GetEnemyTeam(playerTeam), 1);

            int targetY = (keeperPosition.Y < goalMiddle.Y) ? enemyGoal.YCoordinate + enemyGoal.Height - 2 : enemyGoal.YCoordinate + 2;
            int targetX = (enemyGoal.XCoordinate == GameAI.FieldCell.GetLength(0)) ? enemyGoal.XCoordinate - 1 : enemyGoal.XCoordinate - 1;
            Point targetPoint = new Point(targetX, targetY);

            return new PlayerAction(null, targetPoint, ActionType.Shoot);
        }

        /// <summary>
        /// Pass to a team player who stands close to the enemy's penalty kick point.
        /// </summary>
        /// <param name="player">The player who will execute the action.</param>
        /// <returns></returns>
        public PlayerAction PassCloseToGoal(Player player)
        {
            //search teamplayers close to penalty kick point
            Point penaltyKickPoint = GameAI.PenaltyKickPoint(GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal);

            int distanceToGoal = Math.Abs(player.Location.X - penaltyKickPoint.X);
            List<Player> nearPlayers = GameAI.NearestPlayersToPoint(GameAI.PlayersTeam(player), penaltyKickPoint, 2, distanceToGoal / player.ShootSpeed, false);
            nearPlayers.Remove(player);

            Player targetPlayer = targetPlayer = nearPlayers.ElementAt(0);

            targetPlayer.WaitForBall();

            return new PlayerAction(targetPlayer, targetPlayer.Location, ActionType.Pass);
        }

        /// <summary>
        /// Passes to a good standing team player.
        /// Tries to pass into his run way.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerAction Pass(Player player)
        {
            List<Player> goodPlayers = SearchPassPlayers(player);
            Random rnd = new Random();

            int playersCount = (goodPlayers.Count > 4) ? 4 : goodPlayers.Count;
            Player goodPlayer = goodPlayers.ElementAt(rnd.Next(0, playersCount));
            goodPlayer.WaitForBall();

            //return new PlayerAction(goodPlayer, goodPlayer.Location, ActionType.Pass);
            return PassIntoRunWay(player, goodPlayer);
        }

        /// <summary>
        /// Pass to a player who stands close to the middle of the field.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerAction PassDeepIntoField(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point targetPoint = new Point(GameAI.FieldCell.GetLength(0) / 2, GameAI.FieldCell.GetLength(1) / 2);
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));

            int distanceToGoal = Math.Abs(player.Location.X - targetPoint.X);
            List<Player> nearPlayers = GameAI.NearestPlayersToPoint(GameAI.PlayersTeam(player), targetPoint, 2, distanceToGoal / player.ShootSpeed, false);
            nearPlayers.Remove(player);

            Player nearestPlayer = nearPlayers[0];
            nearPlayers.RemoveAll(x => GameAI.IsOffside(x, enemyTeam, 0));

            Player targetPlayer = nearestPlayer;
            if (nearPlayers.Count > 0)
            {
                targetPlayer = targetPlayer = nearPlayers.ElementAt(0);
            }

            //pass into the player's run way if no enemy stands in the shoot way
            if (GameAI.PlayerInBallWay(player.Location, targetPlayer.Location) == null)
            {
                return PassIntoRunWay(player, targetPlayer);
            }
            else
            {
                //shoot to the target player's location so that no enemy has the chance to get the ball
                return new PlayerAction(targetPlayer, targetPlayer.Location, ActionType.Shoot);
            }
        }

        /// <summary>
        /// Returns a PlayerAction wehre the player will pass to a point where another player will run to.
        /// </summary>
        /// <param name="rootPlayer"></param>
        /// <param name="targetPlayer"></param>
        /// <returns></returns>
        public PlayerAction PassIntoRunWay(Player rootPlayer, Player targetPlayer)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(rootPlayer));

            int gridDistance = Pathfinding.GridDistanceBetweenPoints(rootPlayer.Location, targetPlayer.Location);
            int runDistance = gridDistance / 2;

            Point targetPoint = targetPlayer.Location;
            targetPoint.X += pathfinding.CalculateHorizontalDirection(rootPlayer.Location, enemyTeam.TeamGoal.Location) * runDistance;
            targetPoint = SearchFreeTargetPoint(rootPlayer, targetPoint).Value;

            targetPlayer.WaitForBallAtPoint(targetPoint);

            return new PlayerAction(null, targetPoint, ActionType.Pass);
        }

        /// <summary>
        /// For the case that the player has the ball and is not surrounded by enemies.
        /// </summary>
        /// <param name="player">The player who will execute the action.</param>
        /// <returns></returns>
        public virtual PlayerAction ActionWithBallNotSurrounded(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));

            int distanceToEnemyGoal = Math.Abs(player.XCoordinate - enemyTeam.TeamGoal.XCoordinate);

            //true if the player stands close to the enemy's goal but is not in a shoot position
            if (distanceToEnemyGoal < enemyTeam.TeamGoal.Height * 2)
            {
                if (!IsInShootPosition(player))
                {
                    return PassCloseToGoal(player);
                }
                else
                {
                    return Shoot(GameAI.PlayersTeam(player));
                }
            }
            else
            {
                Point targetPoint;
                if (IsDeepInEnemySide(player))
                {
                    return RunToPenaltyKickPoint(player, enemyTeam.TeamGoal);
                }
                else
                {
                    targetPoint = player.Location;
                    targetPoint.X = player.Location.X + pathfinding.CalculateHorizontalDirection(player.Location, enemyTeam.TeamGoal.MiddlePoint) * player.MaxSpeed * 4;

                    Point freePoint = SearchFreeTargetPoint(player, RunRoom(GameAI.PlayersTeam(player)), targetPoint, 4, 0);

                    return new PlayerAction(null, freePoint, ActionType.Run);
                }
            }
        }
        #endregion

        #region Actions - Player doesn't control ball
        public virtual PlayerAction ActionWithoutBall(Player player, GameAction gameAction)
        {
            Team playerTeam = GameAI.PlayersTeam(player);
            Team enemyTeam = GameAI.GetEnemyTeam(playerTeam);

            if (player.Location == gameAction.ActionPoint && gameAction.GameStatus != Status.Normal && !playerTeam.Equals(gameAction.ActionTeam))
            {
                return GoAwayFromActionPoint(gameAction.ActionPoint);
            }

            if (gameAction.GameStatus == Status.CornerKick)
            {
                if (gameAction.ActionTeam.Equals(playerTeam))
                {
                    return CornerAttack(player);
                }
                else
                {
                    return CornerDefense(player);
                }
            }
            else if (gameAction.GameStatus == Status.FreeKick)
            {
                if (gameAction.ActionTeam.Equals(playerTeam))
                {
                    return RunTowardsGoal(player, enemyTeam.TeamGoal);
                }
                else
                {
                    return RunTowardsGoal(player, playerTeam.TeamGoal);
                }
            }
            else if (gameAction.GameStatus == Status.ThrowIn)
            {
                Rectangle throwRoom = gameAction.ThrowRoom;
                if (gameAction.ActionTeam.Equals(playerTeam))
                {
                    return ThrowInAttack(player, gameAction);
                }
                else
                {
                    return ThrowInDefense(player, throwRoom);
                }
            }
            else if (gameAction.GameStatus == Status.GoalKick)
            {
                if (gameAction.ActionTeam.Equals(playerTeam))
                {
                    return GoalKickAttack(player);
                }
                else
                {
                    return RunTowardsGoal(player, playerTeam.TeamGoal);
                }
            }
            else if (GameAI.BallContactTeam == playerTeam && gameAction.GameStatus == Status.Normal || gameAction.ActionTeam.Equals(playerTeam) && (gameAction.GameStatus == Status.ThrowIn || gameAction.GameStatus == Status.FreeKick))
            {
                return ActionWeHaveBall(player, gameAction);
            }
            else
            {
                return ActionEnemyHasBall(player);
            }
        }

        #region Actions - Own team controls ball
        /// <summary>
        /// For the case that the enemy team controls the ball.
        /// Raises ActionGoalFarAway() if the ball is more than 10 cells from the own goal away in the next round.
        /// Raises ActionGoalClose() if it will be less than 11 cells.
        /// </summary>
        /// <param name="player">The player who will execute the action</param>
        /// <param name="gameAction">The current game's GameAction</param>
        /// <returns></returns>
        public virtual PlayerAction ActionWeHaveBall(Player player, GameAction gameAction)
        {
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            int distanceToEnemyGoal = Math.Abs(enemyTeamGoal.XCoordinate - player.XCoordinate);
            Point ballNextRoundLocation = GameAI.BallNextRoundLocation();

            if (distanceToEnemyGoal > 10)
            {
                return ActionGoalFarAway(player, gameAction);
            }
            else
            {
                return ActionGoalClose(player);
            }
        }

        /// <summary>
        /// For the case that the own team controls the ball and the enemy's goal is far away from the player.
        /// </summary>
        /// <param name="player">The player who will execute the action.</param>
        /// <param name="gameAction"></param>
        /// <returns></returns>
        public virtual PlayerAction ActionGoalFarAway(Player player, GameAction gameAction)
        {
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            Pathfinding pathfinding = new Pathfinding();

            int horizontalDistanceToBall = Math.Abs(GameAI.GameBall.XCoordinate - player.XCoordinate);
            int distanceToEnemyGoal = Math.Abs(enemyTeamGoal.XCoordinate - player.XCoordinate);
            int ballDistanceToEnemyGoal = Math.Abs(enemyTeamGoal.XCoordinate - GameAI.GameBall.XCoordinate);
            Point ballNextRoundLocation = GameAI.BallNextRoundLocation();

            Point targetPoint = new Point();

            int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
            targetPoint.Y = (verticalDistanceToStandardLocation > 3) ? player.StartLocation.Y : player.Location.Y;
            if (gameAction.GameStatus == Status.GoalKick)
            {
                targetPoint.X = player.Location.X + (pathfinding.CalculateHorizontalDirection(player.Location, enemyTeamGoal.MiddlePoint) * (player.MaxSpeed * 4));
            }
            else
            {
                targetPoint.X = ballNextRoundLocation.X + (pathfinding.CalculateHorizontalDirection(player.Location, enemyTeamGoal.MiddlePoint) * (player.MaxSpeed * 4));
            }
            Point freePoint = SearchFreeTargetPoint(player, RunRoom(GameAI.PlayersTeam(player)), targetPoint, 3, 0);

            return new PlayerAction(null, freePoint, ActionType.Run);
        }

        /// <summary>
        /// For the case that the own team controls the ball and the player stands close to the enemy's goal.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public virtual PlayerAction ActionGoalClose(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;

            Point ballNextRoundLocation = GameAI.BallNextRoundLocation();
            int ballDistanceToEnemyGoal = Math.Abs(enemyTeamGoal.XCoordinate - ballNextRoundLocation.X);

            Point targetPoint = player.Location;

            targetPoint.Y = enemyTeamGoal.MiddlePoint.Y + pathfinding.CalculateVerticalDirection(player.Location, enemyTeamGoal.MiddlePoint) * 3;
            if (ballDistanceToEnemyGoal > player.MaxSpeed * 2)
            {
                targetPoint.X = ballNextRoundLocation.X + (pathfinding.CalculateHorizontalDirection(player.Location, enemyTeamGoal.MiddlePoint) * 5);
            }

            Point freePoint = SearchFreeTargetPoint(player, RunRoom(GameAI.PlayersTeam(player)), targetPoint, 5, 3);

            return new PlayerAction(null, freePoint, ActionType.Run);
        }

        public virtual PlayerAction ThrowInAttack(Player player, GameAction gameAction)
        {
            Pathfinding pathfinding = new Pathfinding();
            Rectangle throwRoom = gameAction.ThrowRoom;

            Point centerOfThrowRoom = new Point(throwRoom.X + (throwRoom.Width / 2), throwRoom.Y + (throwRoom.Height / 2));

            double distanceToThrowRoom = Pathfinding.DistanceBetweenPoints(centerOfThrowRoom, player.Location);

            if (distanceToThrowRoom < 10)
            {
                Point freePoint = GameAI.SearchFreePointInRectangle(player, throwRoom);

                return new PlayerAction(null, freePoint, ActionType.Run);
            }

            return ActionWeHaveBall(player, gameAction);
        }

        public virtual PlayerAction CornerAttack(Player player)
        {
            return RunToPenaltyKickPoint(player, GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal); //in development
        }

        /// <summary>
        /// Run to the enemy goal.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerAction GoalKickAttack(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            Point targetPoint = new Point();

            int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
            targetPoint.Y = (verticalDistanceToStandardLocation > 3) ? player.StartLocation.Y : player.Location.Y;

            targetPoint.X = player.Location.X + (pathfinding.CalculateHorizontalDirection(player.Location, enemyTeamGoal.MiddlePoint) * (player.MaxSpeed * 4));

            Point freePoint = SearchFreeTargetPoint(player, RunRoom(GameAI.PlayersTeam(player)), targetPoint, 3, 0);

            return new PlayerAction(null, freePoint, ActionType.Run);
        }
        #endregion

        #region Actions - Enemy controls ball
        public virtual PlayerAction ActionEnemyHasBall(Player player)
        {
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            List<Player> allEnemiesInRunRoom = GameAI.PlayerInRectangle(enemyTeam.Players, RunRoom(GameAI.PlayersTeam(player)), 1);

            Point closestPlayerNextLocation = GameAI.NextPointNearestPlayerInRunRoom(player.Location, RunRoom(GameAI.PlayersTeam(player)), GameAI.PlayersTeam(player), 1);
            closestPlayerNextLocation.Y = player.YCoordinate;

            int enemyDistanceToOwnGoal = Math.Abs(closestPlayerNextLocation.X - GameAI.PlayersTeam(player).TeamGoal.XCoordinate);

            if (enemyDistanceToOwnGoal > DefenceBallDistance + 3)
            {
                return ActionEnemyFarAway(player, closestPlayerNextLocation);
            }
            else
            {
                return ActionEnemyClose(player);
            }

        }

        /// <summary>
        /// For the case that the enemy has the ball and is far away.
        /// </summary>
        /// <param name="player">The player who will execute the action.</param>
        /// <param name="closestPlayerNextLocation"></param>
        /// <returns></returns>
        public virtual PlayerAction ActionEnemyFarAway(Player player, Point closestPlayerNextLocation)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point targetPoint = closestPlayerNextLocation;
            targetPoint.X = closestPlayerNextLocation.X + pathfinding.CalculateHorizontalDirection(player.Location, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint) * DefenceBallDistance;
            int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
            if (verticalDistanceToStandardLocation > 4)
            {
                targetPoint.Y = player.StartLocation.Y;
            }

            return new PlayerAction(null, targetPoint, ActionType.Run);
        }

        /// <summary>
        /// For the case that the enemy has the ball and is close to the own goal.
        /// </summary>
        /// <param name="player">The player who will execute the action</param>
        /// <returns></returns>
        public virtual PlayerAction ActionEnemyClose(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            ActionType type;

            List<Player> nearPlayersToPenaltyPoint = GameAI.NearestPlayersToPoint(enemyTeam, GameAI.PenaltyKickPoint(enemyTeam.TeamGoal), 2, 1, false);

            Player nearPlayer;
            if (nearPlayersToPenaltyPoint.ElementAt(0).Equals(player))
            {
                nearPlayer = nearPlayersToPenaltyPoint.ElementAt(1);
            }
            else
            {
                nearPlayer = nearPlayersToPenaltyPoint.ElementAt(0);
            }
            Point nextPlayerLocation = pathfinding.PlayerAtSpecificRound(nearPlayer, 1, out type);

            return new PlayerAction(null, nextPlayerLocation, ActionType.Run);
        }

        public virtual PlayerAction CornerDefense(Player player)
        {
            return RunToPenaltyKickPoint(player, GameAI.PlayersTeam(player).TeamGoal); //in development
        }

        public PlayerAction ThrowInDefense(Player player, Rectangle throwRoom)
        {
            return ActionEnemyHasBall(player);
        }
        #endregion
        #endregion

        /// <summary>
        /// Returns an action where the player will run to the penaly kick point of the goal in the parameter.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public PlayerAction RunToPenaltyKickPoint(Player player, Goal goal)
        {
            Pathfinding pathfinding = new Pathfinding();
            Point targetPoint = goal.MiddlePoint;
            targetPoint.X += pathfinding.CalculateHorizontalDirection(goal.MiddlePoint, new Point(12, 12)) * 3;

            Point freePoint = SearchFreeTargetPoint(player, targetPoint).Value;

            return new PlayerAction(null, freePoint, ActionType.Run);
        }

        /// <summary>
        /// Returns a Run-PlayerAction where the player will run towards the goal in the parameters.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public PlayerAction RunTowardsGoal(Player player, Goal goal)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point targetPoint = player.Location;
            targetPoint.X += pathfinding.CalculateHorizontalDirection(player.Location, goal.MiddlePoint);
            int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
            if (verticalDistanceToStandardLocation > 4)
            {
                targetPoint.Y = player.StartLocation.Y;
            }

            return new PlayerAction(null, targetPoint, ActionType.Run);
        }

        /// <summary>
        /// Instructs the player to go away from the action point.
        /// </summary>
        public PlayerAction GoAwayFromActionPoint(Point actionPoint)
        {
            Pathfinding pathfinding = new Pathfinding();

            List<Point> neighbors = pathfinding.GetValidNeighborPoints(actionPoint);
            if (neighbors.Count > 0)
            {
                return new PlayerAction(null, neighbors[0], ActionType.Run);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether the player stands deep in the enemy side.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool IsDeepInEnemySide(Player player)
        {
            Goal enemyGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            double distanceToGoal = Math.Abs(player.Location.X - enemyGoal.XCoordinate);

            return distanceToGoal < 10;
        }

        /// <summary>
        /// Checks whether the player stands deep in the own side.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool IsDeepInOwnSide(Player player)
        {
            double distanceToGoal = Math.Abs(player.Location.X - GameAI.PlayersTeam(player).TeamGoal.XCoordinate);

            return distanceToGoal < 7;
        }

        /// <summary>
        /// Returns an unmarked player to whom the player could pass.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public List<Player> SearchPassPlayers(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            Point enemyGoalMiddle = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal.MiddlePoint;
            int horizontalDistanceToGoal = Math.Abs(enemyTeam.TeamGoal.XCoordinate - player.XCoordinate) + 3;

            List<Player> bestPlayers = new List<Player>();
            List<int> surroundingEnemies = new List<int>();
            List<int> distancesToGoal = new List<int>();

            List<Player> nearestPlayersToGoal = GameAI.NearestPlayersToPoint(GameAI.PlayersTeam(player), enemyGoalMiddle, GameAI.PlayersTeam(player).Players.Count, 1, false);
            nearestPlayersToGoal.Remove(player);

            foreach (Player teamPlayer in nearestPlayersToGoal)
            {
                Point ownNextPoint = teamPlayer.Location;
                int tmpSurroundingEnemies = GameAI.SurroundingPlayers(enemyTeam, 3, ownNextPoint, 1).Count;
                int tmpDistanceToGoal = Math.Abs(enemyTeam.TeamGoal.XCoordinate - teamPlayer.XCoordinate);

                for (int i = 0; i <= bestPlayers.Count; i++)
                {
                    if (i == bestPlayers.Count || (tmpSurroundingEnemies < surroundingEnemies[i] && tmpDistanceToGoal <= horizontalDistanceToGoal) || (tmpSurroundingEnemies <= surroundingEnemies[i] && tmpDistanceToGoal < distancesToGoal[i]))
                    {
                        bestPlayers.Insert(i, teamPlayer);
                        surroundingEnemies.Insert(i, tmpSurroundingEnemies);
                        distancesToGoal.Insert(i, tmpDistanceToGoal);
                        break;
                    }
                }
            }
            List<Player> playersNotInWay = new List<Player>(nearestPlayersToGoal);
            foreach (Player passPartner in nearestPlayersToGoal)
            {
                Player playerInWay = GameAI.PlayerInBallWay(player.Location, passPartner.Location);
                if (playerInWay != null)
                    playersNotInWay.Remove(passPartner);
            }

            return (playersNotInWay.Count > 0) ? playersNotInWay : bestPlayers;
        }

        /// <summary>
        /// Checks whether the player is surrounded by enemy players.
        /// </summary>
        /// <param name="player">The player in the middle.</param>
        /// <param name="round">The round in which the method should check the other player's position.</param>
        /// <returns></returns>
        public bool IsSurrounded(Player player, int round)
        {
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            int directNeighborsCount = GameAI.SurroundingPlayers(enemyTeam, 2, player.Location, round).Count;
            //int otherNeighborsCount = GameAI.SurroundingPlayers(enemyTeam, 4, player.Location, round).Count;

            bool surrounded = directNeighborsCount > 0;
            return surrounded;
        }

        /// <summary>
        /// Searches a free point where the player could go to in the next point.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="rangeRectangle"></param>
        /// <param name="targetLocation"></param>
        /// <param name="range"></param>
        /// <param name="amountOfPlayers"></param>
        /// <returns></returns>
        public Point SearchFreeTargetPoint(Player player, Rectangle rangeRectangle, Point targetLocation, int range, int amountOfPlayers)
        {
            Pathfinding pathfinding = new Pathfinding();
            Random rnd = new Random();

            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            List<Point> validRunPoints = GameAI.SearchRunPoints(player, GameAI.PlayersTeam(player), targetLocation, range, amountOfPlayers, rangeRectangle);

            int pointsCount = (validRunPoints.Count > 4) ? 4 : validRunPoints.Count;

            Point goodPoint = validRunPoints.ElementAt(rnd.Next(0, pointsCount));
            return goodPoint;
        }

        /// <summary>
        /// Raises SearcHFreeTargetPoint() method with the whole field as the valid rectangle.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetLocation"></param>
        /// <returns></returns>
        public Point? SearchFreeTargetPoint(Player player, Point targetLocation)
        {
            Rectangle validRectangle = new Rectangle(new Point(0, 0), new Size(GameAI.FieldCell.GetLength(0), GameAI.FieldCell.GetLength(1)));
            return SearchFreeTargetPoint(player, validRectangle, targetLocation, 4, 0);
        }

        /// <summary>
        /// Checks whether the player stands in front of the ball or behind the ball.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool StandsInFrontOfBall(Player player)
        {
            if (GameAI.PlayersTeam(player).HasLeftSide)
            {
                return (player.Location.X <= GameAI.GameBall.Location.X);
            }
            else
            {
                return (player.Location.X >= GameAI.GameBall.Location.X);
            }
        }

        /// <summary>
        /// Checks whether the player stands in a shoot position.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool IsInShootPosition(Player player)
        {
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            int horizontalDistance = Math.Abs(player.XCoordinate - enemyTeamGoal.MiddlePoint.X);

            bool isInShootPosition = false;
            if (horizontalDistance < enemyTeamGoal.Height * 2)
            {
                bool tooClose = horizontalDistance < 2 && (player.YCoordinate < enemyTeamGoal.YCoordinate || player.YCoordinate > enemyTeamGoal.YCoordinate + enemyTeamGoal.Height);
                if (!tooClose)
                {
                    if (player.YCoordinate > enemyTeamGoal.YCoordinate - 2 && player.YCoordinate < enemyTeamGoal.YCoordinate + enemyTeamGoal.Height + 2)
                    {
                        isInShootPosition = true;
                    }
                }
            }
            return isInShootPosition;
        }

        public Rectangle RunRoom(Team playerTeam)
        {
            if (playerTeam.HasLeftSide)
            {
                return LeftSideRunRoom;
            }
            else
            {
                return RightSideRunRoom;
            }
        }
    }
}
