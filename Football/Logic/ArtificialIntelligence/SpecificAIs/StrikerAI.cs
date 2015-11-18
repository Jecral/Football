using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class StrikerAI : PlayerAI
    {
        public StrikerAI(AI gameAI) : base(gameAI)
        {
            DefenceBallDistance = -5;
        }
    }
}
