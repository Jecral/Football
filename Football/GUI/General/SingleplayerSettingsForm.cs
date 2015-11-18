using Football.EventArguments;
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
    class SingleplayerSettingsForm : Form
    {
        public SingleplayerSettingsForm()
        {
            Size = new Size(800, 370);
            MaximumSize = new Size(800, 370);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = Properties.Resources.Icon;
            Text = "Game Settings";
            InitializeComponents();
            Show();
        }

        private Button createButton;
        private Button cancelButton;
        private EditableFieldPanel fieldSettings;
        private GameSettingsPanel gameSettingsPanel;

        public event EventHandler<FieldSettingsEventArgs> GameCreated;

        private void InitializeComponents()
        {
            //BackColor = ColorTranslator.FromHtml("#6B260B");
            //BackColor = Color.Transparent;
            BackgroundImage = Properties.Resources.MultiplayerLobby;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            generalTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize, 50));
            generalTlp.AutoSize = true;
            generalTlp.BackColor = Color.Transparent;

            fieldSettings = new EditableFieldPanel();
            fieldSettings.Size = new Size(556, 326);    
            fieldSettings.Anchor = AnchorStyles.None;
            fieldSettings.InitializeComponents(51, 25);
            fieldSettings.BorderStyle = BorderStyle.FixedSingle;

            TableLayoutPanel roundSettingsTlp = new TableLayoutPanel();
            roundSettingsTlp.AutoSize = true;
            roundSettingsTlp.Anchor = AnchorStyles.None;

            #region Create and Cancel
            FlowLayoutPanel buttonFlp = new FlowLayoutPanel();
            buttonFlp.Anchor = AnchorStyles.None;
            buttonFlp.AutoSize = true;

            createButton = new Button();
            createButton.Text = "Create";
            createButton.Click += createButton_Click;
            createButton.Size = new Size(80, 25);
            createButton.Anchor = AnchorStyles.None;
            createButton.Cursor = Cursors.Hand;
            createButton.BackColor = Control.DefaultBackColor;

            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Click += cancelButton_Click;
            cancelButton.Size = new Size(80, 25);
            cancelButton.BackColor = Control.DefaultBackColor;
            cancelButton.Cursor = Cursors.Hand;

            buttonFlp.Controls.Add(createButton);
            buttonFlp.Controls.Add(cancelButton);

            #endregion

            #region Game settings
            gameSettingsPanel = new GameSettingsPanel(false);
            gameSettingsPanel.AutoSize = true;
            gameSettingsPanel.Dock = DockStyle.Fill;
            #endregion

            roundSettingsTlp.Controls.Add(gameSettingsPanel, 0, 0);
            roundSettingsTlp.Controls.Add(buttonFlp, 0, 1);

            generalTlp.Controls.Add(fieldSettings, 0, 0);
            generalTlp.Controls.Add(roundSettingsTlp, 1, 0);

            Controls.Add(generalTlp);
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        void createButton_Click(object sender, EventArgs e)
        {
            GameSettings settings = fieldSettings.CurrentSettings;
            settings.RoundsPerHalf = Int32.Parse(gameSettingsPanel.GameRoundsBox.Text);
            settings.SecondsPerRound = Int32.Parse(gameSettingsPanel.RoundSecondsBox.Text);
            settings.FirstTeamUsesKI = gameSettingsPanel.FirstKiBox.SelectedIndex == 1;
            settings.SecondTeamUsesKI = gameSettingsPanel.SecondKiBox.SelectedIndex == 1;

            FieldSettingsEventArgs args = new FieldSettingsEventArgs(settings);
            GameCreated(this, args);
            Close();
        }
    }
}
