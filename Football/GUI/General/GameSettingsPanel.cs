using Football.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.General
{
    class GameSettingsPanel : Panel
    {
        public GameSettingsPanel(bool isMultiplayer)
        {
            this.isMultiplayer = isMultiplayer;
            InitializeComponents();
        }

        public ComboBox RoundSecondsBox { get; private set; }
        public ComboBox GameRoundsBox { get; private set; }
        public ComboBox FirstKiBox { get; private set; }
        public ComboBox SecondKiBox { get; private set; }

        private bool isMultiplayer;
        public bool IsCreated { get; set; }
        public bool ManualIndexChange { get; set; }
        public bool ForceCloseWithoutEvent { get; set; }

        public event EventHandler GameSettingsChanged;

        public void InitializeComponents()
        {
            TableLayoutPanel roundSettingsTlp = new TableLayoutPanel();
            roundSettingsTlp.AutoSize = true;
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            roundSettingsTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            #region Seconds and rounds
            TableLayoutPanel secondsAndRoundsTlp = new TableLayoutPanel();
            secondsAndRoundsTlp.Size = new Size(180, 60);
            secondsAndRoundsTlp.Dock = DockStyle.Right;

            Label secondsLabel = new Label();
            secondsLabel.Text = "Seconds per Round:";
            secondsLabel.AutoSize = true;
            secondsLabel.Anchor = AnchorStyles.None;
            secondsLabel.ForeColor = Color.White;

            RoundSecondsBox = new ComboBox();
            RoundSecondsBox.Size = new Size(50, 30);
            RoundSecondsBox.Anchor = AnchorStyles.None;
            RoundSecondsBox.SelectedIndexChanged += SelectedIndexChanged;
            RoundSecondsBox.DropDownStyle = ComboBoxStyle.DropDownList;
            for (int i = 1; i < 81; i++)
            {
                RoundSecondsBox.Items.Add("" + ((i >= 61) ? ((i - 60) * 10) + 60 : i));
            }
            RoundSecondsBox.SelectedIndex = 6;

            Label roundsLabel = new Label();
            roundsLabel.Text = "Rounds per Half:";
            roundsLabel.AutoSize = true;
            roundsLabel.Anchor = AnchorStyles.None;
            roundsLabel.ForeColor = Color.White;

            GameRoundsBox = new ComboBox();
            GameRoundsBox.Anchor = AnchorStyles.None;
            GameRoundsBox.Size = new Size(50, 30);
            GameRoundsBox.SelectedIndexChanged += SelectedIndexChanged;
            GameRoundsBox.DropDownStyle = ComboBoxStyle.DropDownList;
            GameRoundsBox.Items.Add("5");
            for (int i = 5; i < 25; i++)
            {
                GameRoundsBox.Items.Add("" + i * 10);
            }
            GameRoundsBox.SelectedIndex = 5;

            secondsAndRoundsTlp.Controls.Add(secondsLabel, 0, 0);
            secondsAndRoundsTlp.Controls.Add(RoundSecondsBox, 1, 0);
            secondsAndRoundsTlp.Controls.Add(roundsLabel, 0, 1);
            secondsAndRoundsTlp.Controls.Add(GameRoundsBox, 1, 1);
            #endregion

            #region first KI box
            TableLayoutPanel firstTeamTlp = new TableLayoutPanel();
            firstTeamTlp.Dock = DockStyle.Fill;
            firstTeamTlp.Anchor = AnchorStyles.None;
            firstTeamTlp.Size = new Size(180, 50);

            Label firstKiLabel = new Label();
            firstKiLabel.Text = "Team Yellow";
            firstKiLabel.Anchor = AnchorStyles.None;
            firstKiLabel.Font = new Font("Arial", 9f, FontStyle.Bold);
            firstKiLabel.AutoSize = true;
            firstKiLabel.ForeColor = Color.LightGoldenrodYellow;

            FirstKiBox = new ComboBox();
            FirstKiBox.Anchor = AnchorStyles.None;
            FirstKiBox.Size = new Size(140, 30);
            FirstKiBox.Items.AddRange(new string[] { "Controlled by the user", "Controlled by user + KI" });
            FirstKiBox.SelectedIndex = 1;
            FirstKiBox.SelectedIndexChanged += SelectedIndexChanged;
            FirstKiBox.DropDownStyle = ComboBoxStyle.DropDownList;

            firstTeamTlp.Controls.Add(firstKiLabel, 0, 0);
            firstTeamTlp.Controls.Add(FirstKiBox, 0, 1);
            #endregion

            #region second KI Box
            TableLayoutPanel secondTeamTlp = new TableLayoutPanel();
            secondTeamTlp.Dock = DockStyle.Fill;
            secondTeamTlp.Anchor = AnchorStyles.None;
            secondTeamTlp.Size = new Size(180, 50);

            Label secondKiLabel = new Label();
            secondKiLabel.Text = "Team Red";
            secondKiLabel.Anchor = AnchorStyles.None;
            secondKiLabel.Font = new Font("Arial", 9f, FontStyle.Bold);
            secondKiLabel.AutoSize = true;
            secondKiLabel.ForeColor = Color.Red;

            SecondKiBox = new ComboBox();
            SecondKiBox.Anchor = AnchorStyles.None;
            SecondKiBox.Size = new Size(140, 30);
            SecondKiBox.Items.AddRange(new string[] { "Controlled by the user", "Controlled by user + KI" });
            SecondKiBox.SelectedIndex = 1;
            SecondKiBox.SelectedIndexChanged += SelectedIndexChanged;
            SecondKiBox.DropDownStyle = ComboBoxStyle.DropDownList;

            secondTeamTlp.Controls.Add(secondKiLabel, 0, 0);
            secondTeamTlp.Controls.Add(SecondKiBox, 0, 1);
            #endregion

            #region space panel
            Panel spacePanel = new Panel();
            spacePanel.Dock = DockStyle.Fill;
            spacePanel.Paint += SpacePanel_Paint;

            Panel secondSpacePanel = new Panel();
            secondSpacePanel.Dock = DockStyle.Fill;
            secondSpacePanel.Paint += SpacePanel_Paint;
            #endregion

            roundSettingsTlp.Controls.Add(secondsAndRoundsTlp, 0, 0);
            roundSettingsTlp.Controls.Add(spacePanel, 0, 1);
            roundSettingsTlp.Controls.Add(firstTeamTlp, 0, 2);
            roundSettingsTlp.Controls.Add(secondTeamTlp, 0, 3);
            roundSettingsTlp.Controls.Add(secondSpacePanel, 0, 4);

            Controls.Add(roundSettingsTlp);
        }

        void SpacePanel_Paint(object sender, PaintEventArgs e)
        {
            Panel roundButtonsFlp = (Panel)sender;
            ControlPaint.DrawBorder(e.Graphics, roundButtonsFlp.ClientRectangle,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.White, 1, ButtonBorderStyle.Solid,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.White, 1, ButtonBorderStyle.Solid);
        }

        /// <summary>
        /// If an index of one of the Settings-comboboxes changed and if that happened not manually (by the user),
        /// collect the current GameSettings and sends it to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isMultiplayer && !ManualIndexChange && Visible && IsCreated && !string.IsNullOrWhiteSpace(GameRoundsBox.Text) && !string.IsNullOrWhiteSpace(RoundSecondsBox.Text))
            {
                GameSettingsChanged(this, EventArgs.Empty);
            }
        }

        public void UpdateSettings(GameSettings settings)
        {
            ManualIndexChange = true;

            int secondsIndex = RoundSecondsBox.FindString(settings.SecondsPerRound + "");
            if (secondsIndex != -1)
            {
                RoundSecondsBox.SelectedIndex = secondsIndex;
            }
            else
            {
                RoundSecondsBox.Items.Add(settings.SecondsPerRound);
                RoundSecondsBox.Text = settings.SecondsPerRound + "";
            }

            int roundIndex = GameRoundsBox.FindString(settings.RoundsPerHalf + "");
            if (roundIndex != -1)
            {
                GameRoundsBox.SelectedIndex = roundIndex;
            }
            else
            {
                GameRoundsBox.Items.Add(settings.RoundsPerHalf);
                GameRoundsBox.Text = settings.RoundsPerHalf + "";
            }

            FirstKiBox.SelectedIndex = (settings.FirstTeamUsesKI) ? 1 : 0;
            SecondKiBox.SelectedIndex = (settings.SecondTeamUsesKI) ? 1 : 0;

            ManualIndexChange = false;
        }
    }
}
