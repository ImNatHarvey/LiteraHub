using System.Drawing.Drawing2D;

namespace LiteraHub.WinForms.Theming;

public static class Gfx
{
    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        if (d <= 0f)
        {
            path.AddRectangle(r);
            return path;
        }

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // t = 1 gives a, t = 0 gives b.
    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.R * t + b.R * (1 - t)),
            (int)(a.G * t + b.G * (1 - t)),
            (int)(a.B * t + b.B * (1 - t)));
    }

    public static void DrawSpinner(Graphics g, Rectangle bounds, float startAngle, Color color, float thickness)
    {
        var previous = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var inset = (int)Math.Ceiling(thickness / 2f);
        var r = Rectangle.Inflate(bounds, -inset, -inset);
        using var track = new Pen(Color.FromArgb(70, color), thickness);
        using var arc = new Pen(color, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawEllipse(track, r);
        g.DrawArc(arc, r, startAngle, 110f);

        g.SmoothingMode = previous;
    }
}