
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public sealed class ThemedAuthorSelector : ThemedUserControl
{
    private readonly FlowLayoutPanel _items = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        Margin = Padding.Empty,
        Padding = new Padding(1)
    };

    private readonly Dictionary<int, Button> _buttons = new();
    private readonly HashSet<int> _selectedIds = new();

    private List<BookLookupOption> _authors = new();

    public event EventHandler? SelectionChanged;

    public IReadOnlyList<int> SelectedAuthorIds =>
        _selectedIds.OrderBy(id => id).ToArray();

    public ThemedAuthorSelector()
    {
        MinimumSize = new Size(0, 80);
        Height = 140;

        Controls.Add(_items);

        _items.Resize += (_, _) => ResizeRows();

        ApplyTheme();
    }

    public void SetAuthors(
        IEnumerable<BookLookupOption> authors,
        IEnumerable<int>? selectedIds = null)
    {
        ArgumentNullException.ThrowIfNull(authors);

        _authors = authors
            .OrderBy(a => a.Name)
            .ToList();

        var requested = selectedIds is null
            ? new HashSet<int>(_selectedIds)
            : new HashSet<int>(selectedIds);

        _selectedIds.Clear();

        foreach (var author in _authors)
        {
            if (requested.Contains(author.Id))
                _selectedIds.Add(author.Id);
        }

        RebuildRows();
    }

    public void SelectAuthor(int authorId)
    {
        if (!_authors.Any(a => a.Id == authorId))
            return;

        if (_selectedIds.Add(authorId))
        {
            RefreshRowStyles();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearSelection()
    {
        if (_selectedIds.Count == 0)
            return;

        _selectedIds.Clear();
        RefreshRowStyles();

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildRows()
    {
        _items.SuspendLayout();

        try
        {
            // Clear before disposing to avoid mutating the collection
            // while enumerating its controls during search/filter refreshes.
            var previousRows = _items.Controls.Cast<Control>().ToArray();
            _items.Controls.Clear();
            foreach (Control control in previousRows)
                control.Dispose();
            _buttons.Clear();

            foreach (var author in _authors)
            {
                int authorId = author.Id;

                var button = new Button
                {
                    Text = author.Name,
                    Height = 40,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    TabStop = true,
                    Margin = new Padding(0, 0, 0, 4),
                    Tag = authorId
                };

                button.Click += (_, _) =>
                    ToggleAuthor(authorId);

                _buttons.Add(authorId, button);
                _items.Controls.Add(button);
            }

            ResizeRows();
            RefreshRowStyles();
        }
        finally
        {
            _items.ResumeLayout(true);
        }
    }

    private void ToggleAuthor(int authorId)
    {
        if (!_selectedIds.Add(authorId))
            _selectedIds.Remove(authorId);

        RefreshRowStyles();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResizeRows()
    {
        int width = Math.Max(
            80,
            _items.ClientSize.Width -
            _items.Padding.Horizontal - 2);

        foreach (var button in _buttons.Values)
            button.Width = width;
    }

    private void RefreshRowStyles()
    {
        var colors = Theme.Colors;

        foreach (var (id, button) in _buttons)
        {
            bool selected = _selectedIds.Contains(id);

            var author = _authors.First(a => a.Id == id);

            button.Text =
                (selected ? "☑   " : "☐   ") +
                author.Name;

            button.BackColor = selected
                ? colors.AccentSoft
                : colors.Surface;

            button.ForeColor = colors.Text;
            button.Font = selected
                ? Theme.BodyStrong
                : Theme.Body;

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor =
                selected ? colors.Accent : colors.Border;

            button.FlatAppearance.MouseOverBackColor =
                colors.AccentSoft;

            button.FlatAppearance.MouseDownBackColor =
                colors.AccentSoft;

            button.AccessibleName =
                $"{author.Name}, " +
                (selected ? "selected" : "not selected");
        }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();

        var colors = Theme.Colors;

        BackColor = colors.Surface;
        _items.BackColor = colors.Surface;

        RefreshRowStyles();
    }
}
