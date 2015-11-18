using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class UsernameChangeEventArgs : EventArgs
    {
        public UsernameChangeEventArgs(string oldname, string newname)
        {
            OldName = oldname;
            NewName = newname;
        }

        public string OldName { get; private set; }
        public string NewName { get; private set; }
    }
}
