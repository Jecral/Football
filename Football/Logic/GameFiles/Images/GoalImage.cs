using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic.GameFiles.Images
{
    public class BallEventImage
    {
        /// <summary>
        /// Saves at which exact location and in which round a specifig team achieved a goal.
        /// </summary>
        /// <param name="round"></param>
        /// <param name="teamId"></param>
        /// <param name="exactLocation"></param>
        public BallEventImage(int round, int teamId, Point exactLocation)
        {
            Round = round;
            TeamId = teamId;
            ExactLocation = exactLocation;
        }

        public BallEventImage()
        {

        }

        [XmlAttribute]
        public int Round { get; set; }
        [XmlAttribute]
        public int TeamId { get; set; }
        public Point ExactLocation { get; set; }
    }
}
