using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>Two-section details layout with optional fixed-width information column.</summary>
internal sealed class ResponsiveDetailsPair : ThemedUserControl
{
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Panel _body = new() { Margin = Padding.Empty };
    private Control? _left, _right;
    private int _leftHeight, _rightHeight, _gutter;
    private bool _gallery, _arranging;
    public int ContentPadding { get; private set; } = 16;
    public event EventHandler? LayoutUpdated;

    public ResponsiveDetailsPair()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Controls.Add(_scroll);
        _scroll.SetContent(_body);
        _scroll.Resize += (_, _) => Arrange();
        ApplyTheme();
    }

    public void Configure(Control left, Control right, int leftHeight, int rightHeight, bool gallery, int gutter)
    {
        _left = left; _right = right;
        _leftHeight = leftHeight; _rightHeight = rightHeight;
        _gallery = gallery; _gutter = gutter;
        left.Dock = right.Dock = DockStyle.None;
        _body.Controls.AddRange(new[] { left, right });
        _scroll.ReservedRightGutter = gutter;
        Arrange();
    }

    private void Arrange()
    {
        if (_arranging || _left is null || _right is null || IsDisposed || _scroll.Width <= 0 || _scroll.Height <= 0) return;
        _arranging = true;
        try
        {
            int width = Math.Max(1, _scroll.ClientSize.Width - _gutter);
            var layout = new ResponsiveSectionLayout(width);
            ContentPadding = layout.Padding;
            int gap = layout.Gap;
            int viewport = Math.Max(1, _scroll.ClientSize.Height);
            int leftMin = _leftHeight + (ContentPadding - 16) * 2;
            int rightMin = _rightHeight + (ContentPadding - 16) * 2;
            bool stacked = width < (_gallery ? 690 : 650);
            int height;
            if (stacked)
            {
                _left.SetBounds(0, 0, width, leftMin);
                _right.SetBounds(0, leftMin + gap, width, rightMin);
                height = leftMin + gap + rightMin;
            }
            else
            {
                int usable = width - gap;
                int first = _gallery ? Math.Clamp(usable * 36 / 100, 320, 440) : usable / 2;
                height = Math.Max(Math.Max(leftMin, rightMin), viewport - 2);
                _left.SetBounds(0, 0, first, height);
                _right.SetBounds(first + gap, 0, usable - first, height);
            }
            _body.Height = Math.Max(viewport, height + 2);
            _scroll.RefreshMetrics();
            LayoutUpdated?.Invoke(this, EventArgs.Empty);
        }
        finally { _arranging = false; }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        _body.BackColor = Theme.Colors.Background;
    }
}
