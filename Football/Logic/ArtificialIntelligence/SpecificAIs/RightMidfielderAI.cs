using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class RightMidfielderAI : MidfielderAI
    {
        public RightMidfielderAI(AI gameAI) : base(gameAI)
        {
            LeftSideRunRoom = new Rectangle(new Point(10, GameAI.FieldCell.GetLength(1) /3 * 2), new Size(GameAI.FieldCell.GetLength(0) - 13, GameAI.FieldCell.GetLength(1) / 3));
            RightSideRunRoom = new Rectangle(new Point(1, 1), new Size(GameAI.FieldCell.GetLength(0) - 13, GameAI.FieldCell.GetLength(1) / 3));

            DefenceBallDistance = 0;
            DistanceToEnemy = 7;
        }

        public override Rectangle LeftSideRunRoom { get; set; }
        public override Rectangle RightSideRunRoom { get; set; }
        public override int DefenceBallDistance { get; set; }
        public override int DistanceToEnemy { get; set; }
    }
}
