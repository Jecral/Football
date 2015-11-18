using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class FilterTextBox : TextBox
    {
        public Regex Filter { get; set; }

        public FilterTextBox()
        {
            Filter = new Regex(".*");
            this.KeyDown += FilterTextBox_KeyDown;
        }

        void FilterTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Filter.IsMatch(Text + (char)e.KeyCode))
            {
                e.Handled = true;
            }
        }
    }
}
