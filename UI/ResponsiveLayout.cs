using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CardChess.View
{
    public static class ResponsiveLayout
    {
        private sealed class ControlSnapshot
        {
            public Control Control;
            public Rectangle Bounds;
            public Font BaseFont;
            public Font ScaledFont;
        }

        public static Size GetFittedClientSize(Form form, Size designClientSize)
        {
            Rectangle workingArea = Screen.FromControl(form).WorkingArea;
            float widthScale = (workingArea.Width - 24f) / designClientSize.Width;
            float heightScale = (workingArea.Height - 48f) / designClientSize.Height;
            float scale = Math.Min(1f, Math.Min(widthScale, heightScale));
            scale = Math.Max(0.5f, scale);

            return new Size(
                Math.Max(640, (int)Math.Floor(designClientSize.Width * scale)),
                Math.Max(420, (int)Math.Floor(designClientSize.Height * scale)));
        }

        public static void Attach(Form form, Size designClientSize)
        {
            if (form == null)
                return;

            List<ControlSnapshot> snapshots = Capture(form.Controls);
            bool applyingLayout = false;

            Action apply = () =>
            {
                if (applyingLayout || form.IsDisposed)
                    return;

                applyingLayout = true;
                try
                {
                    float scaleX = form.ClientSize.Width / (float)designClientSize.Width;
                    float scaleY = form.ClientSize.Height / (float)designClientSize.Height;
                    float fontScale = Math.Max(0.65f, Math.Min(scaleX, scaleY));

                    foreach (ControlSnapshot snapshot in snapshots)
                    {
                        if (snapshot.Control.IsDisposed)
                            continue;

                        snapshot.Control.Bounds = new Rectangle(
                            (int)Math.Round(snapshot.Bounds.X * scaleX),
                            (int)Math.Round(snapshot.Bounds.Y * scaleY),
                            Math.Max(1, (int)Math.Round(snapshot.Bounds.Width * scaleX)),
                            Math.Max(1, (int)Math.Round(snapshot.Bounds.Height * scaleY)));

                        if (snapshot.BaseFont != null)
                        {
                            float newSize = Math.Max(7f, snapshot.BaseFont.Size * fontScale);
                            if (snapshot.ScaledFont == null || Math.Abs(snapshot.ScaledFont.Size - newSize) > 0.2f)
                            {
                                Font oldScaledFont = snapshot.ScaledFont;
                                snapshot.ScaledFont = new Font(
                                    snapshot.BaseFont.FontFamily,
                                    newSize,
                                    snapshot.BaseFont.Style,
                                    snapshot.BaseFont.Unit);
                                snapshot.Control.Font = snapshot.ScaledFont;
                                oldScaledFont?.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    applyingLayout = false;
                }
            };

            form.AutoScaleMode = AutoScaleMode.None;
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.MaximizeBox = true;
            form.MinimumSize = new Size(640, 420);
            form.ClientSize = GetFittedClientSize(form, designClientSize);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Resize += (sender, args) => apply();
            form.FormClosed += (sender, args) =>
            {
                foreach (ControlSnapshot snapshot in snapshots)
                {
                    snapshot.ScaledFont?.Dispose();
                    snapshot.ScaledFont = null;
                }
            };
            apply();
        }

        private static List<ControlSnapshot> Capture(Control.ControlCollection controls)
        {
            List<ControlSnapshot> snapshots = new List<ControlSnapshot>();
            foreach (Control control in controls)
            {
                snapshots.Add(new ControlSnapshot
                {
                    Control = control,
                    Bounds = control.Bounds,
                    BaseFont = control.Font
                });

                if (control.HasChildren)
                {
                    snapshots.AddRange(Capture(control.Controls));
                }
            }
            return snapshots;
        }
    }
}
