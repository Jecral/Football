using Football.GUI.Other;
using Football.Multiplayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI
{
    public enum DirectionLineSetting
    {
        OneCell,
        DirectWay,
        ExactWay,
        NoCell
    }
    class InformationPanel : Panel
    {
        public InformationPanel()
        {
            LineSetting = DirectionLineSetting.ExactWay;

            lineNoCell = Properties.Resources.DirectionNoCell;
            lineOneCell = Properties.Resources.DirectionOneCell;
            lineDirectWay = Properties.Resources.DirectionDirectWay;
            lineExactWay = Properties.Resources.DirectionExactWay;

            InitializeComponents();
            InitializeToolTips();
        }

        public Label StepTimeLabel { get; set; }
        public Label StepLabel { get; set; }
        public Label FirstTeamGoalsLabel { get; set; }
        public Label SecondTeamGoalsLabel { get; set; }
        public Button LineSwitchButton { get; set; }
        public TableLayoutPanel GeneralTlp { get; set; }
        private CustomListView teamList;

        public DirectionLineSetting LineSetting;
        private Image lineNoCell;
        private Image lineOneCell;
        private Image lineExactWay;
        private Image lineDirectWay;

        private string ownName;

        public event EventHandler DirectionLineSettingsChanged;

        /// <summary>
        /// Initializes all UI components.
        /// </summary>
        private void InitializeComponents()
        {
            GeneralTlp = new TableLayoutPanel();
            GeneralTlp.Location = new Point(50, 5);
            GeneralTlp.Size = new Size(1080, 100);

            TableLayoutPanel timeTlp = new TableLayoutPanel();
            timeTlp.AutoSize = true;
            timeTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            timeTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            #region round information box
            GroupBox timeBox = new GroupBox();
            timeBox.Text = "Information";
            timeBox.Size = new Size(230, 50);
            timeBox.ForeColor = Color.LightGoldenrodYellow;

            StepTimeLabel = new Label();
            StepTimeLabel.Text = "Next round in:";
            StepTimeLabel.Font = new Font("Arial", 10);
            StepTimeLabel.AutoSize = true;
            StepTimeLabel.Location = new Point(10, 20);
            StepTimeLabel.ForeColor = Color.White;

            StepLabel = new Label();
            StepLabel.Text = "Round: 0";
            StepLabel.Font = new Font("Arial", 10);
            StepLabel.AutoSize = true;
            StepLabel.Location = new Point(140, 20);
            StepLabel.ForeColor = Color.White;

            timeBox.Controls.Add(StepTimeLabel);
            timeBox.Controls.Add(StepLabel);
            #endregion

            #region goal information box
            //goal box
            GroupBox teamGoalBox = new GroupBox();
            teamGoalBox.Text = "Goals";
            teamGoalBox.Size = new Size(230, 50);
            teamGoalBox.ForeColor = Color.LightGreen;

            FirstTeamGoalsLabel = new Label();
            FirstTeamGoalsLabel.Text = "First Team: 0";
            FirstTeamGoalsLabel.AutoSize = true;
            FirstTeamGoalsLabel.Font = new Font("Arial", 10);
            FirstTeamGoalsLabel.Location = new Point(10, 20);
            FirstTeamGoalsLabel.ForeColor = Color.Yellow;

            SecondTeamGoalsLabel = new Label();
            SecondTeamGoalsLabel.Text = "Second Team: 0";
            SecondTeamGoalsLabel.AutoSize = true;
            SecondTeamGoalsLabel.Font = new Font("Arial", 10);
            SecondTeamGoalsLabel.Location = new Point(100, 20);
            SecondTeamGoalsLabel.ForeColor = Color.Red;
            #endregion

            teamGoalBox.Controls.Add(FirstTeamGoalsLabel);
            teamGoalBox.Controls.Add(SecondTeamGoalsLabel);

            //direction line switcher
            LineSwitchButton = new Button();
            //LineSwitchButton.FlatStyle = FlatStyle.Flat;
            LineSwitchButton.FlatAppearance.BorderSize = 2;
            LineSwitchButton.Cursor = Cursors.Hand;
            LineSwitchButton.BackgroundImage = lineExactWay;
            LineSwitchButton.Size = new Size(89, 50);
            LineSwitchButton.Anchor = AnchorStyles.Left;
            LineSwitchButton.Click += LineSwitchButton_Click;

            timeTlp.Controls.Add(LineSwitchButton, 0, 0);
            timeTlp.Controls.Add(timeBox, 1, 0);
            timeTlp.Controls.Add(teamGoalBox, 2, 0);

            GeneralTlp.Controls.Add(timeTlp, 0, 0);

            Controls.Add(GeneralTlp);
        }

        /// <summary>
        /// Initializes all tooltips.
        /// </summary>
        private void InitializeToolTips()
        {
            ToolTip toolTip = new ToolTip();
            toolTip.UseAnimation = true;

            toolTip.SetToolTip(LineSwitchButton, "Change how the program paints the direction ways.");
        }

        /// <summary>
        /// Changes the current DirectionLineSetting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LineSwitchButton_Click(object sender, EventArgs e)
        {
            if (LineSetting == DirectionLineSetting.OneCell)
            {
                LineSwitchButton.BackgroundImage = lineDirectWay;
                LineSetting = DirectionLineSetting.DirectWay;
            }
            else if (LineSetting == DirectionLineSetting.DirectWay)
            {
                LineSwitchButton.BackgroundImage = lineExactWay;
                LineSetting = DirectionLineSetting.ExactWay;
            }
            else if (LineSetting == DirectionLineSetting.ExactWay)
            {
                LineSwitchButton.BackgroundImage = lineNoCell;
                LineSetting = DirectionLineSetting.NoCell;
            }
            else if (LineSetting == DirectionLineSetting.NoCell)
            {
                LineSwitchButton.BackgroundImage = lineOneCell;
                LineSetting = DirectionLineSetting.OneCell;
            }

            DirectionLineSettingsChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds a user list as a CustomListView to the information panel.
        /// </summary>
        /// <param name="users"></param>
        /// <param name="ownName"></param>
        public void AddUserList(List<User> users, string ownName)
        {
            this.ownName = ownName;

            teamList = new CustomListView();
            teamList.Name = "TeamList";
            teamList.Size = new Size(200, 68);
            teamList.Columns.Add("Team Yellow", teamList.ClientSize.Width/2 + 2);
            teamList.Columns.Add("Team Red", teamList.ClientSize.Width/2 + 2);
            teamList.Dock = DockStyle.Right;
            teamList.View = View.Details;
            teamList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            teamList.GridLines = true;
            teamList.BackColor = ColorTranslator.FromHtml("#6B260B");
            teamList.ForeColor = Color.White;

            RefreshUserList(users);

            GeneralTlp.Controls.Add(teamList, 1, 0);
            Refresh();
        }

        /// <summary>
        /// Raises RefreshUserList() with the user list in the parameter if the information panel is visible.
        /// Raises the method with an invoke if it is required.
        /// </summary>
        /// <param name="users"></param>
        public void UpdateUserList(List<User> users)
        {
            if (this.Visible)
            {
                if (teamList.InvokeRequired)
                {
                    teamList.Invoke((MethodInvoker)(() =>
                    {
                        RefreshUserList(users);
                    }));
                }
                else
                {
                    RefreshUserList(users);
                }
            }
        }

        /// <summary>
        /// Refills the userlist with the received list.
        /// Adds an ☑ to the user name if he is ready, otherwise an ☐.
        /// </summary>
        /// <param name="users"></param>
        private void RefreshUserList(List<User> users)
        {
            teamList.Items.Clear();
            List<User> firstTeam = users.FindAll(x => x.TeamId == 0);
            List<User> secondTeam = users.FindAll(x => x.TeamId == 1);

            int maxCount = (firstTeam.Count > secondTeam.Count) ? firstTeam.Count : secondTeam.Count;
            List<User> biggerList = (firstTeam.Count > secondTeam.Count) ? firstTeam : secondTeam;

            for (int i = 0; i < biggerList.Count; i++)
            {
                string column1 = (i < firstTeam.Count) ? firstTeam[i].Username : "";
                string column2 = (i < secondTeam.Count) ? secondTeam[i].Username : "";
                ListViewItem item = new ListViewItem(new string[] { column1, column2 });
                item.SubItems[0].ForeColor = Color.Yellow;
                item.SubItems[1].ForeColor = Color.Red;

                item.UseItemStyleForSubItems = false;

                for (int c = 0; c < item.SubItems.Count; c++)
                {
                    User user = users.Find(x => x.Username == item.SubItems[c].Text);
                    if (user != null)
                    {
                        if (user.IsReady)
                        {
                            item.SubItems[c].Text += " ☑";
                        }
                        else
                        {
                            item.SubItems[c].Text += " ☐";
                        }
                    }
                }

                teamList.Items.Add(item);
            }
        }

    }
}
