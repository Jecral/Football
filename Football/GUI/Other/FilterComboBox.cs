using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class FilterComboBox : ComboBox
    {
        public Regex Filter { get; set; }

        public FilterComboBox()
        {
            Filter = new Regex(".*");
            KeyPress += FilterComboBox_KeyPress;
        }

        void FilterComboBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Filter.IsMatch(Text + e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
