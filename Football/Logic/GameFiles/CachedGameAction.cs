using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.GameFiles
{
    public class CachedGameAction
    {
        /// <summary>
        /// For serialisation.
        /// </summary>
        /// <param name="gameStatus"></param>
        /// <param name="teamId"></param>
        /// <param name="actionPoint"></param>
        public CachedGameAction(Status gameStatus, int teamId, Point actionPoint)
        {
            TeamId = teamId;
            ActionPoint = actionPoint;
            GameStatus = gameStatus;
            ThrowRoom = new Rectangle(0, 0, 0, 0);
        }

        public CachedGameAction()
        {

        }

        public int TeamId { get; set; }
        public Point ActionPoint { get; set; }
        public Status GameStatus { get; set; }
        public Rectangle ThrowRoom { get; set; }
    }
}
