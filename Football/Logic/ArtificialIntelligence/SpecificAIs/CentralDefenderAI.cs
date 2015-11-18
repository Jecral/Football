using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class CentralDefenderAI : DefenderAI
    {
        public CentralDefenderAI(AI gameAI) : base(gameAI)
        {
            LeftSideRunRoom = new Rectangle(new Point(0, 10), new Size(GameAI.FieldCell.GetLength(0) / 2, GameAI.FieldCell.GetLength(1) - 20));
            RightSideRunRoom = new Rectangle(new Point(GameAI.FieldCell.GetLength(0) / 2, 10), new Size(GameAI.FieldCell.GetLength(0) / 2, GameAI.FieldCell.GetLength(1) - 20));
            DefenceBallDistance = 15;
        }

        public override Rectangle LeftSideRunRoom { get; set; }
        public override Rectangle RightSideRunRoom { get; set; }
        public override int DefenceBallDistance { get; set; }
    }
}
