using Football.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.General
{
    class FieldPanel : Panel
    {
        public FieldPanel(bool isGamePanel)
        {
            this.isGamePanel = isGamePanel;
            DoubleBuffered = true;
            Paint += FieldPanel_Paint;
        }

        public Rectangle[,] Field { get; set; }
        public GameSettings Settings { get; set; }
        public Point TopLeftFieldCorner { get; set; }

        private bool settingsAreSet;
        private bool isGamePanel;

        private int midfieldLineXPosition;
        private int midfieldPointYPosition;
        private int leftGoalHeight;
        private int leftGoalYPosition;
        private int rightGoalHeight;
        private int rightGoalYPosition;

        /// <summary>
        /// Draws the left goal, right goal, outlines, midline as well as the rest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FieldPanel_Paint(object sender, PaintEventArgs e)
        {
            if (settingsAreSet)
            {
                int cellWidth = Field[0, 0].Size.Width;
                int cellHeight = Field[0, 0].Size.Height;
                Pen myPen = new Pen(Color.White, 1);
                Graphics graphics = e.Graphics;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Point topLeftCorner = new Point(TopLeftFieldCorner.X + Field[1, 1].Location.X, TopLeftFieldCorner.Y + Field[1, 1].Location.Y);
                int fieldWidth = (Field.GetLength(0) - 2) * cellWidth;
                int fieldHeight = (Field.GetLength(1) - 2) * cellHeight;

                graphics.DrawRectangle(myPen, topLeftCorner.X - myPen.Width, topLeftCorner.Y - myPen.Width, fieldWidth + myPen.Width, fieldHeight + myPen.Width);

                //drawing of the left goal
                int leftGoalX = topLeftCorner.X - (cellWidth + (int)myPen.Width);
                int leftGoalY = TopLeftFieldCorner.Y + Field[1, leftGoalYPosition].Location.Y;
                graphics.DrawRectangle(myPen, leftGoalX, leftGoalY, cellWidth, cellHeight * leftGoalHeight);

                //drawing of the left six yard box/goal room
                int leftSixYardY = leftGoalY - cellHeight * 2;
                Rectangle leftSixYard = new Rectangle(topLeftCorner.X - (int)myPen.Width, leftSixYardY, cellWidth * 2, cellHeight * 4 + leftGoalHeight * cellHeight);
                graphics.DrawRectangle(myPen, leftSixYard);

                //drawing of the right goal
                int rightGoalY = TopLeftFieldCorner.Y + Field[Field.GetLength(0) - 3, rightGoalYPosition].Location.Y;
                graphics.DrawRectangle(myPen, topLeftCorner.X + fieldWidth, rightGoalY, cellWidth - 1 + myPen.Width, cellHeight * rightGoalHeight);

                //drawing of the right six yard box/goal room
                int rightSixYardY = rightGoalY - cellHeight * 2;
                Rectangle rightSixYard = new Rectangle(topLeftCorner.X + fieldWidth - cellWidth * 2, rightSixYardY, cellWidth * 2, cellHeight * 4 + rightGoalHeight * cellHeight);
                graphics.DrawRectangle(myPen, rightSixYard);

                //drawing of the midline
                int midLineX = topLeftCorner.X + Field[midfieldLineXPosition, 0].Location.X + (cellWidth / 2);
                Point pointTop = new Point(midLineX, topLeftCorner.Y);
                Point pointBottom = new Point(midLineX, topLeftCorner.Y + fieldHeight);
                graphics.DrawLine(myPen, pointTop, pointBottom);

                //drawing of midline-point
                int midPointX = midLineX - (int)myPen.Width - 1;
                int midPointY = topLeftCorner.Y + Field[midfieldLineXPosition, midfieldPointYPosition].Location.Y + cellHeight / 2 - (int)myPen.Width;
                graphics.DrawEllipse(myPen, midPointX, midPointY, 4, 4);

                //drawing of midline circle
                int circleDiameter = 7;
                int circleX = (midPointX - cellWidth * circleDiameter / 2) + (int)myPen.Width / 2;
                int circleY = (midPointY - cellHeight * circleDiameter / 2) + (int)myPen.Width / 2;
                graphics.DrawEllipse(myPen, circleX, circleY, cellWidth * circleDiameter, cellHeight * circleDiameter);

                //drawing of the left penalty kick point
                int leftPenaltyPointY = leftGoalY + (leftGoalHeight * cellHeight) / 2 - (int)myPen.Width;
                int leftPenaltyPointX = topLeftCorner.X + cellHeight * 4 + cellHeight / 2 - (int)myPen.Width - 2;
                graphics.DrawEllipse(myPen, leftPenaltyPointX, leftPenaltyPointY, 4, 4);

                //drawing of the right penalty kick point
                int rightPenaltyPointY = rightGoalY + (rightGoalHeight * cellHeight) / 2 - (int)myPen.Width;
                int rightPenaltyPointX = topLeftCorner.X + fieldWidth - cellHeight * 4 - cellHeight / 2 - (int)myPen.Width;
                graphics.DrawEllipse(myPen, rightPenaltyPointX, rightPenaltyPointY, 4, 4);

                //drawing of the left penalty room
                int lPenaltyRoomY = leftSixYardY - cellHeight * 4;
                int penaltyRoomWidth = cellWidth * 7;
                int lpenaltyRoomHeight = cellHeight * (leftGoalHeight + 12);
                graphics.DrawRectangle(myPen, topLeftCorner.X - (int)myPen.Width, lPenaltyRoomY, penaltyRoomWidth, lpenaltyRoomHeight);

                //drawing of the right penalty room
                int rPenaltyRoomY = rightSixYardY - cellHeight * 4;
                int rpenaltyRoomHeight = cellHeight * (rightGoalHeight + 12);
                graphics.DrawRectangle(myPen, topLeftCorner.X + fieldWidth - penaltyRoomWidth, rPenaltyRoomY, penaltyRoomWidth, rpenaltyRoomHeight);

                //drawing of the corner circles
                DrawCornerCircles(graphics, topLeftCorner, myPen);
                DrawGoalRoomCircles(graphics, topLeftCorner, myPen, leftSixYard, rightSixYard);
            }
        }

        public void UpdateSettings(Rectangle[,] field, GameSettings settings, Point topLeftFieldCorner)
        {
            settingsAreSet = true;
            Field = field;
            Settings = settings;
            TopLeftFieldCorner = topLeftFieldCorner;

            SetSettings(settings);
        }

        /// <summary>
        /// Draws the football-specific quarter circles in all corners.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="topLeftCorner"></param>
        /// <param name="pen"></param>
        private void DrawCornerCircles(Graphics graphics, Point topLeftCorner, Pen pen)
        {
            int cellWidth = Field[0, 0].Size.Width;
            int cellHeight = Field[0, 0].Size.Height;

            //drawing of the topleft corner arc
            Point topPoint = new Point(topLeftCorner.X + Field[0, 0].X, topLeftCorner.Y + Field[0, 0].Y);
            topPoint.X -= cellWidth/2;
            topPoint.Y -= cellHeight/2;

            Rectangle cornerRectangle = new Rectangle(topPoint, new Size(cellWidth, cellHeight));
            graphics.DrawArc(pen, cornerRectangle, 90, -90);

            //drawing of the bottom left corner arc
            topPoint = new Point(topLeftCorner.X + Field[0, Field.GetLength(1) - 2].X, topLeftCorner.Y + Field[0, Field.GetLength(1) - 2].Y);
            topPoint.X -= cellWidth / 2;
            topPoint.Y -= cellHeight / 2;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 270, 90);

            //drawing of the top right corner arc
            Rectangle topRightRectangle = Field[Field.GetLength(0) - 2, 0];
            topPoint = new Point(topLeftCorner.X + topRightRectangle.X, topLeftCorner.Y + topRightRectangle.Y);
            topPoint.X -= cellWidth / 2;
            topPoint.Y -= cellHeight / 2;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 90, 90);

            //drawing of the bottom right corner arc
            Rectangle bottomRightRectangle = Field[Field.GetLength(0) - 2, Field.GetLength(1) - 2];
            topPoint = new Point(topLeftCorner.X + bottomRightRectangle.X, topLeftCorner.Y + bottomRightRectangle.Y);
            topPoint.X -= cellWidth / 2;
            topPoint.Y -= cellHeight / 2;

            cornerRectangle.Location = topPoint;
            graphics.DrawArc(pen, cornerRectangle, 180, 90);
        }

        private void DrawGoalRoomCircles(Graphics graphics, Point topLeftCorner, Pen pen, Rectangle leftSixYard, Rectangle rightSixYard)
        {
            //int cellWidth = Field[0, 0].Width;
            ////left side
            //Rectangle goalRoomRectangle = new Rectangle(topLeftCorner.X + cellWidth * 7, leftSixYard.Y, cellWidth * 5, leftSixYard.Height);
            //goalRoomRectangle.X -= 65;

            //graphics.DrawArc(pen, goalRoomRectangle, -80, 160);
            ////right side
            //goalRoomRectangle = new Rectangle(topLeftCorner.X + rightSixYard.X - cellWidth * 13, rightSixYard.Y, cellWidth * 5, rightSixYard.Height);
            //goalRoomRectangle.X += 35;

            //graphics.DrawArc(pen, goalRoomRectangle, 100, 160);

            int cellWidth = Field[0, 0].Width;
            //left side
            Rectangle goalRoomRectangle = new Rectangle(topLeftCorner.X + cellWidth * 7, leftSixYard.Y, cellWidth * 5, leftSixYard.Height);
            goalRoomRectangle.X -= cellWidth * 2 + (cellWidth / 2);

            graphics.DrawArc(pen, goalRoomRectangle, -90, 180);
            //right side
            goalRoomRectangle = new Rectangle(topLeftCorner.X + rightSixYard.X - cellWidth * 13, rightSixYard.Y, cellWidth * 5, rightSixYard.Height);
            if (isGamePanel)
            {
                goalRoomRectangle.X += cellWidth;
            }

            graphics.DrawArc(pen, goalRoomRectangle, 90, 180);
        }

        /// <summary>
        /// Sets all settings.
        /// </summary>
        /// <param name="settings"></param>
        private void SetSettings(GameSettings settings)
        {
            this.midfieldLineXPosition = settings.MidLineXPosition;
            this.midfieldPointYPosition = settings.MidPointYPosition;
            this.leftGoalHeight = settings.LeftGoalHeight;
            this.leftGoalYPosition = settings.LeftGoalYPosition;
            this.rightGoalHeight = settings.RightGoalHeight;
            this.rightGoalYPosition = settings.RightGoalYPosition;
        }

    }
}
