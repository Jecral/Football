using Football.Logic;
using Football.Logic.GameObjects.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI
{
    class ActionDialog : Control
    {
        private static Form box;
        private static Button buttonOk;
        private static Button buttonCancel;
        private static ComboBox actionComboBox;
        private static CheckBox replaceCheckBox;

        private static Point mouseDownMousePoint;
        private static Point mouseDownBoxPoint;

        private static ToolTip controlToolTip;

        public static DialogResult ShowBox(Player fromPlayer, Player targetPlayer, int xCoord, int yCoord, Status currentStatus, ref string action, ref bool replace)
        {
            box = new Form();
            box.ShowInTaskbar = false;
            box.Opacity = 0.5;
            box.ClientSize = (fromPlayer.PlayerActions.Count > 0) ? new Size(180, 100) : new Size(180, 75);
            box.Text = "Choose an action!";
            box.FormBorderStyle = FormBorderStyle.None;
            box.AcceptButton = buttonOk;
            box.CancelButton = buttonCancel;

            InitializeComponents(fromPlayer, targetPlayer, xCoord, yCoord);
            InitializeToolTips();
            InitializeEvents();
            FillComboBox(fromPlayer, targetPlayer, currentStatus);
            box.StartPosition = FormStartPosition.Manual;
            box.Location = new Point(xCoord, yCoord);

            DialogResult rs = box.ShowDialog();
            action = actionComboBox.SelectedItem.ToString();
            if (fromPlayer.PlayerActions.Count > 0)
            {
                replace = replaceCheckBox.Checked;
            }
            return rs;
        }

        /// <summary>
        /// Initializes all UI components.
        /// </summary>
        /// <param name="actionPlayer"></param>
        /// <param name="targetPlayer"></param>
        /// <param name="xCoord">The x-coordinate on the parent form where the user released the mouse button.</param>
        /// <param name="yCoord">The y-coordinate on the parent form where the user released the mouse button.</param>
        private static void InitializeComponents(Player actionPlayer, Player targetPlayer, int xCoord, int yCoord)
        {
            box.BackColor = Color.Black;
            box.AllowDrop = true;

            Panel rootPanel = new Panel();
            rootPanel.Size = new Size(box.ClientRectangle.Width - 10, box.ClientRectangle.Height - 10);
            rootPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            rootPanel.Location = new Point(5, 5);
            rootPanel.BackColor = Color.Transparent;
            rootPanel.MouseMove += DialogMouseMove;
            rootPanel.MouseDown += DialogMouseDown;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.AutoSize = true;
            generalTlp.Dock = DockStyle.Fill;
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            generalTlp.MouseMove += DialogMouseMove;
            generalTlp.MouseDown += DialogMouseDown;

            Label actionLabel = new Label();
            actionLabel.ForeColor = Color.White;
            actionLabel.Text = "Action:";
            actionLabel.AutoSize = true;
            actionLabel.Anchor = AnchorStyles.None;
            actionLabel.MouseMove += DialogMouseMove;
            actionLabel.MouseDown += DialogMouseDown;

            actionComboBox = new ComboBox();
            actionComboBox.FlatStyle = FlatStyle.Flat;
            actionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionComboBox.Dock = DockStyle.Fill;
            actionComboBox.BackColor = Color.Black;
            actionComboBox.ForeColor = Color.White;
            actionComboBox.MouseMove += DialogMouseMove;
            actionComboBox.MouseDown += DialogMouseDown;

            replaceCheckBox = new CheckBox();
            replaceCheckBox.Text = "Replace Action";
            replaceCheckBox.AutoSize = true;
            replaceCheckBox.FlatAppearance.CheckedBackColor = Color.Black;
            replaceCheckBox.FlatAppearance.MouseOverBackColor = Color.Green;
            replaceCheckBox.ForeColor = Color.White;
            replaceCheckBox.Anchor = AnchorStyles.None;
            replaceCheckBox.CheckedChanged += replaceCheckBox_CheckedChanged;
            replaceCheckBox.MouseMove += DialogMouseMove;
            replaceCheckBox.MouseDown += DialogMouseDown;

            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.Dock = DockStyle.Right;
            flp.AutoSize = true;
            flp.MouseMove += DialogMouseMove;
            flp.MouseDown += DialogMouseDown;

            buttonOk = new Button();
            buttonOk.Text = "Add";
            buttonOk.DialogResult = DialogResult.OK;
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonOk.ForeColor = Color.White;
            buttonOk.FlatAppearance.MouseOverBackColor = Color.Green;
            buttonOk.Cursor = Cursors.Hand;
            buttonOk.MouseMove += DialogMouseMove;
            buttonOk.MouseDown += DialogMouseDown;

            box.AcceptButton = buttonOk;

            buttonCancel = new Button();
            buttonCancel.Text = "Cancel";
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.ForeColor = Color.White;
            buttonCancel.Cursor = Cursors.Hand;
            buttonCancel.FlatAppearance.MouseOverBackColor = Color.Red;
            buttonCancel.MouseMove += DialogMouseMove;
            buttonCancel.MouseDown += DialogMouseDown;

            flp.Controls.Add(buttonOk);
            flp.Controls.Add(buttonCancel);

            generalTlp.Controls.Add(actionLabel, 0, 0);
            generalTlp.Controls.Add(actionComboBox, 1, 0);
            if (actionPlayer.PlayerActions.Count > 0)
            {
                generalTlp.Controls.Add(replaceCheckBox, 0, 1);
                generalTlp.SetColumnSpan(replaceCheckBox, 2);
                generalTlp.Controls.Add(flp, 0, 2);
                generalTlp.SetColumnSpan(flp, 2);
            }
            else
            {
                generalTlp.Controls.Add(flp, 0, 1);
                generalTlp.SetColumnSpan(flp, 2);
            }
            rootPanel.Controls.Add(generalTlp);
            box.Controls.Add(rootPanel);
        }

        /// <summary>
        /// Initializes all tooltips.
        /// </summary>
        private static void InitializeToolTips()
        {
            controlToolTip = new ToolTip();
            controlToolTip.UseAnimation = true;

            controlToolTip.SetToolTip(buttonOk, "Enqueue the action to the existing actions.");
            controlToolTip.SetToolTip(replaceCheckBox, "Replace all existing actions.");
        }

        private static void InitializeEvents()
        {
            box.MouseDown += DialogMouseDown;
            box.MouseMove += DialogMouseMove;
        }

        /// <summary>
        /// Sets the box's location to the new position relative to the position where the user pressed the mouse down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void DialogMouseMove(object sender, MouseEventArgs e)
        {
            if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left && !actionComboBox.DroppedDown)
            {
                int newX = mouseDownBoxPoint.X - (mouseDownMousePoint.X - box.PointToScreen(e.Location).X);
                int newY = mouseDownBoxPoint.Y - (mouseDownMousePoint.Y - box.PointToScreen(e.Location).Y);

                box.Location = new Point(newX, newY);
            }
        }

        /// <summary>
        /// Saves the current box location as well as the current mouse location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void DialogMouseDown(object sender, MouseEventArgs e)
        {
            mouseDownBoxPoint = box.Location;
            mouseDownMousePoint = box.PointToScreen(e.Location);
        }

        /// <summary>
        /// Fills the combobox with actions the user can choose depending on the current game status.
        /// </summary>
        /// <param name="actionPlayer"></param>
        /// <param name="targetPlayer"></param>
        /// <param name="currentStatus"></param>
        private static void FillComboBox(Player actionPlayer, Player targetPlayer, Status currentStatus)
        {
            if (currentStatus != Status.Normal && actionPlayer.HasBall)
            {
                if (currentStatus == Status.ThrowIn)
                {
                    actionComboBox.Items.AddRange(new String[] { "Throw" });
                }
                else
                {
                    actionComboBox.Items.AddRange(new String[] { "Pass", "Shoot" });
                }
            }
            else
            {
                actionComboBox.Items.AddRange(new String[] { "Run", "Pass", "Shoot" });
                if (targetPlayer != null)
                {
                    actionComboBox.Items.Add("Tackle");
                }
            }

            actionComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Sets buttonOk's text to "Replace" if the replaceCheckBox is checked.
        /// Otherwise to "Add".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void replaceCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            buttonOk.Text = (replaceCheckBox.Checked) ? "Replace" : "Add";

            if (replaceCheckBox.Checked)
            {
                controlToolTip.SetToolTip(buttonOk, "Replace all existing actions");
            }
            else
            {
                controlToolTip.SetToolTip(buttonOk, "Enqueue the action to the existing actions");
            }
        }
    }
}
