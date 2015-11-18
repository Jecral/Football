using Football.EventArguments;
using Football.GUI;
using Football.Logic;
using Football.Logic.ArtificialIntelligence;
using Football.Logic.Conflicts;
using Football.Logic.GameFiles;
using Football.Logic.GameFiles.Images;
using Football.Logic.GameObjects;
using Football.Logic.GameObjects.Player;
using Football.Multiplayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Football
{
    class Game
    {
        public Game(Rectangle[,] fieldCell, GameSettings settings, bool isMultiplayer, ReplayManager replay)
        {
            this.FieldCell = fieldCell;
            SetSettings(settings);

            if (replay != null)
            {
                CurrentReplayManager = replay;
            }
            else
            {
                CurrentReplayManager = new ReplayManager(settings);
            }

            round = 0;
            halftime = 1;

            IsStopped = false;
            IsMultiplayer = isMultiplayer;
            pathfindingSystem = new Pathfinding();
        }

        #region Properties
        //game objects
        public Ball GameBall { get; set; }
        public Team FirstTeam { get; set; }
        public Team SecondTeam { get; set; }

        private int round;
        public int Round {
            get { return round; }
            set {
                round = value;
                if (!IsMultiplayer)
                {
                    InitializeNextStep(this, EventArgs.Empty);
                }
                RoundChanged(this, EventArgs.Empty);
            }
        }
        private int halftime;
        private int leftSeconds;
        public int LeftSeconds {
            get{return leftSeconds;}
            set{
                leftSeconds = value;
                if (leftSeconds == -1)
                {
                    if (!IsMultiplayer)
                    {
                        Round++;
                    }
                    else
                    {
                        IsWaitingForHost(this, EventArgs.Empty);
                    }
                }
                if (leftSeconds >= 0)
                {
                    LeftSecondsChanged(this, EventArgs.Empty);
                }
            }
        }

        private Player[] allPlayers;
        public System.Timers.Timer RoundTimer { get; set; }

        #region Settings
        public Rectangle[,] FieldCell { get; set; }
        private int columns;
        private int rows;
        private int midfieldLineXPosition;
        private int midfieldPointYPosition;
        private int leftGoalHeight;
        private int leftGoalYPosition;
        private int rightGoalHeight;
        private int rightGoalYPosition;
        private int secondsPerRound;
        private int roundsPerHalf;
        private bool firstTeamUsesKI;
        private bool secondTeamUsesKI;
        public GameSettings Settings { get; set; }
        #endregion

        public event EventHandler RoundChanged;
        public event EventHandler IsWaitingForHost;
        public event EventHandler CoordinatesChanged;
        public event EventHandler LeftSecondsChanged;
        public event EventHandler InitializeNextStep;
        public event EventHandler NewDirectionLines;
        public event EventHandler GameEnded;
        public event EventHandler<RoundImageEventArgs> GameImageChanged;
        public event EventHandler<ReceivedActionArgs> ActionChosen;
        public event EventHandler<BallEventArgs> GoalsChanged;

        public ReplayManager CurrentReplayManager { get; set; }
        private Pathfinding pathfindingSystem;
        public AI GameAI { get; set; }
        public GameAction CurrentAction { get; set; }
        private List<RoundAction> nextRoundActions; //actions which will be added in the next round
        public List<CachedRoundAction> NextRoundCacheActions { get; set; }

        private GameEventHandler gameEventHandler;
        public bool IsStopped { get; set; }
        public bool SetKickOff { get; set; }
        public bool IsMultiplayer { get; set; }
        public bool IsHost { get; set; }
        public bool IsCalculating { get; set; }

        #endregion

        #region Initialisation
        /// <summary>
        /// Saves all Settings.
        /// </summary>
        /// <param name="Settings"></param>
        private void SetSettings(GameSettings settings)
        {
            this.midfieldLineXPosition = settings.MidLineXPosition;
            this.midfieldPointYPosition = settings.MidPointYPosition;
            this.leftGoalHeight = settings.LeftGoalHeight;
            this.leftGoalYPosition = settings.LeftGoalYPosition;
            this.rightGoalHeight = settings.RightGoalHeight;
            this.rightGoalYPosition = settings.RightGoalYPosition;
            this.columns = FieldCell.GetLength(0);
            this.rows = FieldCell.GetLength(1);
            this.secondsPerRound = settings.SecondsPerRound;
            this.roundsPerHalf = settings.RoundsPerHalf;
            firstTeamUsesKI = settings.FirstTeamUsesKI;
            secondTeamUsesKI = settings.SecondTeamUsesKI;
            this.Settings = settings;
        }

        /// <summary>
        /// Creates the game ball, the two teams as well as the artificial intelligence.
        /// </summary>
        public void InitializeGame()
        {
            GameBall = new Ball(0, null);
            Goal leftGoal = new Goal(leftGoalHeight);
            Goal rightGoal = new Goal(rightGoalHeight);
            leftGoal.SetCoordinates(1, leftGoalYPosition);
            rightGoal.SetCoordinates(columns - 1, rightGoalYPosition);

            FirstTeam = new Team("Team Yellow", 0, leftGoal);
            FirstTeam.InitializeLineup(midfieldLineXPosition, midfieldPointYPosition, true, true);
            FirstTeam.IsAllowedToTakeBall = true;
            SecondTeam = new Team("Team Red", 1, rightGoal);
            SecondTeam.InitializeLineup(midfieldLineXPosition, midfieldPointYPosition, false, false);
            SecondTeam.IsAllowedToTakeBall = false;

            List<Player> allPlayers = new List<Player>();
            allPlayers.AddRange(FirstTeam.Players);
            allPlayers.AddRange(SecondTeam.Players);
            this.allPlayers = allPlayers.ToArray();

            GameAI = new AI(FirstTeam, SecondTeam, GameBall, FieldCell);
            pathfindingSystem = new Pathfinding();
            Pathfinding.SetData(GameAI, this.allPlayers, FieldCell);
            Pathfinding.CurrentBlockedRoom = new BlockedRoom(new Rectangle(0, 0, 0, 0), FirstTeam);

            nextRoundActions = new List<RoundAction>();
            NextRoundCacheActions = new List<CachedRoundAction>();
            CurrentAction = new GameAction(Status.KickOff, FirstTeam, GameBall.Location);
            gameEventHandler = new GameEventHandler(this);

            InitializeEvents();
        }

        /// <summary>
        /// Initializes all player- and ball-events.
        /// </summary>
        private void InitializeEvents()
        {
            InitializeNextStep += HandleNextStep;
            GameBall.BallWillMove += gameEventHandler.BallWillMove;
            foreach (Player player in allPlayers)
            {
                player.BallContact += gameEventHandler.PlayerGetsBall;
                player.LosesBall += gameEventHandler.PlayerLosesBall;
                player.MovesBall += gameEventHandler.BallWillMove;
                player.FouledAPlayer += gameEventHandler.PlayerFouledAPlayer;
            }
        }

        /// <summary>
        /// Sets the ball to the midfieldpoint.
        /// </summary>
        public void SetBallToMidpoint()
        {
            GameBall.SetCoordinates(midfieldLineXPosition + 1, midfieldPointYPosition + 1);
            GameBall.ExactLocation = pathfindingSystem.GetExactLocation(GameBall.Location);
            GameBall.TargetPoint = null;
            GameBall.Speed = 0;
            GameBall.LastPlayer = null;
            GameBall.HasPlayerContact = false;
            allPlayers.ToList().ForEach(x => x.HasBall = false);
            Pathfinding.CurrentBlockedRoom = new BlockedRoom(new Rectangle(0, 0, 0, 0), FirstTeam);
        }

        /// <summary>
        /// Initializes the kickoff lineup for both teams
        /// </summary>
        /// <param name="kickingTeam"></param>
        public void InitializeKickOff(Team kickingTeam)
        {
            if (roundsPerHalf != 0 && Round > 0 && Round % roundsPerHalf == 0)
            {
                //switch the field side
                Goal rightGoal = kickingTeam.TeamGoal;
                kickingTeam.TeamGoal = GameAI.GetEnemyTeam(kickingTeam).TeamGoal;
                GameAI.GetEnemyTeam(kickingTeam).TeamGoal = rightGoal;

                //add changeover to replaymanager
                int leftId = (FirstTeam.HasLeftSide) ? FirstTeam.TeamId : SecondTeam.TeamId;
                int rightId = (FirstTeam.HasLeftSide) ? SecondTeam.TeamId : FirstTeam.TeamId;
                ChangeoverImage image = new ChangeoverImage(Round, leftId, rightId);
                CurrentReplayManager.Image.ChangeoverImages.Add(image);
            }

            FirstTeam.InitializeLineup(midfieldLineXPosition, midfieldPointYPosition, FirstTeam.HasLeftSide, kickingTeam.Equals(FirstTeam));
            FirstTeam.Players.ForEach(p => p.PlayerActions.Clear());
            SecondTeam.InitializeLineup(midfieldLineXPosition, midfieldPointYPosition, SecondTeam.HasLeftSide, kickingTeam.Equals(SecondTeam));
            SecondTeam.Players.ForEach(p => p.PlayerActions.Clear());

            FirstTeam.IsAllowedToTakeBall = kickingTeam.Equals(FirstTeam);
            SecondTeam.IsAllowedToTakeBall = kickingTeam.Equals(SecondTeam);

            SetKickOff = false;
            SetBallToMidpoint();
            ResetPassTargetsAndActions();
        }
        #endregion

        /* See documentation in the method. */
        public void HandleNextStep(object sender, EventArgs e)
        {
            if (!IsCalculating)
            {
                IsCalculating = true;
                //stop the timer
                LeftSeconds = secondsPerRound;
                RoundTimer.Stop();

                //Console.WriteLine("Status: " + CurrentAction.GameStatus + " Allowed Team: " + CurrentAction.ActionTeam.Name + " YellowIsAllowed: " + FirstTeam.IsAllowedToTakeBall + "RedIsAllowed: " + SecondTeam.IsAllowedToTakeBall + " HasLeftSide: " + CurrentAction.ActionTeam.HasLeftSide);
                //Console.WriteLine("BallLocation: " + GameBall.Location + " ActionPoint: " + CurrentAction.ActionPoint + " HasPlayerContact: " + GameBall.HasPlayerContact);

                //set the calculated actions and calc the way
                SetCalculatedActionsAndWay();

                //execute the actions
                ExecutePlayerActions();
                UpdateTargetPoints();

                //Changeover - roundsPerHalf == 0 for no changeover
                if (roundsPerHalf != 0 && Round > 0 && Round % roundsPerHalf == 0)
                {
                    halftime++;
                    Team teamWithLeftSide = (FirstTeam.HasLeftSide) ? SecondTeam : FirstTeam;
                    CurrentAction = new GameAction(Status.KickOff, teamWithLeftSide, new Point(0, 0));
                    SetKickOff = true;
                }
                if (SetKickOff)
                {
                    CurrentAction = new GameAction(Status.KickOff, CurrentAction.ActionTeam, new Point(0, 0));
                    InitializeKickOff(CurrentAction.ActionTeam);
                }

                if (!IsStopped && !IsMultiplayer)
                {
                    CoordinatesChanged(this, EventArgs.Empty);
                }

                //search new actions
                CheckIntelligenceUse();
                CalculateAiActions();

                RoundImage currentImage = CreateRoundImage();
                if (IsMultiplayer)
                {
                    //send the new coordinates to the server if the user plays in a multiplayer game at the moment
                    RoundImageEventArgs args = new RoundImageEventArgs(currentImage);
                    GameImageChanged(this, args);
                    CreateRoundImage();
                }
                else
                {
                    //adds the round image to the replay replay.
                    CurrentReplayManager.Image.RoundImages.Add(currentImage);
                }

                //true if this is the last round of the second halftime --> the game will end.
                if (halftime >= 3)
                {
                    GameEnded(this, EventArgs.Empty);
                }
                else
                {
                    //start the timer for the next round
                    RoundTimer.Start();
                    IsCalculating = false;
                }
            }
        }

        /// <summary>
        /// Enqueues the calculated actions in the appertaining queues.
        /// Sets everyones' stepsLeft value to his maximum speed value and raises every player's RemovePreviewActions()-methhod as well as the CalcWayToTarget()-method.
        /// </summary>
        private void SetCalculatedActionsAndWay()
        {
            //remove all temporary actions
            allPlayers.ToList().ForEach(x => x.RemovePreviewActions());

            foreach (RoundAction newAction in nextRoundActions)
            {
                if (newAction.HeldPlayer.NeedsIntelligence && !newAction.HeldPlayer.IsPassTarget)
                {
                    if (newAction.ClearQueue)
                    {
                        newAction.HeldPlayer.PlayerActions.Clear();
                        newAction.HeldPlayer.IsMarkedUp = false;
                    }

                    newAction.HeldPlayer.PlayerActions.Enqueue(newAction.HeldAction);
                }
            }

            nextRoundActions.Clear();
            foreach (Player player in allPlayers)
            {
                //player.RemovePreviewActions();
                player.LeftSteps = player.MaxSpeed;
                player.LeftActions = 1;
                player.CalculateWaysToTarget(false);
            }
        }

        /// <summary>
        /// Raises every player's ExecuteAction()-method, if he is not affected by a run/tackle-conflict.
        /// Raises the HandleRunConflicts()- and HandleTackleConflicts()-method with the affected players as the parameter.
        /// </summary>
        private void ExecutePlayerActions()
        {
            RunConflictHandler runHandler = new RunConflictHandler();
            List<RunConflict> conflicts = runHandler.SearchRunConflicts(allPlayers);
            TackleConflictHandler tackleHandler = new TackleConflictHandler();
            List<Player> tacklePlayers = tackleHandler.SearchTacklePlayers(allPlayers);

            GameBall.Move();
            foreach (Player player in allPlayers)
            {
                if (!tacklePlayers.Contains(player))
                {
                    bool isInvolvedInConflict = runHandler.ContainsPlayer(conflicts, player);
                    player.ExecuteAction(GameBall, isInvolvedInConflict, CurrentAction.GameStatus);
                }
            }
            HandleRunConflicts(conflicts);
            HandleTackleConflicts(tacklePlayers);
        }

        /// <summary>
        /// Iterates through all players. If the player has no action left and the KI is allowed to control him, his NeedsIntelligence-bool will be set to true;
        /// That means that the AI will choose the player's action.
        /// </summary>
        private void CheckIntelligenceUse()
        {
            FirstTeam.Players.ForEach(x =>
            {
                if (x.PlayerActions.Count == 0 && firstTeamUsesKI && !x.HasPlayerActionToGetBall())
                {
                    x.NeedsIntelligence = true;
                }
            });

            SecondTeam.Players.ForEach(x =>
            {
                if (x.PlayerActions.Count == 0 && secondTeamUsesKI && !x.HasPlayerActionToGetBall())
                {
                    x.NeedsIntelligence = true;
                }
            });
        }

        /// <summary>
        /// Enqueues a new PlayerAction for every player for whom the player did not set an action manually.
        /// </summary>
        private void CalculateAiActions()
        {
            nextRoundActions.Clear();

            //search two players from the team who don't control the ball and instruct them to get the ball
            InstructNearestPlayersToTakeBall();

            foreach (Player player in allPlayers)
            {
                if (!player.LiesOnTheGround && player.NeedsIntelligence && !IsPlayerCalculated(player) && (!player.IsPassTarget || player.HasBall || !player.HasPlayerActionToGetBall()))
                {
                    PlayerAction nextAction = GameAI.CalculatePlayerAction(player, CurrentAction);

                    if (nextAction != null)
                    {
                        RoundAction nextActionHolder = new RoundAction(nextAction, player);

                        nextActionHolder.ClearQueue = true;
                        nextRoundActions.Add(nextActionHolder);
                    }
                }
            }
        }

        /// <summary>
        /// Searches the winner in every run conflict so that he can execute his action first.
        /// </summary>
        /// <param name="conflicts"></param>
        private void HandleRunConflicts(List<RunConflict> conflicts)
        {
            RunConflictHandler runHandler = new RunConflictHandler();
            foreach (RunConflict conflict in conflicts)
            {
                Player winner = runHandler.RunConflictWinner(conflict);
                winner.ExecuteAction(GameBall, false, CurrentAction.GameStatus);
                conflict.InvolvedPlayers.Remove(winner);
                foreach (Player restPlayer in conflict.InvolvedPlayers)
                {
                    restPlayer.ExecuteAction(GameBall, false, CurrentAction.GameStatus);
                }
            }
        }

        /// <summary>
        /// The player who will get tackled will execute his actions first.
        /// We have to do this because a player with a ball must have the chance to kick the ball away.
        /// </summary>
        /// <param name="tacklePlayers"></param>
        private void HandleTackleConflicts(List<Player> tacklePlayers)
        {
            foreach (Player player in tacklePlayers)
            {
                player.PlayerActions.Peek().AffectedPlayer.ExecuteAction(GameBall, false, CurrentAction.GameStatus);
                player.ExecuteAction(GameBall, false, CurrentAction.GameStatus);
            }
        }

        /// <summary>
        /// Updates everyones' target point so that the direction lines can be drawn correctly.
        /// </summary>
        private void UpdateTargetPoints()
        {
            foreach (Player player in allPlayers)
            {
                foreach (PlayerAction action in player.PlayerActions)
                {
                    action.UpdateTargetPoint(player.Location, player.MaxSpeed);
                }
                player.CalculateWaysToTarget(false);
            }
        }

        /// <summary>
        /// Searches one player from the team who doesn't control the ball and instruct him to get the ball.
        /// </summary>
        private void InstructNearestPlayersToTakeBall()
        {
            Player lastPlayerWithBall = (FirstTeam.PlayerWithBall != null) ? FirstTeam.PlayerWithBall : SecondTeam.PlayerWithBall;

            List<Player> nearestPlayersToBall = new List<Player>();
            Point ballNextRoundLocation = GameAI.BallNextRoundLocation();

            //Search good players which stands close to the ball
            int ballHorizontal = pathfindingSystem.CalculateHorizontalDirection(GameBall.Location, ballNextRoundLocation);
            int ballVertical = pathfindingSystem.CalculateVerticalDirection(GameBall.Location, ballNextRoundLocation);

            int ballDirectionToFirstGoal = pathfindingSystem.CalculateHorizontalDirection(ballNextRoundLocation, FirstTeam.TeamGoal.MiddlePoint);
            int ballDirectionToSecondGoal = pathfindingSystem.CalculateHorizontalDirection(ballNextRoundLocation, SecondTeam.TeamGoal.MiddlePoint);

            List<Player> firstTeamGoodPlayers;
            List<Player> secondTeamGoodPlayers;

            firstTeamGoodPlayers = GameAI.PlayersWithOtherHorizontalDirection(ballNextRoundLocation, ballDirectionToFirstGoal, FirstTeam);
            secondTeamGoodPlayers = GameAI.PlayersWithOtherHorizontalDirection(ballNextRoundLocation, ballDirectionToSecondGoal, SecondTeam);

            //instruct both teams if no one ever controlled the ball

            if (CurrentAction.GameStatus == Status.KickOff && CurrentAction.ActionTeam.PlayerWithBall == null)
            {
                nearestPlayersToBall.AddRange(GameAI.NearestPlayersToPoint(CurrentAction.ActionTeam.Players, ballNextRoundLocation, 1, 0, false));
            }
            else if (CurrentAction.GameStatus == Status.Normal)
            {
                if (lastPlayerWithBall == null)
                {
                    nearestPlayersToBall.AddRange(GameAI.NearestPlayersToPoint(firstTeamGoodPlayers, ballNextRoundLocation, 1, 0, false));
                    nearestPlayersToBall.AddRange(GameAI.NearestPlayersToPoint(secondTeamGoodPlayers, ballNextRoundLocation, 1, 0, false));
                }
                else
                {
                    Team lastPlayerTeam = GameAI.PlayersTeam(lastPlayerWithBall);
                    Team enemyTeam = GameAI.GetEnemyTeam(lastPlayerTeam);

                    List<Player> goodPlayers = (enemyTeam.Equals(FirstTeam)) ? firstTeamGoodPlayers : secondTeamGoodPlayers;
                    nearestPlayersToBall.AddRange(GameAI.NearestPlayersToPoint(goodPlayers, ballNextRoundLocation, 1, 0, false));
                }
            }
            else
            {
                if (CurrentAction.ActionTeam.PlayerWithBall == null)
                {
                    nearestPlayersToBall.AddRange(GameAI.NearestPlayersToPoint(CurrentAction.ActionTeam, GameBall.Location, 1, 1, false));
                }
            }

            //adds the action to the players
            foreach (Player player in nearestPlayersToBall)
            {
                PlayerAction nextAction;
                if (GameBall.HasPlayerContact && GameBall.LastPlayer != null)
                {
                    GameBall.LastPlayer.IsMarkedUp = true;
                    nextAction = new PlayerAction(GameBall.LastPlayer, GameBall.LastPlayer.Location, ActionType.Tackle);
                }
                else
                {
                    int distance = (int)Pathfinding.DistanceBetweenPoints(player.Location, GameBall.Location);
                    nextAction = new PlayerAction(null, ballNextRoundLocation, ActionType.Run);
                }
                RoundAction nextActionHolder = new RoundAction(nextAction, player);
                nextActionHolder.ClearQueue = true;
                nextRoundActions.Add(nextActionHolder);
            }
        }

        /// <summary>
        /// Checks whether the artificial intelligence did calculate player action for the player already.
        /// </summary>
        /// <param name="searchPlayer"></param>
        /// <returns></returns>
        private bool IsPlayerCalculated(Player searchPlayer)
        {
            foreach (RoundAction holder in nextRoundActions)
            {
                if (holder.HeldPlayer.Equals(searchPlayer))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The method is raised when a player gets ball contact. Sets every player's IsPassTarget-boolean to false.
        /// Removes all PlayerActions where IsActionToGetBall is true.
        /// </summary>
        public void ResetPassTargetsAndActions()
        {
            allPlayers.ToList().ForEach(x =>
            {
                x.IsPassTarget = false;
                x.PlayerActions.ToList().RemoveAll(action => action.IsActionToGetBall);
            });
        }

        /// <summary>
        /// Opens the action dialog at the player's position and adds the new action to the player, if the user did not cancel the dialog.
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="toActionPoint"></param>
        /// <param name="dialogXPos"></param>
        /// <param name="dialogYPos"></param>
        /// <param name="previewAction">The action which the program calculated for the preview.</param>
        public void OpenActionDialog(Player fromPlayer, PlayerAction toActionPoint, int dialogXPos, int dialogYPos)
        {
            PlayerAction previewAction = fromPlayer.SearchPreviewAction();
            fromPlayer.RemovePreviewActions();

            MessageBoxButtons button = MessageBoxButtons.OK;
            MessageBoxIcon icon = MessageBoxIcon.Error;
            if (CurrentAction.GameStatus == Status.ThrowIn && fromPlayer.HasBall && CurrentAction.ActionTeam.Equals(GameAI.PlayersTeam(fromPlayer)) && !CurrentAction.ThrowRoom.Contains(toActionPoint.TargetPoint))
            {
                MessageBox.Show("Too far away.", "Error!", button, icon);
            }
            else if (Pathfinding.CurrentBlockedRoom.Room.Contains(toActionPoint.TargetPoint) && !Pathfinding.CurrentBlockedRoom.AllowedTeam.Equals(GameAI.PlayersTeam(fromPlayer)))
            {
                MessageBox.Show("This player is not allowed to run into this room at the moment.", "Error!", button, icon);
            }
            else
            {
                string action = "";
                bool replace = false;
                DialogResult result = ActionDialog.ShowBox(fromPlayer, toActionPoint.AffectedPlayer, dialogXPos, dialogYPos, CurrentAction.GameStatus, ref action, ref replace);
                if (result == DialogResult.OK)
                {
                    AddNewPlayerAction(fromPlayer, toActionPoint, previewAction, replace, (ActionType)Enum.Parse(typeof(ActionType), action));
                }
            }
        }

        /// <summary>
        /// Enqueues the chosen action to the player's action-queue.
        /// If the user chooses a run action, the programm will use the way which was calculated for the preview.
        /// In Multiplayer: Sends the action to the server.
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="toActionPoint"></param>
        /// <param name="previewAction"></param>
        public void AddNewPlayerAction(Player fromPlayer, PlayerAction toActionPoint, PlayerAction previewAction, bool replace, ActionType chosenActionType)
        {
            bool calculateWay;
            PlayerAction playerAction;

            if (previewAction != null && (chosenActionType == ActionType.Run || chosenActionType == ActionType.Tackle))
            {
                //do not calculate the way if the user had the action saved already.
                playerAction = previewAction;
                playerAction.IsPreviewAction = false;
                calculateWay = false;
            }
            else
            {
                calculateWay = true;
                playerAction = new PlayerAction(toActionPoint.AffectedPlayer, toActionPoint.TargetPoint, chosenActionType);
            }

            if (!IsMultiplayer)
            {
                if (replace)
                {
                    calculateWay = true;
                    fromPlayer.PlayerActions.Clear();
                }

                fromPlayer.PlayerActions.Enqueue(playerAction);

                fromPlayer.NeedsIntelligence = false;
                if (calculateWay)
                {
                    fromPlayer.CalculateWaysToTarget(true);
                }
            }
            else
            {
                RoundAction holder = new RoundAction(playerAction, fromPlayer);

                ///Send the new action to the server if the user plays in multiplayer mode
                if (ActionChosen != null)
                {
                    CachedPlayerAction cachedPlayerAction = GameAI.ConvertToHolder(holder.HeldAction);
                    CachedPlayer cachedPlayer = new CachedPlayer(holder.HeldPlayer.Id, holder.HeldPlayer.TeamId);
                    CachedRoundAction cachedAction = new CachedRoundAction(cachedPlayerAction, cachedPlayer);
                    cachedAction.ClearQueue = replace;

                    ReceivedActionArgs actionArgs = new ReceivedActionArgs(cachedAction);
                    ActionChosen(this, actionArgs);
                }
            }
        }

        /// <summary>
        /// Adds a cache action to fromPlayer's PlayerAction-Queue.
        /// This action is temporary and will get removed as soon as the user releases the mouse button.
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="targetPoint"></param>
        public void AddCacheAction(Player fromPlayer, Point targetPoint)
        {
            fromPlayer.RemovePreviewActions();

            //fromPlayer.TemporyTargetPoint = targetPoint;
            ActionType type = (CurrentAction.GameStatus != Status.Normal && fromPlayer.HasBall) ? ActionType.Pass : ActionType.Run;
            PlayerAction action = new PlayerAction(null, targetPoint, type);
            action.IsPreviewAction = true;

            fromPlayer.PlayerActions.Enqueue(action);
            fromPlayer.CalculateWaysToTarget(true);
        }

        /// <summary>
        /// Checks whether a player stands on this cell at the moment.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsPlayerOnCell(Point cell)
        {
            return !allPlayers.ToList().TrueForAll(x => x.Location != cell);
        }

        /// <summary>
        /// Starts the round timer.
        /// </summary>
        public void StartRoundTimer()
        {
            RoundTimer = new System.Timers.Timer();
            LeftSeconds = secondsPerRound;
            RoundTimer.Interval = 1000;
            RoundTimer.Elapsed += stepTimer_Elapsed;
            RoundTimer.Start();
        }

        /// <summary>
        /// Decreases the left seconds for this round.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void stepTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            LeftSeconds--;
        }

        /// <summary>
        /// Raises the CoordinatesChanged()-event so that the GUI will redraw all game objects.
        /// </summary>
        public void RefreshField()
        {
            CoordinatesChanged(this, EventArgs.Empty);
        }

        #region Multiplayer

        /// <summary>
        /// Converts the cached action to an uncached action and enqueues it to the player's ActionQueue as a temporary action.
        /// The IsTemporary-bool is important for the host. When he receives all saved actions for a round, he has to delete his saved tempoary actions.
        /// Otherwise there could be multiple clones of the actions.
        /// </summary>
        /// <param name="cachedAction"></param>
        public void AddCachedAction(CachedRoundAction cachedAction)
        {
            Player player = GameAI.ConvertToPlayer(cachedAction.HeldPlayer.PlayerId, cachedAction.HeldPlayer.TeamId);
            PlayerAction action = GameAI.ConvertToAction(cachedAction.HeldAction);

            RoundAction uncachedAction = new RoundAction(action, player);
            action.IsTemporary = true;
            if (cachedAction.ClearQueue)
            {
                player.PlayerActions.Clear();
            }
            player.PlayerActions.Enqueue(action);
            player.CalculateWaysToTarget(true);

            NewDirectionLines(this, EventArgs.Empty);
        }

        /// <summary>
        /// Converts the cached action holders to uncached action holders
        /// and enqueues these actions to the appertaining players.
        /// Increases the current round and raises InitializeNextStep()-event then.
        /// </summary>
        /// <param name="cachedRoundActions"></param>
        public void CalculateRound(List<CachedRoundAction> cachedRoundActions)
        {
            foreach (CachedRoundAction cachedAction in cachedRoundActions)
            {
                Player player = GameAI.ConvertToPlayer(cachedAction.HeldPlayer.PlayerId, cachedAction.HeldPlayer.TeamId);
                List<PlayerAction> actionList = player.PlayerActions.ToList();
                actionList.RemoveAll(x => x.IsTemporary);
                player.PlayerActions = new Queue<PlayerAction>(actionList);

                PlayerAction action = GameAI.ConvertToAction(cachedAction.HeldAction);

                RoundAction uncachedAction = new RoundAction(action, player);
                if (cachedAction.ClearQueue)
                {
                    player.PlayerActions.Clear();
                }
                player.PlayerActions.Enqueue(action);
                player.CalculateWaysToTarget(true);
                player.NeedsIntelligence = false;
            }

            Round++;
            InitializeNextStep(this, EventArgs.Empty);
        }
        #endregion

        #region Images
        /// <summary>
        /// Converts the current round into a RoundImage and returns it.
        /// </summary>
        public RoundImage CreateRoundImage()
        {
            RoundImage currentImage = new RoundImage();

            //Creates a PlayerHolder for each player.
            List<CachedPlayer> players = new List<CachedPlayer>();
            foreach (Player player in allPlayers)
            {
                player.RemovePreviewActions();
                List<CachedPlayerAction> actionHolders = new List<CachedPlayerAction>();
                foreach (PlayerAction action in player.PlayerActions)
                {
                    actionHolders.Add(GameAI.ConvertToHolder(action));
                }

                CachedPlayer holder = new CachedPlayer()
                {
                    LiesOnTheGround = player.LiesOnTheGround,
                    TeamId = player.TeamId,
                    PlayerId = player.Id,
                    HasBall = player.HasBall,
                    Location = player.Location,
                    Actions = actionHolders,
                    IsPassTarget = player.IsPassTarget
                };

                players.Add(holder);
            }

            currentImage.Players = players;

            CachedBall ballHolder = new CachedBall()
            {
                ExactLocation = GameBall.ExactLocation,
                ExactTargetLocation = GameBall.ExactTargetLocation,
                HasPlayerContact = GameBall.HasPlayerContact,
                HasTargetPoint = GameBall.TargetPoint.HasValue,
                Speed = GameBall.Speed,
                IsInShootState = GameBall.IsInShootState,
                LastPlayer = (GameBall.LastPlayer == null) ? null : new CachedPlayer(GameBall.LastPlayer.Id, GameBall.LastPlayer.TeamId)
            };

            currentImage.Ball = ballHolder;
            currentImage.CurrentGameAction = new CachedGameAction()
            {
                TeamId = CurrentAction.ActionTeam.TeamId,
                ActionPoint = CurrentAction.ActionPoint,
                GameStatus = CurrentAction.GameStatus,
                ThrowRoom = CurrentAction.ThrowRoom,
            };

            currentImage.BlockedRoom = Pathfinding.CurrentBlockedRoom.Room;
            currentImage.Number = Round;
            return currentImage;
        }

        /// <summary>
        /// Updates the position of all players as well as the position of the ball.
        /// Updates the CurrentAction.
        /// Starts the RoundTimer at the end if this is not a manual change (for multiplayer).
        /// </summary>
        /// <param name="status"></param>
        public void SetRoundImage(RoundImage status, bool isManualChange)
        {
            //stops the round timer
            LeftSeconds = secondsPerRound;
            RoundTimer.Stop();

            //Update the player
            foreach (CachedPlayer holder in status.Players)
            {
                List<PlayerAction> actions = new List<PlayerAction>();
                foreach (CachedPlayerAction actionHolder in holder.Actions)
                {
                    actions.Add(GameAI.ConvertToAction(actionHolder));
                }

                Player player = GameAI.ConvertToPlayer(holder.PlayerId, holder.TeamId);
                player.LiesOnTheGround = holder.LiesOnTheGround;
                player.HasBall = holder.HasBall;
                player.Location = holder.Location;
                player.IsPassTarget = holder.IsPassTarget;
                player.PlayerActions = new Queue<PlayerAction>(actions);
                player.CalculateWaysToTarget(true);
                player.SetDirectionToNextWayPoint();
                player.CheckPlayerImage();
            }

            //Update the ball
            GameBall.ExactTargetLocation = status.Ball.ExactTargetLocation;
            GameBall.ExactLocation = status.Ball.ExactLocation;
            GameBall.Location = pathfindingSystem.GetGridLocation(status.Ball.ExactLocation);
            GameBall.HasPlayerContact = status.Ball.HasPlayerContact;
            GameBall.IsInShootState = status.Ball.IsInShootState;
            GameBall.Speed = status.Ball.Speed;
            GameBall.LastPlayer = (status.Ball.LastPlayer == null) ? null : GameAI.ConvertToPlayer(status.Ball.LastPlayer.PlayerId, status.Ball.LastPlayer.TeamId);
            if (status.Ball.HasTargetPoint)
            {
                GameBall.TargetPoint = pathfindingSystem.GetGridLocation(GameBall.ExactTargetLocation);
            }
            else
            {
                GameBall.TargetPoint = null;
            }

            //Update the game action
            CurrentAction.ActionPoint = status.CurrentGameAction.ActionPoint;
            CurrentAction.GameStatus = status.CurrentGameAction.GameStatus;
            CurrentAction.ThrowRoom = status.CurrentGameAction.ThrowRoom;
            CurrentAction.ActionTeam = (status.CurrentGameAction.TeamId == 0) ? FirstTeam : SecondTeam;
            CurrentAction.ActionTeam.IsAllowedToTakeBall = true;

            if (CurrentAction.GameStatus != Status.Normal)
            {
                GameAI.GetEnemyTeam(CurrentAction.ActionTeam).IsAllowedToTakeBall = false;
            }
            else
            {
                GameAI.GetEnemyTeam(CurrentAction.ActionTeam).IsAllowedToTakeBall = true;
            }

            Pathfinding.CurrentBlockedRoom.Room = status.BlockedRoom;
            Pathfinding.CurrentBlockedRoom.AllowedTeam = (status.CurrentGameAction.TeamId == 0) ? FirstTeam : SecondTeam;

            //update the goals
            SetCorrectGoals(status.Number);
            SetCorrectGoalsCount(status.Number);

            round = status.Number;
            RoundChanged(this, EventArgs.Empty);
            CoordinatesChanged(this, EventArgs.Empty);

            if (!isManualChange)
            {
                //starts the roundtimer
                RoundTimer.Start();
            }
        }

        /// <summary>
        /// Set the correct field side for each team using the changeover image.
        /// </summary>
        /// <param name="image"></param>
        public void UseChangeoverImage(ChangeoverImage image)
        {
            Goal leftGoal = (FirstTeam.HasLeftSide) ? FirstTeam.TeamGoal : SecondTeam.TeamGoal;
            Goal rightGoal = (FirstTeam.HasLeftSide) ? SecondTeam.TeamGoal : FirstTeam.TeamGoal;

            FirstTeam.TeamGoal = (FirstTeam.TeamId == image.LeftGoalTeamId) ? leftGoal : rightGoal;
            SecondTeam.TeamGoal = (SecondTeam.TeamId == image.LeftGoalTeamId) ? leftGoal : rightGoal;
        }

        /// <summary>
        /// Sets the ball's target point to the exactlocation's gridlocation and let the ball move to this target point.
        /// </summary>
        /// <param name="exactLocation"></param>
        public void UseBallEventImage(BallEventImage image)
        {
            Point exactLocation = image.ExactLocation;
            Point gridLocation = pathfindingSystem.GetGridLocation(exactLocation);
            if (GameBall.Location != gridLocation)
            {
                GameBall.TargetPoint = gridLocation;
                GameBall.ExactTargetLocation = exactLocation;
                GameBall.Speed = 10;
                GameBall.Move();
            }
        }

        /// <summary>
        /// Informs the other players that someone shot a goal.
        /// </summary>
        /// <param name="image"></param>
        public void InformAboutGoal(BallEventImage image)
        {
            BallEventArgs args = new BallEventArgs(image);
            GoalsChanged(this, args);
        }
        #endregion

        #region ReplayManager
        /// <summary>
        /// Jumps to the round in the parameter.
        /// </summary>
        /// <param name="round"></param>
        public void JumpToRound(int round)
        {
            if (CurrentReplayManager != null)
            {
                if (round <= 0)
                {
                    SetRoundImage(CurrentReplayManager.Image.RoundImages[0], true);
                }
                else
                {
                    int target = (round > CurrentReplayManager.Image.RoundImages.Count - 1) ? CurrentReplayManager.Image.RoundImages.Count -1 : round;
                    SetRoundImage(CurrentReplayManager.Image.RoundImages[target], true);
                }

                halftime = CurrentReplayManager.SearchHalfTime(round);
            }
        }

        /// <summary>
        /// Jumps to the previous round.
        /// </summary>
        public void PreviousRound()
        {
            JumpToRound(Round - 1);
        }

        /// <summary>
        /// Jumps to the next round.
        /// </summary>
        public void NextRound()
        {
            JumpToRound(Round + 1);
        }

        /// <summary>
        /// Uses the replay to set which team controls which goal at the current round.
        /// </summary>
        public void SetCorrectGoals(int round)
        {
            int leftGoalTeamId = CurrentReplayManager.LeftGoalOwner(round);

            Goal leftGoal = (FirstTeam.HasLeftSide) ? FirstTeam.TeamGoal : SecondTeam.TeamGoal;
            Goal rightGoal = (FirstTeam.HasLeftSide) ? SecondTeam.TeamGoal : FirstTeam.TeamGoal;

            FirstTeam.TeamGoal = (leftGoalTeamId == FirstTeam.TeamId) ? leftGoal : rightGoal;
            SecondTeam.TeamGoal = (leftGoalTeamId == SecondTeam.TeamId) ? leftGoal : rightGoal;
        }

        /// <summary>
        /// Uses the replay replay to set the amount of goals both teams have at the current round.
        /// </summary>
        public void SetCorrectGoalsCount(int round)
        {
            FirstTeam.GoalsCount = CurrentReplayManager.SearchTeamGoalsCount(FirstTeam.TeamId, round);
            SecondTeam.GoalsCount = CurrentReplayManager.SearchTeamGoalsCount(SecondTeam.TeamId, round);
        }
        #endregion

        #region TestMethods
        private bool IsBallInvisible()
        {
            bool noOneHasBall = allPlayers.ToList().TrueForAll(x => !x.HasBall);
            return noOneHasBall && GameBall.HasPlayerContact;
        }

        private int PassTargets()
        {
            return allPlayers.ToList().FindAll(x => x.IsPassTarget).Count;
        }

        private bool ArePlayerOnSameCell()
        {
            for (int i = 0; i < allPlayers.Length; i++)
            {
                for (int a = 0; a < allPlayers.Length; a++)
                {
                    if (i != a)
                    {
                        if (allPlayers[i].Location == allPlayers[a].Location)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        #endregion
    }
}
