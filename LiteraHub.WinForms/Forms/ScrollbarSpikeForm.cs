using System.Runtime.InteropServices;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

// Temporary form for Step 4B. Compares ways to get dark scrollbars. Deleted once one option is chosen.
public sealed class ScrollbarSpikeForm : Form
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    // DataGridView draws its scrollbars as child controls, so they need the theme applied separately.
    // UNVERIFIED: HorizontalScrollBar and VerticalScrollBar are protected members of DataGridView.
    private sealed class SpikeGrid : DataGridView
    {
        public void ApplyScrollbarTheme(string name)
        {
            SetWindowTheme(Handle, name, null);
            if (VerticalScrollBar.IsHandleCreated) SetWindowTheme(VerticalScrollBar.Handle, name, null);
            if (HorizontalScrollBar.IsHandleCreated) SetWindowTheme(HorizontalScrollBar.Handle, name, null);
        }
    }

    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
    private readonly FlowLayoutPanel _toolbar = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        WrapContents = false,
        Padding = new Padding(Space.S)
    };
    private readonly TableLayoutPanel _body = new() { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
    private readonly Button _light = new() { Text = "Light", AutoSize = true };
    private readonly Button _dark = new() { Text = "Dark", AutoSize = true };
    private readonly CheckBox _toggle = new() { Text = "Apply DarkMode_Explorer", AutoSize = true };
    private readonly Label _info = new() { AutoSize = true, Padding = new Padding(Space.M, 6, 0, 0) };
    private readonly Label[] _headers =
    {
        new() { Text = "1. DataGridView", AutoSize = true },
        new() { Text = "2. ListView", AutoSize = true },
        new() { Text = "3. AutoScroll Panel", AutoSize = true },
        new() { Text = "4. Custom ThinScrollBar", AutoSize = true }
    };
    private readonly SpikeGrid _grid = new();
    private readonly ListView _list = new();
    private readonly Panel _panel = new() { AutoScroll = true };
    private readonly ScrollHost _host = new();
    private readonly Panel _cards = new() { Height = 80 * 64 };
    private readonly List<Label> _panelLabels = new();
    private readonly List<(Panel Card, Label Text)> _cardItems = new();

    public ScrollbarSpikeForm()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Scrollbar spike (Step 4B)";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildContent();

        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        _body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _info.Text = $"Windows {Environment.OSVersion.Version} (build 22000 or higher = Windows 11)";
        _light.Click += (_, _) => Theme.SetMode(ThemeMode.Light);
        _dark.Click += (_, _) => Theme.SetMode(ThemeMode.Dark);
        _toggle.CheckedChanged += (_, _) => ApplyNativeTheme();

        _toolbar.Controls.Add(_light);
        _toolbar.Controls.Add(_dark);
        _toolbar.Controls.Add(_toggle);
        _toolbar.Controls.Add(_info);

        for (var i = 0; i < _headers.Length; i++)
        {
            _headers[i].Margin = new Padding(Space.S, Space.S, Space.S, Space.XS);
            _body.Controls.Add(_headers[i], i, 0);
        }

        foreach (var (control, column) in new (Control, int)[]
                 {
                     (_grid, 0), (_list, 1), (_panel, 2), (_host, 3)
                 })
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(Space.S);
            _body.Controls.Add(control, column, 1);
        }

        _root.Controls.Add(_toolbar, 0, 0);
        _root.Controls.Add(_body, 0, 1);
        Controls.Add(_root);

        Theme.Changed += OnThemeChanged;
        ResumeLayout(performLayout: true);
    }

    private void BuildContent()
    {
        // 1. DataGridView
        _grid.Columns.Add("title", "Title");
        _grid.Columns.Add("author", "Author");
        _grid.Columns.Add("status", "Status");
        for (var i = 0; i < 300; i++)
            _grid.Rows.Add($"Book {i + 1}", $"Author {i % 17}", i % 5 == 0 ? "Damaged" : "Good Condition");
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.RowHeadersVisible = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;

        // 2. ListView
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.BorderStyle = BorderStyle.None;
        _list.Columns.Add("Title", 120);
        _list.Columns.Add("Status", 100);
        for (var i = 0; i < 300; i++)
            _list.Items.Add(new ListViewItem(new[] { $"Book {i + 1}", i % 5 == 0 ? "Damaged" : "Good Condition" }));

        // 3. AutoScroll Panel
        for (var i = 0; i < 80; i++)
        {
            var label = new Label
            {
                Text = $"Row {i + 1}",
                AutoSize = true,
                Location = new Point(Space.M, Space.M + i * 28)
            };
            _panelLabels.Add(label);
            _panel.Controls.Add(label);
        }

        // 4. Custom scroll host with simple "cards"
        _cards.Width = 200;
        for (var i = 0; i < 80; i++)
        {
            var card = new Panel
            {
                Location = new Point(0, i * 64),
                Size = new Size(200, 56),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var text = new Label
            {
                Text = $"Card {i + 1}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Space.M, 0, 0, 0)
            };
            card.Controls.Add(text);
            _cards.Controls.Add(card);
            _cardItems.Add((card, text));
        }

        _host.SetContent(_cards);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var scale = DeviceDpi / 96f;
        var work = Screen.FromControl(this).WorkingArea;
        var frame = new Size(Width - ClientSize.Width, Height - ClientSize.Height);
        ClientSize = new Size(
            Math.Min((int)(1100 * scale), work.Width - frame.Width),
            Math.Min((int)(600 * scale), work.Height - frame.Height));
        CenterToParent();

        ApplyColors();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyNativeTheme();
        _host.RefreshMetrics();
    }

    private void ApplyNativeTheme()
    {
        var name = _toggle.Checked ? "DarkMode_Explorer" : "Explorer";
        _grid.ApplyScrollbarTheme(name);
        SetWindowTheme(_list.Handle, name, null);
        SetWindowTheme(_panel.Handle, name, null);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyColors();

    private void ApplyColors()
    {
        var c = Theme.Colors;
        SuspendLayout();

        BackColor = c.Background;
        ForeColor = c.Text;
        _root.BackColor = c.Background;
        _toolbar.BackColor = c.Background;
        _body.BackColor = c.Background;

        foreach (var button in new[] { _light, _dark })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = c.SurfaceAlt;
            button.ForeColor = c.Text;
            button.FlatAppearance.BorderColor = c.Border;
        }

        _toggle.BackColor = c.Background;
        _toggle.ForeColor = c.Text;
        _info.BackColor = c.Background;
        _info.ForeColor = c.TextMuted;
        foreach (var header in _headers)
        {
            header.BackColor = c.Background;
            header.ForeColor = c.TextMuted;
        }

        _grid.BackgroundColor = c.Surface;
        _grid.GridColor = c.Border;
        _grid.DefaultCellStyle.BackColor = c.Surface;
        _grid.DefaultCellStyle.ForeColor = c.Text;
        _grid.DefaultCellStyle.SelectionBackColor = c.AccentSoft;
        _grid.DefaultCellStyle.SelectionForeColor = c.Text;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = c.SurfaceAlt;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = c.Text;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = c.SurfaceAlt;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = c.Text;

        _list.BackColor = c.Surface;
        _list.ForeColor = c.Text;

        _panel.BackColor = c.Surface;
        foreach (var label in _panelLabels)
        {
            label.BackColor = c.Surface;
            label.ForeColor = c.Text;
        }

        _cards.BackColor = c.Background;
        foreach (var (card, text) in _cardItems)
        {
            card.BackColor = c.Surface;
            text.BackColor = c.Surface;
            text.ForeColor = c.Text;
        }

        ResumeLayout(performLayout: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Theme.Changed -= OnThemeChanged;
        base.Dispose(disposing);
    }
}