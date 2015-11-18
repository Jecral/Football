using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic.GameObjects.Player
{
    [XmlRoot("Player")]
    public class CachedPlayer
    {
        /// <summary>
        /// For serialisation.
        /// </summary>
        public CachedPlayer()
        {

        }

        public CachedPlayer(int playerId, int teamId)
        {
            PlayerId = playerId;
            TeamId = teamId;
        }

        public CachedPlayer(int playerId, int teamId, bool liesOnTheGround)
        {
            PlayerId = playerId;
            TeamId = teamId;
            LiesOnTheGround = liesOnTheGround;
        }

        [XmlAttribute("Id")]
        public int PlayerId { get; set; }

        [XmlAttribute]
        public int TeamId { get; set; }

        [XmlAttribute("OnGround")]
        public bool LiesOnTheGround { get; set; }

        [XmlElement]
        public Point Location { get; set; }

        [XmlAttribute]
        public bool HasBall { get; set; }

        [XmlAttribute("IsPassTarget")]
        public bool IsPassTarget { get; set; }

        [XmlArrayItem(Type = typeof(CachedPlayerAction))]
        public List<CachedPlayerAction> Actions { get; set; }
    }
}
