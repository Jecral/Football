using Football.EventArguments;
using Football.GUI.Other;
using Football.Multiplayer;
using Football.Multiplayer.MessageArguments;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Multiplayer
{
    class ChatTabPage : TabPage
    {
        public ChatTabPage(int id, List<User> users, string ownName)
        {
            Text = (id == 1) ? "General" : "Chat " + id;
            Tag = id;
            InitializeComponents();
            InitializeToolTips();
            InitializeEvents();

            if (users == null)
            {
                ChatPartners = new List<User>();
            }
            else
            {
                ChatPartners = users;
            }

            this.OwnName = ownName;
            StopTitleBlinker = true;
        }

        #region Properties
        public List<User> ChatPartners { get; set; }
        public string OwnName { get; set; }

        public Button SendButton { get; set; }
        private Button hideListButton;
        private TextBox inputTextBox;
        private RichTextBox messagesTextBox;
        private SplitContainer generalSplitter;
        private ToolTip controlToolTip;
        public CustomListView UserList { get; set; }

        public event EventHandler<ChatMessageArgs> MessageCreated;
        public event EventHandler<UserListEventArgs> ChatRequestCreated;
        public event EventHandler<UsernameChangeEventArgs> UserNameChanged;

        private Thread blinkerThread;
        public bool StopTitleBlinker { get; set; }
        public bool IsVisible { get; set; }
        #endregion

        private void InitializeComponents()
        {
            //Color chatBackColor = ColorTranslator.FromHtml("#F2FFF0");
            Color chatBackColor = ColorTranslator.FromHtml("#F7F4F0");
            //Color chatBackColor = Color.White;

            generalSplitter = new SplitContainer();
            generalSplitter.SplitterWidth = 2;
            generalSplitter.Dock = DockStyle.Fill;
            generalSplitter.SplitterDistance = 60;
            generalSplitter.FixedPanel = FixedPanel.Panel2;
            generalSplitter.SplitterMoved += generalSplitter_SplitterMoved;

            Panel contentPanel = generalSplitter.Panel1;

            #region Input and Output - Container

            SplitContainer inputOutputSplitter = new SplitContainer();
            //inputOutputSplitter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            inputOutputSplitter.SplitterWidth = 2;
            inputOutputSplitter.SplitterDistance = 70;
            inputOutputSplitter.Dock = DockStyle.Fill;
            inputOutputSplitter.Orientation = Orientation.Horizontal;
            inputOutputSplitter.Panel2MinSize = 80;

            Panel outputPanel = inputOutputSplitter.Panel1;

            messagesTextBox = new RichTextBox();
            messagesTextBox.Multiline = true;
            messagesTextBox.Size = new Size(outputPanel.Width - 14, outputPanel.Height - 7);
            messagesTextBox.Location = new Point(5, 7);
            messagesTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            messagesTextBox.BorderStyle = BorderStyle.FixedSingle;
            messagesTextBox.ReadOnly = true;
            messagesTextBox.BackColor = chatBackColor;
            messagesTextBox.HideSelection = false;

            outputPanel.Controls.Add(messagesTextBox);

            Panel inputPanel = inputOutputSplitter.Panel2;
            inputPanel.BackColor = Color.Transparent;

            TableLayoutPanel bottomTlp = new TableLayoutPanel();
            bottomTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            bottomTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomTlp.Dock = DockStyle.Fill;
            bottomTlp.BackColor = Color.Transparent;

            inputTextBox = new TextBox();
            inputTextBox.Multiline = true;
            inputTextBox.AcceptsReturn = false;
            inputTextBox.WordWrap = true;
            inputTextBox.Size = new Size(inputPanel.Width - 7, inputPanel.Height - 20);
            inputTextBox.Location = new Point(10, 0);
            inputTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            inputTextBox.ScrollBars = ScrollBars.Vertical;
            inputTextBox.KeyUp += inputTextBox_KeyUp;
            inputTextBox.BackColor = chatBackColor;
            //inputTextBox.AcceptsReturn = true;

            FlowLayoutPanel buttonFlp = new FlowLayoutPanel();
            buttonFlp.FlowDirection = FlowDirection.TopDown;
            buttonFlp.AutoSize = true;
            buttonFlp.Anchor = AnchorStyles.None;

            SendButton = new Button();
            SendButton.Text = "Send";
            SendButton.Size = new Size(50, 40);
            SendButton.Anchor = AnchorStyles.None;
            SendButton.Cursor = Cursors.Hand;
            SendButton.Click += sendButton_Click;
            SendButton.BackColor = Control.DefaultBackColor;

            hideListButton = new Button();
            hideListButton.Text = ">>>";
            hideListButton.Size = new Size(50, 20);
            hideListButton.Anchor = AnchorStyles.None;
            hideListButton.Cursor = Cursors.Hand;
            hideListButton.Click += hideListButton_Click;
            hideListButton.BackColor = Control.DefaultBackColor;
            hideListButton.MouseEnter += hideListButton_MouseEnter;

            buttonFlp.Controls.Add(SendButton);
            buttonFlp.Controls.Add(hideListButton);

            inputPanel.Controls.Add(bottomTlp);

            bottomTlp.Controls.Add(inputTextBox, 0, 0);
            bottomTlp.Controls.Add(buttonFlp, 1, 0);

            #endregion

            contentPanel.Controls.Add(inputOutputSplitter);
            Panel userPanel = generalSplitter.Panel2;

            UserList = new CustomListView();
            UserList.MouseDoubleClick += UserList_MouseDoubleClick;
            UserList.View = View.Details;
            UserList.Columns.Add("User");
            UserList.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            UserList.Size = new Size(userPanel.Width - 10, userPanel.Height - 10);
            UserList.Location = new Point(5, 5);
            UserList.LabelEdit = true;
            UserList.BeforeLabelEdit += UserList_BeforeLabelEdit;
            UserList.AfterLabelEdit += UserList_AfterLabelEdit;
            UserList.KeyDown += UserList_KeyDown;
            UserList.BackColor = chatBackColor;

            userPanel.Controls.Add(UserList);
            Controls.Add(generalSplitter);
        }

        private void InitializeToolTips()
        {
            controlToolTip = new ToolTip();
            controlToolTip.UseAnimation = true;
            controlToolTip.ShowAlways = true;

            controlToolTip.SetToolTip(UserList, "Double click on the own name (in bold print) to change it.\n" +
                "Double click on another name to create a private chat.\n\n" +
                "If you want to create a group chat, select multiple users and press enter.");
            controlToolTip.SetToolTip(messagesTextBox, "Messages received from the server.");
            controlToolTip.SetToolTip(inputTextBox, "Messages you want to send to the server.");
            controlToolTip.SetToolTip(SendButton, "Send the message to the server.");
        }

        private void InitializeEvents()
        {
            Enter += ChatTabPage_Enter;
        }

        void hideListButton_MouseEnter(object sender, EventArgs e)
        {
            if (UserList.Visible)
            {
                controlToolTip.SetToolTip(hideListButton, "Hide the user list");
            }
            else
            {
                controlToolTip.SetToolTip(hideListButton, "Show the user list");
            }
        }

        /// <summary>
        /// Sets StopTitleBlinker-bool to true so that the blinker thread will exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChatTabPage_Enter(object sender, EventArgs e)
        {
            StopTitleBlinker = true;   
        }

        /// <summary>
        /// Raises UserNameChanged()-event if the user changed his name to a valid new name.
        /// Otherwise the edit will get cancelled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UserList_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Label))
            {
                if (e.Label.Contains(';') || e.Label.Contains(':') || e.Label.Contains('<') || e.Label.Contains('>') || e.Label.Contains(','))
                {
                    AddText("[Error]", Color.Blue, DefaultFont, FontStyle.Regular);
                    AddText(" ';' ':' '<' ',' and '>' are not allowed.\n", Color.Black, DefaultFont, FontStyle.Regular);
                }
                else
                {
                    UsernameChangeEventArgs changeArgs = new UsernameChangeEventArgs(OwnName, e.Label);
                    UserNameChanged(this, changeArgs);
                }
            }

            e.CancelEdit = true;
        }

        /// <summary>
        /// Cancels the edit mode if the user has selected either more than one item or another player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void UserList_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (UserList.SelectedItems.Count == 1)
            {
                if (UserList.SelectedItems[0].Text != OwnName)
                {
                    e.CancelEdit = true;
                }
            }
        }

        /// <summary>
        /// If the user has selected > one item and presses enter, the CreateNewChat()-method will be raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void UserList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.C)
            {
                CreateNewChat();
            }
        }

        /// <summary>
        /// If only the own name is selected, the item plain will placed into the edit mode.
        /// Otherwise the method CreateNewChat() will be raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void UserList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (UserList.SelectedItems.Count == 1 && UserList.SelectedItems[0].Text == OwnName)
            {
                UserList.SelectedItems[0].BeginEdit();
            }
            else
            {
                CreateNewChat();
            }
        }

        /// <summary>
        /// Resizes the user-column of the listview so that it will fit to the new listview's size.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void generalSplitter_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UserList.Columns[0].Width = UserList.Width - 4;
        }

        /// <summary>
        /// Raises SendMessage()-method with the text in the input box if only enter is pressed and if the text box does contain something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void inputTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift && !string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                string text = inputTextBox.Text.Trim('\n');
                inputTextBox.Clear();
                e.Handled = true;
                SendMessage(text);
            }
        }

        /// <summary>
        /// Sends the text in the input text box to the server if it contains something.
        /// Clears the text box then.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void sendButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(inputTextBox.Text))
            {
                string text = inputTextBox.Text;
                inputTextBox.Clear();
                SendMessage(text);
            }
        }

        /// <summary>
        /// Hides/Show the userlist --> Changes the generalSplitter.Panel2Collapes bool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void hideListButton_Click(object sender, EventArgs e)
        {
            hideListButton.Text = (hideListButton.Text == ">>>") ? "<<<" : ">>>";
            generalSplitter.Panel2Collapsed = !generalSplitter.Panel2Collapsed;
        }

        /// <summary>
        /// Raises MessageCreated()-event with the text, the current time and the chat's tag as the ChatMessageArgs.
        /// </summary>
        /// <param name="text">The text which will be sent to the server.</param>
        private void SendMessage(string text)
        {
            ChatMessageArgs message = new ChatMessageArgs((int)Tag, "", DateTime.Now, text);
            inputTextBox.Clear();
            inputTextBox.Select(0, 0);
            MessageCreated(this, message);
        }

        /// <summary>
        /// Adds the received message to messagesTextBox. Marks the time blue.
        /// </summary>
        /// <param name="message"></param>
        public void AddChatMessage(ChatMessageArgs message)
        {
            if (IsHandleCreated)
            {
                messagesTextBox.Invoke((MethodInvoker)(() =>
                {
                    AddText("[" + message.Time.ToString("HH:mm:ss") + "]", Color.Blue, DefaultFont, FontStyle.Regular);
                    AddText(" <" + message.UserName + "> " + message.Content + "\n", Color.Black, DefaultFont, FontStyle.Regular);
                    inputTextBox.Focus();

                    StartTitleBlinker();
                }));
            }
        }

        /// <summary>
        /// Adds the received message to the messagesTextBox.
        /// Raises the RefreshUserList()-method if the ServerMessageType is ServerMessage.Join,
        /// otherwise the user who left the server will get removed from the user list.
        /// </summary>
        /// <param name="message"></param>
        public void AddServerMessage(ServerMessageArgs message)
        {
            messagesTextBox.Invoke((MethodInvoker)(() =>
            {
                AddText(">> ", Color.Blue, DefaultFont, FontStyle.Italic);
                AddText("[" + message.Time.ToString("HH:mm:ss") + "]", Color.Blue, DefaultFont, FontStyle.Italic);
                AddText(" " + message.Content + "\n", Color.Black, DefaultFont, FontStyle.Italic);
                inputTextBox.Focus();
            }));

            UserList.Invoke((MethodInvoker)(() =>
            {
                if (message.Type == ServerMessageType.Join)
                {
                    RefreshUserList();
                }
                else
                {
                    UserList.Items.RemoveByKey(message.UserName);
                }
            }));
        }

        /// <summary>
        /// Adds an information message to the messagesTextBox that a user in this room changed his team.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="oldTeamId"></param>
        /// <param name="newTeamId"></param>
        public void AddTeamChange(string username, int oldTeamId, int newTeamId)
        {
            messagesTextBox.Invoke((MethodInvoker)(() =>
            {
                AddText(">> ", Color.Blue, DefaultFont, FontStyle.Italic);
                AddText(username + " switched from ", Color.Black, DefaultFont, FontStyle.Italic);

                if(oldTeamId == 1)
                {
                    AddText("Team Red", Color.Red, DefaultFont, FontStyle.Italic);
                    AddText(" to ", Color.Black, DefaultFont, FontStyle.Italic);
                    AddText("Team Yellow", Color.DarkOrange, DefaultFont, FontStyle.Italic);
                }
                else
                {
                    AddText("Team Yellow", Color.DarkOrange, DefaultFont, FontStyle.Italic);
                    AddText(" to ", Color.Black, DefaultFont, FontStyle.Italic);
                    AddText("Team Red", Color.Red, DefaultFont, FontStyle.Italic);
                }

                AddText(".\n", Color.Black, DefaultFont, FontStyle.Italic);
            }));
        }

        /// <summary>
        /// Clears UserList and refills it.
        /// Own name in bold print.
        /// </summary>
        public void RefreshUserList()
        {
            UserList.Invoke((MethodInvoker)(() =>
            {
                UserList.Items.Clear();
                ChatPartners.Sort((x, y) => string.Compare(x.Username, y.Username));
                foreach (User user in ChatPartners)
                {
                    string name = user.Username;
                    ListViewItem newClient = new ListViewItem(name);
                    newClient.Name = name;
                    if (name == OwnName)
                    {
                        newClient.Font = new Font(UserList.Font, FontStyle.Bold);
                    }

                    UserList.Items.Add(newClient);
                }

            }));
        }

        /// <summary>
        /// Raises ChatRequestCreated()-event with the currently selected users as the UserListEventArgs.
        /// Only if there are selected users.
        /// </summary>
        private void CreateNewChat()
        {
            string usernames = "";
            ListView.SelectedIndexCollection selectedUsers = UserList.SelectedIndices;
            foreach (int index in selectedUsers)
            {
                usernames += ";" + UserList.Items[index].Name;
            }

            usernames = usernames.TrimStart(';');

            if (!string.IsNullOrWhiteSpace(usernames))
            {
                UserListEventArgs listArgs = new UserListEventArgs(usernames.Split(';'), 0, ListType.ChatRoom);
                ChatRequestCreated(this, listArgs);
            }
        }

        /// <summary>
        /// Adds an information message to the messagesTextBox contains the information that a user in this room renamed himself.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        public void ChangeUserName(string oldName, string newName)
        {
            UserList.Invoke((MethodInvoker)(() =>
            {
                if (oldName == OwnName)
                {
                    OwnName = newName;
                }
                RefreshUserList();
                AddText(">>", Color.Blue, DefaultFont, FontStyle.Italic);
                AddText(" <" + oldName + "> is now known as <" + newName + ">.\n", Color.Black, DefaultFont, FontStyle.Italic);
            }));
        }

        /// <summary>
        /// Starts a new thread in which the TabPage's plain changes every 400ms to "*new*".
        /// It will only change if the TabPage is visible.
        /// </summary>
        private void StartTitleBlinker()
        {
            StopTitleBlinker = false;

            if (blinkerThread == null || blinkerThread.ThreadState == ThreadState.Stopped)
            {
                blinkerThread = new Thread(() =>
                {
                    string oldTitle = Text;
                    while (!StopTitleBlinker && !IsVisible && this.IsHandleCreated)
                    {
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            Text = "*new*";
                        }));

                        Thread.Sleep(400);

                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke((MethodInvoker)(() =>
                            {
                                Text = oldTitle;
                            }));
                        }
                        Thread.Sleep(400);
                    }
                });
                blinkerThread.Start();
            }
        }

        /// <summary>
        /// Adds a specific plain with a specific color, font and fontstyle to messagesTextBox (the box with the messages from the server).
        /// </summary>
        /// <param name="plain"></param>
        /// <param name="color"></param>
        /// <param name="font"></param>
        /// <param name="style"></param>
        private void AddText(string text, Color color, Font font, FontStyle style)
        {
            int pos = messagesTextBox.TextLength;
            messagesTextBox.AppendText(text);
            messagesTextBox.Select(pos, text.Length);
            messagesTextBox.SelectionColor = color;
            messagesTextBox.SelectionFont = new Font(font, style);
            messagesTextBox.SelectionStart = messagesTextBox.Text.Length;
        }
    }
}
