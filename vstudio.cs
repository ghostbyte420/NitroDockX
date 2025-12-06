using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NitroDock
{
    [ToolboxItem(true)]
    public class OpacityPanel : Panel
    {
        // Opacity Control for User Controls
        // 0.35f == 35% opacity || 0.50f == 50% || 1.0f == 100%
        private float _opacity = 0.50f;

        public OpacityPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint,
                true);
            BackColor = Color.Transparent;
        }

        [Category("Appearance")]
        [Description("Sets the opacity of the panel (0.0 to 1.0)")]
        [DefaultValue(0.35f)]
        public float Opacity
        {
            get { return _opacity; }
            set
            {
                if (value < 0.0f) _opacity = 0.0f;
                else if (value > 1.0f) _opacity = 1.0f;
                else _opacity = value;
                Invalidate(); // Redraw the panel
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Let the parent draw its background first
            base.OnPaintBackground(e);

            // Draw a semi-transparent overlay
            if (_opacity > 0.0f)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb((int)(_opacity * 255), Color.Black)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }
    }
}
