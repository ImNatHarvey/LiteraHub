using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ThemePickerDialog : Form
{
    public ThemePreset? SelectedPreset { get; private set; }
    private readonly ThemedWindowControls _close = new() { ShowMinimizeButton = false, ShowMaximizeButton = false };
    public ThemePickerDialog()
    {
        var c = Theme.Colors;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(690, 510);
        BackColor = c.Background;
        Padding = Padding.Empty;
        var stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = c.Accent };
        Controls.Add(stripe);
        var title = new Label { Text = "Choose a theme", Font = Theme.Title, ForeColor = c.Text, AutoSize = false, Location = new Point(24, 16), Size = new Size(570, 34) };
        Controls.Add(title);
        title.MouseDown += BeginDrag;
        MouseDown += BeginDrag;
        _close.Location = new Point(ClientSize.Width - _close.Width - 12, 10);
        _close.Bind(this);
        _close.SetSurfaceColor(c.Background);
        Controls.Add(_close);
        var hint = new Label { Text = "Select a color palette to apply it throughout LiteraHub.", Font = Theme.Body, ForeColor = c.TextMuted, Location = new Point(24, 54), Size = new Size(630, 26) };
        Controls.Add(hint);
        hint.MouseDown += BeginDrag;
        var grid = new TableLayoutPanel { Location = new Point(24, 88), Size = new Size(642, 395), ColumnCount = 3, RowCount = 3, Margin = Padding.Empty };
        for (int i = 0; i < 3; i++) { grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3)); }
        int index = 0;
        foreach (var preset in Theme.AvailablePresets)
        {
            var card = new ThemeCard(preset) { Dock = DockStyle.Fill, Margin = new Padding(4) };
            card.Click += (_, _) => { SelectedPreset = preset; DialogResult = DialogResult.OK; Close(); };
            grid.Controls.Add(card, index % 3, index / 3); index++;
        }
        Controls.Add(grid);
        _close.BringToFront();
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private void BeginDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, 0x00A1, (IntPtr)2, IntPtr.Zero);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.Colors.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
    private sealed class ThemeCard : Button
    {
        private readonly ThemePreset _preset;
        public ThemeCard(ThemePreset preset)
        {
            _preset = preset; Text = ""; AccessibleName = Theme.GetDisplayName(preset);
            FlatStyle = FlatStyle.Flat; Cursor = Cursors.Hand; DoubleBuffered = true;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.GetPalette(_preset); var g = e.Graphics;
            g.Clear(p.Background);
            using var surface = new SolidBrush(p.Surface); using var accent = new SolidBrush(p.Accent); using var alt = new SolidBrush(p.SurfaceAlt);
            g.FillRectangle(surface, 12, 12, 38, 65);
            g.FillRectangle(accent, 18, 23, 26, 8);
            g.FillRectangle(alt, 58, 12, Math.Max(1, Width - 70), 24);
            g.FillRectangle(surface, 58, 43, Math.Max(1, Width - 70), 34);
            g.FillRectangle(accent, 65, 50, Math.Max(1, (Width - 84) / 2), 9);
            bool selected = _preset == Theme.Preset;
            TextRenderer.DrawText(g, Theme.GetDisplayName(_preset) + (selected ? " ✓" : ""), Theme.BodyStrong, new Rectangle(8, 82, Width - 16, Height - 88), p.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            using var border = new Pen(selected || ClientRectangle.Contains(PointToClient(System.Windows.Forms.Cursor.Position)) ? p.Accent : p.Border, selected ? 2 : 1);
            g.DrawRectangle(border, 1, 1, Width - 3, Height - 3);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(4, 4, Width - 8, Height - 8));
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }
    }
}
