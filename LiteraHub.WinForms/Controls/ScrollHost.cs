using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

// Scrolls one content control (for example a list of cards) with a ThinScrollBar instead of a native scrollbar.
public sealed class ScrollHost : ThemedUserControl
{
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
    private readonly Panel _viewport = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly ThinScrollBar _bar = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private Control? _content;

    public ScrollHost()
    {
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.Controls.Add(_viewport, 0, 0);
        _layout.Controls.Add(_bar, 1, 0);
        Controls.Add(_layout);

        _bar.ValueChanged += (_, _) => PositionContent();
        _viewport.Resize += (_, _) => UpdateMetrics();
    }

    // The content keeps its own height; the host sets its width and moves it up and down.
    public void SetContent(Control content)
    {
        if (_content is not null) _viewport.Controls.Remove(_content);

        _content = content;
        content.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        content.Location = Point.Empty;
        content.Width = _viewport.ClientSize.Width;
        _viewport.Controls.Add(content);
        UpdateMetrics();
    }

    // Call after the content's height changes.
    public void RefreshMetrics() => UpdateMetrics();

    private void UpdateMetrics()
    {
        if (_content is null) return;
        _content.Width = _viewport.ClientSize.Width;
        _bar.SetRange(_content.Height, _viewport.ClientSize.Height);
        PositionContent();
    }

    private void PositionContent()
    {
        if (_content is not null) _content.Top = -_bar.Value;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        var step = (int)(60 * DeviceDpi / 96f);
        _bar.ScrollBy(e.Delta > 0 ? -step : step);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _viewport.BackColor = c.Background;
        _bar.BackColor = c.Background;
        _bar.Invalidate();
    }
}