using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class ReceivedActionArgs : EventArgs
    {
        public ReceivedActionArgs(CachedRoundAction heldAction)
        {
            HeldActions = new List<CachedRoundAction>();
            HeldActions.Add(heldAction);
        }

        public ReceivedActionArgs(List<CachedRoundAction> heldActions)
        {
            HeldActions = heldActions;
        }

        public List<CachedRoundAction> HeldActions { get; set; }
    }
}
