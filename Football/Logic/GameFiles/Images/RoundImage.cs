using Football.Logic.GameFiles;
using Football.Logic.GameObjects;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic
{
    [XmlRoot("RoundImage")]
    public class RoundImage
    {
        public RoundImage()
        {

        }

        [XmlAttribute]
        public int Number { get; set; }
        [XmlArrayItem(Type = typeof(CachedPlayer), ElementName = "Player")]
        public List<CachedPlayer> Players { get; set; }
        [XmlElement("GameAction")]
        public CachedGameAction CurrentGameAction { get; set; }
        [XmlElement]
        public Rectangle BlockedRoom { get; set; }
        [XmlElement]
        public CachedBall Ball { get; set; }
    }
}
