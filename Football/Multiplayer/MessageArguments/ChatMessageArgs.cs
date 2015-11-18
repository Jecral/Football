
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Multiplayer.MessageArguments
{
    class ChatMessageArgs : EventArgs
    {
        public ChatMessageArgs(int chatId, string userName, DateTime time, string content)
        {
            ChatId = chatId;
            UserName = userName;
            Time = time;
            Content = content;
        }

        public int ChatId { get; set; }
        public string UserName { get; set; }
        public DateTime Time { get; set; }
        public string Content { get; set; }
    }
}
