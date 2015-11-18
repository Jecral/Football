using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.Conflicts
{
    class RunConflict
    {
        public RunConflict(Point conflictPoint)
        {
            ConflictPoint = conflictPoint;
            InvolvedPlayers = new List<Player>();
            ConflictType = ActionType.Run;
        }

        public RunConflict(Point conflictPoint, List<Player> involvedPlayers)
        {
            ConflictPoint = conflictPoint;
            InvolvedPlayers = involvedPlayers;
            ConflictType = ActionType.Run;
        }

        public List<Player> InvolvedPlayers { get; set; }
        public Point ConflictPoint { get; set; }
        public ActionType ConflictType { get; set; }


    }
}
