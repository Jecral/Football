using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class RoundJumpEventArgs : EventArgs
    {
        public RoundJumpEventArgs(int targetRound)
        {
            this.TargetRound = targetRound;
        }

        public int TargetRound { get; set; }
    }
}
