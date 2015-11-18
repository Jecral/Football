using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class GoalkeeperAI : PlayerAI
    {
        public GoalkeeperAI(AI gameAI)
            : base(gameAI)
        {
        }

        /* The keeper will shoot to the unmarked player who is the closest to the enemy's goal. */
        public override PlayerAction ActionWithBall(Player player, GameAction gameAction)
        {
            if (RoundsWithBall < 5)
            {
                RoundsWithBall++;
                return null;
            }
            else
            {
                /* Sets the middle point of the enemy's goal as the target point*/
                Point targetPoint = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal.MiddlePoint;
                /* Searches the nearest unmarked player to the target point */
                Player unmarkedPlayer = SearchPassPlayers(player).ElementAt(0);

                return new PlayerAction(unmarkedPlayer, unmarkedPlayer.Location, ActionType.Pass);
            }
        }

        /* The keeper will move to a new position depending on the ball's position and direction. */
        public override PlayerAction ActionWithoutBall(Player player, GameAction gameAction)
        {
            Pathfinding pathfinding = new Pathfinding();
            Point ballTarget = GameAI.BallNextRoundLocation();
            bool onOwnSide = GameAI.IsPointOnOwnSide(GameAI.PlayersTeam(player), ballTarget);
            if (onOwnSide)
            {
                if (GameAI.GameBall.IsInShootState)
                {
                    Point ballTargetGridLocation = pathfinding.GetGridLocation(GameAI.GameBall.ExactTargetLocation);

                    Goal ownGoal = GameAI.PlayersTeam(player).TeamGoal;

                    Point? pointBeforeGoalEntry = GameAI.PointBeforeRectangleEntry(ownGoal.GoalRectangle);

                    //true if the ball will go through the goal
                    if (pointBeforeGoalEntry.HasValue)
                    {
                        return GoalKeeperShootDefense(player, pointBeforeGoalEntry.Value);
                    }
                }
                else
                {
                    return GoalKeeperPassDefense(player, ballTarget);
                }
            }

            return null;
        }

        public PlayerAction GoalKeeperPassDefense(Player player, Point ballTarget)
        {
            Point nearestEnemyNextLocation = GameAI.NextPointNearestPlayerInRunRoom(GameAI.PlayersTeam(player).TeamGoal.Location, RunRoom(GameAI.PlayersTeam(player)), GameAI.PlayersTeam(player), 1);
            int distanceToOwnGoal = Math.Abs(nearestEnemyNextLocation.X - GameAI.PlayersTeam(player).TeamGoal.Location.X);
            Point newTarget = new Point();

            //the keeper will only move within the own goal's y-range
            if (ballTarget.Y < GameAI.PlayersTeam(player).TeamGoal.Location.Y)
            {
                newTarget.Y = GameAI.PlayersTeam(player).TeamGoal.YCoordinate;
            }
            else if (ballTarget.Y > GameAI.PlayersTeam(player).TeamGoal.Location.Y + GameAI.PlayersTeam(player).TeamGoal.Height)
            {
                newTarget.Y = GameAI.PlayersTeam(player).TeamGoal.Location.Y + GameAI.PlayersTeam(player).TeamGoal.Height - 1;
            }
            else
            {
                newTarget.Y = ballTarget.Y;
            }

            newTarget.X = AdjustXTargetPoint(GameAI.PlayersTeam(player), true, distanceToOwnGoal);

            return new PlayerAction(null, newTarget, ActionType.Run);
        }

        /// <summary>
        /// Runs to the last point where the ball will be before he enters the goal.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="locationBeforeGoalEntry"></param>
        /// <returns></returns>
        public PlayerAction GoalKeeperShootDefense(Player player, Point locationBeforeGoalEntry)
        {
             return new PlayerAction(null, locationBeforeGoalEntry, ActionType.Run);
        }

        private int AdjustXTargetPoint(Team playerTeam, bool onOwnSide, int distanceToOwnGoal)
        {
            Pathfinding pathfinding = new Pathfinding();

            //the keeper will run in the direction of the ball - only if the ball will be on the own side in the next round and is controlled by the enemy.
            int keeperDistanceToGoal;
            Goal enemyGoal = GameAI.GetEnemyTeam(playerTeam).TeamGoal;

            if (distanceToOwnGoal > 0)
            {
                keeperDistanceToGoal = playerTeam.TeamGoal.XCoordinate + pathfinding.CalculateHorizontalDirection(playerTeam.TeamGoal.Location, enemyGoal.Location) * (1 + distanceToOwnGoal / 5);
            }
            else
            {
                keeperDistanceToGoal = playerTeam.TeamGoal.XCoordinate + pathfinding.CalculateHorizontalDirection(playerTeam.TeamGoal.Location, enemyGoal.Location) * 1;
            }

            return keeperDistanceToGoal;
        }
    }
}
