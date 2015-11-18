using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class CustomColorTable : ProfessionalColorTable
    {
        public override Color ButtonSelectedHighlightBorder
        {
            get
            {
                return ColorTranslator.FromHtml("#6B260B");
            }
        }

        public override Color ButtonSelectedHighlight
        {
            get
            {
                return Color.LightGreen;
            }
        }
        //public override Color MenuStripGradientEnd
        //{
        //    get
        //    {
        //        return ColorTranslator.FromHtml("#6B260B");
        //    }
        //}

        //public override Color MenuStripGradientBegin
        //{
        //    get
        //    {
        //        return Color.White;
        //    }
        //}
    }
}
