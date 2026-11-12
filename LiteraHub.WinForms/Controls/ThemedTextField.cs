using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedTextField : ThemedUserControl
{
    private const int EmGetFirstVisibleLine = 0x00CE;
    private const int EmLineScroll = 0x00B6;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr handle, int message, IntPtr wParam, IntPtr lParam);

    private readonly TextBox _input = new()
    {
        BorderStyle = BorderStyle.None,
        Multiline = false,
        ScrollBars = ScrollBars.None,
        WordWrap = true,
        Margin = Padding.Empty
    };

    private readonly ScrollTrack _scroll = new();
    private bool _focused;
    private bool _multiline;
    private bool _updatingScroll;

    public new event EventHandler? TextChanged;

    [AllowNull]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
        get => _input.Text;
        set => _input.Text = value ?? "";
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxLength
    {
        get => _input.MaxLength;
        set => _input.MaxLength = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;
            _input.Multiline = value;
            _input.ScrollBars = ScrollBars.None;
            LayoutInput();
        }
    }

    public ThemedTextField()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Height = 40;
        MinimumSize = new Size(80, 40);
        Cursor = Cursors.IBeam;

        Controls.Add(_input);
        Controls.Add(_scroll);
        _scroll.Visible = false;
        _scroll.ScrollRequested += ScrollToLine;

        _input.TextChanged += (_, _) =>
        {
            UpdateScroll();
            TextChanged?.Invoke(this, EventArgs.Empty);
        };
        _input.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _input.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        _input.KeyUp += (_, _) => UpdateScroll();
        _input.MouseUp += (_, _) => UpdateScroll();
        _input.MouseWheel += (_, _) =>
        {
            if (IsHandleCreated)
                BeginInvoke((Action)UpdateScroll);
        };

        Click += (_, _) => _input.Focus();
        Resize += (_, _) => LayoutInput();
        LayoutInput();
    }

    public void FocusInput() => _input.Focus();

    private int VisibleLines => Math.Max(1,
        _input.ClientSize.Height / Math.Max(1, _input.Font.Height));

    private int TotalLines
    {
        get
        {
            if (!_input.IsHandleCreated)
                return 1;
            return Math.Max(1,
                _input.GetLineFromCharIndex(_input.TextLength) + 1);
        }
    }

    private int FirstVisibleLine => _input.IsHandleCreated
        ? SendMessage(_input.Handle, EmGetFirstVisibleLine,
            IntPtr.Zero, IntPtr.Zero).ToInt32()
        : 0;

    private void ScrollToLine(int target)
    {
        if (!_input.IsHandleCreated)
            return;
        int max = Math.Max(0, TotalLines - VisibleLines);
        int difference = Math.Clamp(target, 0, max) - FirstVisibleLine;
        if (difference != 0)
            SendMessage(_input.Handle, EmLineScroll,
                IntPtr.Zero, new IntPtr(difference));
        UpdateScroll();
    }

    private void UpdateScroll()
    {
        if (_updatingScroll || IsDisposed || Disposing)
            return;
        _updatingScroll = true;
        try
        {
            bool needsScroll = _multiline &&
                TotalLines > VisibleLines;
            if (_scroll.Visible != needsScroll)
            {
                _scroll.Visible = needsScroll;
                LayoutInputCore();
            }
            _scroll.Configure(TotalLines, VisibleLines, FirstVisibleLine);
        }
        finally
        {
            _updatingScroll = false;
        }
    }

    private void LayoutInput()
    {
        if (IsDisposed || Disposing)
            return;
        LayoutInputCore();
        UpdateScroll();
    }

    private void LayoutInputCore()
    {
        const int left = 12;
        const int right = 12;
        const int barWidth = 12;
        int width = Math.Max(1, ClientSize.Width - left - right -
            (_multiline && _scroll.Visible ? barWidth + 3 : 0));

        if (_multiline)
        {
            _input.SetBounds(left, 10, width,
                Math.Max(1, ClientSize.Height - 20));
            _scroll.SetBounds(ClientSize.Width - right - barWidth,
                10, barWidth, Math.Max(1, ClientSize.Height - 20));
            _scroll.BringToFront();
        }
        else
        {
            _scroll.Visible = false;
            int height = _input.PreferredHeight;
            _input.SetBounds(left,
                Math.Max(0, (ClientSize.Height - height) / 2),
                width, height);
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Surface;
        _input.BackColor = c.Surface;
        _input.ForeColor = c.Text;
        _input.Font = Theme.Body;
        _scroll.BackColor = c.Surface;
        _scroll.Invalidate();
        LayoutInput();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var c = Theme.Colors;
        using var pen = new Pen(_focused ? c.Accent : c.Border);
        var rect = ClientRectangle;
        rect.Width--;
        rect.Height--;
        if (rect.Width > 0 && rect.Height > 0)
            e.Graphics.DrawRectangle(pen, rect);
    }

    private sealed class ScrollTrack : Control
    {
        private int _total = 1;
        private int _visible = 1;
        private int _first;
        private bool _dragging;
        private int _dragStart;
        private int _firstAtDrag;

        public event Action<int>? ScrollRequested;

        public ScrollTrack()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public void Configure(int total, int visible, int first)
        {
            _total = Math.Max(1, total);
            _visible = Math.Max(1, visible);
            _first = Math.Clamp(first, 0,
                Math.Max(0, _total - _visible));
            Invalidate();
        }

        private Rectangle Thumb
        {
            get
            {
                int height = Math.Clamp(
                    (int)Math.Round(Height * Math.Min(1d,
                        (double)_visible / _total)), 20, Math.Max(20, Height));
                height = Math.Min(Height, height);
                int travel = Math.Max(0, Height - height);
                int max = Math.Max(1, _total - _visible);
                int top = (int)Math.Round(travel * (double)_first / max);
                return new Rectangle(2, top, Math.Max(3, Width - 4), height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var c = Theme.Colors;
            e.Graphics.Clear(c.Surface);
            using var track = new SolidBrush(c.SurfaceAlt);
            using var thumb = new SolidBrush(c.Accent);
            e.Graphics.FillRectangle(track, ClientRectangle);
            e.Graphics.FillRectangle(thumb, Thumb);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            Capture = true;
            _dragStart = e.Y;
            _firstAtDrag = _first;
            if (!Thumb.Contains(e.Location))
                MoveTo(e.Y - Thumb.Height / 2);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging) return;
            int travel = Math.Max(1, Height - Thumb.Height);
            int max = Math.Max(0, _total - _visible);
            int target = _firstAtDrag +
                (int)Math.Round((double)(e.Y - _dragStart) * max / travel);
            ScrollRequested?.Invoke(Math.Clamp(target, 0, max));
        }

        private void MoveTo(int y)
        {
            int travel = Math.Max(1, Height - Thumb.Height);
            int max = Math.Max(0, _total - _visible);
            ScrollRequested?.Invoke(Math.Clamp(
                (int)Math.Round((double)y * max / travel), 0, max));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
        }
    }
}
