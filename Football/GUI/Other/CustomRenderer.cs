using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Football.GUI.Other
{
    class CustomRenderer : ToolStripProfessionalRenderer
    {
        public CustomRenderer(ProfessionalColorTable colorTable)
            : base(colorTable)
        {

        }

        //protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        //{
        //    if (!e.AffectedBounds.Equals(e.ToolStrip.Bounds))
        //    {
        //        base.OnRenderToolStripBorder(e);
        //    }
        //    else
        //    {
        //        ControlPaint.DrawBorder(e.Graphics, e.AffectedBounds, Color.Green, 0, ButtonBorderStyle.None, Color.Green, 0, ButtonBorderStyle.None, Color.Green, 0, ButtonBorderStyle.None, ColorTranslator.FromHtml("#6B260B"), 1, ButtonBorderStyle.Dotted);
        //    }
        //}
    }
}
