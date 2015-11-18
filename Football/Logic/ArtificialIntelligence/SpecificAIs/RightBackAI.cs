using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class RightBackAI : DefenderAI
    {
        public RightBackAI(AI gameAI) : base(gameAI)
        {
            LeftSideRunRoom = new Rectangle(new Point(0, (GameAI.FieldCell.GetLength(1) / 3) * 2), new Size(GameAI.FieldCell.GetLength(0), GameAI.FieldCell.GetLength(1) / 3));
            RightSideRunRoom = new Rectangle(new Point(GameAI.FieldCell.GetLength(0) /2, 0), new Size(GameAI.FieldCell.GetLength(0) /2, GameAI.FieldCell.GetLength(1) / 3));
            DefenceBallDistance = 10;
            DistanceToEnemy = 3;
        }

        public override Rectangle LeftSideRunRoom { get; set; }
        public override Rectangle RightSideRunRoom { get; set; }
        public override int DefenceBallDistance { get; set; }
        public override int DistanceToEnemy { get; set; }
    }
}
