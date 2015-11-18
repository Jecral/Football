
using Football.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football
{
    abstract class GameObject
    {
        public GameObject()
        {
            TargetPoint = new Point?();
        }

        public int Speed { get; set; } //how many cells per round
        public int XDirection { get; set; } // current horizontal direction
        public int YDirection { get; set; } //current vertical direction
        public Point? TargetPoint { get; set; }

        public int XCoordinate { get; set; }
        public int YCoordinate { get; set; }
        public Point Location
        {
            get { return new Point(XCoordinate, YCoordinate); }
            set { XCoordinate = value.X; YCoordinate = value.Y; }
        }
        public Image ObjectImage { get; set; }

        /* Let the object move to his target point. */
        public void Move()
        {
            int tmpSpeed = Speed;
            while (tmpSpeed > 0)
            {
                int newXCoordinate = XCoordinate + this.XDirection;
                int newYCoordinate = YCoordinate + this.YDirection;
                SetCoordinates(newXCoordinate, newYCoordinate);
                SetDirectionToAim();
                tmpSpeed--;
            }
        }

        /* Sets the x- and y-coordinates */
        public void SetCoordinates(int x, int y)
        {
            XCoordinate = x;
            YCoordinate = y;
        }

        /* Sets object's direction towards it's target.
         * If xDirection or yDirection is '0', the object won't move.
         * If xDirection is '-1' or '1' the object will move in left/right direction.
         * If yDirection is '-1' or '1' the object will move in up/down direction. */
        public void SetDirectionToAim()
        {
            if (TargetPoint.HasValue)
            {
                int xDirection = 0;
                int yDirection = 0;

                Pathfinding.TargetPointDirection(ref xDirection, ref yDirection, new Point(XCoordinate, YCoordinate), TargetPoint.Value);
                XDirection = xDirection;
                YDirection = yDirection;
            }
            else
            {
                XDirection = 0;
                YDirection = 0;
            }
        }
    }
}
