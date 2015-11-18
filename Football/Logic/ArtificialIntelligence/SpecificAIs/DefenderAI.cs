using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence.SpecificAIs
{
    class DefenderAI : PlayerAI
    {
        public DefenderAI(AI gameAI)
            : base(gameAI)
        {

        }

        public override PlayerAction ActionWeHaveBall(Player player, GameAction gameAction)
        {
            Goal enemyTeamGoal = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player)).TeamGoal;
            Pathfinding pathfinding = new Pathfinding();
            int horizontalDistanceToBall = Math.Abs(GameAI.GameBall.XCoordinate - player.XCoordinate);

            int ballDistanceToOwnGoal = Math.Abs(GameAI.BallNextRoundLocation().X - player.XCoordinate);

            Point targetPoint = new Point();

            if (gameAction.GameStatus == Status.GoalKick)
            {
                targetPoint.X = player.Location.X + (pathfinding.CalculateHorizontalDirection(player.Location, enemyTeamGoal.MiddlePoint) * (player.MaxSpeed * 4));
            }
            else
            {
                if (ballDistanceToOwnGoal > 7)
                {
                    int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
                    targetPoint.Y = (verticalDistanceToStandardLocation > 4) ? player.StartLocation.Y : player.Location.Y;
                    targetPoint.X = GameAI.BallNextRoundLocation().X + ((pathfinding.CalculateHorizontalDirection(player.Location, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint) * (player.MaxSpeed * 6)));
                }
                else
                {
                    targetPoint = GameAI.PlayersTeam(player).TeamGoal.MiddlePoint;
                    targetPoint.X -= (pathfinding.CalculateHorizontalDirection(player.Location, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint) * 4);
                }
            }

            Rectangle rangeRectangle = RunRoom(GameAI.PlayersTeam(player));
            Point freePoint = SearchFreeTargetPoint(player, rangeRectangle, targetPoint, 2, 0);

            return new PlayerAction(null, freePoint, ActionType.Run);
        }

        public override PlayerAction ActionEnemyHasBall(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();
            int enemyDistanceToOwnGoal; 
            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));

            List<Player> allEnemiesInRunRoom = GameAI.PlayerInRectangle(enemyTeam.Players, RunRoom(GameAI.PlayersTeam(player)), 1);
            Point closestPlayerNextLocation;

            if (allEnemiesInRunRoom.Count == 0)
            {
                closestPlayerNextLocation = GameAI.BallNextRoundLocation();
                enemyDistanceToOwnGoal = Math.Abs(closestPlayerNextLocation.X - GameAI.PlayersTeam(player).TeamGoal.MiddlePoint.X);
            }
            else
            {
                Player closestPlayerInRunRoom = GameAI.NearestPlayersToPoint(allEnemiesInRunRoom, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint, 1, 1, false).ElementAt(0);
                ActionType type;
                closestPlayerNextLocation = pathfinding.PlayerAtSpecificRound(closestPlayerInRunRoom, 1, out type);

                enemyDistanceToOwnGoal = Math.Abs(closestPlayerNextLocation.X - GameAI.PlayersTeam(player).TeamGoal.XCoordinate);
            }


            if (enemyDistanceToOwnGoal > DefenceBallDistance )
            {
                Point targetPoint = player.Location;
                targetPoint.X = closestPlayerNextLocation.X + pathfinding.CalculateHorizontalDirection(player.Location, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint) * DefenceBallDistance;
                int verticalDistanceToStandardLocation = Math.Abs(player.Location.Y - player.StartLocation.Y);
                targetPoint.Y = closestPlayerNextLocation.Y;

                return new PlayerAction(null, targetPoint, ActionType.Run);
            }
            else
            {
                List<Player> nearPlayers = GameAI.NearestPlayersToPoint(enemyTeam.Players, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint, enemyTeam.Players.Count, 1, false);

                Player markPlayer = nearPlayers.ElementAt(0);
                foreach (Player nearPlayer in nearPlayers)
                {
                    if (!nearPlayer.IsMarkedUp)
                    {
                        markPlayer = nearPlayer;
                    }
                }

                markPlayer.IsMarkedUp = true;
                ActionType type;
                Point targetLocation = pathfinding.PlayerAtSpecificRound(markPlayer, 1, out type);
                    

                int horizontalDistanceToPlayer = Math.Abs(GameAI.PlayersTeam(player).TeamGoal.MiddlePoint.X - targetLocation.X);
                if (horizontalDistanceToPlayer > 10)
                {
                    targetLocation.X = player.XCoordinate;
                }
                else
                {
                    targetLocation.X += pathfinding.CalculateHorizontalDirection(markPlayer.Location, GameAI.PlayersTeam(player).TeamGoal.MiddlePoint) * 2;
                }

                return new PlayerAction(null, targetLocation, ActionType.Run);
            }
        }

        public override PlayerAction CornerAttack(Player player)
        {
            Pathfinding pathfinding = new Pathfinding();

            Team enemyTeam = GameAI.GetEnemyTeam(GameAI.PlayersTeam(player));
            Point targetPoint = player.StartLocation;
            targetPoint.X = enemyTeam.TeamGoal.XCoordinate + pathfinding.CalculateHorizontalDirection(enemyTeam.TeamGoal.MiddlePoint, new Point(12, 12)) * DefenceBallDistance;

            return new PlayerAction(null, targetPoint, ActionType.Run);
        }
    }
}
