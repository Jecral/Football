using Football.Logic.GameFiles.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class BallEventArgs : EventArgs
    {
        public BallEventArgs(BallEventImage image)
        {
            Image = image;
        }

        public BallEventImage Image { get; set; }
    }
}
