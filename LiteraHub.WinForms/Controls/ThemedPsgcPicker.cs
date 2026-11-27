using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Searchable, single-selection geographic picker. Options use stable PSGC codes.</summary>
public sealed class ThemedPsgcPicker : Button
{
    public sealed record Option(string Code, string Name);
    private readonly string _placeholder;
    private Option[] _options = [];
    private Form? _popup;
    public string? SelectedCode { get; private set; }
    public string SelectedName => _options.FirstOrDefault(x => x.Code == SelectedCode)?.Name ?? "";
    public event EventHandler? SelectionChanged;

    public ThemedPsgcPicker(string placeholder)
    {
        _placeholder = placeholder;
        UseMnemonic = false;
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
        AutoEllipsis = true;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(10, 0, 8, 0);
        Cursor = Cursors.Hand;
        Click += (_, _) => OpenPicker();
        Theme.Changed += ThemeChanged;
        ApplyTheme();
        UpdateCaption();
    }
    public void SetOptions(IEnumerable<Option> options, string? selected = null)
    {
        _popup?.Close();
        _options = options.ToArray();
        SelectedCode = _options.Any(x => x.Code == selected) ? selected : null;
        UpdateCaption();
    }
    private void UpdateCaption()
    {
        Text = (SelectedCode is null ? _placeholder : SelectedName) + "  ▾";
        AccessibleName = Text;
        Enabled = _options.Length > 0;
        Invalidate();
    }
    public void ApplyTheme()
    {
        var c = Theme.Colors;
        Font = Theme.Body;
        BackColor = c.Surface;
        ForeColor = c.Text;
        FlatAppearance.BorderColor = c.Border;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.MouseOverBackColor = c.AccentSoft;
        FlatAppearance.MouseDownBackColor = c.AccentSoft;
    }
    private void ThemeChanged(object? sender, EventArgs e)
    {
        _popup?.Close();
        ApplyTheme();
    }
    private void OpenPicker()
    {
        if (_popup is not null || !Enabled || _options.Length == 0) return;
        var c = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(Math.Max(Width, 300), 294),
            KeyPreview = true
        };
        _popup = popup;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = c.Surface,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var search = new ThemedSearchBox { Dock = DockStyle.Fill, Placeholder = "Search name or code...", Margin = new Padding(0, 0, 0, 8) };
        var scroll = new ScrollHost { Dock = DockStyle.Fill, Margin = Padding.Empty };
        scroll.UseSurfaceBackground();
        var panel = new Panel { BackColor = c.Surface, Margin = Padding.Empty };
        scroll.SetContent(panel);
        layout.Controls.Add(search, 0, 0); layout.Controls.Add(scroll, 0, 1); popup.Controls.Add(layout);
        void SelectOption(Option option)
        {
            bool changed = SelectedCode != option.Code;
            SelectedCode = option.Code;
            UpdateCaption();
            popup.Close();
            if (changed) SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        Option[] filtered = [];
        void Populate()
        {
            string q = search.Text.Trim();
            filtered = _options.Where(x => x.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) || x.Code.Contains(q, StringComparison.Ordinal)).ToArray();
            panel.SuspendLayout();
            try
            {
                foreach (Control old in panel.Controls.Cast<Control>().ToArray()) old.Dispose();
                int y = 0;
                foreach (var option in filtered)
                {
                    var button = new Button
                    {
                        Text = option.Name,
                        AccessibleName = option.Name,
                        UseMnemonic = false,
                        AutoEllipsis = true,
                        FlatStyle = FlatStyle.Flat,
                        UseVisualStyleBackColor = false,
                        Font = Theme.Body,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(8, 0, 8, 0),
                        Cursor = Cursors.Hand,
                        BackColor = option.Code == SelectedCode ? c.AccentSoft : c.Surface,
                        ForeColor = c.Text,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                    };
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.MouseOverBackColor = c.AccentSoft;
                    button.SetBounds(0, y, Math.Max(1, panel.ClientSize.Width), 36); y += 36;
                    button.Click += (_, _) => SelectOption(option);
                    button.MouseWheel += (_, e) => scroll.ScrollByPixels(e.Delta > 0 ? -72 : 72);
                    panel.Controls.Add(button);
                }
                if (filtered.Length == 0)
                {
                    panel.Controls.Add(new Label
                    {
                        Text = "No matching locations",
                        ForeColor = c.TextMuted,
                        Font = Theme.Body,
                        AutoSize = true,
                        Location = new Point(8, 8)
                    });
                    y = 36;
                }
                panel.Height = y;
            }
            finally { panel.ResumeLayout(); }
            scroll.RefreshMetrics();
            scroll.ScrollByPixels(-1000000);
        }
        search.TextChanged += (_, _) => Populate();
        popup.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { e.Handled = true; popup.Close(); }
            else if (e.KeyCode == Keys.Enter && filtered.Length == 1) { e.Handled = true; e.SuppressKeyPress = true; SelectOption(filtered[0]); }
        };
        var area = Screen.FromControl(this).WorkingArea;
        popup.Width = Math.Min(popup.Width, area.Width);
        popup.Height = Math.Min(popup.Height, area.Height);
        var below = PointToScreen(new Point(0, Height));
        int popupY = below.Y + popup.Height <= area.Bottom ? below.Y : PointToScreen(Point.Empty).Y - popup.Height;
        popup.Location = new Point(Math.Clamp(below.X, area.Left, area.Right - popup.Width), Math.Clamp(popupY, area.Top, area.Bottom - popup.Height));
        popup.Deactivate += (_, _) => popup.Close();
        popup.FormClosed += (_, _) => { if (ReferenceEquals(_popup, popup)) _popup = null; popup.Dispose(); };
        popup.Shown += (_, _) => { Populate(); search.FocusInput(); };
        var owner = FindForm();
        if (owner is null) popup.Show(); else popup.Show(owner);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Theme.Changed -= ThemeChanged; _popup?.Close(); }
        base.Dispose(disposing);
    }
}
