using Football.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class FieldSettingsEventArgs : EventArgs
    {
        public FieldSettingsEventArgs(GameSettings settings)
        {
            Settings = settings;
        }

        public GameSettings Settings { get; private set; }
    }
}
