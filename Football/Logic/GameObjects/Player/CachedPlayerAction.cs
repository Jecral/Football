using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Football.Logic.GameObjects.Player
{
    [XmlRoot("PlayerAction")]
    public class CachedPlayerAction
    {
        /// <summary>
        /// For serialisation.
        /// </summary>
        public CachedPlayerAction()
        {

        }

        public CachedPlayerAction(CachedPlayer affectedPlayer, Point targetPoint, ActionType actionType, bool isActionToGetBall)
        {
            AffectedPlayer = affectedPlayer;
            TargetPoint = targetPoint;
            Type = actionType;
            IsActionToGetBall = isActionToGetBall;
        }

        public CachedPlayer AffectedPlayer { get; set; }

        [XmlAttribute]
        public ActionType Type { get; set; }

        [XmlElement]
        public Point TargetPoint { get; set; }

        [XmlAttribute]
        public bool IsActionToGetBall { get; set; }
    }
}
