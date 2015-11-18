using Football.EventArguments;
using Football.GUI.Other;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.General
{
    class ReplayManagerForm : Form
    {
        public ReplayManagerForm(int maximumRound)
        {
            this.maximumRound = maximumRound;

            Text = "Replay Manager";
            Icon = Properties.Resources.Icon;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(320, 180);
            MaximumSize = new Size(400, 180);
            MinimumSize = new Size(320, 180);
            InitializeComponents(maximumRound);
            InitializeToolTips();

            FormClosing += ReplayManagerForm_FormClosing;
            Resize += ReplayManagerForm_Resize;

            InitializeTimer();
            Show();
        }

        public bool IsRunning { get; set; }
        public Label RoundLabel { get; set; }
        public int Round {
            set
            {
                RoundLabel.Text = "Current Round: " + value;
                if (value == maximumRound)
                {
                    SwitchJumper();
                }
            }
        }
        private int maximumRound;
        public bool ManualClose { get; set; }

        private FilterComboBox jumpComboBox;
        private FilterComboBox secondComboBox;
        private Button goButton;
        private Button nextRoundButton;
        private Button previousRoundButton;

        public event EventHandler NextRound;
        public event EventHandler PreviousRound;
        public event EventHandler<RoundJumpEventArgs> RoundJump;
        public event EventHandler ManagerClosed;

        public System.Timers.Timer JumpTimer { get; set; }

        private void InitializeComponents(int maximumRound)
        {
            Panel rootPanel = new Panel();
            rootPanel.Size = new Size(ClientSize.Width - 10, ClientSize.Height - 10);
            rootPanel.Location = new Point(5, 5);
            rootPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.Dock = DockStyle.Fill;
            generalTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            #region first row
            Panel roundBorderPanel = new Panel();
            roundBorderPanel.Dock = DockStyle.Fill;
            roundBorderPanel.Paint += roundFlp_Paint;

            RoundLabel = new Label();
            RoundLabel.Text = "Current Round: ";
            RoundLabel.AutoSize = true;
            RoundLabel.Location = new Point(90, 5);
            RoundLabel.Font = new Font("Arial", 11f, FontStyle.Regular);

            roundBorderPanel.Controls.Add(RoundLabel);
            #endregion

            #region second row
            Label secondLabel = new Label();
            secondLabel.Text = "Jump each x milliseconds: ";
            secondLabel.AutoSize = true;
            secondLabel.Anchor = AnchorStyles.Right;

            secondComboBox = new FilterComboBox();
            secondComboBox.Filter = new Regex("^(([1-9])([0-9]{0,4}))|([\u0008])$");
            secondComboBox.Dock = DockStyle.Fill;
            secondComboBox.Items.AddRange(new string[] { "50", "150", "250", "500", "750", "1000", "2000" });
            secondComboBox.SelectedIndex = 1;

            Button updateButton = new Button();
            updateButton.Text = "Update";
            updateButton.Size = new Size(60, 24);
            updateButton.Anchor = AnchorStyles.None;
            updateButton.Click += updateButton_Click;
            updateButton.Cursor = Cursors.Hand;
            #endregion

            #region third row
            Label jumpLabel = new Label();
            jumpLabel.Text = "Jump to round: ";
            jumpLabel.AutoSize = true;
            jumpLabel.Anchor = AnchorStyles.Right;

            jumpComboBox = new FilterComboBox();
            jumpComboBox.Filter = new Regex("^(([1-9])([0-9]{0,4}))|([\u0008])$");
            jumpComboBox.Dock = DockStyle.Fill;
            for (int i = 0; i <= maximumRound; i++)
            {
                jumpComboBox.Items.Add(i.ToString());
            }
            jumpComboBox.SelectedIndex = 0;

            Button jumpButton = new Button();
            jumpButton.Text = "Jump";
            jumpButton.Size = new Size(60, 24);
            jumpButton.Anchor = AnchorStyles.None;
            jumpButton.Click += jumpButton_Click;
            jumpButton.Cursor = Cursors.Hand;
            #endregion

            Panel roundButtonBorderPanel = new Panel();
            roundButtonBorderPanel.Paint += roundButtonBorderPanel_Paint;
            roundButtonBorderPanel.Dock = DockStyle.Fill;

            FlowLayoutPanel roundButtonsFlp = new FlowLayoutPanel();
            roundButtonsFlp.Size = new Size(160, 30);
            roundButtonsFlp.Location = new Point(75, 5);

            #region RoundButtons
            previousRoundButton = new Button();
            previousRoundButton.Text = "<<";
            previousRoundButton.Cursor = Cursors.Hand;
            previousRoundButton.Anchor = AnchorStyles.None;
            previousRoundButton.Size = new Size(40, 22);
            previousRoundButton.Click += previousRoundButton_Click;

            goButton = new Button();
            goButton.Text = (IsRunning) ? "Stop" : "Go";
            goButton.Size = new Size(50, 22);
            goButton.Anchor = AnchorStyles.None;
            goButton.Click += goButton_Click;
            goButton.Cursor = Cursors.Hand;

            nextRoundButton = new Button();
            nextRoundButton.Text = ">>";
            nextRoundButton.Cursor = Cursors.Hand;
            nextRoundButton.Anchor = AnchorStyles.None;
            nextRoundButton.Size = new Size(40, 22);
            nextRoundButton.Click += nextRoundButton_Click;
            #endregion

            roundButtonsFlp.Controls.AddRange(new Control[] { previousRoundButton, goButton, nextRoundButton });
            roundButtonBorderPanel.Controls.Add(roundButtonsFlp);

            generalTlp.Controls.Add(roundBorderPanel, 0, 0);
            generalTlp.SetColumnSpan(roundBorderPanel, 3);
            generalTlp.Controls.Add(secondLabel, 0, 1);
            generalTlp.Controls.Add(secondComboBox, 1, 1);
            generalTlp.Controls.Add(updateButton, 2, 1);
            generalTlp.Controls.Add(jumpLabel, 0, 2);
            generalTlp.Controls.Add(jumpComboBox, 1, 2);
            generalTlp.Controls.Add(jumpButton, 2, 2);
            generalTlp.Controls.Add(roundButtonBorderPanel, 0, 3);
            generalTlp.SetColumnSpan(roundButtonBorderPanel, 3);

            rootPanel.Controls.Add(generalTlp);

            Controls.Add(rootPanel);
        }

        /// <summary>
        /// Initialize all tool tips.
        /// </summary>
        private void InitializeToolTips()
        {
            ToolTip toolTip = new ToolTip();
            toolTip.UseAnimation = true;

            toolTip.SetToolTip(previousRoundButton, "Previous round.");
            toolTip.SetToolTip(nextRoundButton, "Next round.");
        }

        /// <summary>
        /// Creates a new timer and sets his interval to 150.
        /// </summary>
        private void InitializeTimer()
        {
            JumpTimer = new System.Timers.Timer();
            JumpTimer.Elapsed += JumpTimer_Elapsed;
            JumpTimer.Interval = 150;
        }

        /// <summary>
        /// Raises the SwichJumper()-method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void goButton_Click(object sender, EventArgs e)
        {
            SwitchJumper();
        }

        /// <summary>
        /// Updates the intervall of the timer who raises the NextRound-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void updateButton_Click(object sender, EventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$");
            if (regex.IsMatch(secondComboBox.Text))
            {
                JumpTimer.Interval = Convert.ToInt32(secondComboBox.Text);
            }
            else
            {
                MessageBoxIcon icon = MessageBoxIcon.Error;
                MessageBox.Show("Invalid input", "Error", MessageBoxButtons.OK, icon);
            }
        }

        /// <summary>
        /// Raises the RoundJump()-event if the round-value is valid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void jumpButton_Click(object sender, EventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$");
            if (regex.IsMatch(jumpComboBox.Text))
            {
                RoundJumpEventArgs args = new RoundJumpEventArgs(Convert.ToInt32(jumpComboBox.Text));
                RoundJump(this, args);
            }
            else
            {
                MessageBoxIcon icon = MessageBoxIcon.Error;
                MessageBox.Show("Invalid input", "Error", MessageBoxButtons.OK, icon);
            }
        }

        /// <summary>
        /// Raises the PreviousRound()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void previousRoundButton_Click(object sender, EventArgs e)
        {
            PreviousRound(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the NextRound()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void nextRoundButton_Click(object sender, EventArgs e)
        {
            NextRound(this, EventArgs.Empty);
        }

        /// <summary>
        /// Paints a DarkSlaeBlue border line at the top.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void roundButtonBorderPanel_Paint(object sender, PaintEventArgs e)
        {
            Panel roundButtonsFlp = (Panel)sender;
            ControlPaint.DrawBorder(e.Graphics, roundButtonsFlp.ClientRectangle,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.DarkSlateBlue, 1, ButtonBorderStyle.Solid,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.DarkSlateBlue, 0, ButtonBorderStyle.Solid);
        }

        /// <summary>
        /// Paints a DarkSlateBlue border line at the bottom.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void roundFlp_Paint(object sender, PaintEventArgs e)
        {
            Panel roundButtonsFlp = (Panel)sender;
            ControlPaint.DrawBorder(e.Graphics, roundButtonsFlp.ClientRectangle,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.DarkSlateBlue, 0, ButtonBorderStyle.Solid,
            Color.DarkGray, 0, ButtonBorderStyle.Solid,
            Color.DarkSlateBlue, 1, ButtonBorderStyle.Solid);
        }

        /// <summary>
        /// Hides the Form and Raises the ManagerClosed()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ReplayManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ManualClose)
            {
                Hide();
                ManagerClosed(this, EventArgs.Empty);
                e.Cancel = true;
            }
        }

        void ReplayManagerForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ManagerClosed(this, EventArgs.Empty);
                Hide();
            }
        }

        void JumpTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            NextRound(this, EventArgs.Empty);
        }

        /// <summary>
        /// If the current button's plain is "go", the plain will change to "Stop" and the JumpTimer will start.
        /// Otherwise the opposite.
        /// </summary>
        private void SwitchJumper()
        {
            if (goButton.Text == "Go")
            {
                goButton.Text = "Stop";
                JumpTimer.Start();
            }
            else
            {
                goButton.Text = "Go";
                JumpTimer.Stop();
            }
        }
    }
}
