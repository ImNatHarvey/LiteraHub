using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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

    public event EventHandler? SelectionChanged;

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
        return unmatched;
    }

    private static string Normalize(string value) =>
        string.Join(" ", value.Trim().Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();

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

    private void OpenPicker()
    {
        if (_popup is not null || _authors.Count == 0 || !Enabled)
            return;

        var c = Theme.Colors;
        var popup = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            BackColor = c.Border,
            Padding = new Padding(1),
            Size = new Size(Math.Clamp(Width, 270, 480), Math.Clamp(108 + _authors.Count * 44, 154, 310))
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = c.Surface,
            Padding = new Padding(8),
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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
        var selector = new ThemedAuthorSelector();
        scroll.SetContent(selector);
        // Keep the popup compact for short lists, while retaining themed scrolling
        // for long author lists. Never leave a large empty dropdown panel.

        void RefreshRows()
        {
            var filtered = _authors.Where(a =>
                a.Name.Contains(search.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase)).ToArray();
            selector.SetAuthors(filtered, _selected);
            selector.Height = Math.Max(80, filtered.Length * 44 + 4);
            popup.Height = Math.Clamp(108 + filtered.Length * 44, 154, 310);
            scroll.RefreshMetrics();
        }

        selector.SelectionChanged += (_, _) =>
        {
            // The displayed selection covers only filtered rows.
            var displayedIds = _authors
                .Where(a => a.Name.Contains(search.Text.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Id).ToHashSet();
            _selected.RemoveWhere(displayedIds.Contains);
            foreach (int id in selector.SelectedAuthorIds)
                _selected.Add(id);
            UpdateCaption();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        };

        search.TextChanged += (_, _) => RefreshRows();
        layout.Controls.Add(search, 0, 0);
        layout.Controls.Add(scroll, 0, 1);
        popup.Controls.Add(layout);
        _popup = popup;
        popup.Deactivate += (_, _) => popup.Close();
        popup.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup))
                _popup = null;
            popup.Dispose();
        };

        RefreshRows();
        var origin = PointToScreen(new Point(0, Height + 4));
        var bounds = Screen.FromControl(this).WorkingArea;
        popup.Location = new Point(
            Math.Clamp(origin.X, bounds.Left, Math.Max(bounds.Left, bounds.Right - popup.Width)),
            origin.Y + popup.Height <= bounds.Bottom
                ? origin.Y
                : Math.Max(bounds.Top, PointToScreen(Point.Empty).Y - popup.Height - 4));
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
