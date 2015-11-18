using Football.GUI.Other;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Multiplayer
{
    class ChatForm : Form
    {
        public ChatForm(TabControl currentChats)
        {
            Opacity = 0.95;
            Icon = Properties.Resources.Icon;
            Text = "Chat";
            Size = new Size(600, 400);
            StartPosition = FormStartPosition.CenterScreen;
            CurrentChats = currentChats;
            InitializeComponents();
            FormClosing += ChatForm_FormClosing;
            Resize += ChatForm_Resize;

            AcceptButton = ((ChatTabPage)CurrentChats.SelectedTab).SendButton;
        }

        public TabControl CurrentChats { get; set; }

        /// <summary>
        /// Raises the ChatClosed()-event if the FromWindowState is FormWindowState.Minimized.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChatForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ChatClosed(this, EventArgs.Empty);
                Hide();
            }
        }

        public event EventHandler ChatClosed;

        /// <summary>
        /// Raises the ChatClosed()-event and hides the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ChatClosed(this, EventArgs.Empty);
            e.Cancel = true;
            Hide();
        }

        private void InitializeComponents()
        {
            BackgroundImage = Properties.Resources.MultiplayerLobby;
            CurrentChats.Dock = DockStyle.Fill;
            CurrentChats.SelectedIndexChanged += CurrentChats_SelectedIndexChanged;
            Controls.Add(CurrentChats);
        }

        void CurrentChats_SelectedIndexChanged(object sender, EventArgs e)
        {
            AcceptButton = ((ChatTabPage)CurrentChats.SelectedTab).SendButton;
        }
    }
}
