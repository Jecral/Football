using Football.Logic.GameFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class GameImageEventArgs : EventArgs
    {
        public GameImageEventArgs(GameImage image)
        {
            Image = image;
        }

        public GameImage Image { get; private set; }
    }
}
