using Football.EventArguments;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Multiplayer
{
    class ConnectionForm : Form
    {
        public ConnectionForm(string[,] connections)
        {
            Icon = Properties.Resources.Icon;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Settings";
            Size = new Size(220, 140);
            MaximumSize = new Size(220, 140);

            InitComponents(connections);
        }

        private Button buttonOk;
        private Button buttonCancel;

        public ComboBox IpComboBox { get; set; }
        public ComboBox PortComboBox { get; set; }

        public event EventHandler<ConnectionSettingsEventArgs> SettingsPicked;

        /* Initializes the components. */
        private void InitComponents(string[,] connections)
        {
            Panel rootPanel = new Panel();
            rootPanel.Size = new Size(ClientSize.Width - 10, ClientSize.Height - 10);
            rootPanel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            rootPanel.Location = new Point(5, 5);
            rootPanel.BackColor = Color.Transparent;

            TableLayoutPanel generalTlp = new TableLayoutPanel();
            generalTlp.AutoSize = true;
            generalTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label ipLabel = new Label();
            ipLabel.Text = "IP-Address:";
            ipLabel.AutoSize = true;
            ipLabel.Anchor = AnchorStyles.None;

            IpComboBox = new ComboBox();
            IpComboBox.AutoSize = true;
            IpComboBox.Anchor = AnchorStyles.None;
            IpComboBox.SelectedIndexChanged += ipComboBox_SelectedIndexChanged;

            Label portLabel = new Label();
            portLabel.Text = "Port:";
            portLabel.AutoSize = true;
            portLabel.Anchor = AnchorStyles.None;

            PortComboBox = new ComboBox();
            PortComboBox.AutoSize = true;
            PortComboBox.Anchor = AnchorStyles.None;

            for (int i = 0; i < connections.GetLength(0); i++)
            {
                IpComboBox.Items.Add(connections[i, 0]);
                PortComboBox.Items.Add(connections[i, 1]);
            }

            if (connections.Length > 0)
            {
                IpComboBox.SelectedIndex = 0;
                PortComboBox.SelectedIndex = 0;
            }

            generalTlp.Controls.Add(ipLabel, 0, 0);
            generalTlp.Controls.Add(IpComboBox, 1, 0);
            generalTlp.Controls.Add(portLabel, 0, 1);
            generalTlp.Controls.Add(PortComboBox, 1, 1);

            FlowLayoutPanel flp = new FlowLayoutPanel();
            flp.AutoSize = true;
            flp.Dock = DockStyle.Right;

            buttonOk = new Button();
            buttonOk.Text = "OK";
            buttonOk.DialogResult = DialogResult.OK;
            buttonOk.FlatStyle = FlatStyle.Flat;
            buttonOk.FlatAppearance.MouseOverBackColor = Color.LightGreen;
            buttonOk.Click += buttonOk_Click;
            buttonOk.Cursor = Cursors.Hand;
            AcceptButton = buttonOk;

            buttonCancel = new Button();
            buttonCancel.Text = "Cancel";
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatAppearance.MouseOverBackColor = Color.Red;
            buttonCancel.Click += buttonCancel_Click;
            buttonCancel.Cursor = Cursors.Hand;

            flp.Controls.Add(buttonOk);
            flp.Controls.Add(buttonCancel);

            generalTlp.Controls.Add(flp, 0, 2);
            generalTlp.SetColumnSpan(flp, 2);

            rootPanel.Controls.Add(generalTlp);
            Controls.Add(rootPanel);
        }

        /// <summary>
        /// Closes the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Raises SettingsPicked()-event and closes the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void buttonOk_Click(object sender, EventArgs e)
        {
            ConnectionSettingsEventArgs args = new ConnectionSettingsEventArgs(PortComboBox.Text, IpComboBox.Text);
            SettingsPicked(this, args);
        }

        void ipComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            PortComboBox.SelectedIndex = IpComboBox.SelectedIndex;
        }
    }
}
