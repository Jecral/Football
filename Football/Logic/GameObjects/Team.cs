using Football.Logic;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football
{
    class Team
    {
        public Team(string name, int number, Goal goal)
        {
            this.Name = name;
            this.TeamId = number;
            TeamColor = (number == 0) ? Color.Yellow : Color.Red;
            this.GoalsCount = 0;
            this.TeamGoal = goal;

            Players = new List<Player>();
            CreatePlayers();
        }

        public List<Player> Players { get; set; }
        public string Name { get; set; }
        public Goal TeamGoal { get; set; }
        public int GoalsCount
        {
            get { return goalsCount; }
            set
            {
                goalsCount = value;
                if (GoalsChanged != null)
                {
                    GoalsChanged(this, EventArgs.Empty);
                }
            }
        }
        public Color TeamColor { get; set; }
        public bool IsAllowedToTakeBall
        {
            get { return isAllowedToTakeBall; }
            set
            {
                Players.ForEach(x => x.IsAllowedToTakeBall = value);
                isAllowedToTakeBall = value;
            }
        }

        public int TeamId { get; set; }
        private int goalsCount;
        private bool isAllowedToTakeBall;
        public event EventHandler GoalsChanged;

        /// <summary>
        /// Checks whether the team has the left side at the moment
        /// </summary>
        public bool HasLeftSide
        {
            get { return TeamGoal.MiddlePoint.X == 1; }
        }

        /* Sets the players on their lineup-positions. */
        public void InitializeLineup(int midlineXPosition, int midpointYPosition, bool hasLeftSide, bool isAttack)
        {
            List<Point> kickOffLineup = CalculateKickOffLineup(midlineXPosition, midpointYPosition, hasLeftSide, isAttack);
            for (int i = 0; i < kickOffLineup.Count; i++)
            {
                Point point = kickOffLineup.ElementAt(i);
                if (Players[i].Position != PlayerPosition.Goalkeeper)
                {
                    if (point.X < 4)
                        point.X += 4 - point.X;
                    if (point.X > 48)
                        point.X -= point.X - 48;
                    if (point.Y < 1)
                        point.Y += 1 - point.Y;
                    if (point.Y > 25)
                        point.Y -= point.Y - 25;
                }
                Players[i].StartLocation = point;
                Players[i].Location = point;
            }
        }

        public void CreatePlayers()
        {
            Players.Add(new Player(TeamId, 1, PlayerPosition.Goalkeeper, 180, 70, this));
            Players.Add(new Player(TeamId, 2, PlayerPosition.LeftBack, 180, 70, this));
            Players.Add(new Player(TeamId, 3, PlayerPosition.CentralDefender, 180, 70, this));
            Players.Add(new Player(TeamId, 4, PlayerPosition.CentralDefender, 180, 70, this));
            Players.Add(new Player(TeamId, 5, PlayerPosition.RightBack, 180, 70, this));
            Players.Add(new Player(TeamId, 6, PlayerPosition.LeftMidfielder, 180, 70, this));
            Players.Add(new Player(TeamId, 7, PlayerPosition.Striker, 180, 70, this));
            Players.Add(new Player(TeamId, 8, PlayerPosition.RightMidfielder, 180, 70, this));
            Players.Add(new Player(TeamId, 9, PlayerPosition.LeftMidfielder, 180, 70, this));
            Players.Add(new Player(TeamId, 10, PlayerPosition.Striker, 180, 70, this));
            Players.Add(new Player(TeamId, 11, PlayerPosition.CentralMidfielder, 180, 70, this));
        }

        /// <summary>
        /// Calculates the KickOff lineup.
        /// Multiply the x-coordinate with -1 if hasLeftSide is false --> for the team on the right side.
        /// Adds 3 or 4 to the x-coordinate if isAttack is true.
        /// </summary>
        /// <param name="midlineX">x-coordinate of the midline</param>
        /// <param name="midpointY">y-coordinate of the midpoint</param>
        /// <param name="hasLeftSide">Controls the team the left side?</param>
        /// <param name="isAttack">Is the team the attacking team at the moment?</param>
        /// <returns>The KickOff lineup in a list of points.</returns>
        private List<Point> CalculateKickOffLineup(int midlineX, int midpointY, bool hasLeftSide, bool isAttack)
        {
            List<Point> AttackKickOffLineup = new List<Point>();
            AttackKickOffLineup.Add(new Point((hasLeftSide) ? 1 : 51, TeamGoal.MiddlePoint.Y)); // Goalkeeper
            AttackKickOffLineup.Add(new Point(midlineX - (11 + ((isAttack) ? 0 : 3)) * ((!hasLeftSide) ? -1 : 1), 6));
            AttackKickOffLineup.Add(new Point(midlineX - (11 + ((isAttack) ? 0 : 3)) * ((!hasLeftSide) ? -1 : 1), 11));
            AttackKickOffLineup.Add(new Point(midlineX - (11 + ((isAttack) ? 0 : 3)) * ((!hasLeftSide) ? -1 : 1), 16));
            AttackKickOffLineup.Add(new Point(midlineX - (11 + ((isAttack) ? 0 : 3)) * ((!hasLeftSide) ? -1 : 1), 21));
            AttackKickOffLineup.Add(new Point(midlineX - (1 + ((isAttack) ? 0 : 4)) * ((!hasLeftSide) ? -1 : 1), 5));
            AttackKickOffLineup.Add(new Point(midlineX + 1 + ((isAttack) ? 0 : 4) * ((hasLeftSide) ? -1 : 1), midpointY));
            AttackKickOffLineup.Add(new Point(midlineX - (1 + ((isAttack) ? 0 : 4)) * ((!hasLeftSide) ? -1 : 1), 23));
            AttackKickOffLineup.Add(new Point(midlineX - (5 + ((isAttack) ? 0 : 4)) * ((!hasLeftSide) ? -1 : 1), 8));
            AttackKickOffLineup.Add(new Point(midlineX + 1 + ((isAttack) ? 0 : 4) * ((hasLeftSide) ? -1 : 1), midpointY + 2));
            AttackKickOffLineup.Add(new Point(midlineX - (5 + ((isAttack) ? 0 : 4)) * ((!hasLeftSide) ? -1 : 1), 20));

            return AttackKickOffLineup;
        }

        /// <summary>
        /// **In Development**
        /// </summary>
        /// <param name="targetGoal"></param>
        /// <returns></returns>
        private List<Point> CalculatePenaltyKickLineUp(Goal targetGoal)
        {
            List<Point> lineup = new List<Point>();


            return lineup;
        }

        /* Checks whether a player from the team has ball contact. */
        public Player PlayerWithBall
        {
            get
            {
                foreach (Player player in Players)
                {
                    if (player.HasBall)
                    {
                        return player;
                    }
                }
                return null;
            }
        }

        /* Returns the goalkeeper. */
        public Player Goalkeeper
        {
            get
            {
                return Players.Find(player => player.Position == PlayerPosition.Goalkeeper);
            }
        }
    }
}
