using Football.GUI.Other;
using Football.Multiplayer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Multiplayer
{
    class TeamChangerForm : Form
    {
        /// <summary>
        /// string[0] = username
        /// string[1] = team
        /// </summary>
        /// <param name="Users"></param>
        public TeamChangerForm(List<User> users, string ownName)
        {
            Icon = Properties.Resources.Icon;
            Text = "Teamswitcher";
            Size = new Size(300, 400);
            MaximumSize = new Size(300, 400);
            StartPosition = FormStartPosition.CenterScreen;
            this.ownName = ownName;
            InitializeComponents(users, ownName);
            Show();
        }

        private string ownName;
        private CustomListView teamList;
        private Button switchButton;

        public event EventHandler SwitchTeamRequest;

        private void InitializeComponents(List<User> users, string ownName)
        {
            Color brownColor = ColorTranslator.FromHtml("#6B260B");
            BackColor = brownColor;

            Panel rootPanel = new Panel();
            rootPanel.Size = new Size(ClientSize.Width - 10, ClientSize.Height - 10);
            rootPanel.Location = new Point(5, 5);
            rootPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            rootPanel.BackColor = brownColor;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            generalTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            generalTlp.Dock = DockStyle.Fill;
            generalTlp.BackColor = brownColor;

            teamList = new CustomListView();
            teamList.Columns.Add("Team Yellow", 132);
            teamList.Columns.Add("Team Red", 132);
            teamList.View = View.Details;
            teamList.GridLines = true;
            teamList.Dock = DockStyle.Fill;
            teamList.DrawSubItem += teamList_DrawSubItem;

            UpdateUserList(users);

            switchButton = new Button();
            switchButton.Text = "Switch Team";
            switchButton.AutoSize = true;
            switchButton.Anchor = AnchorStyles.None;
            switchButton.Cursor = Cursors.Hand;
            switchButton.BackColor = Control.DefaultBackColor;
            switchButton.Click += switchButton_Click;

            generalTlp.Controls.Add(teamList, 0, 0);
            generalTlp.Controls.Add(switchButton, 0, 1);

            rootPanel.Controls.Add(generalTlp);

            Controls.Add(rootPanel);
        }

        void switchButton_Click(object sender, EventArgs e)
        {
            SwitchTeamRequest(this, EventArgs.Empty);
        }

        /// <summary>
        /// Draws the ListViewSubItems of the user list.
        /// If the plain of the item equals the own name, it will be bold.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void teamList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Pen myPen = new Pen(Color.Black, 1);
            Font font;
            if (e.Item.Text == ownName)
            {
                font = new Font(DefaultFont, FontStyle.Bold);
            }
            else
            {
                font = new Font(DefaultFont, FontStyle.Regular);
            }

            e.Graphics.DrawString(e.Item.Text, font, Brushes.Black, new Point(0, 0));
        }

        public void UpdateUserList(List<User> users)
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

        /// <summary>
        /// Clears the user list and refills it then.
        /// Own name in bold print.
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
                item.UseItemStyleForSubItems = false;

                for (int c = 0; c < item.SubItems.Count; c++)
                {
                    if (item.SubItems[c].Text == ownName)
                    {
                        item.SubItems[c].Font = new Font(DefaultFont, FontStyle.Bold);
                    }
                }

                teamList.Items.Add(item);
            }
        }
    }
}
