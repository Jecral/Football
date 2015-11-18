using Football.EventArguments;
using Football.GUI;
using Football.GUI.Multiplayer;
using Football.GUI.Other;
using Football.Logic;
using Football.Logic.GameObjects.Player;
using Football.Multiplayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.General
{
    class FieldForm : Form
    {
        public FieldForm(GameSettings settings)
        {
            StartGuiInitialisation(settings);
            StartGameInitialisation(settings, false, null);
        }

        public FieldForm(string savegamePath, GameSettings standardSettings)
        {
            ReplayManager manager = new ReplayManager(savegamePath);
            manager.Load();

            if (manager.Image != null)
            {
                StartGuiInitialisation(manager.Image.Settings);
                StartGameWithReplayManager(savegamePath);
            }
            else
            {
                StartGuiInitialisation(standardSettings);
                StartGameInitialisation(standardSettings, false, null);
            }
        }

        #region Properties
        private Rectangle[,] fieldCell;
        private Rectangle currentMouseRectangle;

        private Game currentGame;
        private BufferedPanel fieldGrid;
        private Panel directionLinesPanel;
        private FieldPanel stadiumPanel;
        private InformationPanel informationPanel;
        private StatusStrip informationStatusStrip;

        private int midfieldLineXPosition;
        private int midfieldPointYPosition;
        private int leftGoalHeight;
        private int leftGoalYPosition;
        private int rightGoalHeight;
        private int rightGoalYPosition;

        private bool isChooseMode;
        private bool waitsForHost;
        private Player chosenPlayer;

        public MultiplayerHandler CurrentMpHandler { get; set; }
        private ReplayManagerForm replayManager;
        #endregion

        #region Initialisation
        /// <summary>
        /// Saves the game settings, sets the settings for the form and initializes all gui components.
        /// </summary>
        /// <param name="settings"></param>
        private void StartGuiInitialisation(GameSettings settings)
        {
            this.midfieldLineXPosition = settings.MidLineXPosition;
            this.midfieldPointYPosition = settings.MidPointYPosition;
            this.leftGoalHeight = settings.LeftGoalHeight;
            this.leftGoalYPosition = settings.LeftGoalYPosition;
            this.rightGoalHeight = settings.RightGoalHeight;
            this.rightGoalYPosition = settings.RightGoalYPosition;

            Icon = Properties.Resources.Icon;
            Text = "Football";
            ClientSize = new Size(1200, 760);
            MaximumSize = new Size(1216, 798);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            InitializeMenuStrip();
            InitializeComponents(settings);
            InitializeEvents();

            Show();
        }

        /// <summary>
        /// Starts the initialization of all UI components as well as the initialization of the game.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        /// <param name="secondsPerStep"></param>
        /// <param name="isHost"></param>
        /// <param name="IsMultiplayer"></param>
        public void StartGameInitialisation(GameSettings settings, bool isMultiplayer, ReplayManager replay)
        {
            currentGame = new Game(fieldCell, settings, isMultiplayer, replay);
            currentGame.InitializeGame();
            InitializeGameEvents();
            currentGame.SetBallToMidpoint();

            //add the start round to the replay manager
            if (replay == null)
            {
                currentGame.CurrentReplayManager.Image.RoundImages.Add(currentGame.CreateRoundImage());
            }

            Show();
            Refresh();

            if (isMultiplayer)
            {
                AddOpenChatButton();
            }
            fieldGrid.Focus();
            currentGame.StartRoundTimer();
            currentGame.IsWaitingForHost += WaitingForHostChanged;
        }

        /// <summary>
        /// Raises the CreateNewGame()-method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MultiplayerGameCreated(object sender, FieldSettingsEventArgs e)
        {
            CreateNewGame(e.Settings, true, null);
            CurrentMpHandler.MultiplayerGame = currentGame;
        }

        /// <summary>
        /// Raises the CreateNewGame()-method with the game image in a new replay manager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GameContinued(object sender, GameImageEventArgs e)
        {
            ReplayManager manager = new ReplayManager();
            manager.Image = e.Image;
            CreateNewGame(e.Image.Settings, true, manager);
            currentGame.SetRoundImage(manager.SearchImageAtRound(manager.SearchLastRoundNumber()), false);
            currentGame.SetCorrectGoals(currentGame.Round);
            currentGame.SetCorrectGoalsCount(currentGame.Round);
            CurrentMpHandler.MultiplayerGame = currentGame;
        }

        /// <summary>
        /// Raises the CreateNewGame()-method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SingleplayerGameCreated(object sender, FieldSettingsEventArgs e)
        {
            CreateNewGame(e.Settings, false, null);
        }

        /// <summary>
        /// Starts a new game with the given GameSettings.
        /// 
        /// Opens a new ChatForm (hidden in the status strip) and a userlist to the form if it is a multiplayer game.
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="isMultiplayer"></param>
        private void CreateNewGame(GameSettings settings, bool isMultiplayer, ReplayManager replay)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                Cursor = Cursors.Default;

                if (isMultiplayer)
                {
                    CurrentMpHandler.CurrentChatForm = new ChatForm(CurrentMpHandler.CurrentChats);
                    CurrentMpHandler.CurrentChatForm.ChatClosed += CurrentChatForm_ChatClosed;
                    CurrentMpHandler.CurrentLobby.ForceClose = true;
                    CurrentMpHandler.CurrentLobby.Close();
                }

                #region Settings
                int columns = settings.Columns;
                int rows = settings.Rows;
                int secondsPerRound = settings.SecondsPerRound;

                leftGoalHeight = settings.LeftGoalHeight;
                leftGoalYPosition = settings.LeftGoalYPosition;
                rightGoalHeight = settings.RightGoalHeight;
                rightGoalYPosition = settings.RightGoalYPosition;
                midfieldLineXPosition = settings.MidLineXPosition;
                midfieldPointYPosition = settings.MidPointYPosition;

                stadiumPanel.UpdateSettings(fieldCell, settings, directionLinesPanel.Location);
                #endregion

                if (currentGame != null)
                {
                    currentGame.IsStopped = true;
                    currentGame.RoundTimer.Stop();
                }

                StartGameInitialisation(settings, isMultiplayer, replay);
                if (isMultiplayer)
                {
                    informationPanel.AddUserList(CurrentMpHandler.Users, CurrentMpHandler.CurrentClient.ClientName);
                    CurrentMpHandler.RefreshUserList += CurrentMpHandler_RefreshUserList;
                    AddConnectionInformationLabel();
                }

                UpdateGoalsLabels();
                UpdateRoundLabel();
            }));
        }

        /// <summary>
        ///  Initializes the components.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        private void InitializeComponents(GameSettings settings)
        {
            int columns = settings.Columns;
            int rows = settings.Rows;

            #region Statusstrip
            informationStatusStrip = new StatusStrip();
            informationStatusStrip.Renderer = new CustomRenderer(new CustomColorTable());

            Controls.Add(informationStatusStrip);
            #endregion

            #region Game Panels
            Panel rootPanel = new Panel();
            rootPanel.Size = ClientSize;
            rootPanel.BackgroundImage = Properties.Resources.Stadium;
            rootPanel.Location = new Point(0, 20);
            rootPanel.BorderStyle = BorderStyle.FixedSingle;

            informationPanel = new InformationPanel();
            informationPanel.Size = new Size(ClientSize.Width, 130);
            informationPanel.Location = new Point(10, 0);
            informationPanel.BackColor = Color.Transparent;

            //Initialize stadium panel
            stadiumPanel = new FieldPanel(true);
            stadiumPanel.Size = new Size(ClientSize.Width, ClientSize.Height);
            stadiumPanel.Location = new Point(0, 60);
            stadiumPanel.BackColor = Color.Transparent;

            int edgeLength = 20;

            columns += 2;
            rows += 2;

            directionLinesPanel = new Panel();
            directionLinesPanel.Size = new Size((columns) * edgeLength, (rows) * edgeLength);
            directionLinesPanel.BackColor = Color.Transparent;
            directionLinesPanel.Paint += directionLinesPanel_Paint;
            directionLinesPanel.Location = new Point(70, 85);

            fieldGrid = new BufferedPanel();
            fieldGrid.Size = directionLinesPanel.Size;
            fieldGrid.BackColor = Color.Transparent;
            fieldGrid.Location = new Point(0, 0);
            fieldGrid.Paint += fieldGrid_Paint;
            fieldGrid.MouseMove += fieldGrid_MouseMove;
            fieldGrid.MouseDown += fieldGrid_MouseDown;
            fieldGrid.MouseUp += fieldGrid_MouseUp;

            fieldCell = new Rectangle[columns, rows];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Rectangle cell = new Rectangle();
                    cell.Size = new Size(edgeLength, edgeLength);
                    cell.Location = new Point(c * edgeLength, r * edgeLength);
                    fieldCell[c, r] = cell;
                }
            }

            directionLinesPanel.Controls.Add(fieldGrid);

            stadiumPanel.Controls.Add(directionLinesPanel);
            stadiumPanel.UpdateSettings(fieldCell, settings, directionLinesPanel.Location);
            #endregion

            rootPanel.Controls.Add(informationPanel);
            rootPanel.Controls.Add(stadiumPanel);

            Controls.Add(rootPanel);
        }

        /// <summary>
        /// Initializes all UI events.
        /// </summary>
        private void InitializeEvents()
        {
            informationPanel.DirectionLineSettingsChanged += informationPanel_DirectionLineSettingsChanged;
            FormClosing += FieldForm_FormClosing;
            fieldGrid.PreviewKeyDown += fieldGrid_PreviewKeyDown;
        }

        /// <summary>
        /// Initializes all game events.
        /// </summary>
        private void InitializeGameEvents()
        {
            currentGame.CoordinatesChanged += CoordinatesChanged;
            currentGame.RoundChanged += RoundChanged;
            currentGame.LeftSecondsChanged += LeftSecondsChanged;
            currentGame.FirstTeam.GoalsChanged += GoalsChanged;
            currentGame.SecondTeam.GoalsChanged += GoalsChanged;
            currentGame.GameEnded += GameEnded;

            currentGame.NewDirectionLines += ((object sender, EventArgs e) =>
            {
                directionLinesPanel.Invoke((MethodInvoker)(() =>
                {
                    directionLinesPanel.Invalidate();
                }));
            });
        }

        /// <summary>
        /// Initializes the menu strip with all it's dropdownitems.
        /// </summary>
        private void InitializeMenuStrip()
        {
            MenuStrip mainMenuStrip = new MenuStrip();
            mainMenuStrip.Renderer = new CustomRenderer(new CustomColorTable());

            ToolStripMenuItem file = new ToolStripMenuItem("&File");

            file.DropDownItems.Add(new ToolStripMenuItem("Save", Properties.Resources.MenuItemSaveImage, new EventHandler(this.SaveAGame_Clicked), Keys.Control | Keys.S));
            file.DropDownItems.Add(new ToolStripMenuItem("Load", Properties.Resources.MenuItemOpenImage, new EventHandler(this.LoadAGame_Clicked), Keys.Control | Keys.L));
            file.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem exit = new ToolStripMenuItem("Exit");
            exit.Image = Properties.Resources.MenuItemExitImage;
            exit.Click += this.Exit_Clicked;
            exit.ShortcutKeys = Keys.Alt | Keys.F4;

            file.DropDownItems.Add(exit);

            ToolStripMenuItem gameItem = new ToolStripMenuItem("&Game");

            gameItem.DropDownItems.Add(new ToolStripMenuItem("Create a Singleplayer Game", Properties.Resources.MenuItemPencilImage, new EventHandler(this.CreateAGame_Clicked), Keys.Control | Keys.H));
            gameItem.DropDownItems.Add(new ToolStripMenuItem("Join a Server", Properties.Resources.MenuItemInternetImage, new EventHandler(this.JoinAGame_Clicked), Keys.Control | Keys.J));

            mainMenuStrip.Items.Add(file);
            mainMenuStrip.Items.Add(gameItem);
            this.Controls.Add(mainMenuStrip);
        }
        #endregion

        #region PaintMethods

        /// <summary>
        /// Draws all game objects (players & ball).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void fieldGrid_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Image objectImage;
            Rectangle rectangle;
            Pen myPen = new Pen(Color.Black, 1);

            List<Player> allPlayers = new List<Player>();
            allPlayers.AddRange(currentGame.FirstTeam.Players);
            allPlayers.AddRange(currentGame.SecondTeam.Players);

            //Draws the players
            foreach (Player player in allPlayers)
            {
                rectangle = fieldCell[player.XCoordinate, player.YCoordinate];
                objectImage = player.ObjectImage;
                graphics.DrawImageUnscaledAndClipped(objectImage, rectangle);
            }

            //Draws the current rectangle
            if (isChooseMode)
            {
                rectangle = currentMouseRectangle;
                rectangle.X += 1;
                rectangle.Y += 1;
                rectangle.Width -= 2;
                rectangle.Height -= 2;
                graphics.DrawRectangle(myPen, rectangle);
            }

            //Draws the ball
            if (!currentGame.GameBall.HasPlayerContact)
            {
                objectImage = currentGame.GameBall.ObjectImage;
                Point targetPoint = currentGame.GameBall.ExactLocation;
                targetPoint.X -= 5;
                targetPoint.Y -= 5;
                graphics.DrawImage(objectImage, targetPoint);
            }

            //myPen.Color = Color.Red;
            //graphics.DrawRectangle(myPen, fieldCell[currentGame.GameBall.Location.X, currentGame.GameBall.Location.Y]);
        }

        /// <summary>
        /// Draws an arrow line from the start location to the target point of every players' action.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void directionLinesPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int cellWidth = fieldCell[0, 0].Size.Width;
            int cellHeight = fieldCell[0, 0].Size.Height;

            Pathfinding pathfinding = new Pathfinding();

            AdjustableArrowCap arrow = new AdjustableArrowCap(3, 3);
            Pen myPen = new Pen(Color.Blue, 1);
            myPen.CustomEndCap = arrow;

            Pen dp = new Pen(Brushes.Plum, 1.3f);
            dp.DashPattern = new float[] { 5f, 7f, 5f, 2f };
            dp.Color = Pathfinding.CurrentBlockedRoom.AllowedTeam.TeamColor;

            //draws the blocked room rectangle
            Rectangle drawRoom = new Rectangle(Pathfinding.CurrentBlockedRoom.Room.X * cellWidth, Pathfinding.CurrentBlockedRoom.Room.Y * cellHeight, Pathfinding.CurrentBlockedRoom.Room.Width * cellWidth, Pathfinding.CurrentBlockedRoom.Room.Height * cellHeight);
            graphics.DrawRectangle(dp, drawRoom);

            //draws the throw-room
            if (currentGame.CurrentAction.GameStatus == Status.ThrowIn && currentGame.GameBall.HasPlayerContact)
            {
                dp.DashPattern = new float[] { 20, 5 };
                dp.Color = Color.Black;
                Rectangle throwRoom = currentGame.CurrentAction.ThrowRoom;
                graphics.DrawRectangle(dp, throwRoom.X * cellWidth, throwRoom.Y * cellHeight, throwRoom.Width * cellWidth, throwRoom.Height * cellHeight);
            }

            if (informationPanel.LineSetting != DirectionLineSetting.NoCell)
            {
                //draw the player-directions
                List<Player> allPlayers = new List<Player>();
                allPlayers.AddRange(currentGame.FirstTeam.Players);
                allPlayers.AddRange(currentGame.SecondTeam.Players);
                foreach (Player player in allPlayers)
                {
                    if (player.PlayerActions.Count > 0 && !new Point(player.XCoordinate, player.YCoordinate).Equals(player.PlayerActions.Peek().TargetPoint))
                    {
                        DrawActionWays(graphics, player);
                    }
                }
            }

            //draw the ball direction if the setting "no-direction-lines" is not selected.
            if (informationPanel.LineSetting != DirectionLineSetting.NoCell && currentGame.GameBall.TargetPoint.HasValue && !currentGame.GameBall.HasPlayerContact)
            {
                myPen.Color = Color.Black;
                myPen.DashPattern = new float[] { 5f, 7f, 5f, 2f };
                Point from = currentGame.GameBall.ExactLocation;
                Point targetPoint = currentGame.GameBall.ExactTargetLocation;

                graphics.DrawLine(myPen, from, targetPoint);
            }
        }

        /// <summary>
        /// Draws football-specific quarter circles in all corners of the field.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="topLeftCorner"></param>
        /// <param name="pen"></param>
        private void DrawCornerCircles(Graphics graphics, Point topLeftCorner, Pen pen)
        {
            //drawing of the topleft corner arc
            Point topPoint = new Point(topLeftCorner.X + fieldCell[0, 0].X, topLeftCorner.Y + fieldCell[0, 0].Y);
            topPoint.X -= 10;
            topPoint.Y -= 10;

            Rectangle cornerRectangle = new Rectangle(topPoint, new Size(20, 20));
            graphics.DrawArc(pen, cornerRectangle, 90, -90);

            //drawing of the bottom left corner arc
            topPoint = new Point(topLeftCorner.X + fieldCell[0, fieldCell.GetLength(1) - 2].X, topLeftCorner.Y + fieldCell[0, fieldCell.GetLength(1) - 2].Y);
            topPoint.X -= 10;
            topPoint.Y -= 10;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 270, 90);

            //drawing of the top right corner arc
            Rectangle topRightRectangle = fieldCell[fieldCell.GetLength(0) - 2, 0];
            topPoint = new Point(topLeftCorner.X + topRightRectangle.X, topLeftCorner.Y + topRightRectangle.Y);
            topPoint.X -= 10;
            topPoint.Y -= 10;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 90, 90);

            //drawing of the bottom right corner arc
            Rectangle bottomRightRectangle = fieldCell[fieldCell.GetLength(0) - 2, fieldCell.GetLength(1) - 2];
            topPoint = new Point(topLeftCorner.X + bottomRightRectangle.X, topLeftCorner.Y + bottomRightRectangle.Y);
            topPoint.X -= 10;
            topPoint.Y -= 10;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 180, 90);

        }

        private void DrawGoalRoomCircles(Graphics graphics, Point topLeftCorner, Pen pen, Rectangle leftSixYard, Rectangle rightSixYard)
        {
            //int cellWidth = fieldCell[0, 0].Width;
            ////left side
            //Rectangle goalRoomRectangle = new Rectangle(topLeftCorner.X + cellWidth * 7, leftSixYard.Y, cellWidth * 5, leftSixYard.Height);
            //goalRoomRectangle.X -= 65;

            //graphics.DrawArc(pen, goalRoomRectangle, -80, 160);
            ////right side
            //goalRoomRectangle = new Rectangle(topLeftCorner.X + rightSixYard.X - cellWidth * 13, rightSixYard.Y, cellWidth * 5, rightSixYard.Height);
            //goalRoomRectangle.X += 35;

            //graphics.DrawArc(pen, goalRoomRectangle, 100, 160);

            int cellWidth = fieldCell[0, 0].Width;
            //left side
            Rectangle goalRoomRectangle = new Rectangle(topLeftCorner.X + cellWidth * 7, leftSixYard.Y, cellWidth * 5, leftSixYard.Height);
            goalRoomRectangle.X -= 50;

            graphics.DrawArc(pen, goalRoomRectangle, -90, 180);
            //right side
            goalRoomRectangle = new Rectangle(topLeftCorner.X + rightSixYard.X - cellWidth * 13, rightSixYard.Y, cellWidth * 5, rightSixYard.Height);
            goalRoomRectangle.X += 20;

            graphics.DrawArc(pen, goalRoomRectangle, 90, 180);
        }

        /// <summary>
        /// Draws the player's actions' ways to their target on the graphics depending on the current line Settings.
        /// </summary>
        /// <param name="graphics">The directionlinePanel's graphics</param>
        /// <param name="action"></param>
        /// <param name="player">The player who executes the actions</param>
        private void DrawActionWays(Graphics graphics, Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            AdjustableArrowCap arrow = new AdjustableArrowCap(3, 3);
            Pen myPen = new Pen(Color.Blue, 1);

            PlayerAction[] playerActions = player.PlayerActions.ToArray();
            Point lastAimPoint = pathfinding.GetExactLocation(player.Location);
            bool drawOneCell = informationPanel.LineSetting == DirectionLineSetting.OneCell;

            for (int i = 0; i < playerActions.Length; i++)
            {
                PlayerAction action = playerActions[i];

                myPen.Color = (action.Type == ActionType.Shoot || action.Type == ActionType.Pass) ? Color.Black : (currentGame.FirstTeam.Players.Contains(player)) ? currentGame.FirstTeam.TeamColor : (currentGame.SecondTeam.Players.Contains(player)) ? currentGame.SecondTeam.TeamColor : Color.DarkBlue;

                //set the correct dash pattern for each action type
                if (action.Type == ActionType.Pass || action.Type == ActionType.Shoot)
                {
                    myPen.DashPattern = new float[] { 5f, 7f, 5f, 2f };
                }
                else
                {
                    myPen.DashPattern = new float[] { 1f };
                }

                Point nextPoint = pathfinding.GetExactLocation(action.TargetPoint);

                if (informationPanel.LineSetting != DirectionLineSetting.DirectWay && action.Type == ActionType.Run || action.Type == ActionType.Tackle)
                {
                    lock (action.WayToTarget)
                    {
                        List<Point> waypoints = action.WayToTarget.ToList();
                        foreach (Point waypoint in waypoints)
                        {
                            nextPoint = pathfinding.GetExactLocation(waypoint);

                            if (waypoint.Equals(action.WayToTarget.ElementAt(action.WayToTarget.Count - 1)) || drawOneCell)
                            {
                                myPen.CustomEndCap = arrow;
                            }
                            else
                            {
                                myPen.EndCap = LineCap.NoAnchor;
                            }

                            graphics.DrawLine(myPen, lastAimPoint, nextPoint);
                            lastAimPoint = nextPoint;

                            if (drawOneCell)
                            {
                                lastAimPoint = pathfinding.GetExactLocation(action.TargetPoint);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    myPen.CustomEndCap = arrow;
                    graphics.DrawLine(myPen, lastAimPoint, nextPoint);
                }

                if (action.Type != ActionType.Shoot && action.Type != ActionType.Pass)
                {
                    lastAimPoint = nextPoint;
                    if (drawOneCell)
                    {
                        break;
                    }
                }
            }

        }
        #endregion

        #region MultiplayerEvents
        /// <summary>
        /// If the user is connected to a server at the moment, the program will ask him whether he
        /// wants to disconnect.
        /// 
        /// </summary>
        /// <returns>True if he wants do disconnect, otherwise false.
        /// Returns null if there is no connection.</returns>
        private Boolean? AskForDisconnect()
        {
            if (CurrentMpHandler != null && CurrentMpHandler.CurrentClient != null && CurrentMpHandler.CurrentClient.IsConnected)
            {
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                MessageBoxIcon icon = MessageBoxIcon.Warning;

                DialogResult rs = MessageBox.Show("You are still connected to a server.\n"
                    + "This action will disconnect you. Continue?", "Warning!", buttons, icon);

                return rs == System.Windows.Forms.DialogResult.Yes;
            }
            return null;
        }

        /// <summary>
        /// Refreshes the user list of the current multiplayer game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentMpHandler_RefreshUserList(object sender, EventArgs e)
        {
            informationPanel.UpdateUserList(CurrentMpHandler.Users);
        }

        /// <summary>
        /// Removes the label with the client's connection information, the chat-button and the team list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void CurrentMpHandler_ClosedConnection(object sender, EventArgs e)
        {
            this.BeginInvoke((MethodInvoker)(() =>
            {
                currentGame.IsMultiplayer = false;
                Cursor = Cursors.Default;

                informationStatusStrip.Items.Clear();
                informationPanel.GeneralTlp.Controls.RemoveByKey("TeamList");
                informationStatusStrip.Items.RemoveByKey("ChatButton");
            }));
        }

        /// <summary>
        /// Adds a label with the client's connection information to the status strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void CurrentMpHandler_LoginSucceeded(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                AddConnectionInformationLabel();

                //Opens the MultiplayerLobbyForm and joins the lobby
                CurrentMpHandler.JoinLobby(true);
            }));
        }

        /// <summary>
        /// Adds an "Open"-button to the status strip to that the server can open the chat again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentChatForm_ChatClosed(object sender, EventArgs e)
        {
            AddOpenChatButton();
        }

        /// <summary>
        /// Sets the Cursor to the WaitCursor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WaitingForHostChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                fieldGrid.Invoke((MethodInvoker)(() =>
                {
                    Cursor = Cursors.WaitCursor;
                }));
            }
        }
        #endregion

        #region MenuBar events
        /// <summary>
        /// Opens a new FieldSettingsPanel where the user can create a new singleplayer game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void CreateAGame_Clicked(object sender, EventArgs e)
        {
            if (replayManager != null)
            {
                RemoveReplayManagerComponents();
            }

            bool openForm = false;
            Boolean? disconnect = AskForDisconnect();
            if (disconnect.HasValue)
            {
                if (disconnect.Value)
                {
                    //close the connection to the multiplayer session
                    currentGame.IsMultiplayer = false;
                    Cursor = Cursors.Default;
                    CurrentMpHandler.CloseConnection();

                    openForm = true;
                }
            }
            else
            {
                openForm = true;
            }

            if (openForm)
            {
                SingleplayerSettingsForm spSettings = new SingleplayerSettingsForm();
                spSettings.GameCreated += SingleplayerGameCreated;
            }
        }

        /// <summary>
        /// Opens a new MultiplayerHnadler and asks for an ip-address and a Port.
        /// If the user is connected to a server currently, the program will ask the player whether he wants
        /// to disconnect from the server.
        /// </summary>
        /// <param name="sender">The menu item "Join a game"</param>
        /// <param name="message"></param>
        private void JoinAGame_Clicked(object sender, EventArgs e)
        {
            if (replayManager != null)
            {
                RemoveReplayManagerComponents();
            }

            bool openNewConnection = true;
            Boolean? disconnect = AskForDisconnect();
            if (disconnect.HasValue)
            {
                if (disconnect.Value)
                {
                    currentGame.IsMultiplayer = false;
                    Cursor = Cursors.Default;
                    CurrentMpHandler.CloseConnection();
                }
                else
                {
                    openNewConnection = false;
                }
            }

            if (openNewConnection)
            {
                CurrentMpHandler = new MultiplayerHandler();
                CurrentMpHandler.LoginSucceeded += CurrentMpHandler_LoginSucceeded;
                CurrentMpHandler.ConnectionClosed += CurrentMpHandler_ClosedConnection;
                CurrentMpHandler.GameCreated += MultiplayerGameCreated;
                CurrentMpHandler.GameContinued += GameContinued;
                CurrentMpHandler.GameEnded += GameEnded;
                CurrentMpHandler.OpenConnectionForm();
            }
        }

        /// <summary>
        /// Opens a new OpenFileDialog so that the user can load a xml/save file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadAGame_Clicked(object sender, EventArgs e)
        {
            bool loadAGame = true;
            Boolean? disconnect = AskForDisconnect();
            if (disconnect.HasValue)
            {
                if (disconnect.Value)
                {
                    currentGame.IsMultiplayer = false;
                    Cursor = Cursors.Default;
                    CurrentMpHandler.CloseConnection();
                }
                else
                {
                    loadAGame = false;
                }
            }

            if (loadAGame)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.Filter = "FSG-Files (*.fsg) | *.fsg";
                fileDialog.CheckFileExists = true;
                DialogResult result = fileDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (replayManager != null)
                    {
                        RemoveReplayManagerComponents();
                        replayManager.JumpTimer.Stop();
                    }

                    string path = fileDialog.InitialDirectory + fileDialog.FileName;
                    StartGameWithReplayManager(path);
                }
            }
        }

        /// <summary>
        /// Raises ChooseFileAndSave()-method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAGame_Clicked(object sender, EventArgs e)
        {
            ChooseFileAndSave();
        }

        /// <summary>
        /// Closes the program as soon as the user clickes the exit menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Exit_Clicked(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Game events
        /// <summary>
        /// Updates the goals count of the teams.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GoalsChanged(object sender, EventArgs e)
        {
            UpdateGoalsLabels();
        }

        /// <summary>
        /// Updates the labels which contains the amount of the goals.
        /// </summary>
        private void UpdateGoalsLabels()
        {
            if (informationPanel.IsHandleCreated)
            {
                informationPanel.BeginInvoke((MethodInvoker)(() =>
                {
                    informationPanel.FirstTeamGoalsLabel.Text = "First Team: " + currentGame.FirstTeam.GoalsCount;
                    //informationPanel.FirstTeamGoalsLabel.ForeColor = currentGame.FirstTeam.TeamColor;
                    informationPanel.SecondTeamGoalsLabel.Text = "Second Team: " + currentGame.SecondTeam.GoalsCount;
                    //informationPanel.SecondTeamGoalsLabel.ForeColor = currentGame.SecondTeam.TeamColor;
                }));
            }
        }

        /// <summary>
        /// Updates the label with the current round.
        /// Updates the label of the replay manager too.
        /// </summary>
        private void UpdateRoundLabel()
        {
            informationPanel.StepLabel.Text = "Round: " + currentGame.Round;

            if (replayManager != null && replayManager.IsHandleCreated)
            {
                replayManager.Round = currentGame.Round;
            }
        }

        /// <summary>
        /// Updates the leftSeconds label.
        /// Uses an infinite sign if the number of the left seconds is below 0.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LeftSecondsChanged(object sender, EventArgs e)
        {
            if (!currentGame.IsStopped && !informationPanel.IsDisposed && informationPanel.Visible && this.IsHandleCreated)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    DateTime time;
                    if (currentGame.LeftSeconds < 0 || currentGame.LeftSeconds >= 3600)
                    {
                        informationPanel.StepTimeLabel.Text = "Next round in: ∞";
                    }
                    else
                    {
                        if (currentGame.LeftSeconds > 59)
                        {
                            time = DateTime.Parse("00:" + currentGame.LeftSeconds / 60 + ":" + currentGame.LeftSeconds % 60);
                        }
                        else
                        {
                            time = DateTime.Parse("00:00:" + currentGame.LeftSeconds);
                        }
                        informationPanel.StepTimeLabel.Text = "Next round in: " + time.ToString("mm:ss");
                    }
                    informationPanel.Update();
                }));
            }
        }

        /// <summary>
        /// Updates the roundLabel with the current round number.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RoundChanged(object sender, EventArgs e)
        {
            if (!currentGame.IsStopped)
            {
                this.BeginInvoke((MethodInvoker)(() =>
                {
                    UpdateRoundLabel();
                }));
            }
        }

        /// <summary>
        /// Refreshes the FieldGrid-panel, sets the Cursor to the default Cursor the waitsForHost-boolean to false.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CoordinatesChanged(object sender, EventArgs e)
        {
            if (!currentGame.IsStopped && this.IsHandleCreated)
            {
                fieldGrid.BeginInvoke((MethodInvoker)(() =>
                {
                    waitsForHost = false;
                    Cursor = Cursors.Default;
                    fieldGrid.Refresh();
                }));
            }
        }

        /// <summary>
        /// Raises ShowWinnerMessage()-method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GameEnded(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                //inform the other players
                if (currentGame.IsMultiplayer && currentGame.IsHost)
                {
                    CurrentMpHandler.CurrentClient.Send("end<>:");
                }

                ShowWinnerMessage();

                if (currentGame.IsMultiplayer)
                {
                    Cursor = Cursors.Default;
                    informationPanel.GeneralTlp.Controls.RemoveByKey("TeamList");
                    informationStatusStrip.Items.RemoveByKey("ChatButton");

                    CurrentMpHandler.JoinLobby(false);
                }
                else
                {
                    CreateNewGame(currentGame.Settings, false, null);
                }
            }));
        }

        /// <summary>
        /// Shows the winner of the game in a MessageBox and starts a new game.
        /// The user has the chance to save the current game in a replay.
        /// </summary>
        private void ShowWinnerMessage()
        {
            string winnerString = "";
            string whatWillHappenString = "";
            if (currentGame.FirstTeam.GoalsCount != currentGame.SecondTeam.GoalsCount)
            {
                winnerString = ((currentGame.FirstTeam.GoalsCount > currentGame.SecondTeam.GoalsCount) ? currentGame.FirstTeam.Name : currentGame.SecondTeam.Name) +
                    " won the game. Congratulations!";
            }
            else
            {
                winnerString = "Draw.";
            }

            whatWillHappenString = (currentGame.IsMultiplayer) ? "\nYou will go back to the lobby." : "\nA new game will start automatically.";

            DialogResult result = MessageBox.Show(winnerString + whatWillHappenString + "\n\nWould you like to save the replay?", "Winner", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                ChooseFileAndSave();
            }
        }
        #endregion

        #region UI events
        /// <summary>
        /// Singleplayer: Sets the LeftSeconds of the current game to -1, which will raise the Game.NextStep-Method as soon as the user clicks spacebar.
        /// Multiplayer: Sends the server the information that the user is ready.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void fieldGrid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                //isChooseMode = false;
                if (currentGame.IsMultiplayer && !waitsForHost)
                {
                    waitsForHost = true;
                    CurrentMpHandler.CurrentClient.Send("ready<>:");
                    Cursor = Cursors.WaitCursor;
                }
                else
                {
                    if (replayManager != null && replayManager.IsHandleCreated)
                    {
                        RemoveReplayManagerComponents();
                    }
                    currentGame.LeftSeconds = -1;
                }
            }
        }

        /// <summary>
        /// Checks whether the mouse entered or leaved a rectangle and marks the rectangle then.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void fieldGrid_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.Location;
            Point gridLocation = SearchGridIndex(mousePoint);
            if (gridLocation.X >= 0 && gridLocation.X < fieldCell.GetLength(0) && gridLocation.Y >= 0 && gridLocation.Y < fieldCell.GetLength(1))
            {
                Rectangle rectangle = fieldCell[gridLocation.X, gridLocation.Y];

                if (rectangle.Contains(mousePoint))
                {
                    if (!rectangle.Equals(currentMouseRectangle))
                    {
                        //Console.WriteLine("GridLocation: " + gridLocation);

                        Rectangle invalidateRectangle = new Rectangle(currentMouseRectangle.Location, new Size(currentMouseRectangle.Width + 1, currentMouseRectangle.Height + 1));
                        fieldGrid.Invalidate(invalidateRectangle);
                        currentMouseRectangle = rectangle;
                        invalidateRectangle = new Rectangle(rectangle.Location, new Size(rectangle.Width + 1, rectangle.Height + 1));
                        fieldGrid.Invalidate(invalidateRectangle);

                        if (isChooseMode && !currentGame.IsCalculating)
                        {
                            currentGame.AddCacheAction(chosenPlayer, gridLocation);
                            directionLinesPanel.Invalidate();
                        }
                        else if (!waitsForHost)
                        {
                            if (currentGame.IsPlayerOnCell(gridLocation))
                            {
                                Cursor = Cursors.Hand;
                            }
                            else
                            {
                                Cursor = Cursors.Default;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the chosenPlayer to the currently selected player and starts the choose mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void fieldGrid_MouseDown(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.Location;
            if (!(currentGame.IsMultiplayer && waitsForHost))
            {
                if (currentMouseRectangle.Contains(mousePoint))
                {
                    int x = -1;
                    int y = -1;
                    SearchCurrentRectangleIndex(ref x, ref y);
                    if (x >= 0 && y >= 0)
                    {
                        Player rectanglePlayer = SearchRectanglePlayer(x, y);
                        if (rectanglePlayer != null)
                        {
                            chosenPlayer = rectanglePlayer;
                            //Console.WriteLine("Player \nPosition: " + chosenPlayer.Position + "\nHasBall: " + chosenPlayer.HasBall + "\nNeedsIntelligence: " + chosenPlayer.NeedsIntelligence + "\n\nBall \nHasPlayerContact: " + currentGame.GameBall.HasPlayerContact);
                            isChooseMode = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Opens the action dialog if the user is in the choose mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void fieldGrid_MouseUp(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.Location;
            if (currentMouseRectangle.Contains(mousePoint) && isChooseMode)
            {
                int x = -1;
                int y = -1;
                SearchCurrentRectangleIndex(ref x, ref y);
                if (x >= 0 && y >= 0)
                {
                    Player rectanglePlayer = SearchRectanglePlayer(x, y);
                    directionLinesPanel.Refresh();

                    int dialogXPos = Location.X + ClientRectangle.Location.X + stadiumPanel.Location.X + directionLinesPanel.Location.X + fieldCell[x, y].Location.X + fieldCell[x, y].Size.Width + 5;
                    int dialogYPos = Location.Y + ClientRectangle.Location.Y + stadiumPanel.Location.Y + directionLinesPanel.Location.Y + fieldCell[x, y].Location.Y + fieldCell[x, y].Size.Height * 2 + 10;
                    if (dialogXPos + 200 > Location.X + Size.Width)
                    {
                        dialogXPos -= 180;
                    }
                    currentGame.OpenActionDialog(chosenPlayer, new PlayerAction(rectanglePlayer, new Point(x, y), ActionType.Nothing), dialogXPos, dialogYPos);
                    directionLinesPanel.Refresh();
                    chosenPlayer = null;
                    isChooseMode = false;
                }
            }
        }

        /// <summary>
        /// Stops the round-timer and closes the connection to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FieldForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            currentGame.IsStopped = true;
            currentGame.RoundTimer.Stop();
            if (CurrentMpHandler != null)
            {
                CurrentMpHandler.CloseConnection();
            }
        }

        /// <summary>
        /// Refreshes the field as soon as the user changes the setting how the direction lines should be drawed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void informationPanel_DirectionLineSettingsChanged(object sender, EventArgs e)
        {
            directionLinesPanel.Invalidate();
            fieldGrid.Focus();
        }

        /// <summary>
        /// Searches the fieldCell's index of currentRectangle.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void SearchCurrentRectangleIndex(ref int x, ref int y)
        {
            for (int r = 0; r < fieldCell.GetLength(1); r++)
            {
                for (int c = 0; c < fieldCell.GetLength(0); c++)
                {
                    if (fieldCell[c, r].Equals(currentMouseRectangle))
                    {
                        x = c;
                        y = r;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Searches the cell's location in the grid at a specific position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Point SearchGridIndex(Point position)
        {
            int height = fieldCell[0, 0].Height;
            int width = fieldCell[0, 0].Width;

            return new Point(position.X / width, position.Y / height);
        }

        /// <summary>
        /// Returns the on this rectangle standing player.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Player SearchRectanglePlayer(int x, int y)
        {
            Player chosenPlayer = null;

            List<Player> allPlayers = new List<Player>();
            allPlayers.AddRange(currentGame.FirstTeam.Players);
            allPlayers.AddRange(currentGame.SecondTeam.Players);
            foreach (Player player in allPlayers)
            {
                if (player.XCoordinate == x && player.YCoordinate == y)
                {
                    chosenPlayer = player;
                }
            }

            return chosenPlayer;
        }
        #endregion

        #region StatusStrip
        /// <summary>
        /// Adds a button to the status strip, which will reopen the replay manager.
        /// </summary>
        private void AddReplayButton()
        {
            ToolStripButton replayButton = new ToolStripButton();
            replayButton.Text = "Replay Manager";
            replayButton.Name = "Replay Manager";
            replayButton.Click +=replayButton_Click;

            informationStatusStrip.Items.Insert(0, replayButton);
        }

        /// <summary>
        /// Shows the replay manager and removes the button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void replayButton_Click(object sender, EventArgs e)
        {
            ToolStripButton button = (ToolStripButton)sender;
            informationStatusStrip.Items.Remove(button);
            replayManager.Show();
            replayManager.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Adds a label to the statusstripbar, where the user can see the ip-address as well as the port of the server.
        /// </summary>
        private void AddConnectionInformationLabel()
        {
            if (!IsSpaceLabelThere())
            {
                AddSpaceLabel();
            }
            //delete old connection label
            informationStatusStrip.Items.RemoveByKey("Connection");
            Label connectionLabel = new Label() { Text = "Server: " + CurrentMpHandler.CurrentClient.ServerAddress + ":" + CurrentMpHandler.CurrentClient.Port };
            connectionLabel.AutoSize = true;
            informationStatusStrip.Items.Add(new ToolStripControlHost(connectionLabel) { Name = "Connection" });
        }

        /// <summary>
        /// Adds the OpenChatButton to the status strip.
        /// </summary>
        private void AddOpenChatButton()
        {
            ToolStripButton openButton = new ToolStripButton();
            openButton.Text = "Open Chat";
            openButton.Size = new Size(60, 20);
            openButton.Name = "ChatButton";
            openButton.Anchor = AnchorStyles.None;
            openButton.Click += (s, ea) =>
            {
                ToolStripItem[] openButtonItem = informationStatusStrip.Items.Find("ChatButton", true);
                if (openButtonItem.Length == 1)
                {
                    informationStatusStrip.Items.Remove(openButtonItem.ElementAt(0));
                }

                CurrentMpHandler.CurrentChatForm.Show();
                CurrentMpHandler.CurrentChatForm.WindowState = FormWindowState.Normal;
            };

            informationStatusStrip.Items.Insert(0, openButton);
        }

        /// <summary>
        /// Adds a ToolStripLabel to the status strip which will fill all of the available space.
        /// </summary>
        private void AddSpaceLabel()
        {
            ToolStripStatusLabel space = new ToolStripStatusLabel();
            space.Text = string.Empty;
            space.Spring = true;
            space.Name = "Space";
            informationStatusStrip.Items.Add(space);
        }

        private bool IsSpaceLabelThere()
        {
            ToolStripItem[] spaceItems = informationStatusStrip.Items.Find("Space", true);
            return (spaceItems.Length >= 1);
        }
        #endregion

        #region ReplayManager

        /// <summary>
        /// Let the user choose a file for the savegame through an SaveFileDiloag and saves the current game.
        /// </summary>
        private void ChooseFileAndSave()
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.DefaultExt = "xml";
            fileDialog.Filter = "Football Savegame File (*.fsg) | *.fsg";

            DialogResult result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string path = fileDialog.InitialDirectory + fileDialog.FileName;
                currentGame.CurrentReplayManager.Save(path);
            }
        }

        /// <summary>
        /// Loads the savegame at the path and opens the replay manager.
        /// </summary>
        /// <param name="path"></param>
        private void StartGameWithReplayManager(string path)
        {
            ReplayManager rm = new ReplayManager(path);
            rm.Load();

            //creates a new game if the game image has been loaded
            if (rm.Image != null)
            {
                CreateNewGame(rm.Image.Settings, false, rm);

                replayManager = new ReplayManagerForm(rm.SearchLastRoundNumber());
                replayManager.Round = currentGame.Round;
                currentGame.LeftSeconds = -2;
                InitializeManagerEvents();
            }
        }

        /// <summary>
        /// Initializes all replay manager events.
        /// </summary>
        private void InitializeManagerEvents()
        {
            replayManager.ManagerClosed += (o, ea) =>
            {
                AddReplayButton();
            };

            replayManager.RoundJump += (o, ev) =>
            {
                currentGame.JumpToRound(ev.TargetRound);
            };

            replayManager.NextRound += (o, nr) =>
            {
                currentGame.NextRound();
            };

            replayManager.PreviousRound += (o, pr) =>
            {
                currentGame.PreviousRound();
            };
        }

        /// <summary>
        /// Removes all replay manager components (the form and the button in the status strip).
        /// </summary>
        private void RemoveReplayManagerComponents()
        {
            ToolStripItem[] items = informationStatusStrip.Items.Find("Replay Manager", true);
            if (items.Length > 0)
            {
                foreach (ToolStripItem item in items)
                {
                    informationStatusStrip.Items.Remove(item);
                }
            }

            if (replayManager != null)
            {
                replayManager.ManualClose = true;
                replayManager.JumpTimer.Stop();
                replayManager.Close();
            }
        
        }
        #endregion
    }
}