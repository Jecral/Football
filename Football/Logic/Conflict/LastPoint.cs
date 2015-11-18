using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.Conflicts
{
    class LastPoint
    {
        public LastPoint(Player affectedPlayer, Point targetPoint)
        {
            AffectedPlayer = affectedPlayer;
            TargetPoint = targetPoint;
        }

        public Player AffectedPlayer { get; set; }
        public Point TargetPoint { get; set; }
    }
}
