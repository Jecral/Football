using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class BoolEventArgs : EventArgs 
    {
        public BoolEventArgs(bool boolean)
        {
            Bool = boolean;
        }

        public bool Bool { get; private set; }
    }
}
