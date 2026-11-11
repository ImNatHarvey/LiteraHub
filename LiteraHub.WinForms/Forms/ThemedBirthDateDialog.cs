
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ThemedBirthDateDialog : Form
{
    private DateTime _month;
    private DateTime _selection;
    private readonly CalendarSurface _surface;

    private DateTime DisplayMonth => _month;

    private DateTime SelectedCalendarDay
    {
        get => _selection;
        set => _selection = value;
    }

    public DateTime? SelectedDate { get; private set; }

    public ThemedBirthDateDialog(DateTime initial)
    {
        _selection = initial.Date > DateTime.Today
            ? DateTime.Today
            : initial.Date;

        _month = new DateTime(
            _selection.Year,
            _selection.Month,
            1);

        Text = "Select Birth Date";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(310, 322);
        Font = Theme.Body;
        BackColor = Theme.Colors.Background;
        ForeColor = Theme.Colors.Text;

        var previous = MakeButton("‹", 12, 12, 40, 36);
        var next = MakeButton("›", 258, 12, 40, 36);

        var monthLabel = new Label
        {
            Bounds = new Rectangle(54, 12, 202, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Theme.Colors.Text,
            Font = Theme.BodyStrong
        };

        _surface = new CalendarSurface(this)
        {
            Bounds = new Rectangle(12, 54, 286, 205)
        };

        void RefreshMonth()
        {
            monthLabel.Text = _month.ToString(
                "MMMM yyyy",
                CultureInfo.CurrentCulture);

            _surface.Invalidate();
        }

        previous.Click += (_, _) =>
        {
            if (_month.Year > 1 || _month.Month > 1)
            {
                _month = _month.AddMonths(-1);
                RefreshMonth();
            }
        };

        next.Click += (_, _) =>
        {
            if (_month < new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1))
            {
                _month = _month.AddMonths(1);
                RefreshMonth();
            }
        };

        var clear = MakeButton("Clear", 12, 273, 130, 36);
        var select = MakeButton("Select Date", 150, 273, 148, 36);

        select.BackColor = Theme.Colors.Accent;
        select.ForeColor = Theme.Colors.OnAccent;
        select.FlatAppearance.BorderSize = 0;

        clear.Click += (_, _) =>
        {
            SelectedDate = null;
            DialogResult = DialogResult.OK;
            Close();
        };

        select.Click += (_, _) =>
        {
            SelectedDate = SelectedCalendarDay;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            previous,
            next,
            monthLabel,
            _surface,
            clear,
            select
        });

        RefreshMonth();
    }

    private static Button MakeButton(
        string text,
        int x,
        int y,
        int width,
        int height)
    {
        var c = Theme.Colors;

        var button = new Button
        {
            Text = text,
            Bounds = new Rectangle(x, y, width, height),
            BackColor = c.Surface,
            ForeColor = c.Text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = Theme.Body
        };

        button.FlatAppearance.BorderColor = c.Border;

        return button;
    }

    private sealed class CalendarSurface : Control
    {
        private readonly ThemedBirthDateDialog _owner;

        private const int HeaderHeight = 31;
        private const int CellHeight = 28;

        public CalendarSurface(ThemedBirthDateDialog owner)
        {
            _owner = owner;

            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            BackColor = Theme.Colors.Surface;
            ForeColor = Theme.Colors.Text;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            var c = Theme.Colors;

            g.Clear(c.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var border = new Pen(c.Border);

            g.DrawRectangle(
                border,
                0,
                0,
                Width - 1,
                Height - 1);

            string[] days =
            {
                "Sun", "Mon", "Tue", "Wed",
                "Thu", "Fri", "Sat"
            };

            for (int i = 0; i < 7; i++)
            {
                DrawCentered(
                    g,
                    days[i],
                    new Rectangle(
                        i * Width / 7,
                        4,
                        Width / 7,
                        25),
                    c.TextMuted);
            }

            int offset =
                (int)_owner.DisplayMonth.DayOfWeek;

            int count = DateTime.DaysInMonth(
                _owner.DisplayMonth.Year,
                _owner.DisplayMonth.Month);

            for (int day = 1; day <= count; day++)
            {
                int index = offset + day - 1;

                var rect = new Rectangle(
                    (index % 7) * Width / 7 + 3,
                    HeaderHeight +
                    (index / 7) * CellHeight + 2,
                    Width / 7 - 6,
                    CellHeight - 3);

                var date = new DateTime(
                    _owner.DisplayMonth.Year,
                    _owner.DisplayMonth.Month,
                    day);

                bool selected =
                    date == _owner.SelectedCalendarDay;

                if (selected)
                {
                    using var brush =
                        new SolidBrush(c.Accent);

                    g.FillRectangle(brush, rect);
                }
                else if (date == DateTime.Today)
                {
                    g.DrawRectangle(border, rect);
                }

                DrawCentered(
                    g,
                    day.ToString(
                        CultureInfo.InvariantCulture),
                    rect,
                    date > DateTime.Today
                        ? c.TextMuted
                        : selected
                            ? c.OnAccent
                            : c.Text);
            }
        }

        private static void DrawCentered(
            Graphics g,
            string text,
            Rectangle rect,
            Color color)
        {
            TextRenderer.DrawText(
                g,
                text,
                Theme.Body,
                rect,
                color,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left ||
                e.Y < HeaderHeight)
                return;

            int col = Math.Clamp(
                e.X * 7 / Width,
                0,
                6);

            int row =
                (e.Y - HeaderHeight) / CellHeight;

            int day =
                row * 7 +
                col -
                (int)_owner.DisplayMonth.DayOfWeek + 1;

            if (day < 1 ||
                day > DateTime.DaysInMonth(
                    _owner.DisplayMonth.Year,
                    _owner.DisplayMonth.Month))
                return;

            var date = new DateTime(
                _owner.DisplayMonth.Year,
                _owner.DisplayMonth.Month,
                day);

            if (date > DateTime.Today)
                return;

            _owner.SelectedCalendarDay = date;

            Invalidate();
        }
    }
}
