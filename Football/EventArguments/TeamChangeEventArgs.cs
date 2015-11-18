using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class TeamChangeEventArgs : EventArgs
    {
        public TeamChangeEventArgs(string username, int oldTeamId, int newTeamId)
        {
            Username = username;
            OldTeamId = oldTeamId;
            NewTeamId = newTeamId;
        }

        public string Username { get; private set; }
        public int OldTeamId { get; private set; }
        public int NewTeamId { get; private set; }
    }
}
