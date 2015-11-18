using Football.EventArguments;
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
    public enum FieldComponent
    {
        Nothing = 0,
        MidlineX,
        Midpoint,
        LeftGoalHeight,
        LeftGoalY,
        LeftGoalHeightAndY,
        RightGoalHeight,
        RightGoalY,
        RightGoalHeightAndY,
    }

    class EditableFieldPanel : Panel
    {
        public EditableFieldPanel()
        {
            DoubleBuffered = true;
            MidfieldLineXPosition = 25; //standard 25
            MidfieldPointYPosition = 12; //standard 12
            LeftGoalHeight = 5; //standard 5
            LeftGoalYPosition = 10; //standard 10
            RightGoalHeight = 5; //standard 5
            RightGoalYPosition = 10; //standard 10

            currentFieldComponent = FieldComponent.Nothing;
        }

        private Rectangle[,] fieldCell;
        private Rectangle currentMouseRectangle;
        private Point oldGridLocation;
        private FieldComponent currentFieldComponent;

        public int MidfieldLineXPosition { get; set; }
        public int MidfieldPointYPosition { get; set; }
        public int LeftGoalHeight { get; set; }
        public int LeftGoalYPosition { get; set; }
        public int RightGoalHeight { get; set; }
        public int RightGoalYPosition { get; set; }
        public int Columns { get; set; }
        public int Rows { get; set; }
        public GameSettings CurrentSettings
        {
            get
            {
                return new GameSettings(Columns, Rows, MidfieldLineXPosition, MidfieldPointYPosition, LeftGoalHeight, LeftGoalYPosition, RightGoalHeight, RightGoalYPosition, 7, 100, true, true);
            }
        }

        private bool settingsChanged;
        public bool IsBlocked { get; set; }

        public FieldPanel StadiumPanel;
        public BufferedPanel FieldGrid;
        public event EventHandler<FieldSettingsEventArgs> FieldSettingsChanged;

        public void InitializeComponents(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;

            StadiumPanel = new FieldPanel(false);
            StadiumPanel.Size = new Size(Width, Height);
            StadiumPanel.BackColor = Color.Transparent;
            StadiumPanel.BackgroundImage = Properties.Resources.FieldSettingsBackground;

            int cellWidth = (Width - 60) / columns;
            int cellHeight = (Height - 60)/ rows;

            columns += 2;
            rows += 2;

            FieldGrid = new BufferedPanel();
            FieldGrid.Size = new Size((columns) * cellWidth, (rows) * cellHeight);
            FieldGrid.BackColor = Color.Transparent;
            FieldGrid.Location = new Point(40, 30);
            FieldGrid.Paint += fieldGrid_Paint;
            FieldGrid.MouseMove += FieldGrid_MouseMove;
            FieldGrid.MouseUp += FieldGrid_MouseUp;

            fieldCell = new Rectangle[columns, rows];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Rectangle cell = new Rectangle();
                    cell.Size = new Size(cellWidth, cellHeight);
                    cell.Location = new Point(c * cellWidth, r * cellHeight);
                    fieldCell[c, r] = cell;
                }
            }

            StadiumPanel.Controls.Add(FieldGrid);
            StadiumPanel.UpdateSettings(fieldCell, CurrentSettings, FieldGrid.Location);
            Controls.Add(StadiumPanel);
        }

        /* Draws all game objects. */
        void fieldGrid_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            Rectangle rectangle;
            Pen myPen = new Pen(Color.Black, 1);

            //Draws the current rectangle
            if (!IsBlocked && currentFieldComponent == FieldComponent.Nothing)
            {
                rectangle = currentMouseRectangle;
                rectangle.X += 1;
                rectangle.Y += 1;
                rectangle.Width -= 2;
                rectangle.Height -= 2;
                graphics.DrawRectangle(myPen, rectangle);
            }
        }

        void FieldGrid_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePoint = e.Location;

            Point gridLocation = SearchGridIndex(mousePoint);
            if (gridLocation.X >= 0 && gridLocation.X < fieldCell.GetLength(0) && gridLocation.Y >= 0 && gridLocation.Y < fieldCell.GetLength(1))
            {
                Rectangle rectangle = fieldCell[gridLocation.X, gridLocation.Y];

                if (rectangle.Contains(mousePoint) && !rectangle.Equals(currentMouseRectangle))
                {
                    if (IsBlocked)
                    {
                        FieldGrid.Cursor = Cursors.No;
                    }
                    else
                    {
                        if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
                        {
                            int horizontalDirection = gridLocation.X - oldGridLocation.X;
                            int verticalDirection = gridLocation.Y - oldGridLocation.Y;

                            MoveCurrentFieldComponent(horizontalDirection, verticalDirection);
                            StadiumPanel.UpdateSettings(fieldCell, CurrentSettings, FieldGrid.Location);
                            StadiumPanel.Invalidate(GetChangedRectangle(currentFieldComponent));
                        }
                        else
                        {
                            Rectangle invalidateRectangle = new Rectangle(currentMouseRectangle.Location, new Size(currentMouseRectangle.Width + 1, currentMouseRectangle.Height + 1));
                            FieldGrid.Invalidate(invalidateRectangle);

                            currentMouseRectangle = rectangle;
                            invalidateRectangle = new Rectangle(rectangle.Location, new Size(rectangle.Width + 1, rectangle.Height + 1));
                            FieldGrid.Invalidate(invalidateRectangle);

                            currentFieldComponent = GetFieldComponent(gridLocation);
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }

                        currentMouseRectangle = rectangle;

                        oldGridLocation = gridLocation;
                    }

                }
            }
        }

        /// <summary>
        /// Raises the FieldSetingsChanged()-event with the current Settings saved in the FieldSettingsEventArgs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FieldGrid_MouseUp(object sender, MouseEventArgs e)
        {
            if (settingsChanged && !IsBlocked)
            {
                if (FieldSettingsChanged != null)
                {
                    GameSettings settings = CurrentSettings;
                    FieldSettingsEventArgs args = new FieldSettingsEventArgs(settings);

                    if (FieldSettingsChanged != null)
                    {
                        FieldSettingsChanged(this, args);
                    }
                }
                settingsChanged = false;
            }

            if (!IsBlocked)
            {
                FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
            }
        }

        /// <summary>
        /// Searches the cell's location in the grid
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Point SearchGridIndex(Point position)
        {
            int height = fieldCell[0, 0].Height;
            int width = fieldCell[0, 0].Width;

            return new Point(position.X / width, position.Y / height);
        }

        /// <summary>
        /// Returns which FieldComponent is located at these coordinates.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private FieldComponent GetFieldComponent(Point coordinate)
        {
            if (coordinate.X - 1 == MidfieldLineXPosition) //Check midpoint and midline
            {
                if (coordinate.Y - 1== MidfieldPointYPosition)
                {
                    return FieldComponent.Midpoint;
                }
                else
                {
                    return FieldComponent.MidlineX;
                }
            }
            else if (coordinate.X == 0) //Check left goal
            {
                int leftGoalBottom = LeftGoalYPosition + LeftGoalHeight - 1;
                if (coordinate.Y == LeftGoalYPosition - 1)
                {
                    return FieldComponent.LeftGoalHeightAndY;
                }
                else if (coordinate.Y == LeftGoalYPosition + LeftGoalHeight)
                {
                    return FieldComponent.LeftGoalHeight;
                }
                else if (coordinate.Y >= LeftGoalYPosition && coordinate.Y < LeftGoalYPosition + LeftGoalHeight)
                {
                    return FieldComponent.LeftGoalY;
                }
            }
            else if (coordinate.X == Columns + 1) // Check right goal
            {
                int rightGoalBottom = RightGoalYPosition + RightGoalHeight - 1;
                if (coordinate.Y == RightGoalYPosition - 1)
                {
                    return FieldComponent.RightGoalHeightAndY;
                }
                else if (coordinate.Y == RightGoalYPosition + RightGoalHeight)
                {
                    return FieldComponent.RightGoalHeight;
                }
                else if (coordinate.Y >= RightGoalYPosition && coordinate.Y < RightGoalYPosition + RightGoalHeight)
                {
                    return FieldComponent.RightGoalY;
                }
            }

            return FieldComponent.Nothing;
        }

        /// <summary>
        /// Returns which Cursor the program should show for the specific FieldComponent.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        private Cursor GetFittingCursor(FieldComponent component)
        {
            switch (component)
            {
                case FieldComponent.LeftGoalHeight:
                    return Cursors.SizeNS;
                case FieldComponent.LeftGoalY:
                    return Cursors.Hand;
                case FieldComponent.LeftGoalHeightAndY:
                    return Cursors.SizeNS;
                case FieldComponent.MidlineX:
                    return Cursors.SizeWE;
                case FieldComponent.Midpoint:
                    return Cursors.SizeAll;
                case FieldComponent.RightGoalHeight:
                    return Cursors.SizeNS;
                case FieldComponent.RightGoalY:
                    return Cursors.Hand;
                case FieldComponent.RightGoalHeightAndY:
                    return Cursors.SizeNS;
                default:
                    return Cursors.Cross;
            }
        }

        /// <summary>
        /// Moves the fieldcomponent in the horizontal and vertical direction
        /// </summary>
        /// <param name="horizontalDirection"></param>
        /// <param name="verticalDirection"></param>
        private void MoveCurrentFieldComponent(int horizontalDirection, int verticalDirection)
        {
            if (currentFieldComponent != FieldComponent.Nothing && (horizontalDirection != 0 || verticalDirection != 0))
            {
                settingsChanged = true;

                switch (currentFieldComponent)
                {
                    case FieldComponent.LeftGoalHeight:
                        if (IsValidGoal(LeftGoalYPosition, LeftGoalHeight + verticalDirection))
                        {
                            LeftGoalHeight += verticalDirection;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;

                    case FieldComponent.LeftGoalY:
                        if (IsValidGoal(LeftGoalYPosition + verticalDirection, LeftGoalHeight))
                        {
                            LeftGoalYPosition += verticalDirection;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;

                    case FieldComponent.LeftGoalHeightAndY:
                        if (IsValidGoal(LeftGoalYPosition + verticalDirection, LeftGoalHeight + verticalDirection * -1))
                        {
                            LeftGoalYPosition += verticalDirection;
                            LeftGoalHeight += verticalDirection * -1;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;

                    case FieldComponent.MidlineX:
                        if (IsValidMidline(MidfieldLineXPosition + horizontalDirection, MidfieldPointYPosition))
                        {
                            MidfieldLineXPosition += horizontalDirection;
                        }
                        break;

                    case FieldComponent.Midpoint:
                        if (IsValidMidline(MidfieldLineXPosition + horizontalDirection, MidfieldPointYPosition + verticalDirection))
                        {
                            MidfieldPointYPosition += verticalDirection;
                            MidfieldLineXPosition += horizontalDirection;
                        }
                        break;

                    case FieldComponent.RightGoalHeight:
                        if (IsValidGoal(RightGoalYPosition, RightGoalHeight + verticalDirection))
                        {
                            RightGoalHeight += verticalDirection;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;

                    case FieldComponent.RightGoalY:
                        if (IsValidGoal(RightGoalYPosition + verticalDirection, RightGoalHeight))
                        {
                            RightGoalYPosition += verticalDirection;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;

                    case FieldComponent.RightGoalHeightAndY:
                        if (IsValidGoal(RightGoalYPosition + verticalDirection, RightGoalHeight + verticalDirection * -1))
                        {
                            RightGoalYPosition += verticalDirection;
                            RightGoalHeight += verticalDirection * -1;
                            FieldGrid.Cursor = GetFittingCursor(currentFieldComponent);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Checks whether this bounds are valid goal bounds.
        /// </summary>
        /// <param name="yPosition"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private bool IsValidGoal(int yPosition, int height)
        {
            if (height < 1)
            {
                return false;
            }
            else if (yPosition - 6 <= 1)
            {
                return false;
            }
            else if (yPosition + height + 6 >= fieldCell.GetLength(1) - 1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks whether this bounds are valid midline bounds.
        /// </summary>
        /// <param name="lineXPosition"></param>
        /// <param name="pointYPosition"></param>
        /// <returns></returns>
        private bool IsValidMidline(int lineXPosition, int pointYPosition)
        {
            return !(lineXPosition < 13 || lineXPosition > fieldCell.GetLength(0) - 16 || pointYPosition < 4 || pointYPosition > fieldCell.GetLength(1) - 7);
        }

        private Rectangle GetChangedRectangle(FieldComponent component)
        {
            int cellWidth = fieldCell[0, 0].Width;
            int cellHeight = fieldCell[0, 0].Height;

            Rectangle changedRectangle = new Rectangle(0, 0, 0, 0);
            switch (component)
            {
                case FieldComponent.MidlineX:
                case FieldComponent.Midpoint:
                    Point location = fieldCell[MidfieldLineXPosition - 3, 0].Location;
                    location.X -= 5;

                    Size size = new Size(cellWidth * 17 + 10, (fieldCell.GetLength(1) + 2) * cellHeight);
                    changedRectangle = new Rectangle(location, size);
                    break;

                case FieldComponent.LeftGoalHeight:
                case FieldComponent.LeftGoalHeightAndY:
                case FieldComponent.LeftGoalY:
                    Point lLocation = fieldCell[0, 0].Location;

                    Size lSize = new Size(cellWidth * 17 + 10, (fieldCell.GetLength(1) + 2) * cellHeight);
                    changedRectangle = new Rectangle(lLocation, lSize);
                    break;
                case FieldComponent.RightGoalHeight:
                case FieldComponent.RightGoalHeightAndY:
                case FieldComponent.RightGoalY:
                    Point rLocation = fieldCell[fieldCell.GetLength(0) - 8, 0].Location;

                    Size rSize = new Size(cellWidth * 17 + 10, (fieldCell.GetLength(1) + 2) * cellHeight);
                    changedRectangle = new Rectangle(rLocation, rSize);
                    break;
            }

            return changedRectangle;
        }

        /// <summary>
        /// Updates the saved Settings with the new Settings and refreshed the stadium panel
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="rows"></param>
        /// <param name="midfieldLineXPosition"></param>
        /// <param name="midfieldPointYPosition"></param>
        /// <param name="leftGoalHeight"></param>
        /// <param name="leftGoalYPosition"></param>
        /// <param name="rightGoalHeight"></param>
        /// <param name="rightGoalYPosition"></param>
        public void UpdateSettings(GameSettings settings)
        {
            Columns = settings.Columns;
            Rows = settings.Rows;
            MidfieldLineXPosition = settings.MidLineXPosition;
            MidfieldPointYPosition = settings.MidPointYPosition;
            LeftGoalHeight = settings.LeftGoalHeight;
            LeftGoalYPosition = settings.LeftGoalYPosition;
            RightGoalHeight = settings.RightGoalHeight;
            RightGoalYPosition = settings.RightGoalYPosition;

            StadiumPanel.Invoke((MethodInvoker)(() =>
            {
                StadiumPanel.UpdateSettings(fieldCell, settings, FieldGrid.Location);
                StadiumPanel.Refresh();
            }));
        }
    }
}
