
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.GameObjects.Player
{
    class RoundAction
    {
        public RoundAction(PlayerAction heldAction, Player heldPlayer)
        {
            HeldAction = heldAction;
            HeldPlayer = heldPlayer;
        }

        public PlayerAction HeldAction { get; set; }
        public Player HeldPlayer { get; set; } //the player who will execute the action
        public bool ClearQueue { get; set; }
    }
}
