using Football.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    public enum ListType
    {
        ChatRoom,
        Team
    }

    class UserListEventArgs : EventArgs
    {
        public UserListEventArgs(string[] users, int teamNumber, ListType type)
        {
            Users = users;
            Id = teamNumber;
            Type = type;
        }

        public UserListEventArgs(List<User> users, int teamNumber, ListType type)
        {
            UserList = users;
            Id = teamNumber;
            Type = type;
        }

        public List<User> UserList { get; private set; }
        public string[] Users { get; private set; }
        public int Id { get; private set; }
        public ListType Type { get; private set; }
    }
}
