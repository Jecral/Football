using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class IntegerTextBox : TextBox
    {

        public IntegerTextBox()
        {
            this.KeyPress += new KeyPressEventHandler(IntegerTextBox_KeyPress);
        }

        void IntegerTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            int keyValue = e.KeyChar;
            //MessageBox.Show(keyValue + "");
            if (!((keyValue >= 48 && keyValue <= 57) || keyValue == 8))
            {
                e.Handled = true;
            }
        }

    }
}
