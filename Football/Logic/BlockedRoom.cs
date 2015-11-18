using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic
{
    class BlockedRoom
    {
        public BlockedRoom(Rectangle room, Team team)
        {
            Room = room;
            AllowedTeam = team;
        }

        public Team AllowedTeam { get; set; }
        public Rectangle Room { get; set; }
    }
}
