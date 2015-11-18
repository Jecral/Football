using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class MidfielderAI : PlayerAI
    {
        public MidfielderAI(AI gameAI)
            : base(gameAI)
        {
            AttackLine = 10;
        }

        public override PlayerAction CornerDefense(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point targetPoint = player.StartLocation;
            targetPoint.X = GameAI.PlayersTeam(player).TeamGoal.MiddlePoint.X + pathfinding.CalculateHorizontalDirection(GameAI.PlayersTeam(player).TeamGoal.MiddlePoint, new Point(12, 12)) * 7;

            return new PlayerAction(null, targetPoint, ActionType.Run);
        }
    }
}
