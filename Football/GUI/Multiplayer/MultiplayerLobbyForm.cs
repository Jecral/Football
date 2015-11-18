using Football.EventArguments;
using Football.GUI.General;
using Football.GUI.Other;
using Football.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Multiplayer
{
    class MultiplayerLobbyForm : Form
    {
        public MultiplayerLobbyForm(TabControl currentChats)
        {
            Icon = Properties.Resources.Icon;
            CurrentChats = currentChats;
            Text = "Multiplayer Lobby";
            Size = new Size(810, 700);
            MaximumSize = new Size(810, 700);
            MinimumSize = new Size(810, 700);
            StartPosition = FormStartPosition.CenterScreen;
            InitializeComponents();
            InitializeToolTips();
            InitializeEvents();
            Show();

            created = true;
            gameSettingsPanel.IsCreated = true;
            manualIndexChange = false;

            CurrentFieldSettingsPanel.Focus();
        }

        public TabControl CurrentChats { get; set; }
        public EditableFieldPanel CurrentFieldSettingsPanel { get; set; }

        public bool ForceClose;
        private bool created;
        private bool manualIndexChange;
        public bool ForceCloseWithoutEvent { get; set; }

        private Button startButton;
        public CheckBox UseSavefileBox { get; set; }
        private GameSettingsPanel gameSettingsPanel;

        public event EventHandler LeftLobby;
        public event EventHandler ShowTeamsClicked;
        public event EventHandler<FieldSettingsEventArgs> GameSettingsChanged;
        public event EventHandler StartGameRequest;
        public event EventHandler SaveFileUploadRequest;

        #region Initialisation
        /// <summary>
        /// Initializes all components
        /// </summary>
        private void InitializeComponents()
        {
            BackgroundImage = Properties.Resources.MultiplayerLobby;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            generalTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            generalTlp.Dock = DockStyle.Fill;
            generalTlp.BackColor = Color.Transparent;

            TableLayoutPanel settingsTlp = new TableLayoutPanel();
            settingsTlp.AutoSize = true;
            settingsTlp.Anchor = AnchorStyles.None;
            settingsTlp.BackColor = Color.Transparent;

            CurrentFieldSettingsPanel = new EditableFieldPanel();
            CurrentFieldSettingsPanel.Size = new Size(556, 326);
            CurrentFieldSettingsPanel.Anchor = AnchorStyles.None;
            CurrentFieldSettingsPanel.InitializeComponents(51, 25);
            CurrentFieldSettingsPanel.BorderStyle = BorderStyle.FixedSingle;
            CurrentFieldSettingsPanel.FieldSettingsChanged += FieldSettingsChanged;

            TableLayoutPanel roundSettingsTlp = new TableLayoutPanel();
            roundSettingsTlp.AutoSize = true;
            roundSettingsTlp.Dock = DockStyle.Right;
            roundSettingsTlp.Anchor = AnchorStyles.None;
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            #region Game Settings
            gameSettingsPanel = new GameSettingsPanel(true);
            gameSettingsPanel.Dock = DockStyle.Right;
            gameSettingsPanel.AutoSize = true;
            gameSettingsPanel.GameSettingsChanged += SelectedIndexChanged;
            #endregion

            #region change team and start button flp
            FlowLayoutPanel teamAndStartFlp = new FlowLayoutPanel();
            teamAndStartFlp.AutoSize = true;
            teamAndStartFlp.Anchor = AnchorStyles.None;

            Button openTeamSwitcher = new Button();
            openTeamSwitcher.Text = "Show Teams";
            openTeamSwitcher.Size = new Size(110, 25);
            openTeamSwitcher.Click += openTeamSwitcher_Click;
            openTeamSwitcher.Anchor = AnchorStyles.None;
            openTeamSwitcher.BackColor = Control.DefaultBackColor;
            openTeamSwitcher.Cursor = Cursors.Hand;

            startButton = new Button();
            startButton.Text = "Start";
            startButton.Size = new Size(50, 25);
            startButton.BackColor = Control.DefaultBackColor;
            startButton.Anchor = AnchorStyles.None;
            startButton.Click += startButton_Click;
            startButton.Cursor = Cursors.Hand;

            teamAndStartFlp.Controls.Add(openTeamSwitcher);
            teamAndStartFlp.Controls.Add(startButton);
            #endregion

            #region upload round flp
            FlowLayoutPanel savefileFlp = new FlowLayoutPanel();
            savefileFlp.AutoSize = true;
            savefileFlp.Anchor = AnchorStyles.None;

            Button savefileButton = new Button();
            savefileButton.Text = "Upload Savegame";
            savefileButton.BackColor = Control.DefaultBackColor;
            savefileButton.Anchor = AnchorStyles.None;
            savefileButton.Click += uploadButton_Click;
            savefileButton.Cursor = Cursors.Hand;
            savefileButton.Size = new Size(110, 25);

            UseSavefileBox = new CheckBox();
            UseSavefileBox.Text = "Use";
            UseSavefileBox.Size = new Size(50, 25);
            UseSavefileBox.ForeColor = Color.White;
            UseSavefileBox.Anchor = AnchorStyles.None;

            savefileFlp.Controls.Add(savefileButton);
            savefileFlp.Controls.Add(UseSavefileBox);

            #endregion

            roundSettingsTlp.Controls.Add(gameSettingsPanel, 0, 0);
            roundSettingsTlp.Controls.Add(savefileFlp, 0, 1);
            roundSettingsTlp.Controls.Add(teamAndStartFlp, 0, 2);

            settingsTlp.Controls.Add(CurrentFieldSettingsPanel, 0, 0);
            settingsTlp.Controls.Add(roundSettingsTlp, 1, 0);

            CurrentChats.Size = new Size(400, 300);
            CurrentChats.Location = new Point(Width / 2 - CurrentChats.Width / 2, Height - (CurrentChats.Height + 50));
            CurrentChats.Anchor = AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            CurrentChats.SelectedIndexChanged += CurrentChats_SelectedIndexChanged;

            generalTlp.Controls.Add(settingsTlp, 0, 0);
            generalTlp.Controls.Add(CurrentChats, 0, 1);

            Controls.Add(generalTlp);
        }

        private void InitializeToolTips()
        {
            ToolTip toolTip = new ToolTip();
            toolTip.UseAnimation = true;
            toolTip.SetToolTip(UseSavefileBox, "Use the uploaded savegame.");
            toolTip.SetToolTip(startButton, "Start the game.");
        }

        void space_Paint(object sender, PaintEventArgs e)
        {
            Panel roundButtonsFlp = (Panel)sender;
            ControlPaint.DrawBorder(e.Graphics, roundButtonsFlp.ClientRectangle,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.White, 1, ButtonBorderStyle.Solid,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.White, 1, ButtonBorderStyle.Solid);
        }
        #endregion

        /// <summary>
        /// Opens an OpenFileDialog so that the user can choose a savefile.
        /// The client will send the last round image of the savefile to the server so that the game will start with this situation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void uploadButton_Click(object sender, EventArgs e)
        {
            SaveFileUploadRequest(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the StartGameRequest()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void startButton_Click(object sender, EventArgs e)
        {
            StartGameRequest(this, EventArgs.Empty);
        }

        /// <summary>
        /// If an index of one of the Settings-comboboxes changed and if that happened not manually (by the user),
        /// collect the current GameSettings and sends it to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!manualIndexChange && Visible && created && !string.IsNullOrWhiteSpace(gameSettingsPanel.GameRoundsBox.Text) && !string.IsNullOrWhiteSpace(gameSettingsPanel.RoundSecondsBox.Text))
            {
                GameSettings settings = CurrentFieldSettingsPanel.CurrentSettings;
                settings.RoundsPerHalf = Int32.Parse(gameSettingsPanel.GameRoundsBox.Text);
                settings.SecondsPerRound = Int32.Parse(gameSettingsPanel.RoundSecondsBox.Text);
                settings.FirstTeamUsesKI = gameSettingsPanel.FirstKiBox.SelectedIndex == 1;
                settings.SecondTeamUsesKI = gameSettingsPanel.SecondKiBox.SelectedIndex == 1;

                RaiseSettingsEvent(settings);
            }
        }

        /// <summary>
        /// Creates the current game Settings and raises the RaiseSettingsEvent()-method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FieldSettingsChanged(object sender, FieldSettingsEventArgs e)
        {
            GameSettings settings = e.Settings;
            settings.RoundsPerHalf = Int32.Parse(gameSettingsPanel.GameRoundsBox.Text);
            settings.SecondsPerRound = Int32.Parse(gameSettingsPanel.RoundSecondsBox.Text);
            settings.FirstTeamUsesKI = gameSettingsPanel.FirstKiBox.SelectedIndex == 1;
            settings.SecondTeamUsesKI = gameSettingsPanel.SecondKiBox.SelectedIndex == 1;

            RaiseSettingsEvent(settings);
        }

        /// <summary>
        /// Raises the GameSettingsChanged()-event with the Settings in the parameter as the argument.
        /// </summary>
        /// <param name="Settings"></param>
        private void RaiseSettingsEvent(GameSettings settings)
        {
            FieldSettingsEventArgs args = new FieldSettingsEventArgs(settings);
            GameSettingsChanged(this, args);
        }

        /// <summary>
        /// Initializes the FormClosing()-event
        /// </summary>
        private void InitializeEvents()
        {
            FormClosing += MultiplayerLobbyForm_FormClosing;
        }

        /// <summary>
        /// Updates the rounds per second and rounds per game Settings which are saved in the game Settings.
        /// </summary>
        /// <param name="Settings"></param>
        public void UpdateSettings(GameSettings settings)
        {
            this.Invoke((MethodInvoker)(() =>
            {
                manualIndexChange = true;

                gameSettingsPanel.UpdateSettings(settings);
                CurrentFieldSettingsPanel.UpdateSettings(settings);

                manualIndexChange = false;
            }));
        }

        /// <summary>
        /// Raises the ShowTeamsClicked()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void openTeamSwitcher_Click(object sender, EventArgs e)
        {
            ShowTeamsClicked(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the SetChatsNotVisible()-method which will set all others ChatTabPages' IsVisible-bool to false.
        /// Sets the currently selected ChatTabPage's IsVisible-bool to true.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentChats_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChatTabPage chatPage = (ChatTabPage)CurrentChats.SelectedTab;
            chatPage.StopTitleBlinker = true;
            SetChatsNotVisible();
            chatPage.IsVisible = true;
        }

        /// <summary>
        /// Sets all ChatTagPage's IsVisible-bool in CurrentChats to false.
        /// </summary>
        private void SetChatsNotVisible()
        {
            foreach (TabPage page in CurrentChats.TabPages)
            {
                ChatTabPage chat = (ChatTabPage)page;
                chat.IsVisible = false;
            }
        }

        void MultiplayerLobbyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ForceClose)
            {
                MessageBoxButtons button = MessageBoxButtons.YesNoCancel;
                MessageBoxIcon icon = MessageBoxIcon.Warning;

                DialogResult rs = MessageBox.Show("You are leaving the server. Continue?", "Warning", button, icon);
                if (rs == System.Windows.Forms.DialogResult.No || rs == System.Windows.Forms.DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else
                {
                    SetChatsNotVisible();
                    LeftLobby(null, EventArgs.Empty);
                }
            }
        }
    }
}
