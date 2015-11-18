using Football.EventArguments;
using Football.GUI;
using Football.GUI.Multiplayer;
using Football.GUI.Other;
using Football.Logic;
using Football.Logic.GameFiles;
using Football.Logic.GameObjects.Player;
using Football.Multiplayer.MessageArguments;
using Football.Multiplayer.Network;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Football.Multiplayer
{
    public enum MultiplayerStatus
    {
        Lobby,
        Game
    }

    /// <summary>
    /// Handler for everything multiplayer related
    /// </summary>
    class MultiplayerHandler
    {
        public MultiplayerHandler()
        {
            databaseHandler = new ConnectionDatabaseManager();
            Users = new List<User>();
            ClosedTabPages = new List<ChatTabPage>();
        }

        #region Properties
        public MultiplayerLobbyForm CurrentLobby { get; set; }
        private TeamChangerForm teamChanger;

        public List<User> Users { get; set; }
        public ChatForm CurrentChatForm { get; set; }
        public TabControl CurrentChats { get; set; }
        public List<ChatTabPage> ClosedTabPages { get; set; }
        public Client CurrentClient { get; set; }
        public Game MultiplayerGame { get; set; }
        public bool IsHost { get; set; }

        private GameImage CachedSavefile;
        private ConnectionDatabaseManager databaseHandler;

        public event EventHandler LoginSucceeded;
        public event EventHandler ConnectionClosed;
        public event EventHandler<FieldSettingsEventArgs> GameCreated;
        public event EventHandler<GameImageEventArgs> GameContinued;
        public event EventHandler GameEnded;
        public event EventHandler RefreshUserList;
        #endregion

        #region Chathandling
        private void AddChat(int chatId, List<User> users)
        {
            CurrentChats.Invoke((MethodInvoker)(() =>
            {
                ChatTabPage chat = new ChatTabPage(chatId, users, CurrentClient.ClientName);
                chat.MessageCreated += chat_MessageCreated;
                chat.ChatRequestCreated += chat_ChatRequestCreated;
                chat.UserNameChanged += UsernameChangeRequest;
                chat.BackColor = ColorTranslator.FromHtml("#6B260B");

                CurrentChats.TabPages.Add(chat);
                chat.UserList.Columns[0].Width = chat.UserList.ClientRectangle.Width;
                if (chatId == 1)
                {
                    //CurrentLobby.AcceptButton = chat.SendButton;
                    chat.IsVisible = true;
                }
                if (users == null)
                {
                    CurrentClient.Send("userlist<" + chatId + ">:");
                }
                else
                {
                    chat.RefreshUserList();
                }
            }));
        }

        void ChatMessageReceived(object sender, ChatMessageArgs message)
        {
            OpenTagPageIfNeeded(message.ChatId);
            foreach (TabPage page in CurrentChats.TabPages)
            {
                ChatTabPage chatGroup = (ChatTabPage)page;
                if ((int)chatGroup.Tag == message.ChatId)
                {
                    chatGroup.AddChatMessage(message);
                }
            }
        }

        void ServerMessageReceived(object sender, ServerMessageArgs message)
        {
            string username = message.UserName;

            if (message.Type == ServerMessageType.Join)
            {
                OpenTagPageIfNeeded(0);
                User newUser = new User(username, message.TeamId);
                Users.Add(newUser);

                ChatTabPage chatGroup = (ChatTabPage)CurrentChats.TabPages[0];
                chatGroup.AddServerMessage(message);
            }
            else
            {
                Users.Remove(Users.Find(x => x.Username == message.UserName));
                foreach (TabPage page in CurrentChats.TabPages)
                {
                    ChatTabPage chatGroup = (ChatTabPage)page;
                    User user = chatGroup.ChatPartners.Find(x => x.Username == username);
                    if (user != null)
                    {
                        chatGroup.ChatPartners.Remove(user);
                        chatGroup.RefreshUserList();
                    }
                    if (Convert.ToInt32(chatGroup.Tag) == 1 || user != null)
                    {
                        chatGroup.AddServerMessage(message);
                    }
                }
            }

            if (teamChanger != null && teamChanger.Visible)
            {
                teamChanger.UpdateUserList(Users);
            }

            if (RefreshUserList != null)
            {
                RefreshUserList(this, EventArgs.Empty);
            }
        }

        void UsernameChangeRequest(object sender, UsernameChangeEventArgs e)
        {
            CurrentClient.Send("name<>:" + e.NewName);
        }

        void chat_ChatRequestCreated(object sender, UserListEventArgs e)
        {
            string[] users = e.Users;
            string usernames = "";
            foreach (string user in users)
            {
                usernames += ";" + user;
            }

            usernames = usernames.TrimStart(';');

            CurrentClient.Send("chat<>:" + usernames);
        }

        void chat_MessageCreated(object sender, ChatMessageArgs message)
        {
            CurrentClient.Send("message<" + message.ChatId + ">:" + message.Content);
        }

        void UserNameChanged(object sender, UsernameChangeEventArgs e)
        {
            User user = Users.Find(x => x.Username == e.OldName);
            if (user != null)
            {
                user.Username = e.NewName;
            }

            ChatTabPage firstchat = (ChatTabPage)CurrentChats.TabPages[0];
            if (teamChanger != null && teamChanger.Visible)
            {
                teamChanger.UpdateUserList(Users);
            }

            if (string.IsNullOrWhiteSpace(e.OldName))
            {
                AddChat(1, null);
            }

            ChangeUsername(e.OldName, e.NewName);
        }

        void ChatUserListReceived(object sender, UserListEventArgs e)
        {

            int chatId = e.Id;
            string[] usernames = e.Users;
            
            List<User> chatUserList = new List<User>();
            if (chatId == 1)
            {
                foreach (string name in e.Users)
                {
                    User existingUser = Users.Find(x => x.Username == name);
                    if (existingUser == null)
                    {
                        chatUserList.Add(new User(name, e.Id));
                    }
                }

                Users = chatUserList;
            }
            else
            {

                foreach (string name in e.Users)
                {
                    User chatUser = Users.Find(x => x.Username == name);
                    if (chatUser != null)
                    {
                        chatUserList.Add(chatUser);
                    }
                }
            }

            foreach (TabPage page in CurrentChats.TabPages)
            {
                ChatTabPage chatGroup = (ChatTabPage)page;
                if ((int)chatGroup.Tag == chatId)
                {
                    chatGroup.ChatPartners = chatUserList;
                    chatGroup.RefreshUserList();
                }
            }

        }

        void NewChatCreated(object sender, UserListEventArgs e)
        {
            int chatId = e.Id;

            List<User> chatUserList = new List<User>();
            foreach (string name in e.Users)
            {
                User chatUser = Users.Find(x => x.Username == name);
                if (chatUser != null)
                {
                    chatUserList.Add(chatUser);
                }
            }

            if (!DoesChatPageExist(chatId))
            {
                bool openedClosedChat = OpenTagPageIfNeeded(chatId);
                if (!openedClosedChat)
                {
                    AddChat(chatId, chatUserList);
                }
            }
        }

        void customTabControl_TabClosing(object sender, TabControlCancelEventArgs e)
        {
            ChatTabPage chatPage = (ChatTabPage)e.TabPage;
            if ((int)chatPage.Tag != 1)
            {
                chatPage.IsVisible = false;
                ClosedTabPages.Add(chatPage);
            }
            else
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Opens the ChatTabPage with this chat id if it is closed.
        /// </summary>
        /// <param name="chatId"></param>
        private bool OpenTagPageIfNeeded(int chatId)
        {
            ChatTabPage tabPage = ClosedTabPages.Find(page => (int) page.Tag == chatId);
            if (tabPage != null)
            {
                CurrentChats.BeginInvoke((MethodInvoker)(() =>
                {
                    CurrentChats.TabPages.Add(tabPage);
                }));

                while (!CurrentChats.TabPages.Contains(tabPage)) { }

                ClosedTabPages.Remove(tabPage);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether a chat page with this chat id is open.
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        private bool DoesChatPageExist(int chatId)
        {
            foreach (TabPage page in CurrentChats.TabPages)
            {
                ChatTabPage chatPage = (ChatTabPage)page;
                if ((int)chatPage.Tag == chatId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Changes the username of an user from the old name to the new name
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        private void ChangeUsername(string oldName, string newName)
        {
            //update the xml-file
            if (oldName == CurrentClient.ClientName)
            {
                databaseHandler.UpdateUsername(CurrentClient.ServerAddress, newName);
            }

            //update the user
            User user = Users.Find(x => x.Username == oldName);
            if (user != null)
            {
                user.Username = newName;
            }

            //inform the user list
            if (RefreshUserList != null)
            {
                RefreshUserList(this, EventArgs.Empty);
            }

            //update the chats
            foreach (TabPage page in CurrentChats.TabPages)
            {
                ChatTabPage chatGroup = (ChatTabPage)page;
                chatGroup.ChangeUserName(oldName, newName);
            }
        }
        #endregion

        #region TeamHandler
        void CurrentClient_UserChangedTeam(object sender, TeamChangeEventArgs e)
        {
            User user = Users.Find(x => x.Username == e.Username && x.TeamId == e.OldTeamId);
            if (user != null)
            {
                user.TeamId = e.NewTeamId;
            }

            if (teamChanger != null && teamChanger.Visible)
            {
                teamChanger.UpdateUserList(Users);
            }

            //write it into the chat
            ChatTabPage chatGroup = (ChatTabPage)CurrentChats.TabPages[0];
            chatGroup.AddTeamChange(e.Username, e.OldTeamId, e.NewTeamId);
        }

        void Lobby_ShowTeamsClicked(object sender, EventArgs e)
        {
            if (teamChanger != null && teamChanger.IsHandleCreated)
            {
                teamChanger.Show();
                teamChanger.BringToFront();
            }
            else
            {
                teamChanger = new TeamChangerForm(Users, CurrentClient.ClientName);
                teamChanger.SwitchTeamRequest += SwitchTeamRequest;
            }
        }

        void SwitchTeamRequest(object sender, EventArgs e)
        {
            CurrentClient.Send("team<!>:");
        }

        /// <summary>
        /// Saves the received team list
        /// </summary>
        /// <param name="sender">An object array
        /// sender[0] : teamnumber
        /// sender[1] : userlist as an string array</param>
        /// <param name="message"></param>
        void CurrentClient_TeamListReceived(object sender, UserListEventArgs e)
        {
            foreach (string name in e.Users)
            {
                User existingUser = Users.Find(x => x.Username == name);
                if (existingUser == null)
                {
                    Users.Add(new User(name, e.Id));
                }
                
            }
        }

        /// <summary>
        /// Saved the userlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_CompleteUserListReceived(object sender, UserListEventArgs e)
        {
            Users = e.UserList;
            ChatTabPage page = (ChatTabPage)CurrentChats.TabPages[0];
            page.ChatPartners = Users;
            page.RefreshUserList();
        }

        /// <summary>
        /// Closes the team switcher form.
        /// </summary>
        private void CloseTeamswitcher()
        {
            if (teamChanger != null && teamChanger.IsHandleCreated)
            {
                teamChanger.Invoke((MethodInvoker)(() =>
                {
                    teamChanger.Close();
                }));
            }
        }
        #endregion

        #region ConnectionHandler
        /// <summary>
        /// Opens a new ConnectionForm
        /// </summary>
        public void OpenConnectionForm()
        {
            string[,] connections = databaseHandler.ReadConnections();

            //Take the standard connection if no connection is saved.
            if (connections.Length == 0)
            {
                connections = new string[1, 3];
                connections[0, 0] = Client.LocalIP;
                connections[0, 1] = 7070 + "";
                connections[0, 2] = "";
            }
            ConnectionForm connectionPicker = new ConnectionForm(connections);
            connectionPicker.SettingsPicked += SettingsPicked;
            connectionPicker.ShowDialog();
        }

        /// <summary>
        /// Raises the CreateClient()-method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void SettingsPicked(object sender, ConnectionSettingsEventArgs e)
        {
            ConnectionForm connectionSettings = (ConnectionForm)sender;
            connectionSettings.Dispose();
            CreateClient(new string[] { e.IP, e.Port });
        }

        /// <summary>
        /// Connects to localhost, if the user is the host.
        /// Otherwise the user has to type in an ip-address and a Port.
        /// 
        /// connection[0] --> ip
        /// connection[1] --> port
        /// </summary>
        private void CreateClient(string[] connection)
        {
            IPAddress address;
            Regex portPattern = new Regex(@"^([0-9]{0,4})$");

            //true if the input - the ip address and the port - is valid.
            if (IPAddress.TryParse(connection[0], out address) && portPattern.IsMatch(connection[1]))
            {
                CurrentClient = new Client(address, Int32.Parse(connection[1]));
                CurrentClient.ConnectionFailed += CurrentClient_ConnectionFailed;
                CurrentClient.ConnectionIsOpen += CurrentClient_ConnectionIsOpen;
                CurrentClient.ConnectionProblem += CurrentClient_ConnectionProblem;
                CurrentClient.ServerShutdown += CurrentClient_ServerShutdown;
                CurrentClient.Connect();
            }
            else
            {
                HandleInvalidInput(connection[0] + ":" + connection[1]);
            }
        }

        /// <summary>
        /// Closes and removes all controls which belongs to the multiplayer.
        /// Raises ConnectionClosed()-event at the end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void CurrentClient_ConnectionProblem(object sender, EventArgs e)
        {
            if (CurrentLobby != null && CurrentLobby.Visible)
            {
                CloseTeamswitcher();

                CurrentLobby.ForceClose = true;
                CurrentLobby.Invoke((MethodInvoker)(() =>
                {
                    CurrentLobby.Close();
                }));
            }

            if (CurrentChatForm != null && CurrentChatForm.IsHandleCreated)
            {
                CurrentChatForm.Invoke((MethodInvoker) (() =>
                {
                    CurrentChatForm.Close();
                }));
            }

            if (ConnectionClosed != null)
            {
                ConnectionClosed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Opens a new multiplayer lobby form and asks the server for the user's name.
        /// Adds the client events as well.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void CurrentClient_ConnectionIsOpen(object sender, EventArgs e)
        {
            CurrentClient.NewChatMessage += ChatMessageReceived;
            CurrentClient.NewServerMessage += ServerMessageReceived;
            CurrentClient.UserNameChanged += UserNameChanged;
            CurrentClient.ChatUserListReceived += ChatUserListReceived;
            CurrentClient.NewChatCreated += NewChatCreated;
            CurrentClient.LoginNameAssigned += LoginNameAssigned;
            CurrentClient.TeamListReceived += CurrentClient_TeamListReceived;
            CurrentClient.UserChangedTeam += CurrentClient_UserChangedTeam;
            CurrentClient.FieldSettingsReceived += CurrentClient_FieldSettingsReceived;
            CurrentClient.GameCreated += CurrentClient_GameCreated;
            CurrentClient.UserIsReady += CurrentClient_UserIsReady;
            CurrentClient.CompleteUserListReceived += CurrentClient_CompleteUserListReceived;
            CurrentClient.ActionReceived += CurrentClient_ActionReceived;
            CurrentClient.RoundActionsReceived += CurrentClient_RoundActionsReceived;
            CurrentClient.RoundImageReceived += CurrentClient_RoundImageReceived;
            CurrentClient.GameImageReceived += CurrentClient_GameImageReceived;
            CurrentClient.GoalImageReceived += CurrentClient_GoalImageReceived;
            CurrentClient.MultiplayerStatusReceived += CurrentClient_MultiplayerStatusReceived;
            CurrentClient.UseSavegameChanged += CurrentClient_UseSavegameChanged;
            CurrentClient.ChangeoverReceived += CurrentClient_ChangeoverReceived;
            CurrentClient.GameEndReceived += CurrentClient_GameEndReceived;

            //LoginSucceeded(this, EventArgs.Empty);
            CurrentClient.Send("status<?>:");
        }

        /// <summary>
        /// Gets raised when the connection to the server was closed successfull due to a server shutdown.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_ServerShutdown(object sender, EventArgs e)
        {
            if (CurrentLobby != null && !CurrentLobby.IsDisposed && CurrentLobby.IsHandleCreated)
            {
                CurrentLobby.Invoke((MethodInvoker)(() =>
                {
                    CurrentLobby.ForceClose = true;
                    CurrentLobby.Close();
                }));
            }

            if (CurrentChatForm != null && !CurrentChatForm.IsDisposed && CurrentChatForm.IsHandleCreated)
            {
                CurrentChatForm.Invoke((MethodInvoker)(() =>
                {
                    CurrentChatForm.Close();
                }));
            }

            MessageBox.Show("Server shutdown.", "Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ConnectionClosed(this, EventArgs.Empty);
        }

        /// <summary>
        /// Important for the login. If the received multiplayer status is MultiplayerStatus.Game, the client is not allowed to join the server,
        /// because the game has started already.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_MultiplayerStatusReceived(object sender, GameStatusEventArgs e)
        {
            if (e.Status == MultiplayerStatus.Lobby)
            {
                LoginSucceeded(this, EventArgs.Empty);
            }
            else
            {
                CurrentClient.IsConnected = false;
                CurrentClient.Send("exit");
                MessageBoxButtons button = MessageBoxButtons.YesNo;
                MessageBoxIcon icon = MessageBoxIcon.Error;
                DialogResult rs = MessageBox.Show("The game has started already.\n" +
                    "Would you like to connect to another server?", "Error!", button, icon);

                if (rs == System.Windows.Forms.DialogResult.Yes)
                {
                    OpenConnectionForm();
                }
            }
        }

        /// <summary>
        /// Tries to create a CustomTabControl. If the assembly is not available, a FileNotFoundException will get thrown.
        /// The program will use a normal TabControl then.
        /// Opens a new MultiplayerLobbyForm as well.
        /// Asks the server for the login name and requests the current Settings.
        /// </summary>
        public void JoinLobby(bool isLogin)
        {
            //create a new chat if the chat does not exist
            if (CurrentChats == null)
            {
                if (File.Exists("CustomTabControl.dll"))
                {
                    CreateChatTabControl();
                }
                else
                {
                    MessageBox.Show("CustomTabControl.dll is missing from your computer.\nThe program will use the standard TabControl.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CurrentChats = new TabControl();
                }
            }

            CurrentLobby = new MultiplayerLobbyForm(CurrentChats);
            CurrentLobby.BringToFront();

            CurrentLobby.LeftLobby += Lobby_LeftLobby;
            CurrentLobby.ShowTeamsClicked += Lobby_ShowTeamsClicked;
            CurrentLobby.GameSettingsChanged += Lobby_GameSettingsChanged;
            CurrentLobby.StartGameRequest += Lobby_StartGameRequest;
            CurrentLobby.SaveFileUploadRequest += Lobby_SaveFileUploadRequest;
            CurrentLobby.UseSavefileBox.CheckedChanged += UseSavefileBox_CheckedChanged;

            if (isLogin)
            {
                RequestName();
            }
            else
            {
                RequestSettings();
            }
        }

        /// <summary>
        /// Creates the CustomTabControl.
        /// </summary>
        /// <returns></returns>
        private void CreateChatTabControl()
        {
            CustomTabControl customTabControl = new CustomTabControl();
            customTabControl.DisplayStyle = TabStyle.VS2010;
            customTabControl.TabClosing += customTabControl_TabClosing;
            CurrentChats = customTabControl;
        }

        /// <summary>
        /// Shows a message box with a message that the client was unable to connect to the server
        /// and asks whether the user wants to connect to another server.
        /// </summary>
        private void HandleFailedConnection(string serverString)
        {
            MessageBoxButtons button = MessageBoxButtons.YesNo;
            MessageBoxIcon icon = MessageBoxIcon.Error;

            DialogResult rs = MessageBox.Show("Unable to connect to the server at " + serverString + ".\n" +
                "Would you like to try another server?", "Error!", button, icon);

            if (rs == System.Windows.Forms.DialogResult.Yes)
            {
                OpenConnectionForm();
            }
        }

        /// <summary>
        /// Shows a message box with a message that the user entered something invalid
        /// and asks whether the user wants to try it again.
        /// </summary>
        /// <param name="invalidInput"></param>
        private void HandleInvalidInput(string invalidInput)
        {
            MessageBoxButtons button = MessageBoxButtons.YesNo;
            MessageBoxIcon icon = MessageBoxIcon.Error;

            DialogResult rs = MessageBox.Show(invalidInput + " is an invalid IP address/port.\n" +
                "Would you like to try it again?", "Error!", button, icon);

            if (rs == System.Windows.Forms.DialogResult.Yes)
            {
                OpenConnectionForm();
            }
        }

        /// <summary>
        /// Raises the HandleFailedConnection()-method
        /// </summary>
        /// <param name="sender">parameter for HandleFailedConnection()-method</param>
        /// <param name="message"></param>
        void CurrentClient_ConnectionFailed(object sender, EventArgs e)
        {
            Client client = (Client)sender;
            HandleFailedConnection(client.ServerAddress + ":" + client.Port);
        }

        /// <summary>
        /// Raises the CloseConnection()-method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void Lobby_LeftLobby(object sender, EventArgs e)
        {
            CloseTeamswitcher();
            CloseConnection();
        }

        /// <summary>
        /// Sends an "exit"-message to the server and closes the client
        /// </summary>
        public void CloseConnection()
        {
            if (CurrentClient != null && CurrentClient.IsConnected)
            {
                CurrentClient.Send("exit");
                CurrentClient.IsConnected = false;
            }

            if (CurrentLobby != null && CurrentLobby.IsHandleCreated)
            {
                CurrentLobby.ForceClose = true;
                CurrentLobby.Invoke((MethodInvoker) (() =>
                {
                    CurrentLobby.Close();
                }));
            }

            if (ConnectionClosed != null)
            {
                ConnectionClosed(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Login
        /// <summary>
        /// Asks the server for the login name.
        /// </summary>
        private void RequestName()
        {
            if (!databaseHandler.DoesConnectionExist(CurrentClient.ServerAddress))
            {
                databaseHandler.InsertConnection(CurrentClient.ServerAddress, CurrentClient.Port, "", DateTime.Now);
                CurrentClient.Send("name<?>:");
            }
            else
            {
                databaseHandler.UpdatePort(CurrentClient.ServerAddress, CurrentClient.Port);
                databaseHandler.UpdateLastLoginDate(CurrentClient.ServerAddress, DateTime.Now);
                CurrentClient.Send("name<?>:" + databaseHandler.GetUsername(CurrentClient.ServerAddress));
            }
        }

        /// <summary>
        /// Updates the own name in the database and adds the general chat page to the chat if it is not added yet.
        /// If it is added, this is a normal change of the own username.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LoginNameAssigned(object sender, UsernameChangeEventArgs e)
        {
            string savedName = databaseHandler.GetUsername(CurrentClient.ServerAddress);
            if (string.IsNullOrWhiteSpace(savedName) || savedName.Contains(';') || savedName.Contains(':') || savedName.Contains('<') || savedName.Contains('>'))
            {
                databaseHandler.UpdateUsername(CurrentClient.ServerAddress, e.NewName);
            }

            if (CurrentChats.TabPages.Count == 0)
            {
                AddChat(1, null);
            }
            else
            {
                ChangeUsername(e.OldName, e.NewName);
            }
        }

        /// <summary>
        /// Asks the server for the current settings.
        /// </summary>
        private void RequestSettings()
        {
            CurrentClient.Send("settings<?>:");
        }
        #endregion

        #region SettingsHandler
        /// <summary>
        /// Changes whether this multiplayer game session will use a savegame or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_UseSavegameChanged(object sender, BoolEventArgs e)
        {
            if (CurrentLobby.IsHandleCreated)
            {
                CurrentLobby.Invoke((MethodInvoker)(() =>
                {
                    CurrentLobby.UseSavefileBox.Checked = e.Bool;
                }));
            }
        }

        /// <summary>
        /// Saves the received game image.
        /// It's last roundimage will be used if the "Use"-checkbox in the multiplayer lobby is selected.
        /// That will continue the saved multiplayer game.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_GameImageReceived(object sender, GameImageEventArgs e)
        {
            CachedSavefile = e.Image;
            CurrentLobby.CurrentFieldSettingsPanel.IsBlocked = true;

            CurrentLobby.BeginInvoke((MethodInvoker)(() =>
            {
                CurrentLobby.UseSavefileBox.CheckedChanged -= UseSavefileBox_CheckedChanged;
                CurrentLobby.UseSavefileBox.Checked = true;
                CurrentLobby.UseSavefileBox.CheckedChanged += UseSavefileBox_CheckedChanged;
            }));
        }

        /// <summary>
        /// If the UseSavefileBox is checked and there is no CachedSavefile, raise the ChooseSavefile()-method.
        /// Decheck the checkbox if the user does not select any savefile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UseSavefileBox_CheckedChanged(object sender, EventArgs e)
        {
            CurrentLobby.UseSavefileBox.CheckedChanged -= UseSavefileBox_CheckedChanged;

            CheckBox box = (CheckBox)sender;
            if (box.Checked)
            {
                bool chosedFile = ChooseSavefile();
                if (!chosedFile)
                {
                    CurrentLobby.UseSavefileBox.Checked = false;
                }
            }
            else
            {
                CurrentLobby.CurrentFieldSettingsPanel.IsBlocked = false;
            }
            CurrentLobby.UseSavefileBox.CheckedChanged += UseSavefileBox_CheckedChanged;
            CurrentClient.Send("useSavegame<>:" + CurrentLobby.UseSavefileBox.Checked.ToString());
        }

        /// <summary>
        /// Sends the selected save file to the server,
        /// who will refresh the game Settings then.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lobby_SaveFileUploadRequest(object sender, EventArgs e)
        {
            ChooseSavefile();
        }

        /// <summary>
        /// Updates the FieldSettingsPanel of the lobby.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_FieldSettingsReceived(object sender, FieldSettingsEventArgs e)
        {
            if (CurrentLobby != null && CurrentLobby.Visible)
            {
                CurrentLobby.CurrentFieldSettingsPanel.UpdateSettings(e.Settings);
                CurrentLobby.UpdateSettings(e.Settings);
            }
        }

        /// <summary>
        /// Sends the changed game Settings to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lobby_GameSettingsChanged(object sender, FieldSettingsEventArgs e)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            CurrentClient.Send("settings<>:" + serializer.Serialize(e.Settings));
        }

        private bool ChooseSavefile()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "FSG-Files (*.fsg) | *.fsg";
            openDialog.CheckFileExists = true;
            DialogResult result = openDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = openDialog.InitialDirectory + openDialog.FileName;
                ReplayManager manager = new ReplayManager(path);
                manager.Load();
                if (manager.Image != null)
                {
                    manager.Image.RoundImages.RemoveRange(0, manager.Image.RoundImages.Count - 1);

                    CurrentClient.Send("game<>:" + serializer.Serialize(manager.Image));
                    CurrentClient.Send("settings<>:" + serializer.Serialize(manager.Image.Settings));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region GameHandler

        /// <summary>
        /// Sends the server a request to start the game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Lobby_StartGameRequest(object sender, EventArgs e)
        {
            CurrentClient.Send("start<>:");
        }

        /// <summary>
        /// Raises the GameEnded()-event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_GameEndReceived(object sender, EventArgs e)
        {
            GameEnded(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the GameCreated()-event.
        /// This will create a new game, which will be set as MultiplayerGame.
        /// Adds the game's events then.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_GameCreated(object sender, FieldSettingsEventArgs e)
        {
            if (teamChanger != null && teamChanger.IsHandleCreated)
            {
                teamChanger.Close();
            }

            if (CurrentLobby.UseSavefileBox.Checked)
            {
                CachedSavefile.Settings = e.Settings;
                GameImageEventArgs gameArgs = new GameImageEventArgs(CachedSavefile);
                GameContinued(this, gameArgs);
            }
            else
            {
                GameCreated(this, e);
            }
            MultiplayerGame.ActionChosen += MultiplayerGame_ActionChosen;
            MultiplayerGame.GameImageChanged += MultiplayerGame_RoundImageChanged;
            MultiplayerGame.GoalsChanged += MultiplayerGame_GoalsChanged;
            CurrentClient.Send("gameLoaded<>:");
        }

        /// <summary>
        /// Serialized the changed status and sends it to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MultiplayerGame_RoundImageChanged(object sender, RoundImageEventArgs e)
        {
            RoundImage status = e.CurrentImage;

            string statusSerialized = new JavaScriptSerializer().Serialize(status);

            CurrentClient.Send("round<>:" + statusSerialized);
        }

        /// <summary>
        /// Sends the chosen action to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MultiplayerGame_ActionChosen(object sender, ReceivedActionArgs e)
        {
            CachedRoundAction cachedAction = e.HeldActions.ElementAt(0);
            string serializedAction = new JavaScriptSerializer().Serialize(cachedAction);
            CurrentClient.Send("action<>:" + serializedAction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_UserIsReady(object sender, UserListEventArgs e)
        {
            User user = Users.Find(x => x.Username == e.Users.ElementAt(0));
            if (user != null)
            {
                user.IsReady = true;

                //inform the user list
                if (RefreshUserList != null)
                {
                    RefreshUserList(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Informs the server about a new ball location which resulted in a goal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MultiplayerGame_GoalsChanged(object sender, BallEventArgs e)
        {
            string serializedGoalImage = new JavaScriptSerializer().Serialize(e.Image);
            CurrentClient.Send("goal<>:" + serializedGoalImage);
        }

        /// <summary>
        /// Adds the received action to the current game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_ActionReceived(object sender, ReceivedActionArgs e)
        {
            MultiplayerGame.AddCachedAction(e.HeldActions.ElementAt(0));
        }

        /// <summary>
        /// Calculates the next round, creates a new GameStatus-object and sends it back to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_RoundActionsReceived(object sender, ReceivedActionArgs e)
        {
            MultiplayerGame.IsHost = true;
            MultiplayerGame.CalculateRound(e.HeldActions);
        }

        /// <summary>
        /// Refreshes the user list if needed and sets the received game status.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_RoundImageReceived(object sender, RoundImageEventArgs e)
        {
            bool refreshNotNeeded = Users.TrueForAll(x => !x.IsReady);
            if (!refreshNotNeeded)
            {
                Users.ForEach(x => x.IsReady = false);
                RefreshUserList(this, EventArgs.Empty);
            }
            MultiplayerGame.CurrentReplayManager.Image.RoundImages.Add(e.CurrentImage);
            MultiplayerGame.SetRoundImage(e.CurrentImage, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CurrentClient_GoalImageReceived(object sender, BallEventArgs e)
        {
            MultiplayerGame.UseBallEventImage(e.Image);
        }

        void CurrentClient_ChangeoverReceived(object sender, ChangeoverEventArgs e)
        {
            MultiplayerGame.UseChangeoverImage(e.Image);
            MultiplayerGame.CurrentReplayManager.Image.ChangeoverImages.Add(e.Image);
        }
        #endregion
    }
}
