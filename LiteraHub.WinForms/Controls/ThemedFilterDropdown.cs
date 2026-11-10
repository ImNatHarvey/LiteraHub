
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedFilterDropdown : Button
{
    private readonly string[] _options;
    private int _selectedIndex;
    private Form? _popup;

    public event EventHandler? SelectedIndexChanged;

    [DesignerSerializationVisibility(
        DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= _options.Length)
                return;

            if (_selectedIndex == value)
                return;

            _selectedIndex = value;
            UpdateCaption();

            SelectedIndexChanged?.Invoke(
                this, EventArgs.Empty);
        }
    }

    public ThemedFilterDropdown()
        : this(new[] { "All", "Active", "Inactive" })
    {
    }

    public ThemedFilterDropdown(string[] options)
    {
        if (options is null || options.Length == 0)
        {
            throw new ArgumentException(
                "At least one option is required.",
                nameof(options));
        }

        _options = (string[])options.Clone();

        FlatStyle = FlatStyle.Flat;
        Cursor = Cursors.Hand;
        TextAlign = ContentAlignment.MiddleCenter;
        TabStop = true;

        UpdateCaption();

        Click += (_, _) => OpenPopup();

        Theme.Changed += HandleThemeChanged;

        ApplyTheme();
    }

    private void UpdateCaption()
    {
        Text = _options[_selectedIndex] + "  ▾";

        AccessibleName =
            $"Status filter: {_options[_selectedIndex]}";
    }

    private void OpenPopup()
    {
        if (_popup is not null || !Enabled)
            return;

        var c = Theme.Colors;

        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(
                Math.Max(Width, 125),
                _options.Length * 38 + 2),
            TopMost = true
        };

        _popup = popup;

        var list = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = _options.Length,
            BackColor = c.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        list.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < _options.Length; i++)
        {
            int index = i;

            list.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100f / _options.Length));

            var item = new Button
            {
                Text = (i == SelectedIndex ? "✓  " : "    ")
                    + _options[i],
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Theme.Body,
                Cursor = Cursors.Hand,
                BackColor = i == SelectedIndex
                    ? c.AccentSoft
                    : c.Surface,
                ForeColor = c.Text,
                Margin = Padding.Empty,
                TabStop = true
            };

            item.FlatAppearance.BorderSize = 0;

            item.FlatAppearance.MouseOverBackColor =
                c.AccentSoft;

            item.Click += (_, _) =>
            {
                SelectedIndex = index;
                popup.Close();
            };

            list.Controls.Add(item, 0, i);
        }

        popup.Controls.Add(list);

        Point screen = PointToScreen(
            new Point(0, Height));

        Rectangle area =
            Screen.FromControl(this).WorkingArea;

        int x = Math.Min(
            screen.X,
            area.Right - popup.Width);

        int y = screen.Y;

        if (y + popup.Height > area.Bottom)
        {
            y = PointToScreen(
                Point.Empty).Y - popup.Height;
        }

        popup.Location = new Point(
            Math.Max(area.Left, x),
            Math.Max(area.Top, y));

        popup.Deactivate += (_, _) =>
            popup.Close();

        popup.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup))
                _popup = null;

            popup.Dispose();
        };

        popup.Show(FindForm());
        popup.Activate();
    }

    private void HandleThemeChanged(
        object? sender,
        EventArgs e)
    {
        if (IsDisposed)
            return;

        _popup?.Close();

        ApplyTheme();
    }

    public void ApplyTheme()
    {
        var c = Theme.Colors;

        BackColor = c.Surface;
        ForeColor = c.Text;
        Font = Theme.Body;

        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = c.Border;

        FlatAppearance.MouseOverBackColor =
            c.SurfaceAlt;

        FlatAppearance.MouseDownBackColor =
            c.AccentSoft;

        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= HandleThemeChanged;
            _popup?.Close();
        }

        base.Dispose(disposing);
    }
}
