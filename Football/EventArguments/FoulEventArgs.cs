using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class FoulEventArgs : EventArgs
    {
        public FoulEventArgs(Player foulPlayer, Player fouledPlayer)
        {
            FoulPlayer = foulPlayer;
            FouledPlayer = fouledPlayer;
        }

        public Player FoulPlayer { get; set; }
        public Player FouledPlayer { get; set; }
    }
}
