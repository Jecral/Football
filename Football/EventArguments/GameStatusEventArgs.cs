using Football.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class GameStatusEventArgs : EventArgs
    {
        public GameStatusEventArgs(MultiplayerStatus status)
        {
            Status = status;
        }

        public MultiplayerStatus Status { get; set; }
    }
}
