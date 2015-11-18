using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic.GameObjects
{
    public class CachedBall
    {
        /// <summary>
        /// For serialisation.
        /// </summary>
        public CachedBall()
        {

        }

        public Point ExactTargetLocation { get; set; }
        public Point ExactLocation { get; set; }
        public bool HasTargetPoint { get; set; }
        public bool HasPlayerContact { get; set; }
        public int Speed { get; set; }
        public bool IsInShootState { get; set; }
        public CachedPlayer LastPlayer { get; set; }
    }
}
