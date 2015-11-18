using System;
using System.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Football.Logic.GameObjects.Player;
using Football.Logic.ArtificialIntelligence;

namespace Football.Logic
{
    class Pathfinding
    {
        public Pathfinding()
        {
            randomGenerator = new Random();
        }

        public static Player[] AllPlayers { get; set; }
        private static List<int[]> allDirections = new List<int[]> { new int[] { -1, -1 }, new int[] { 0, -1 }, new int[] { 1, -1 }, new int[] { 1, 0 }, new int[] { 1, 1 }, new int[] { 0, 1 }, new int[] { -1, 1 }, new int[] { -1, 0 } };
        public static Rectangle[,] FieldCell { get; set; }
        public static BlockedRoom CurrentBlockedRoom { get; set; }
        public static AI GameAI { get; set; }
        public List<int[]> AllDirections { get { return allDirections; } }
        private Random randomGenerator;

        /* Saves AllPlayers and FieldCell. */
        public static void SetData(AI gameAI, Player[] allPlayers, Rectangle[,] fieldCell)
        {
            AllPlayers = allPlayers;
            FieldCell = fieldCell;
            GameAI = gameAI;
        }

        /* Returns xDirection = 0 and yDirection = 0, if the 
         * 
         * Point is on the same cell as the fromPoint.
         * Returns xDirection -1 or 1 if the targetPoint is left to/right to the fromPoint.
         * Returns yDirection -1 or 1 if the targetPoint is above/under the fromPoint. */
        public static void TargetPointDirection(ref int xDirection, ref int yDirection, Point fromPoint, Point targetPoint)
        {
            xDirection = (targetPoint.X > fromPoint.X) ? 1 : (targetPoint.X < fromPoint.X) ? -1 : 0;
            yDirection = (targetPoint.Y > fromPoint.Y) ? 1 : (targetPoint.Y < fromPoint.Y) ? -1 : 0;
        }

        /* Returns distance between two points. */
        public static double DistanceBetweenPoints(Point firstPoint, Point secondPoint)
        {
            double distance = 0;

            int firstX = firstPoint.X;
            int secondX = secondPoint.X;
            int firstY = firstPoint.Y;
            int secondY = secondPoint.Y;

            distance = Math.Sqrt(Math.Pow((firstX - secondX), 2) + Math.Pow((firstY - secondY), 2));
            return Math.Abs(distance);
        }

        /// <summary>
        /// Returns the distance in the grid between two points.
        /// </summary>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="secondPoint">The second point</param>
        /// <returns>The horizontal or vertical distance.</returns>
        public static int GridDistanceBetweenPoints(Point firstPoint, Point secondPoint)
        {
            int verticalDistance = Math.Abs(firstPoint.Y - secondPoint.Y);
            int horizontalDistance = Math.Abs(firstPoint.X - secondPoint.X);

            return (verticalDistance > horizontalDistance) ? verticalDistance : horizontalDistance;
        }

        /// <summary>
        /// The recursive pathfinding-method.
        /// </summary>
        /// <param name="targetPoint">The target point.</param>
        /// <param name="currentPoint">The current point in the grid.</param>
        /// <param name="playerPosition">The position of the player in the grid.</param>
        /// <param name="way"></param>
        /// <param name="shortestWayLength">The length of the best found path to the target.</param>
        /// <param name="failedTriesToBeatPath"></param>
        /// <param name="maximumTries">The maximum amount of tries the algorithm has to beat the best found path.</param>
        /// <returns></returns>
        public List<Point> CalculateShortestWayToTarget(Point targetPoint, Point currentPoint, Point playerPosition, List<Point> way, ref int shortestWayLength, ref int failedTriesToBeatPath, int maximumTries, ref int recursionStep)
        {
            recursionStep++;

            //do not add the current point if it equals the player's position
            if (!playerPosition.Equals(currentPoint))
            {
                way.Add(currentPoint);
            }

            //returns an empty list if the current way is already longer than the best found way.
            //True if no tries are left.
            //True if the recursion step is too high --> emergency exit
            if (recursionStep > 150 || failedTriesToBeatPath >= maximumTries || (shortestWayLength > -1 && way.Count > shortestWayLength))
            {
                failedTriesToBeatPath++;
                return new List<Point>();
            }

            //true if the current point equals the target point or if the target point is unreachable.
            if (currentPoint.Equals(targetPoint) || (!IsValidPosition(targetPoint) && ArePointsNeighbors(currentPoint, targetPoint)) || GetValidNeighborPoints(targetPoint).Count == 0)
            {
                //returns the way and sets the shortest way length if this way is shorter than the currently best way
                if (shortestWayLength == -1 || way.Count < shortestWayLength)
                {
                    shortestWayLength = way.Count;
                    List<Point> pointList = new List<Point>();
                    pointList.Add(currentPoint);
                    return pointList;
                }
                else
                {
                    failedTriesToBeatPath++;
                    return new List<Point>();
                }
            }
            else
            {
                //get current point's neighbors
                List<Point> neighbors = GetValidNeighborPoints(currentPoint);
                Point nearestPoint = new Point();
                List<Point> bestWaypoints = new List<Point>();
                List<Point> newWaypoints = new List<Point>();

                //test all directions
                while(neighbors.Count > 0)
                {
                    nearestPoint = GetNearestPointToTarget(neighbors, targetPoint);
                    if (!way.Contains(nearestPoint))
                    {
                        newWaypoints = CalculateShortestWayToTarget(targetPoint, nearestPoint, playerPosition, new List<Point>(way), ref shortestWayLength, ref failedTriesToBeatPath, maximumTries, ref recursionStep);

                        /*  Adds the found waypoints to the way if the recursive method returns waypoints.
                         *  Remember: It returns waypoints if it finds the target.
                         *  Additionally the new way must be shorter than the current saved test way. */
                        if (newWaypoints.Count > 0 && (bestWaypoints.Count == 0 || newWaypoints.Count < bestWaypoints.Count))
                        {
                            bestWaypoints = new List<Point>(newWaypoints);
                        }
                    }
                    neighbors.Remove(nearestPoint);
                }

                /* Returns the bestWay-list if it's longer than 0. Remember: It's longer if a deeper recursive-method found the target.
                 * Otherwise an empty list. */
                if (bestWaypoints.Count > 0)
                {
                    if (!currentPoint.Equals(playerPosition))
                    {
                        bestWaypoints.Insert(0, currentPoint);
                    }
                    return bestWaypoints;
                }
                else
                {
                    return new List<Point>();
                }
            }
        }

        /// <summary>
        /// Returns a list with valid neighbors of the root point.
        /// </summary>
        /// <param name="rootPoint">The rootpoint</param>
        /// <returns>Valid neighbors as a List<Point></returns>
        public List<Point> GetValidNeighborPoints(Point rootPoint)
        {
            List<Point> neighbors = new List<Point>();
            foreach (int[] direction in allDirections)
            {
                Point neighbor = new Point(rootPoint.X + direction[0], rootPoint.Y + direction[1]);
                if (IsValidPosition(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Returns the nearest point to the target.
        /// If multiple points have the same distance to the target, choose a random one from them.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private Point GetNearestPointToTarget(List<Point> points, Point target)
        {
            List<Point> nearestPoints = new List<Point>();
            double bestDistance = -1;
            foreach (Point point in points)
            {
                double tmpDistance = DistanceBetweenPoints(point, target);
                if (bestDistance == -1 || tmpDistance <= bestDistance)
                {
                    if (tmpDistance < bestDistance)
                    {
                        nearestPoints.Clear();
                    }
                    nearestPoints.Add(point);
                    bestDistance = tmpDistance;
                }
            }

            return nearestPoints.ElementAt(randomGenerator.Next(0, nearestPoints.Count));
        }

        /* Searches all points which are neighbors and trims the path between them. */
        public List<Point> ReduceWay(List<Point> way)
        {
            List<Point> reducedWay = new List<Point>(way);
            int wayLength = (way.Count - 1);
            for (int a = 0; a < way.Count; a++)
            {
                for (int b = wayLength; b > a + 1; b--)
                {
                    Point from = way[a];
                    Point to = way[b];
                    if (ArePointsNeighbors(way[a], way[b]))
                    {
                        if (reducedWay.Contains(from) && reducedWay.Contains(to))
                        {
                            int aIndex = reducedWay.IndexOf(way[a]);
                            int range = reducedWay.IndexOf(way[b]) - aIndex;
                            reducedWay.RemoveRange(aIndex + 1, range - 1);
                        }
                    }
                }
            }

            return reducedWay;
        }

        /* Returns a list with all points of the way which are invalid. */
        public List<Point> InvalidWayPoints(List<Point> way)
        {
            List<Point> invalidWayPoints = new List<Point>();

            foreach (Point wayPoint in way)
            {
                if (!IsValidPosition(wayPoint))
                {
                    invalidWayPoints.Add(wayPoint);
                }
            }

            return invalidWayPoints;
        }

        /* Check whether the points are neighbors */
        public bool ArePointsNeighbors(Point firstPoint, Point secondPoint)
        {
            bool areNeighbors = false;
            int horizontalDifference = Math.Abs(firstPoint.X - secondPoint.X);
            int verticalDifference = Math.Abs(firstPoint.Y - secondPoint.Y);
            areNeighbors = (verticalDifference <= 1 && horizontalDifference <= 1) ? true : false;

            return areNeighbors;
        }

        /* Checks whether a player will stand on this position in x steps - returns the player if true, null if not */
        public Player StandsPlayerOnPosition(Point position, int step)
        {
            foreach (Player player in AllPlayers)
            {
                ActionType type;
                Point playerNewLocation = PlayerAtSpecificRound(player, step, out type);
                if (player.Location.Equals(position))
                {
                    return player;
                }
            }
            return null;
        }

        /* Checks whether this position is in the field and no player stands on it. */
        public bool IsValidPosition(Point position)
        {
            //check the field
            if (!(position.X >= 0 && position.X <= FieldCell.GetLength(0) - 1 && position.Y >= 0 && position.Y <= FieldCell.GetLength(1) - 1))
            {
                return false;
            }

            //check all players - null if no player is on that position
            return StandsPlayerOnPosition(position, 0) == null;
        }

        /* Checks whether the point is within the field. */
        public bool IsWithinField(Point position)
        {
            return position.X > 0 && position.X < FieldCell.GetLength(0) - 1 && position.Y > 0 && position.Y < FieldCell.GetLength(1) - 1;
        }

        /* Returns an array with two values - the first value is the exact horizontal direction to the target
         * and the second the vertical direction. */
        public double[] GetExactDirection(Point from, Point exactTargetLocation)
        {
            double[] exactDirection = new double[2];
            exactDirection[0] = exactTargetLocation.X - from.X;
            exactDirection[1] = exactTargetLocation.Y - from.Y;

            //norm vector - each component/absolute value of the vector
            double length = Math.Sqrt(Math.Pow(exactDirection[0], 2) + Math.Pow(exactDirection[1], 2));
            exactDirection[0] = exactDirection[0] / length;
            exactDirection[1] = exactDirection[1] / length;

            return exactDirection;
        }

        /* Returns the exact location of the target point - the target point contains indexes of the fieldgrid. */
        public Point GetExactLocation(Point targetPoint)
        {
            int width = FieldCell[0, 0].Width;
            int height = FieldCell[0, 0].Height;
            Rectangle targetRectangle = new Rectangle(new Point(targetPoint.X * width, targetPoint.Y * height), new Size(width, height));
            Point exactTargetPoint = new Point(targetRectangle.X + targetRectangle.Width / 2, targetRectangle.Y + targetRectangle.Height / 2);
            return exactTargetPoint;
        }

        /* Returns the location/index in the grid at the exact location on the panel. */
        public Point GetGridLocation(Point exactLocation)
        {
            Point gridLocation = new Point();
            gridLocation.X = exactLocation.X / FieldCell[0, 0].Width;
            gridLocation.Y = exactLocation.Y / FieldCell[0, 0].Height;

            return gridLocation;
        }

        /* Returns the point where the player will be in a specific amount of rounds and which action type.
         * Round 0 will return the current player's location. */
        public Point PlayerAtSpecificRound(Player player, int round, out ActionType type)
        {
            Point currentPoint = player.Location;

            if (player.PlayerActions.Count > 0)
            {
                Queue<PlayerAction> playerActions = new Queue<PlayerAction>(player.PlayerActions);
                int leftSpeed = player.Speed * round;
                
                while (leftSpeed > 0)
                {
                    foreach (Point wayPoint in playerActions.Peek().WayToTarget)
                    {
                        leftSpeed--;
                        currentPoint = wayPoint;
                        if (leftSpeed == 0)
                        {
                            type = playerActions.Peek().Type;
                            return currentPoint;
                        }
                    }
                    if (playerActions.Count > 1)
                    {
                        playerActions.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //return the last point the player will reach
            type = ActionType.Nothing;
            return currentPoint;

        }

        public Point PlayerAtSpecificRound(int playerId, int teamId, int round, out ActionType type)
        {
            Player player = GameAI.ConvertToPlayer(playerId, teamId);
            return PlayerAtSpecificRound(player, round, out type);
        }

        /* Returns the horizontal direction from the root point o he target point */
        public int CalculateHorizontalDirection(Point rootPoint, Point targetPoint)
        {
            int xDirection = 0;
            int yDirection = 0;
            TargetPointDirection(ref xDirection, ref yDirection, rootPoint, targetPoint);

            return xDirection;
        }

        /* Returns the vertical direction from the root point o he target point*/
        public int CalculateVerticalDirection(Point rootPoint, Point targetPoint)
        {
            int xDirection = 0;
            int yDirection = 0;
            TargetPointDirection(ref xDirection, ref yDirection, rootPoint, targetPoint);

            return yDirection;
        }
    }

}
