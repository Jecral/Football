using Football.Logic.ArtificialIntelligence.SpecificAIs;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic.ArtificialIntelligence
{
    class AI
    {
        public AI(Team firstTeam, Team secondTeam, Ball gameBall, Rectangle[,] fieldCell)
        {
            this.FirstTeam = firstTeam;
            this.SecondTeam = secondTeam;
            this.GameBall = gameBall;
            this.FieldCell = fieldCell;

            List<Player> allPlayers = new List<Player>();
            allPlayers.AddRange(firstTeam.Players);
            allPlayers.AddRange(secondTeam.Players);
            this.AllPlayers = allPlayers.ToArray();

            goalkeeperAI = new GoalkeeperAI(this);
            strikerAI = new StrikerAI(this);
            centralDefenderAI = new CentralDefenderAI(this);
            centralMidfielderAI = new CentralMidfielderAI(this);
            leftBackAI = new LeftBackAI(this);
            leftMidfielderAI = new LeftMidfielderAI(this);
            rightBackAI = new RightBackAI(this);
            rightMidfielderAI = new RightMidfielderAI(this);
        }

        public Team FirstTeam { get; set; }
        public Team SecondTeam { get; set; }
        public Ball GameBall { get; set; }
        public Player[] AllPlayers { get; set; }
        public Rectangle[,] FieldCell { get; set; }

        private CentralDefenderAI centralDefenderAI;
        private CentralMidfielderAI centralMidfielderAI;
        private LeftBackAI leftBackAI;
        private LeftMidfielderAI leftMidfielderAI;
        private RightBackAI rightBackAI;
        private RightMidfielderAI rightMidfielderAI;
        private GoalkeeperAI goalkeeperAI;
        private StrikerAI strikerAI;

        /* Returns the team which has the ball at the moment.
         * Returns null if none of the two owns it. */
        public Team BallContactTeam
        {
            get
            {
                return PlayersTeam(GameBall.LastPlayer);
            }
        }

        /* Returns a sorted list with a specific amount of team players depending on their distance to the targetpoint in X rounds. */
        public List<Player> NearestPlayersToPoint(Team team, Point targetPoint, int playerCount, int round, bool involveGoalKeeper)
        {
            return NearestPlayersToPoint(team.Players, targetPoint, playerCount, round, involveGoalKeeper);
        }

        /* Returns a sorted list with a specific amount of team players depending on their distance to the targetpoint in X rounds. */
        public List<Player> NearestPlayersToPoint(List<Player> playerList, Point targetPoint, int playerCount, int round, bool involveGoalKeeper)
        {
            List<Player> nearestPlayers = new List<Player>();
            List<double> playerDistances = new List<double>();
            Pathfinding pathfinding = new Pathfinding();
            double currentDistance;
            Point nextStepPoint;
            foreach (Player player in playerList)
            {
                if (involveGoalKeeper || player.Position != PlayerPosition.Goalkeeper)
                {
                    ActionType type;
                    nextStepPoint = pathfinding.PlayerAtSpecificRound(player, round, out type);
                    currentDistance = Pathfinding.DistanceBetweenPoints(nextStepPoint, targetPoint);
                    if (nearestPlayers.Count == 0)
                    {
                        nearestPlayers.Add(player);
                        playerDistances.Add(currentDistance);
                    }
                    else
                    {
                        for (int i = 0; i <= playerDistances.Count; i++)
                        {
                            if (i == playerDistances.Count || currentDistance < playerDistances[i])
                            {
                                nearestPlayers.Insert(i, player);
                                playerDistances.Insert(i, currentDistance);
                                break;
                            }
                        }

                        //removes last player if there are too many players saved already.
                        if (nearestPlayers.Count > playerCount)
                        {
                            nearestPlayers.RemoveAt(nearestPlayers.Count - 1);
                            playerDistances.RemoveAt(playerDistances.Count - 1);
                        }
                    }
                }
            }

            return nearestPlayers;
        }

        /* Returns the team where the player belongs to. */
        public Team PlayersTeam(Player player)
        {
            if (FirstTeam.Players.Contains(player))
            {
                return FirstTeam;
            }
            if (SecondTeam.Players.Contains(player))
            {
                return SecondTeam;
            }

            return null;
        }

        /* Returns the other team */
        public Team GetEnemyTeam(Team team)
        {
            return (team.Equals(FirstTeam)) ? SecondTeam : FirstTeam;
        }

        /*Returns the location where the ball will be in the next round */
        public Point BallNextRoundLocation()
        {
            Point ballTarget;

            if (GameBall.HasPlayerContact && GameBall.LastPlayer != null)
            {
                Pathfinding pathfinding = new Pathfinding();
                ActionType type;
                //sets the balltarget to the point where the player who controls the ball at the moment will go to in the next round. */
                ballTarget = pathfinding.PlayerAtSpecificRound(GameBall.LastPlayer, 1, out type);
            }
            else if (GameBall.TargetPoint.HasValue)
            {
                Pathfinding pathfinding = new Pathfinding();
                Point tmpBallExactLocation = GameBall.ExactLocation;
                Point tmpBallGridLocation = pathfinding.GetGridLocation(tmpBallExactLocation);

                double[] direction = pathfinding.GetExactDirection(tmpBallExactLocation, pathfinding.GetExactLocation(GameBall.TargetPoint.Value));
                for (int i = 0; i < GameBall.Speed; i++)
                {
                    tmpBallExactLocation.X += (int)(direction[0] * 20);
                    tmpBallExactLocation.Y += (int)(direction[1] * 20);
                }
                ballTarget = pathfinding.GetGridLocation(tmpBallExactLocation);
            }
            else
            {
                ballTarget = GameBall.Location;
            }

            return ballTarget;
        }

        /* Returns a player if he will stand in the way of the ball
         * Returns null if no player stands in the way. */
        public Player PlayerInBallWay(Point shotPoint, Point targetPoint)
        {
            Pathfinding pathfinding = new Pathfinding();
            Point tmpBallExactLocation = pathfinding.GetExactLocation(shotPoint);
            Point tmpBallGridLocation = pathfinding.GetGridLocation(tmpBallExactLocation);
            
            for(int i = 0; i < 3; i++)
            {
                if (!tmpBallGridLocation.Equals(shotPoint))
                {
                    Player playerOnPosition = pathfinding.StandsPlayerOnPosition(tmpBallGridLocation, 1);
                    if (playerOnPosition != null)
                    {
                        return playerOnPosition;
                    }
                }
                double[] direction = pathfinding.GetExactDirection(tmpBallExactLocation, pathfinding.GetExactLocation(targetPoint));
                tmpBallExactLocation.X += (int)(direction[0] * 20);
                tmpBallExactLocation.Y += (int)(direction[1] * 20);
                tmpBallGridLocation = pathfinding.GetGridLocation(tmpBallExactLocation);
            }

            return null;
        }

        /* Returns the position where the goalkeeper of the team will be in x rounds. */
        public Point KeeperPosition(Team team, int round)
        {
            Player keeper = team.Goalkeeper;

            Pathfinding pathfinding = new Pathfinding();
            ActionType type;

            return pathfinding.PlayerAtSpecificRound(keeper, round, out type);
        }

        /* Checks whether this point is on the team's side of the field. */
        public bool IsPointOnOwnSide(Team playerTeam, Point rootPoint)
        {
            return (Math.Abs(playerTeam.TeamGoal.Location.X - rootPoint.X) < (FieldCell.GetLength(0) - 3) / 2);
        }

        /* Returns all enemies which stands around this location in a specific range and specific round */
        public List<Player> SurroundingPlayers(Team enemyTeam, double range, Point location, int round)
        {
            List<Player> enemies = new List<Player>();
            Pathfinding pathfinding = new Pathfinding();
            foreach (Player player in enemyTeam.Players)
            {
                if (player.Position != PlayerPosition.Goalkeeper)
                {
                    ActionType type;
                    Point nextPoint = pathfinding.PlayerAtSpecificRound(player, round, out type);
                    double distance = Pathfinding.DistanceBetweenPoints(location, nextPoint);
                    if (distance <= range)
                    {
                        enemies.Add(player);
                    }
                }
            }

            return enemies;
        }

        /* Returns a PlayerAction which the player has to execute in the next round depending on his position and the current game situation. */
        public PlayerAction CalculatePlayerAction(Player player, GameAction gameAction)
        {
            Team playerTeam = PlayersTeam(player);
            PlayerAction roundAction = null;
            switch (player.Position)
            {
                case PlayerPosition.CentralDefender:
                    roundAction = centralDefenderAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.CentralMidfielder:
                    roundAction = centralMidfielderAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.Goalkeeper:
                     roundAction = goalkeeperAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.LeftBack:
                    roundAction = leftBackAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.LeftMidfielder:
                    roundAction = leftMidfielderAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.RightBack:
                    roundAction = rightBackAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.RightMidfielder:
                    roundAction = rightMidfielderAI.CalculateRoundAction(player, gameAction);
                    break;
                case PlayerPosition.Striker:
                    roundAction = strikerAI.CalculateRoundAction(player, gameAction);
                    break;
            }
            return roundAction;
        }

        /// <summary>
        /// Calculates the distances to the enemy of every point around the target and returns the points ordered in a list.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerTeam"></param>
        /// <param name="targetPoint"></param>
        /// <param name="range">The range around the target point</param>
        /// <param name="amountOfPlayers">The amount of players which are allowed to stand around the calculated point </param>
        /// <param name="rangeRectangle"></param>
        /// <returns></returns>
        public List<Point> SearchRunPoints(Player player, Team playerTeam, Point targetPoint, int range, int amountOfPlayers, Rectangle rangeRectangle)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point rootPoint = targetPoint;
            List<Point> neighbors = new List<Point>();
            List<double> distancesToTarget = new List<double>();

            double rootDistanceToTarget = Pathfinding.DistanceBetweenPoints(player.Location, targetPoint);

            Point? bestNeighbor = null;
            int bestSurroundingEnemies = -1;

            for (int v = -2; v <= 2; v++)
            {
                for (int h = -2; h <= 2; h++)
                {
                    Point neighbor = new Point(rootPoint.X + h, rootPoint.Y + v);
                    int surroundingEnemies = SurroundingPlayers(GetEnemyTeam(playerTeam), range, neighbor, 1).Count;
                    double tmpDistanceToTarget = Pathfinding.DistanceBetweenPoints(neighbor, targetPoint);
                    if (pathfinding.IsWithinField(neighbor) && !neighbor.Equals(player.Location) && (!Pathfinding.CurrentBlockedRoom.Room.Contains(neighbor) || Pathfinding.CurrentBlockedRoom.AllowedTeam.Equals(playerTeam)))
                    {
                        if (tmpDistanceToTarget < rootDistanceToTarget && (bestSurroundingEnemies == -1 || surroundingEnemies < bestSurroundingEnemies))
                        {
                            bestNeighbor = neighbor;
                            bestSurroundingEnemies = surroundingEnemies;
                        }
                        if (rangeRectangle.Contains(neighbor) && pathfinding.StandsPlayerOnPosition(neighbor, 1) == null && surroundingEnemies <= amountOfPlayers)
                        {

                            if (neighbors.Count == 0)
                            {
                                neighbors.Add(neighbor);
                                distancesToTarget.Add(tmpDistanceToTarget);
                            }
                            else
                            {
                                for (int i = 0; i <= distancesToTarget.Count; i++)
                                {
                                    if (i == distancesToTarget.Count || tmpDistanceToTarget < distancesToTarget[i])
                                    {
                                        neighbors.Insert(i, neighbor);
                                        distancesToTarget.Insert(i, tmpDistanceToTarget);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (neighbors.Count == 0)
            {
                if (bestNeighbor.HasValue)
                {
                    neighbors.Add(bestNeighbor.Value);
                }
                else
                {
                    neighbors.Add(targetPoint);
                }
                
            }
            return neighbors;
        }

        /* Sorts the target points - points nearer to the endTarget will be preferred */
        public void OrderTargetPoints(List<Point> targetPoints, int start, int end, Point endTarget)
        {
            Point pivot = targetPoints[(start + end) / 2];
            int tmpStart = start;
            int tmpEnd = end;

            while (tmpStart <= tmpEnd)
            {
                while (Pathfinding.DistanceBetweenPoints(targetPoints[tmpStart], endTarget) < Pathfinding.DistanceBetweenPoints(pivot, endTarget))
                {
                    tmpStart++;
                }

                while (Pathfinding.DistanceBetweenPoints(targetPoints[tmpEnd], endTarget) > Pathfinding.DistanceBetweenPoints(pivot, endTarget))
                {
                    tmpEnd--;
                }

                if (tmpStart <= tmpEnd)
                {
                    Point tmpStartItem = targetPoints[tmpStart];
                    targetPoints[tmpStart] = targetPoints[tmpEnd];
                    targetPoints[tmpEnd] = tmpStartItem;
                    tmpStart++;
                    tmpEnd--;
                }
            }

            if (start < tmpEnd)
            {
                OrderTargetPoints(targetPoints, start, tmpEnd, endTarget);
            }
            if (tmpStart < end)
            {
                OrderTargetPoints(targetPoints, tmpStart, end, endTarget);
            }
        }

        /* Checks whether the target point would be offside. */
        public bool IsOffside(Player targetPlayer, Team enemyTeam, int round)
        {
            Pathfinding pathfinding = new Pathfinding();

            int targetPointDistance = Math.Abs(enemyTeam.TeamGoal.XCoordinate - targetPlayer.XCoordinate);

            Player lastEnemyPlayer = NearestPlayersToPoint(enemyTeam, enemyTeam.TeamGoal.MiddlePoint, 1, 0, false).ElementAt(0);
            int distanceToGoal = Math.Abs(enemyTeam.TeamGoal.XCoordinate - lastEnemyPlayer.XCoordinate);

            return (targetPointDistance < distanceToGoal);
        }

        /* Returns all players from the list who will stand within a specific rectangle in a specific round. */
        public List<Player> PlayerInRectangle(List<Player> targetPlayers, Rectangle runRectangle, int round)
        {
            Pathfinding pathfinding = new Pathfinding();
            List<Player> playerInRectangle = new List<Player>();
            foreach (Player player in targetPlayers)
            {
                if (player.Position != PlayerPosition.Goalkeeper)
                {
                    ActionType type;
                    Point nextPoint = pathfinding.PlayerAtSpecificRound(player, round, out type);
                    if (runRectangle.Contains(nextPoint))
                    {
                        playerInRectangle.Add(player);
                    }
                }
            }

            return playerInRectangle;
        }

        /* Returns the penalty kick point from this goal. */
        public Point PenaltyKickPoint(Goal goal)
        {
            Pathfinding pathfinding = new Pathfinding();

            Point penaltyKickPoint = goal.MiddlePoint;
            penaltyKickPoint.X += pathfinding.CalculateHorizontalDirection(goal.MiddlePoint, new Point(12, 12)) * 4;

            return penaltyKickPoint;
        }

        /* Returns the player's specific round position of the player from the team who will be the nearest to the root point.
         * Returns the ball's specific round position, if there will be no player in the run room. */
        public Point NextPointNearestPlayerInRunRoom(Point rootPoint, Rectangle runRoom, Team playerTeam, int round)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team enemyTeam = GetEnemyTeam(playerTeam);
            ActionType type;

            List<Player> allEnemiesInRunRoom = PlayerInRectangle(enemyTeam.Players, runRoom, 1);
            Point closestPlayerNextLocation;

            // takes the ball's next round position if no enemy stands in the player's run room
            if (allEnemiesInRunRoom.Count == 0)
            {
                closestPlayerNextLocation = BallNextRoundLocation();
            }
            else
            {
                Player closestPlayerInRunRoom = NearestPlayersToPoint(allEnemiesInRunRoom, playerTeam.TeamGoal.MiddlePoint, 1, round, false).ElementAt(0);
                if (closestPlayerInRunRoom.IsMarkedUp)
                {
                    closestPlayerNextLocation = BallNextRoundLocation();
                }
                else
                {
                    closestPlayerNextLocation = pathfinding.PlayerAtSpecificRound(closestPlayerInRunRoom, round, out type);
                    closestPlayerInRunRoom.IsMarkedUp = true;
                }
            }

            return closestPlayerNextLocation;
        }

        /* Returns all player from the team who have another horizontal direction to the target */
        public List<Player> PlayersWithOtherHorizontalDirection(Point targetPoint, int horizontalDirection, Team team)
        {
            Pathfinding pathfinding = new Pathfinding();
            List<Player> validPlayers = new List<Player>();

            foreach (Player player in team.Players)
            {
                int movementDirection = pathfinding.CalculateHorizontalDirection(player.Location, targetPoint);
                if (movementDirection != horizontalDirection)
                {
                    validPlayers.Add(player);
                }
            }

            return validPlayers;
        }

        /* Returns all player from the team who have another vertical direction to the target */
        public List<Player> PlayersWithOtherVerticalDirection(Point targetPoint, int verticalDirection, Team team)
        {
            Pathfinding pathfinding = new Pathfinding();
            List<Player> validPlayers = new List<Player>();

            foreach (Player player in team.Players)
            {
                int movementDirection = pathfinding.CalculateVerticalDirection(player.Location, targetPoint);
                if (movementDirection != verticalDirection)
                {
                    validPlayers.Add(player);
                }
            }

            return validPlayers;
        }

        public bool IsEnemyInBlockedRoom
        {
            get
            {
                Team enemyTeam = GetEnemyTeam(Pathfinding.CurrentBlockedRoom.AllowedTeam);
                Rectangle blockedRoom = Pathfinding.CurrentBlockedRoom.Room;
                foreach (Player enemy in enemyTeam.Players)
                {
                    if (blockedRoom.Contains(enemy.Location))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Checks whether the game ball in his current state (with his target point) will go through the target rectangle.
        /// Returns the last grid location where the ball will be before it goes into the rectangle.
        /// </summary>
        /// <param name="targetRectangle"></param>
        /// <returns></returns>
        public Point? PointBeforeRectangleEntry(Rectangle targetRectangle)
        {
            if (GameBall.TargetPoint.HasValue || GameBall.IsInShootState)
            {
                Pathfinding pathfinding = new Pathfinding();

                if (targetRectangle.Contains(GameBall.ExactTargetLocation))
                {
                    return pathfinding.GetGridLocation(GameBall.ExactTargetLocation);
                }
                else
                {
                    Point temporaryExactLocation = GameBall.ExactLocation;
                    Point lastGridLocation = pathfinding.GetGridLocation(temporaryExactLocation);

                    while (pathfinding.GetGridLocation(temporaryExactLocation) != pathfinding.GetGridLocation(GameBall.ExactTargetLocation))
                    {
                        lastGridLocation = pathfinding.GetGridLocation(temporaryExactLocation);
                        double[] direction = pathfinding.GetExactDirection(temporaryExactLocation, GameBall.ExactTargetLocation);
                        temporaryExactLocation.X += (int)(direction[0] * 5);
                        temporaryExactLocation.Y += (int)(direction[1] * 5);

                        if (targetRectangle.Contains(pathfinding.GetGridLocation(temporaryExactLocation)))
                        {
                            return lastGridLocation;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the best unmarked location within the rectangle
        /// </summary>
        /// <param name="player"></param>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public Point SearchFreePointInRectangle(Player player, Rectangle rectangle)
        {
            Pathfinding pathfinding = new Pathfinding();
            Team playerTeam = PlayersTeam(player);
            Team enemyTeam = GetEnemyTeam(playerTeam);
            Point centerOfRectangle= new Point(rectangle.X + (rectangle.Width/2), rectangle.Y + (rectangle.Height/2));

            Point bestPoint = centerOfRectangle;
            int amountOfPlayers = -1;
            for (int x = rectangle.X; x < rectangle.X + rectangle.Width; x++)
            {
                for (int y = rectangle.Y; y < rectangle.Y + rectangle.Height; y++)
                {
                    int tmpAmount = SurroundingPlayers(playerTeam,2, new Point(x, y), 5).Count + SurroundingPlayers(enemyTeam, 2, new Point(x, y), 5).Count;
                    if (pathfinding.IsWithinField(new Point(x, y)))
                    {
                        if (amountOfPlayers == -1 || tmpAmount < amountOfPlayers)
                        {
                            bestPoint = new Point(x, y);
                        }
                    }
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Returns a player with this player id & this team id
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public Player ConvertToPlayer(int playerId, int teamId)
        {
            Player player = AllPlayers.Single(x => x.Id == playerId && x.TeamId == teamId);
            return player;
        }

        /// <summary>
        /// Converts a PlayerAction to a PlayerActionHolder
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public CachedPlayerAction ConvertToHolder(PlayerAction action)
        {
            CachedPlayer affectedPlayer = null;
            if (action.AffectedPlayer != null)
            {
                affectedPlayer = new CachedPlayer(action.AffectedPlayer.Id, action.AffectedPlayer.TeamId);
            }

            return new CachedPlayerAction(affectedPlayer, action.TargetPoint, action.Type, action.IsActionToGetBall);
        }

        /// <summary>
        /// Converts a PlayerActionHolder to a PlayerAction
        /// </summary>
        /// <param name="actionHolder"></param>
        /// <returns></returns>
        public PlayerAction ConvertToAction(CachedPlayerAction actionHolder)
        {
            Player affectedPlayer = null;
            if (actionHolder.AffectedPlayer != null)
            {
                affectedPlayer = ConvertToPlayer(actionHolder.AffectedPlayer.PlayerId, actionHolder.AffectedPlayer.TeamId);
            }

            PlayerAction convertedAction = new PlayerAction(affectedPlayer, actionHolder.TargetPoint, actionHolder.Type);
            convertedAction.IsActionToGetBall = actionHolder.IsActionToGetBall;

            return convertedAction;
        }

    }
}
