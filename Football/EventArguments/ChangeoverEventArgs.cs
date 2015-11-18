using Football.Logic.GameFiles.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class ChangeoverEventArgs : EventArgs
    {
        /// <summary>
        /// Saves which team owns which goal.
        /// </summary>
        public ChangeoverEventArgs(ChangeoverImage image)
        {
            Image = image;
        }

        public ChangeoverImage Image { get; set; }
    }
}
