using Football.EventArguments;
using Football.Logic;
using Football.Logic.GameFiles;
using Football.Logic.GameFiles.Images;
using Football.Logic.GameObjects.Player;
using Football.Multiplayer;
using Football.Multiplayer.MessageArguments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Football.Multiplayer.Network
{
    class Client
    {
        public Client(IPAddress serverAddress, int port)
        {
            this.ServerAddress = serverAddress;
            this.Port = port;

            serializer = new JavaScriptSerializer();
        }

        #region Properties
        public static string LocalIP
        {
            get
            {
                IPHostEntry host;
                string localIP = "";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                    }
                }
                return localIP;
            }
        }
        public IPAddress ServerAddress;
        public int Port;
        private TcpClient client;
        private Thread listenerThread;
        private NetworkStream networkStream;
        private BinaryReader binaryReader;
        private BinaryWriter binaryWriter;

        private JavaScriptSerializer serializer;

        public event EventHandler ConnectionFailed;
        public event EventHandler ConnectionIsOpen;
        public event EventHandler ConnectionProblem;
        public event EventHandler ServerShutdown;

        public event EventHandler<ChatMessageArgs> NewChatMessage;
        public event EventHandler<ServerMessageArgs> NewServerMessage;
        public event EventHandler<UsernameChangeEventArgs> UserNameChanged;
        public event EventHandler<UserListEventArgs> ChatUserListReceived;
        public event EventHandler<UserListEventArgs> NewChatCreated;
        public event EventHandler<UsernameChangeEventArgs> LoginNameAssigned;
        public event EventHandler<UserListEventArgs> TeamListReceived;
        public event EventHandler<TeamChangeEventArgs> UserChangedTeam;
        public event EventHandler<FieldSettingsEventArgs> FieldSettingsReceived;
        public event EventHandler<FieldSettingsEventArgs> GameCreated;
        public event EventHandler<UserListEventArgs> UserIsReady;
        public event EventHandler<UserListEventArgs> CompleteUserListReceived;
        public event EventHandler<ReceivedActionArgs> ActionReceived;
        public event EventHandler<ReceivedActionArgs> RoundActionsReceived;
        public event EventHandler<RoundImageEventArgs> RoundImageReceived;
        public event EventHandler<GameImageEventArgs> GameImageReceived;
        public event EventHandler<BallEventArgs> GoalImageReceived;
        public event EventHandler<GameStatusEventArgs> MultiplayerStatusReceived;
        public event EventHandler<BoolEventArgs> UseSavegameChanged;
        public event EventHandler<ChangeoverEventArgs> ChangeoverReceived;
        public event EventHandler GameEndReceived;

        public string ClientName { get; set; }
        public bool IsConnected { get; set; }
        #endregion

        public void Connect()
        {
            try
            {
                if (TryConnection())
                {
                    client = new TcpClient();
                    client.Connect(new IPEndPoint(ServerAddress, Port));
                    IsConnected = true;

                    listenerThread = new Thread(new ThreadStart(Listen));
                    listenerThread.IsBackground = false;
                    listenerThread.Name = "Client Listener Thread";
                    listenerThread.Start();
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            catch (Exception se)
            {
                Console.WriteLine("Failed to connect to server at {0}:{1} - {2}.", ServerAddress, Port, se.Message);
                ConnectionFailed(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sends a ping to the server and returns whether is was successfull or not.
        /// </summary>
        /// <returns></returns>
        public bool TryPing()
        {
            Ping sender = new Ping();
            PingReply result = sender.Send(ServerAddress);

            return result.Status == IPStatus.Success;
        }

        public bool TryConnection()
        {
            using (TcpClient tcp = new TcpClient())
            {
                IAsyncResult asyncResult = tcp.BeginConnect(ServerAddress, Port, null, null);
                if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2), false))
                {
                    return false;
                }

                tcp.EndConnect(asyncResult);
            }
            return true;
        }

        private void Listen()
        {
            using (networkStream = client.GetStream())
            {
                binaryReader = new BinaryReader(networkStream);
                binaryWriter = new BinaryWriter(networkStream);

                ConnectionIsOpen(this, EventArgs.Empty);

                try
                {
                    string message;
                    while (IsConnected && (message = binaryReader.ReadString()) != null)
                    {
                        //Console.WriteLine(Program.TimeString + " from server: " + message);
                        if (message == "exit")
                        {
                            IsConnected = false;
                        }
                        else if (message == "shutdown")
                        {
                            IsConnected = false;
                            ServerShutdown(this, EventArgs.Empty);
                        }
                        else if(ValidateMessage(message))
                        {
                            Interpret(message);
                        }
                    }
                }
                catch (Exception e)
                {
                    ConnectionProblem(e.Message, EventArgs.Empty);
                    IsConnected = false;
                    Console.WriteLine("[{0}] {1}: {2}", e.GetType(), e.Message, client.Client.RemoteEndPoint);

                    MessageBoxButtons button = MessageBoxButtons.OK;
                    MessageBoxIcon icon = MessageBoxIcon.Error;

                    MessageBox.Show("The client lost the connection to the server.\nPlease have a look at the log file (" + Program.LogfilePath + ").", "Error", button, icon);
                    File.AppendAllText(Program.LogfilePath, Program.TimeString + " [" + e.GetType() + "]: " + e.Message + " Source: " + e.Source + "\nInnerException: " + e.InnerException + "\nHelpLink: " + e.HelpLink + "\nStackTrace: " + e.StackTrace + "\n\n");
                }

                binaryWriter.Close();
                binaryWriter.Dispose();
                binaryReader.Close();
                binaryReader.Dispose();
            }

            client.Close();
        }

        /// <summary>
        /// Sends the message through a binary writer to the server.
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            try
            {
                if (IsConnected)
                {
                    binaryWriter.Write(message);
                    binaryWriter.Flush();
                }
            }
            catch (Exception e)
            {
                IsConnected = false;
                ConnectionProblem(e.Message, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Tests whether the message which the client received from the server is a valid message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool ValidateMessage(string message)
        {
            Regex regex = new Regex("^(.*<.*>:.*)$");
            return regex.IsMatch(message);
        }

        /// <summary>
        /// Interprets the mesage and raises the suitable event.
        /// 
        /// Syntax:
        /// 
        /// chat<chatId>:Player1;Player2;Player3 --> Raises NewChatCreated()-event with the received users as the UserListEventArgs.
        /// That will create a new chatroom with these users.
        /// 
        /// userlist<chatId>:serialized List<User> --> The userlist of chatroom with this chat id.
        /// info<HH:mm:ss,Username,ServermessageType>: --> The information that someone has joined/left the server or changed the team.
        /// message<chatId,Username,HH:mm:ss>:message content --> A chat message from an user in a specific chatroom
        /// name<>:name --> Raises LoginNameAssigned()-event if the name equals the own name - raises UserNameChanged()-event if not.
        /// settings<>:serialized GameSettings--> Raises HandleReceivedSettings()-method with the serialized GameSetting as the parameter.
        /// start<>:serialized GameSettings  --> Raises the GameCreated()-event with the received settings.
        /// change<>:serialized ChangeOverImage --> Raises the ChangeoverReceived()-event.
        /// useSavegame<>:bool --> Raises UseSavegameChanged()-event with the received bool as the BoolEventArgs.
        /// end<>: --> Raises GameEnded()-event.
        /// action<>:serialized CachedRoundAction --> Raises ActionReceived()-event with the received CachedRoundAction as the ReceivedActionArgs.
        /// team<username, oldTeamId, newTeamId>: --> The information that someone switched the team. Raises HandleTeamEvent()-method.
        /// </summary>
        /// <param name="message">The message from the server</param>
        private void Interpret(string message)
        {
            int paramsStart = message.IndexOf('<');
            int paramsEnd = message.IndexOf('>');

            string paramsString = message.Substring(paramsStart + 1, paramsEnd - (paramsStart + 1));
            string[] parameters = paramsString.Split(',');
            message = message.Remove(paramsStart, paramsEnd - (paramsStart - 1)); //type:content

            string[] splitted = message.Split(':');

            string type = splitted[0];
            string content = message.Substring(message.IndexOf(':') + 1, (message.Length - (message.IndexOf(':') + 1)));

            switch (type)
            {
                case "chat":
                    string[] names = content.Split(';');
                    UserListEventArgs args = new UserListEventArgs(names, Convert.ToInt32(parameters[0]), ListType.ChatRoom);
                    NewChatCreated(this, args);
                    break;

                case "userlist":
                    if (Convert.ToInt32(parameters[0]) == 1)
                    {
                        List<User> users = serializer.Deserialize<List<User>>(content);
                        UserListEventArgs userListArgs = new UserListEventArgs(users, Convert.ToInt32(parameters[0]), ListType.ChatRoom);
                        CompleteUserListReceived(this, userListArgs);
                    }
                    else
                    {
                        string[] usernames = content.Split(';');
                        UserListEventArgs userListArgs = new UserListEventArgs(usernames, Convert.ToInt32(parameters[0]), ListType.ChatRoom);
                        ChatUserListReceived(this, userListArgs);
                    }
                    break;

                case "name":
                    UsernameChangeEventArgs changeArgs;
                    if (parameters[0] == "")
                    {
                        string oldName = ClientName;
                        ClientName = content;
                        changeArgs = new UsernameChangeEventArgs(oldName, content);
                        LoginNameAssigned(this, changeArgs);
                    }
                    else
                    {
                        changeArgs = new UsernameChangeEventArgs(parameters[0], content);
                        UserNameChanged(this, changeArgs);
                        if (parameters[0] == ClientName)
                        {
                            ClientName = content;
                        }
                    }
                    break;

                case "info":
                    HandleInfo(parameters, content);
                    break;

                case "message":
                    HandleNewMessage(parameters, content);
                    break;

                case "action":
                    CachedRoundAction cachedAction = serializer.Deserialize<CachedRoundAction>(content);
                    ReceivedActionArgs actionArgs = new ReceivedActionArgs(cachedAction);
                    ActionReceived(this, actionArgs);
                    break;

                case "team":
                    HandleTeamEvent(parameters, content);
                    break;

                case "settings":
                    HandleReceivedSettings(content);
                    break;

                case "start":
                    GameSettings createSettings = serializer.Deserialize<GameSettings>(content);
                    FieldSettingsEventArgs createSettingsArgs = new FieldSettingsEventArgs(createSettings);
                    GameCreated(this, createSettingsArgs);
                    break;

                case "ready":
                    UserListEventArgs readyArgs = new UserListEventArgs(new string[] { content }, 1, ListType.Team);
                    UserIsReady(this, readyArgs);
                    break;

                case "calculate":
                    List<string> listOfActions = serializer.Deserialize<List<string>>(content);
                    List<CachedRoundAction> actions = new List<CachedRoundAction>();
                    foreach (string serializedAction in listOfActions)
                    {
                        actions.Add(serializer.Deserialize<CachedRoundAction>(serializedAction));
                    }

                    ReceivedActionArgs roundActionsArgs = new ReceivedActionArgs(actions);
                    RoundActionsReceived(this, roundActionsArgs);
                    break;

                case "round":
                    RoundImage status = serializer.Deserialize<RoundImage>(content);
                    RoundImageEventArgs statusArgs = new RoundImageEventArgs(status);
                    RoundImageReceived(this, statusArgs);
                    break;

                case "game":
                    GameImage gameImage = serializer.Deserialize<GameImage>(content);
                    GameImageEventArgs gameArgs = new GameImageEventArgs(gameImage);
                    GameImageReceived(this, gameArgs);
                    break;

                case "goal":
                    BallEventImage image = serializer.Deserialize<BallEventImage>(content);
                    BallEventArgs goalArgs = new BallEventArgs(image);
                    GoalImageReceived(this, goalArgs);
                    break;

                case "status":
                    MultiplayerStatus mpGameStatus = (MultiplayerStatus)Enum.Parse(typeof(MultiplayerStatus), content);
                    GameStatusEventArgs mpStatusArgs = new GameStatusEventArgs(mpGameStatus);
                    MultiplayerStatusReceived(this, mpStatusArgs);
                    break;

                case "useSavegame":
                    BoolEventArgs boolArgs = new BoolEventArgs(Convert.ToBoolean(content));
                    UseSavegameChanged(this, boolArgs);
                    break;

                case "changeover":
                    ChangeoverImage changeImage = serializer.Deserialize<ChangeoverImage>(content);
                    ChangeoverEventArgs changeoverArgs = new ChangeoverEventArgs(changeImage);
                    ChangeoverReceived(this, changeoverArgs);
                    break;

                case "end":
                    GameEndReceived(this, EventArgs.Empty);
                    break;

                default:
                    Console.WriteLine("There is no function for the message: " + message);
                    break;
            }
        }

        /// <summary>
        /// Deserializes the received GameSettings and raises the FieldSettingsReceived()-event with the settings as the argument.
        /// </summary>
        /// <param name="content"></param>
        private void HandleReceivedSettings(string content)
        {
            GameSettings settingsObject = serializer.Deserialize<GameSettings>(content);
            FieldSettingsEventArgs settingsArgs = new FieldSettingsEventArgs(settingsObject);
            FieldSettingsReceived(this, settingsArgs);
        }

        /// <summary>
        /// Raises the TeamListReceived()-event or the UserChangedTeam()-event depending on the received parameters and the content.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="content"></param>
        private void HandleTeamEvent(string[] parameters, string content)
        {
            if (parameters.Length == 1)
            {
                string[] teamNames;
                if (content.Contains(";"))
                {
                    teamNames = content.Split(';');
                }
                else
                {
                    teamNames = new string[] { content };
                }
                UserListEventArgs teamArgs = new UserListEventArgs(teamNames, Convert.ToInt32(parameters[0]), ListType.Team);
                TeamListReceived(this, teamArgs); //parametes[0] --> team number
            }
            else
            {
                string username = parameters[0];
                int oldTeamId = Convert.ToInt32(parameters[1]);
                int newTeamId = Convert.ToInt32(parameters[2]);

                TeamChangeEventArgs teamChangeArgs = new TeamChangeEventArgs(username, oldTeamId, newTeamId);
                UserChangedTeam(this, teamChangeArgs);
            }
        }

        /// <summary>
        /// Informs the user that another user has joined or left the server.
        /// </summary>
        /// <param name="settingsArray"></param>
        private void HandleInfo(string[] parameters, string content)
        {
            DateTime time = DateTime.Parse(parameters[0]);
            ServerMessageType type = (ServerMessageType)Enum.Parse(typeof(ServerMessageType), parameters[2]);
            int teamId = Convert.ToInt32(parameters[3]);

            NewServerMessage(this, new ServerMessageArgs(time, parameters[1], type, content, teamId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingsArray">[0]: the chat id
        /// [1]: the user id
        /// [2]: the time when the server recieved the message</param>
        /// <param name="content"> the content of the message</param>
        private void HandleNewMessage(string[] parameters, string content)
        {
            int chatId = Int32.Parse(parameters[0]);
            string userName = parameters[1];
            DateTime time = DateTime.Parse(parameters[2]);

            NewChatMessage(this, new ChatMessageArgs(chatId, userName, time, content));
        }
    }
}
