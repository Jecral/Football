using Football.Logic.GameFiles.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic.GameFiles
{
    [XmlRoot]
    public class GameImage
    {
        public GameImage()
        {
            RoundImages = new List<RoundImage>();
            GoalImages = new List<BallEventImage>();
            ChangeoverImages = new List<ChangeoverImage>();
        }

        [XmlElement(Type = typeof(GameSettings))]
        public GameSettings Settings { get; set; }

        //[XmlArray("Rounds")]
        [XmlArrayItem(Type = typeof(RoundImage), ElementName = "Round")]
        public List<RoundImage> RoundImages { get; set; }

        //[XmlArray("Goals")]
        [XmlArrayItem(Type = typeof(BallEventImage), ElementName = "Goal")]
        public List<BallEventImage> GoalImages { get; set; }

        //[XmlArray("ChangeOvers")]
        [XmlArrayItem(Type = typeof(ChangeoverImage), ElementName = "Changeover")]
        public List<ChangeoverImage> ChangeoverImages { get; set; }
    }
}
