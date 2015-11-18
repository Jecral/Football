using Football.EventArguments;
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
    class Ball : GameObject
    {
        public Ball(int speed, Player lastPlayer)
            : base()
        {
            base.ObjectImage = Properties.Resources.LittleBallImage;
            LastPlayer = lastPlayer;
            HasPlayerContact = (lastPlayer == null) ? false : true;
        }

        public bool HasPlayerContact { get; set; }
        public Player LastPlayer { get; set; }
        public event EventHandler<PointReceivedArgs> BallWillMove;
        public Point ExactLocation { get; set; }
        public Point ExactTargetLocation { get; set; }
        public double ExactXDirection { get; set; }
        public double ExactYDirection { get; set; }

        public Image BallImage { get; set; }
        public Point RootPoint { get; set; }

        public bool IsInShootState { get; set; }
        public bool HasReachedTargetPoint { get; set; }

        new public void Move()
        {
            for (int tmpSpeed = Speed; tmpSpeed > 0; tmpSpeed--)
            {
                if (TargetPoint.HasValue || IsInShootState)
                {
                    if (!HasReachedTargetPoint)
                    {
                        SetDirectionToTargetPoint();
                    }

                    Pathfinding pathfinding = new Pathfinding();
                    Point newLocation = ExactLocation;

                    newLocation.X += (int)(ExactXDirection * 20);
                    newLocation.Y += (int)(ExactYDirection * 20);
                    ExactLocation = newLocation;

                    Point gridLocation = pathfinding.GetGridLocation(newLocation);
                    PointReceivedArgs args = new PointReceivedArgs(gridLocation, newLocation);
                    BallWillMove(this, args);
                    if (TargetPoint.HasValue && Location.Equals(TargetPoint.Value))
                    {
                        if (IsInShootState)
                        {
                            HasReachedTargetPoint = true;
                            TargetPoint = null;
                        }
                        else
                        {
                            TargetPoint = null;
                            break;
                        }
                    }
                    if (IsInShootState && HasReachedTargetPoint)
                    {
                        Speed -= 1;
                        if (Speed <= 0)
                        {
                            TargetPoint = null;
                            break;
                        }
                    }
                }
            }
        }

        /* Sets object's direction towards it's target.
        * If xDirection or yDirection is '0', the object won't move.
        * If xDirection is '-1' or '1' the object will move in left/right direction.
        * If yDirection is '-1' or '1' the object will move in up/down direction. */
        public void SetDirectionToTargetPoint()
        {
            if (TargetPoint.HasValue)
            {
                Pathfinding pathfinding = new Pathfinding();

                double[] exactDirection = pathfinding.GetExactDirection(ExactLocation, pathfinding.GetExactLocation(TargetPoint.Value));
                if (double.IsNaN(exactDirection[0]) || double.IsNaN(exactDirection[1]))
                {
                    ExactXDirection = 0;
                    ExactYDirection = 0;
                }
                else
                {
                    ExactXDirection = exactDirection[0];
                    ExactYDirection = exactDirection[1];
                }
            }
            else
            {
                ExactXDirection = 0;
                ExactYDirection = 0;
            }
        }
    }
}