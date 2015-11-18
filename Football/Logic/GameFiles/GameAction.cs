using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic
{
    public enum Status
    {
        Normal,
        FreeKick,
        ThrowIn,
        CornerKick,
        SpotKick,
        GoalKick,
        KickOff
    };

    class GameAction
    {
        public GameAction(Status gameStatus, Team actionTeam, Point actionPoint)
        {
            ActionTeam = actionTeam;
            ActionPoint = actionPoint;
            GameStatus = gameStatus;
            ThrowRoom = new Rectangle(0, 0, 0, 0);
        }

        public GameAction()
        {

        }

        public Team ActionTeam { get; set; }
        public Point ActionPoint { get; set; }
        public Status GameStatus { get; set; }
        public Rectangle ThrowRoom { get; set; }
    }
}
