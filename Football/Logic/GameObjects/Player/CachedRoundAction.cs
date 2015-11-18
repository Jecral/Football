using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.GameObjects.Player
{
    public class CachedRoundAction
    {
        /// <summary>
        /// For serialisation.
        /// </summary>
        public CachedRoundAction()
        {

        }

        public CachedRoundAction(CachedPlayerAction heldAction, CachedPlayer heldPlayer)
        {
            HeldAction = heldAction;
            HeldPlayer = heldPlayer;
        }

        public CachedPlayerAction HeldAction { get; set; }
        public CachedPlayer HeldPlayer { get; set; } //the player who will execute the action
        public bool ClearQueue { get; set; }
    }
}
