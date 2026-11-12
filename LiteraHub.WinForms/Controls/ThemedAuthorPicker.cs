using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// A compact, themed multi-author picker. The selection is stored as Author IDs,
/// never as free-form names. It fits the existing 40px Book Editor field.
/// </summary>
public sealed class ThemedAuthorPicker : Button
{
    private readonly List<BookLookupOption> _authors = new();
    private readonly HashSet<int> _selected = new();
    private Form? _popup;
    private string _suggestedSearch = "";

    public event EventHandler? SelectionChanged;
    public event EventHandler? AddAuthorRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AllowCreateAuthor { get; set; }
    public string RequestedAuthorName { get; private set; } = "";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<int> SelectedAuthorIds =>
        _selected.OrderBy(id => id).ToArray();

    public ThemedAuthorPicker()
    {
        FlatStyle = FlatStyle.Flat;
        TextAlign = ContentAlignment.MiddleLeft;
        Cursor = Cursors.Hand;
        TabStop = true;
        Theme.Changed += ThemeChanged;
        Click += (_, _) => OpenPicker();
        KeyPress += (_, e) =>
        {
            if (char.IsControl(e.KeyChar)) return;
            OpenPicker(e.KeyChar.ToString());
            e.Handled = true;
        };
        ApplyTheme();
        UpdateCaption();
    }

    public void SetAuthors(
        IEnumerable<BookLookupOption> authors,
        IEnumerable<int>? selectedIds = null)
    {
        ArgumentNullException.ThrowIfNull(authors);
        var requested = selectedIds?.ToHashSet() ?? _selected.ToHashSet();
        _authors.Clear();
        _authors.AddRange(authors.Where(a => a.IsActive)
            .GroupBy(a => a.Id).Select(g => g.First())
            .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase));
        _selected.Clear();
        foreach (var a in _authors)
            if (requested.Contains(a.Id))
                _selected.Add(a.Id);

        _popup?.Close();
        UpdateCaption();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Matches full author names from ISBN metadata against active DB authors.
    /// Existing selections are preserved; ambiguous names are NOT auto-selected.
    /// Returns names that still require a librarian's attention.
    /// </summary>
    public IReadOnlyList<string> MatchMetadataNames(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var unmatched = new List<string>();
        bool changed = false;
        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var key = Normalize(name);
            var matches = _authors.Where(a => Normalize(a.Name) == key).ToArray();
            if (matches.Length == 1)
                changed |= _selected.Add(matches[0].Id);
            else
                unmatched.Add(name.Trim());
        }

        if (changed)
        {
            UpdateCaption();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
        _suggestedSearch = unmatched.Count > 0 ? unmatched[0] : "";
        return unmatched;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (char ch in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) ==
                UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToUpperInvariant(ch));
        }
        return builder.ToString();
    }

    public void SelectAuthor(int authorId)
    {
        if (!_authors.Any(a => a.Id == authorId))
            return;
        if (_selected.Add(authorId))
        {
            _suggestedSearch = "";
            UpdateCaption();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCaption()
    {
        if (_selected.Count == 0)
            Text = _authors.Count == 0
                ? "No authors loaded  ▾"
                : "Select author(s)  ▾";
        else
        {
            var names = _authors.Where(a => _selected.Contains(a.Id))
                .Select(a => a.Name).ToArray();
            Text = names.Length <= 2
                ? string.Join(", ", names) + "  ▾"
                : $"{names.Length} authors selected  ▾";
        }
        AccessibleName = "Authors: " + Text;
    }

    private void OpenPicker(string initialSearch = "")
    {
        if (_popup is not null || !Enabled ||
            (_authors.Count == 0 && !AllowCreateAuthor))
            return;

        if (initialSearch.Length == 0 && _suggestedSearch.Length > 0)
            initialSearch = _suggestedSearch;

        var c = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(Math.Clamp(Width, 270, 480), 250)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = AllowCreateAuthor ? 3 : 2,
            BackColor = c.Surface,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        if (AllowCreateAuthor)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var search = new ThemedTextField
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        var scroll = new ScrollHost
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        scroll.UseSurfaceBackground();

        var selector = new ThemedAuthorSelector
        {
            Margin = Padding.Empty
        };
        var simpleHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = c.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoScroll = false
        };
        simpleHost.Controls.Add(selector);
        selector.Dock = DockStyle.Fill;

        void RefreshRows()
        {
            var filtered = _authors.Where(a => a.Name.Contains(
                search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
            selector.SetAuthors(filtered, _selected);
            int contentHeight = Math.Max(48, filtered.Length * 44);
            int footerHeight = AllowCreateAuthor ? 46 : 0;
            popup.Height = Math.Clamp(
                108 + contentHeight + footerHeight,
                154 + footerHeight,
                356);

            bool needsScroll = filtered.Length > 4;
            scroll.Visible = needsScroll;
            simpleHost.Visible = !needsScroll;

            if (needsScroll)
            {
                if (!ReferenceEquals(selector.Parent, scroll))
                {
                    simpleHost.Controls.Remove(selector);
                    selector.Dock = DockStyle.None;
                    scroll.SetContent(selector);
                }
                selector.Height = contentHeight;
                scroll.RefreshMetrics();
            }
            else
            {
                if (!ReferenceEquals(selector.Parent, simpleHost))
                {
                    scroll.Controls.Remove(selector);
                    simpleHost.Controls.Add(selector);
                }
                selector.Dock = DockStyle.Fill;
            }
        }

        selector.SelectionChanged += (_, _) =>
        {
            var displayedIds = _authors.Where(a => a.Name.Contains(
                search.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Id).ToHashSet();
            _selected.RemoveWhere(displayedIds.Contains);
            foreach (int id in selector.SelectedAuthorIds)
                _selected.Add(id);
            UpdateCaption();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        };

        search.TextChanged += (_, _) => RefreshRows();
        layout.Controls.Add(search, 0, 0);

        var listContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = c.Surface
        };
        listContainer.Controls.Add(simpleHost);
        listContainer.Controls.Add(scroll);
        scroll.Visible = false;
        simpleHost.Visible = true;
        layout.Controls.Add(listContainer, 0, 1);

        if (AllowCreateAuthor)
        {
            var add = new Button
            {
                Text = "+ Add Author",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = c.Accent,
                ForeColor = c.OnAccent,
                Font = Theme.BodyStrong,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            add.FlatAppearance.BorderSize = 0;
            add.FlatAppearance.MouseOverBackColor = c.Accent;
            add.FlatAppearance.MouseDownBackColor = c.Accent;
            add.Click += (_, _) =>
            {
                RequestedAuthorName = search.Text.Trim().Length > 0
                    ? search.Text.Trim()
                    : _suggestedSearch;
                _suggestedSearch = "";
                popup.Close();
                AddAuthorRequested?.Invoke(this, EventArgs.Empty);
            };
            layout.Controls.Add(add, 0, 2);
        }

        popup.Controls.Add(layout);
        _popup = popup;
        popup.Deactivate += (_, _) => popup.Close();
        popup.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup))
                _popup = null;
            popup.Dispose();
        };

        if (initialSearch.Length > 0)
            search.Text = initialSearch;
        RefreshRows();

        var origin = PointToScreen(new Point(0, Height + 4));
        var bounds = Screen.FromControl(this).WorkingArea;
        popup.Location = new Point(
            Math.Clamp(origin.X, bounds.Left,
                Math.Max(bounds.Left, bounds.Right - popup.Width)),
            origin.Y + popup.Height <= bounds.Bottom
                ? origin.Y
                : Math.Max(bounds.Top,
                    PointToScreen(Point.Empty).Y - popup.Height - 4));
        if (FindForm() is Form owner)
            popup.Show(owner);
        else
            popup.Show();
        search.FocusInput();
    }

    private void ThemeChanged(object? sender, EventArgs e) => ApplyTheme();

    public void ApplyTheme()
    {
        if (IsDisposed) return;
        var c = Theme.Colors;
        BackColor = c.Surface;
        ForeColor = c.Text;
        Font = Theme.Body;
        FlatAppearance.BorderSize = 1;
        FlatAppearance.BorderColor = c.Border;
        FlatAppearance.MouseOverBackColor = c.AccentSoft;
        FlatAppearance.MouseDownBackColor = c.AccentSoft;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= ThemeChanged;
            _popup?.Close();
        }
        base.Dispose(disposing);
    }
}
