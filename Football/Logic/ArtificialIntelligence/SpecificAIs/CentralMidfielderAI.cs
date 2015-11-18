using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class CentralMidfielderAI : MidfielderAI
    {
        public CentralMidfielderAI(AI gameAI) : base(gameAI)
        {
            LeftSideRunRoom = new Rectangle(new Point(15, 7), new Size(GameAI.FieldCell.GetLength(0) - 15, GameAI.FieldCell.GetLength(1) - 14));
            RightSideRunRoom = new Rectangle(new Point(0, 7), new Size(GameAI.FieldCell.GetLength(0) - 15, GameAI.FieldCell.GetLength(1) - 14));
            DefenceBallDistance = 5;
        }

        public override Rectangle LeftSideRunRoom { get; set; }
        public override Rectangle RightSideRunRoom { get; set; }
        public override int DefenceBallDistance { get; set; }
    }
}
