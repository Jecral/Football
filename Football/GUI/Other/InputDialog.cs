using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class InputDialog : Control
    {
        private static Form box;
        private static Button buttonOk;
        private static Button buttonCancel;

        private static TextBox[] inputFields;

        public static DialogResult ShowBox(string[] titles, out string[] returns)
        {
            box = new Form();
            box.ClientSize = new Size(180, (titles.Length+1)*35);
            box.Text = "Input";
            box.AcceptButton = buttonOk;
            box.CancelButton = buttonCancel;
            box.FormBorderStyle = FormBorderStyle.FixedDialog;
            box.StartPosition = FormStartPosition.CenterScreen;

            InitComponents(titles);
            
            DialogResult rs = box.ShowDialog();
            returns = new string[titles.Length];

            for (int i = 0; i < titles.Length; i++)
            {
                returns[i] = inputFields[i].Text;
            }
            return rs;
        }


        /* Initializes the components. */
        private static void InitComponents(string[] titles)
        {
            Panel rootPanel = new Panel();
            rootPanel.Size = new Size(box.ClientRectangle.Width - 10, box.ClientRectangle.Height - 10);
            rootPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            rootPanel.Location = new Point(5, 5);
            rootPanel.BackColor = Color.Transparent;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.AutoSize = true;
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            inputFields = new TextBox[titles.Length];

            for (int i = 0; i < titles.Length; i++)
            {
                Label inputLabel = new Label();
                inputLabel.AutoSize = true;
                inputLabel.Anchor = AnchorStyles.None;
                inputLabel.Text = titles[i];

                inputFields[i] = new TextBox();
                inputFields[i].Size = new Size(100, 30);

                generalTlp.Controls.Add(inputLabel, 0, i);
                generalTlp.Controls.Add(inputFields[i], 1, i);
            }

            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.AutoSize = true;

            buttonOk = new Button();
            buttonOk.Text = "OK";
            buttonOk.DialogResult = DialogResult.OK;
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonOk.FlatAppearance.MouseOverBackColor = Color.DarkGreen;

            buttonCancel = new Button();
            buttonCancel.Text = "Cancel";
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatAppearance.MouseOverBackColor = Color.Red;

            flp.Controls.Add(buttonOk);
            flp.Controls.Add(buttonCancel);

            generalTlp.Controls.Add(flp, 0, titles.Length);
            generalTlp.SetColumnSpan(flp, 2);

            rootPanel.Controls.Add(generalTlp);
            box.Controls.Add(rootPanel);
        }
    }
}
