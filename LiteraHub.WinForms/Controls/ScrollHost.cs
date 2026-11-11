
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

// Scrolls one content control using a themed ThinScrollBar.
// Automatically hides the scrollbar when content fits.
public sealed class ScrollHost : ThemedUserControl
{
    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly Panel _viewport = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = Padding.Empty,
        AutoScroll = false
    };

    private readonly ThinScrollBar _bar = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };

    private Control? _content;
    private bool _useSurfaceBackground;
    private bool _updatingMetrics;

    public ScrollHost()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 0));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _layout.Controls.Add(_viewport, 0, 0);
        _layout.Controls.Add(_bar, 1, 0);

        Controls.Add(_layout);

        _bar.Visible = false;

        _bar.ValueChanged += (_, _) =>
            PositionContent();

        _viewport.Resize += (_, _) =>
            UpdateMetrics();

        ApplyTheme();
    }

    // Used by popups that should match Theme.Colors.Surface.
    public void UseSurfaceBackground()
    {
        _useSurfaceBackground = true;
        ApplyTheme();
    }

    public void SetContent(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (_content is not null)
        {
            _viewport.Controls.Remove(_content);
        }

        _content = content;

        content.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left |
            AnchorStyles.Right;

        content.Location = Point.Empty;
        content.Width = _viewport.ClientSize.Width;

        _viewport.Controls.Add(content);

        UpdateMetrics();
    }

    // Call after the content height changes.
    public void RefreshMetrics()
    {
        UpdateMetrics();
    }

    private void UpdateMetrics()
    {
        if (_content is null || _updatingMetrics)
            return;

        _updatingMetrics = true;

        try
        {
            int contentHeight = Math.Max(
                0,
                _content.Height);

            int viewportHeight = Math.Max(
                0,
                _viewport.ClientSize.Height);

            bool needsScroll =
                contentHeight > viewportHeight &&
                viewportHeight > 0;

            // Reserve scrollbar width only when necessary.
            int scrollbarWidth = needsScroll
                ? Math.Max(12, (int)Math.Round(
                    12f * DeviceDpi / 96f))
                : 0;

            _layout.ColumnStyles[1].Width =
                scrollbarWidth;

            _bar.Visible = needsScroll;

            _layout.PerformLayout();

            _content.Width =
                _viewport.ClientSize.Width;

            _bar.SetRange(
                contentHeight,
                Math.Max(1, _viewport.ClientSize.Height));

            if (!needsScroll)
                _bar.SetValue(0);

            PositionContent();
        }
        finally
        {
            _updatingMetrics = false;
        }
    }

    private void PositionContent()
    {
        if (_content is null)
            return;

        _content.Top = -_bar.Value;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!_bar.CanScroll)
            return;

        int step = (int)(
            60 * DeviceDpi / 96f);

        _bar.ScrollBy(
            e.Delta > 0
                ? -step
                : step);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        Color background = _useSurfaceBackground
            ? c.Surface
            : c.Background;

        BackColor = background;
        _layout.BackColor = background;
        _viewport.BackColor = background;
        _bar.BackColor = background;

        _bar.Invalidate();
    }
}
