using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace launcher {
    public class CustomLabel : Label {
        public Color outlineForeColor { get; set; }
        public float outlineWidth { get; set; }

        public CustomLabel() {
            outlineForeColor = Color.Black;
            outlineWidth = 3.0f;
        }
        
        protected override void OnPaint(PaintEventArgs pea) {
            pea.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
            using (var gp = new GraphicsPath())
            using (var outline = new Pen(outlineForeColor, outlineWidth)
                { LineJoin = LineJoin.Round })
            using (var sf = new StringFormat())
            using (var foreBrush = new SolidBrush(ForeColor)) {
                sf.Alignment = (StringAlignment) TextAlign;
                sf.LineAlignment = (StringAlignment) TextAlign;
                gp.AddString(Text, Font.FontFamily, (int) Font.Style,
                    Font.Size * 1.5f, ClientRectangle, sf);
                pea.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                pea.Graphics.DrawPath(outline, gp);
                pea.Graphics.FillPath(foreBrush, gp);
            }
        }
    }
}
