using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Football.Logic
{
    class Goal : GameObject
    {
        public Goal(int height)
            : base()
        {
            this.Height = height;
        }

        public int Height { get; set; }
        public Point MiddlePoint {
            get {
                return new Point(XCoordinate, YCoordinate + (Height / 2));
            }
        }
        public Rectangle PenaltyRoom
        {
            get
            {
                Rectangle penaltyRoom = new Rectangle();
                if (XCoordinate == 1)
                {
                    penaltyRoom.Location = new Point(this.XCoordinate, this.YCoordinate - 6);
                }
                else
                {
                    penaltyRoom.Location = new Point(this.XCoordinate - 6, this.YCoordinate - 6);
                }

                penaltyRoom.Size = new Size(6, 12 + Height);

                return penaltyRoom;
            }
        }
        public Rectangle GoalRectangle
        {
            get
            {
                Rectangle goalRectangle = new Rectangle();
                goalRectangle.Location = this.Location;
                goalRectangle.Size = new Size(1, Height);

                return goalRectangle;
            }
        }

        public bool Contains(Point point)
        {
            Pathfinding pathfinding = new Pathfinding();
            int back = (Location.X == 1) ? 0 : 52;

            return point.X == back && point.Y >= YCoordinate && point.Y < YCoordinate + Height;
        }
    }
}
