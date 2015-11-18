using Football.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class RoundImageEventArgs : EventArgs
    {
        public RoundImageEventArgs(RoundImage status)
        {
            CurrentImage = status;
        }

        public RoundImage CurrentImage { get; set; }
    }
}
