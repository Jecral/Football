using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Multiplayer.MessageArguments
{
    public enum ServerMessageType
    {
        Join,
        Leave
    }

    class ServerMessageArgs : EventArgs
    {
        public ServerMessageArgs(DateTime time, string userName, ServerMessageType type, string content, int teamId)
        {
            Time = time;
            UserName = userName;
            Type = type;
            Content = content;
            TeamId = teamId;
        }

        public DateTime Time { get; set; }
        public ServerMessageType Type { get; set; }
        public string UserName { get; set; }
        public string Content { get; set; }
        public int TeamId { get; set; }
    }
}
