
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ThemedBirthDateDialog : Form
{
    private DateTime _month;
    private DateTime _selection;

    private readonly CalendarSurface _surface;
    private readonly ThemedFilterDropdown _monthPicker;
    private readonly TextBox _yearText;
    private readonly Button _yearUp;
    private readonly Button _yearDown;

    private bool _updating;

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

        // All header controls are exactly 36px high.
        const int headerY = 12;
        const int headerHeight = 36;

        var previous = MakeButton(
            "‹", 12, headerY, 40, headerHeight);

        var next = MakeButton(
            "›", 258, headerY, 40, headerHeight);

        // Month dropdown: 36px high.
        string[] months = new string[12];

        for (int i = 0; i < months.Length; i++)
        {
            months[i] = CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(i + 1);
        }

        _monthPicker = new ThemedFilterDropdown(months)
        {
            Bounds = new Rectangle(
                58, headerY, 113, headerHeight),
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Year container: also 36px high.
        // A one-pixel outer border matches the other controls.
        var yearContainer = new Panel
        {
            Bounds = new Rectangle(
                177, headerY, 75, headerHeight),
            BackColor = Theme.Colors.Border,
            Padding = new Padding(1),
            Margin = Padding.Empty
        };

        var yearInner = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Colors.Surface,
            Margin = Padding.Empty
        };

        _yearText = new TextBox
        {
            Bounds = new Rectangle(3, 8, 48, 22),
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            BackColor = Theme.Colors.Surface,
            ForeColor = Theme.Colors.Text,
            Font = Theme.Body,
            MaxLength = 4,
            Margin = Padding.Empty
        };

        // Each spinner button is 17px tall.
        // Icons are painted as triangles rather than text,
        // preventing font clipping and tiny arrows.
        _yearUp = MakeSpinnerButton(
            new Rectangle(53, 0, 20, 17),
            pointingUp: true);

        _yearDown = MakeSpinnerButton(
            new Rectangle(53, 17, 20, 17),
            pointingUp: false);

        yearInner.Controls.Add(_yearText);
        yearInner.Controls.Add(_yearUp);
        yearInner.Controls.Add(_yearDown);
        yearContainer.Controls.Add(yearInner);

        // Calendar surface remains in its original position.
        _surface = new CalendarSurface(this)
        {
            Bounds = new Rectangle(12, 54, 286, 205)
        };

        var clear = MakeButton(
            "Clear", 12, 273, 130, 36);

        var select = MakeButton(
            "Select Date", 150, 273, 148, 36);

        select.BackColor = Theme.Colors.Accent;
        select.ForeColor = Theme.Colors.OnAccent;
        select.FlatAppearance.BorderSize = 0;

        // Month navigation.
        previous.Click += (_, _) =>
            ChangeMonth(-1);

        next.Click += (_, _) =>
            ChangeMonth(1);

        _monthPicker.SelectedIndexChanged += (_, _) =>
        {
            if (!_updating)
                ChangePickerDate();
        };

        // Year spinner navigation.
        _yearUp.Click += (_, _) =>
            ChangeYear(1);

        _yearDown.Click += (_, _) =>
            ChangeYear(-1);

        // Only numeric years are allowed.
        _yearText.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) &&
                !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        };

        _yearText.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitYear();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                ChangeYear(1);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                ChangeYear(-1);
                e.SuppressKeyPress = true;
            }
        };

        _yearText.Leave += (_, _) =>
            CommitYear();

        clear.Click += (_, _) =>
        {
            SelectedDate = null;
            DialogResult = DialogResult.OK;
            Close();
        };

        select.Click += (_, _) =>
        {
            CommitYear();

            SelectedDate = SelectedCalendarDay;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            previous,
            next,
            _monthPicker,
            yearContainer,
            _surface,
            clear,
            select
        });

        RefreshMonth();
    }

    // Custom themed spinner button.
    // The triangle is drawn at a fixed size regardless
    // of button state, font size, or Windows scaling.
    private static Button MakeSpinnerButton(
        Rectangle bounds,
        bool pointingUp)
    {
        var c = Theme.Colors;

        var button = new Button
        {
            Bounds = bounds,
            BackColor = c.SurfaceAlt,
            ForeColor = c.Text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TabStop = false,
            Text = "",
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor =
            c.AccentSoft;
        button.FlatAppearance.MouseDownBackColor =
            c.AccentSoft;

        button.Paint += (_, e) =>
        {
            var g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = button.ClientSize.Width / 2;
            int cy = button.ClientSize.Height / 2;

            // Fixed-size, clearly visible arrow triangles.
            Point[] triangle = pointingUp
                ? new[]
                {
                    new Point(cx, cy - 4),
                    new Point(cx - 5, cy + 3),
                    new Point(cx + 5, cy + 3)
                }
                : new[]
                {
                    new Point(cx - 5, cy - 3),
                    new Point(cx + 5, cy - 3),
                    new Point(cx, cy + 4)
                };

            using var brush =
                new SolidBrush(Theme.Colors.Text);

            g.FillPolygon(brush, triangle);

            // Divider between spinner buttons.
            if (!pointingUp)
            {
                using var pen =
                    new Pen(Theme.Colors.Border);

                g.DrawLine(
                    pen,
                    0,
                    0,
                    button.ClientSize.Width,
                    0);
            }
        };

        return button;
    }

    private void ChangeMonth(int offset)
    {
        DateTime candidate;

        try
        {
            candidate = _month.AddMonths(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        if (candidate > FirstDayOfCurrentMonth())
            return;

        _month = candidate;
        AdjustSelection();
        RefreshMonth();
    }

    private void ChangeYear(int offset)
    {
        // First commit a year the user may have typed.
        CommitYear();

        int year = _month.Year + offset;

        if (year < 1 ||
            year > DateTime.Today.Year)
            return;

        SetMonth(year, _month.Month);
    }

    private void CommitYear()
    {
        if (_updating)
            return;

        if (int.TryParse(
            _yearText.Text.Trim(),
            out int year) &&
            year >= 1 &&
            year <= DateTime.Today.Year)
        {
            SetMonth(year, _month.Month);
        }
        else
        {
            RefreshMonth();
        }
    }

    private void ChangePickerDate()
    {
        if (_monthPicker.SelectedIndex < 0)
            return;

        SetMonth(
            _month.Year,
            _monthPicker.SelectedIndex + 1);
    }

    private void SetMonth(int year, int month)
    {
        DateTime candidate =
            new DateTime(year, month, 1);

        if (candidate > FirstDayOfCurrentMonth())
        {
            candidate = FirstDayOfCurrentMonth();
        }

        _month = candidate;

        AdjustSelection();
        RefreshMonth();
    }

    private void AdjustSelection()
    {
        int day = Math.Min(
            SelectedCalendarDay.Day,
            DateTime.DaysInMonth(
                DisplayMonth.Year,
                DisplayMonth.Month));

        DateTime candidate = new DateTime(
            DisplayMonth.Year,
            DisplayMonth.Month,
            day);

        SelectedCalendarDay =
            candidate > DateTime.Today
                ? DateTime.Today
                : candidate;
    }

    private void RefreshMonth()
    {
        _updating = true;

        try
        {
            _monthPicker.SelectedIndex =
                DisplayMonth.Month - 1;

            _yearText.Text =
                DisplayMonth.Year.ToString(
                    CultureInfo.InvariantCulture);
        }
        finally
        {
            _updating = false;
        }

        _surface.Invalidate();
    }

    private static DateTime FirstDayOfCurrentMonth()
    {
        return new DateTime(
            DateTime.Today.Year,
            DateTime.Today.Month,
            1);
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
            Bounds = new Rectangle(
                x, y, width, height),
            BackColor = c.Surface,
            ForeColor = c.Text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = Theme.Body,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor =
            c.Border;

        button.FlatAppearance.MouseOverBackColor =
            c.SurfaceAlt;

        return button;
    }

    private sealed class CalendarSurface : Control
    {
        private readonly ThemedBirthDateDialog _owner;

        private const int HeaderHeight = 31;
        private const int CellHeight = 28;

        public CalendarSurface(
            ThemedBirthDateDialog owner)
        {
            _owner = owner;

            DoubleBuffered = true;
            Cursor = Cursors.Hand;

            BackColor = Theme.Colors.Surface;
            ForeColor = Theme.Colors.Text;
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            var c = Theme.Colors;

            g.Clear(c.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var border =
                new Pen(c.Border);

            g.DrawRectangle(
                border,
                0,
                0,
                Width - 1,
                Height - 1);

            string[] days =
            {
                "Sun",
                "Mon",
                "Tue",
                "Wed",
                "Thu",
                "Fri",
                "Sat"
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

            DateTime displayMonth =
                _owner.DisplayMonth;

            DateTime selectedDay =
                _owner.SelectedCalendarDay;

            int offset =
                (int)displayMonth.DayOfWeek;

            int count = DateTime.DaysInMonth(
                displayMonth.Year,
                displayMonth.Month);

            for (int day = 1; day <= count; day++)
            {
                int index = offset + day - 1;

                Rectangle rect = new Rectangle(
                    (index % 7) * Width / 7 + 3,
                    HeaderHeight +
                    (index / 7) * CellHeight + 2,
                    Width / 7 - 6,
                    CellHeight - 3);

                DateTime date = new DateTime(
                    displayMonth.Year,
                    displayMonth.Month,
                    day);

                bool selected =
                    date == selectedDay;

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

        protected override void OnMouseUp(
            MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left ||
                e.Y < HeaderHeight)
                return;

            DateTime displayMonth =
                _owner.DisplayMonth;

            int col = Math.Clamp(
                e.X * 7 / Width,
                0,
                6);

            int row =
                (e.Y - HeaderHeight) / CellHeight;

            int day =
                row * 7 +
                col -
                (int)displayMonth.DayOfWeek + 1;

            if (day < 1 ||
                day > DateTime.DaysInMonth(
                    displayMonth.Year,
                    displayMonth.Month))
                return;

            DateTime date = new DateTime(
                displayMonth.Year,
                displayMonth.Month,
                day);

            if (date > DateTime.Today)
                return;

            _owner.SelectedCalendarDay = date;
            Invalidate();
        }
    }
}
