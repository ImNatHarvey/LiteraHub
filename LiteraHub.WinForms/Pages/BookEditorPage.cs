
using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

public sealed class BookEditorPage : ThemedUserControl
{
    private readonly Book? _existing;

    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 3,
        Margin = Padding.Empty
    };

    private readonly TableLayoutPanel _header = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty
    };

    private readonly Button _back = new()
    {
        Text = "‹  Back to Books",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        TextAlign = ContentAlignment.MiddleLeft,
        Cursor = Cursors.Hand,
        Margin = new Padding(0, 4, 12, 4)
    };

    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Label _description = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Panel _preview = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(20)
    };

    private readonly Label _previewText = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

    public event EventHandler? BackRequested;

    public BookEditorPage(Book? existing = null)
    {
        _existing = existing;

        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);

        _heading.Text = existing is null
            ? "Add Book"
            : "Edit Book";

        _description.Text = existing is null
            ? "Create a new book record."
            : $"Editing: {existing.Title}";

        _previewText.Text =
            "Book Editor page foundation\n\n" +
            "The full editor fields, ISBN lookup, " +
            "cover preview, author selection, " +
            "category, condition, and inventory " +
            "controls will be added next.\n\n" +
            "Saving is not enabled in this checkpoint.";

        BuildLayout();

        _back.Click += (_, _) =>
            BackRequested?.Invoke(this, EventArgs.Empty);

        Resize += (_, _) => UpdateResponsiveLayout();

        ApplyTheme();
    }

    private void BuildLayout()
    {
        _layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42));

        _layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 150));

        _header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        _header.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _header.Controls.Add(_back, 0, 0);
        _header.Controls.Add(_heading, 1, 0);

        _preview.Controls.Add(_previewText);

        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_description, 0, 1);
        _layout.Controls.Add(_preview, 0, 2);

        Controls.Add(_layout);
    }

    private void UpdateResponsiveLayout()
    {
        bool compact = Width < 480;

        _header.ColumnStyles[0].Width =
            compact ? 100 : 150;

        _back.Text = compact
            ? "‹ Back"
            : "‹  Back to Books";

        _heading.Font = compact
            ? Theme.BodyStrong
            : Theme.Title;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;

        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;

        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;

        _description.BackColor = c.Background;
        _description.ForeColor = c.TextMuted;
        _description.Font = Theme.Body;

        _preview.BackColor = c.Surface;
        _previewText.BackColor = c.Surface;
        _previewText.ForeColor = c.TextMuted;
        _previewText.Font = Theme.Body;

        _back.BackColor = c.Surface;
        _back.ForeColor = c.Text;
        _back.Font = Theme.Body;
        _back.FlatAppearance.BorderColor = c.Border;
        _back.FlatAppearance.BorderSize = 1;

        UpdateResponsiveLayout();
    }
}
