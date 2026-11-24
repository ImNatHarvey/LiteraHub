using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Reusable associated-books gallery for Author/Category Details.
/// Owns rendering, themed scrollbar, cover cards and session-cached covers;
/// the hosting page owns queries, CRUD, permissions and navigation history.
/// </summary>
public sealed class AssociatedBooksGallery : ThemedUserControl
{
    private const int Gap = 12;
    private const int PreferredRowHeight = 244;
    private const int TitleHeight = 42;
    private const int AccentSpine = 7;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 2,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Associated Books"
    };
    private readonly FlowLayoutPanel _gallery = new()
    {
        AutoScroll = false,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly ScrollHost _scroll = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty
    };
    private readonly ToolTip _tips = new();
    private bool _arranging;
    private int _bookCount;

    /// <summary>Optional limits for details pages with larger gallery viewports.
    /// Zero means no cap and preserves the accepted Author layout.</summary>

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int MaximumCardHeight { get; set; }

    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int MaximumCardWidth { get; set; }



    public event EventHandler<int>? BookRequested;

    public AssociatedBooksGallery()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.Controls.Add(_heading, 0, 0);
        _scroll.SetContent(_gallery);
        _layout.Controls.Add(_scroll, 0, 1);
        Controls.Add(_layout);
        _scroll.Resize += (_, _) => ArrangeCards();
        _gallery.MouseWheel += (_, e) => ScrollByWheel(e.Delta);
        RefreshAppearance();
    }

    public void ShowBeforeSave() => ShowMessage("Associated Books (available after saving)", "");

    public void ShowError(string message) =>
        ShowMessage("Associated Books", message);

    private void ShowMessage(string heading, string message)
    {
        ClearCards();
        _bookCount = 0;
        _heading.Text = heading;
        if (!string.IsNullOrEmpty(message))
        {
            _gallery.Controls.Add(new Label
            {
                Text = message,
                AutoSize = true,
                ForeColor = Theme.Colors.TextMuted,
                Font = Theme.Body
            });
        }
        ArrangeCards();
    }

    public void SetBooks(IEnumerable<Book> books, string emptyText = "No associated books.")
    {
        ArgumentNullException.ThrowIfNull(books);
        var items = books.ToArray();
        ClearCards();
        _bookCount = items.Length;
        _heading.Text = $"Associated Books  ({_bookCount})";

        _gallery.SuspendLayout();
        try
        {
            if (_bookCount == 0)
            {
                _gallery.Controls.Add(new Label
                {
                    Text = emptyText,
                    AutoSize = true,
                    ForeColor = Theme.Colors.TextMuted,
                    Font = Theme.Body
                });
            }
            foreach (var book in items)
            {
                var card = MakeCard(book, out var picture);
                _gallery.Controls.Add(card);
                _ = CatalogCoverCache.LoadIntoAsync(picture, book.CoverPath);
            }
        }
        finally { _gallery.ResumeLayout(true); }
        ArrangeCards();
    }

    private void ClearCards()
    {
        _gallery.SuspendLayout();
        try
        {
            foreach (Control control in _gallery.Controls.Cast<Control>().ToArray())
            {
                foreach (var picture in control.Controls.OfType<Panel>()
                    .SelectMany(p => p.Controls.OfType<PictureBox>()))
                {
                    var oldImage = picture.Image;
                    picture.Image = null;
                    oldImage?.Dispose();
                }
                _gallery.Controls.Remove(control);
                control.Dispose();
            }
        }
        finally { _gallery.ResumeLayout(true); }
    }

    // Identical geometry to the accepted Author gallery (including its
    // complete-row viewport and centered 42px title footer).
    private void ArrangeCards()
    {
        if (_arranging || IsDisposed || Disposing) return;
        _arranging = true;
        try
        {
            int viewportWidth = Math.Max(150, _scroll.ClientSize.Width - 18);
            int viewportHeight = Math.Max(1, _scroll.ClientSize.Height);
            int columns = Math.Max(1, (viewportWidth + Gap) / (170 + Gap));
            int cardWidth = Math.Max(125, (viewportWidth - columns * Gap) / columns);
            if (MaximumCardWidth > 0)
                cardWidth = Math.Min(MaximumCardWidth, cardWidth);
            int rows = (Math.Max(1, _bookCount) + columns - 1) / columns;
            int visibleRows = Math.Max(1, viewportHeight / PreferredRowHeight);
            int rowHeight = Math.Max(PreferredRowHeight, viewportHeight / visibleRows + 1);
            int cardHeight = rowHeight - Gap;
            if (MaximumCardHeight > 0)
                cardHeight = Math.Min(MaximumCardHeight, cardHeight);
            int frameHeight = cardHeight - TitleHeight;

            _gallery.SuspendLayout();
            try
            {
                _gallery.Width = viewportWidth;
                foreach (var card in _gallery.Controls.OfType<Panel>())
                {
                    card.Size = new Size(cardWidth, cardHeight);
                    var frame = card.Controls.OfType<Panel>()
                        .FirstOrDefault(p => Equals(p.Tag, "cover-frame"));
                    if (frame != null)
                    {
                        frame.Bounds = new Rectangle(0, 0, cardWidth, frameHeight);
                        var picture = frame.Controls.OfType<PictureBox>().FirstOrDefault();
                        if (picture != null)
                            picture.Bounds = new Rectangle(AccentSpine, 0,
                                Math.Max(1, cardWidth - AccentSpine), frameHeight);
                    }
                    foreach (var label in card.Controls.OfType<Label>())
                        if (Equals(label.Tag, "title"))
                            label.Bounds = new Rectangle(0, frameHeight, cardWidth, TitleHeight);
                }

                _gallery.Height = _bookCount == 0
                    ? Math.Min(64, Math.Max(1, viewportHeight - 4))
                    : rows * rowHeight + 1;

            }
            finally { _gallery.ResumeLayout(true); }
            _scroll.RefreshMetrics();
        }
        finally { _arranging = false; }
    }

    private Control MakeCard(Book book, out PictureBox cover)
    {
        var c = Theme.Colors;
        var card = new Panel
        {
            Size = new Size(150, 232),
            Margin = new Padding(0, 0, Gap, Gap),
            Cursor = Cursors.Hand,
            BackColor = c.SurfaceAlt
        };
        var frame = new Panel
        {
            Tag = "cover-frame",
            Bounds = new Rectangle(0, 0, 150, 190),
            BackColor = c.AccentSoft,
            Cursor = Cursors.Hand
        };
        frame.Paint += (_, e) =>
        {
            using var brush = new SolidBrush(Theme.Colors.Accent);
            e.Graphics.FillRectangle(brush, 0, 0,
                Math.Min(AccentSpine, frame.Width), frame.Height);
        };
        cover = new PictureBox
        {
            Bounds = new Rectangle(AccentSpine, 0, 150 - AccentSpine, 190),
            BackColor = c.AccentSoft,
            SizeMode = PictureBoxSizeMode.Zoom,
            Cursor = Cursors.Hand
        };
        frame.Controls.Add(cover);
        var title = new Label
        {
            Tag = "title",
            Bounds = new Rectangle(0, 190, 150, TitleHeight),
            Text = book.Title,
            AutoEllipsis = true,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Caption,
            BackColor = c.SurfaceAlt,
            ForeColor = c.Text,
            Cursor = Cursors.Hand,
            Padding = new Padding(3, 1, 3, 1)
        };
        card.Controls.AddRange(new Control[] { frame, title });
        foreach (var control in new Control[] { card, frame, cover, title })
        {
            control.Click += (_, _) => BookRequested?.Invoke(this, book.BookId);
            control.MouseWheel += (_, e) => ScrollByWheel(e.Delta);
            _tips.SetToolTip(control, "View Book: " + book.Title);
        }
        return card;
    }

    private void ScrollByWheel(int delta) =>
        _scroll.ScrollByPixels(delta > 0 ? -244 : 244);

    private void RefreshAppearance()
    {
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.BodyStrong;
        _gallery.BackColor = c.Background;
        _scroll.BackColor = c.Background;
        foreach (Control child in _gallery.Controls)
        {
            if (child is not Panel card)
            {
                child.ForeColor = c.TextMuted;
                child.Font = Theme.Body;
                continue;
            }
            card.BackColor = c.SurfaceAlt;
            foreach (var frame in card.Controls.OfType<Panel>())
            {
                frame.BackColor = c.AccentSoft;
                frame.Invalidate();
                foreach (var picture in frame.Controls.OfType<PictureBox>())
                    picture.BackColor = c.AccentSoft;
            }
            foreach (var title in card.Controls.OfType<Label>())
            {
                title.BackColor = c.SurfaceAlt;
                title.ForeColor = c.Text;
                title.Font = Theme.Caption;
            }
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        RefreshAppearance();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tips.Dispose();
            ClearCards();
        }
        base.Dispose(disposing);
    }
}
