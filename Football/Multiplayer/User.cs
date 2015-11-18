using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Multiplayer
{
    class User
    {
        public User(string username, int teamId)
        {
            Username = username;
            TeamId = teamId;
        }

        public User()
        {

        }

        public string Username { get; set; }
        public bool IsReady { get; set; }
        public int TeamId { get; set; }
    }
}
