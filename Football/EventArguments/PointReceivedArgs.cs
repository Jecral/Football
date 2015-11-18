using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.EventArguments
{
    class PointReceivedArgs : EventArgs
    {
        public PointReceivedArgs(Point point)
        {
            EventPoint = point;
        }

        public PointReceivedArgs(Point gridPoint, Point exactPoint)
        {
            EventPoint = gridPoint;
            ExactEventPoint = exactPoint;
        }

        public Point EventPoint { get; private set; }
        public Point ExactEventPoint { get; private set; }
    }
}
