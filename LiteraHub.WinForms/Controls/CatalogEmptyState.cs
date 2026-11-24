using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Controls;

public enum CatalogEmptyKind
{
    NoRecords,
    NoSearchResults,
    NoFilterResults
}

/// <summary>
/// Shared, themed empty-result surface for catalog grids. The owning page
/// decides when to show it and handles the requested action.
/// </summary>
public sealed class CatalogEmptyState : ThemedUserControl
{
    private readonly Panel _card = new()
    {
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly Label _glyph = new()
    {
        Text = "⌕",
        TextAlign = ContentAlignment.MiddleCenter,
        AutoSize = false
    };
    private readonly Label _title = new()
    {
        TextAlign = ContentAlignment.MiddleCenter,
        AutoSize = false
    };
    private readonly Label _description = new()
    {
        TextAlign = ContentAlignment.MiddleCenter,
        AutoSize = false
    };
    private readonly Button _action = new()
    {
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Height = 40,
        Width = 156,
        TextAlign = ContentAlignment.MiddleCenter
    };

    // Explicit action state: Control.Visible also reflects parent visibility,
    // so it must not be used to calculate layout while the page is hidden.
    private bool _showAction;

    public event EventHandler? ActionRequested;

    public CatalogEmptyState()
    {
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        Visible = false;
        AccessibleName = "Catalog empty results";
        _card.Paint += PaintCardBorder;
        _card.Controls.AddRange(new Control[]
        {
            _glyph, _title, _description, _action
        });
        Controls.Add(_card);
        Resize += (_, _) => ArrangeCard();
        VisibleChanged += (_, _) => { if (Visible) ArrangeCard(); };
        _card.Resize += (_, _) => ArrangeContents();
        _action.Click += (_, _) => ActionRequested?.Invoke(this, EventArgs.Empty);
        Configure(CatalogEmptyKind.NoRecords, false);
    }

    public void Configure(CatalogEmptyKind kind, bool allowAdd,
        string itemSingular = "book", string itemPlural = "books",
        string searchFields = "title, ISBN, author, or keyword")
    {
        switch (kind)
        {
            case CatalogEmptyKind.NoRecords:
                _glyph.Text = "▤";
                _title.Text = "No " + itemPlural + " in the catalog yet";
                _description.Text = allowAdd
                    ? "Get started by adding your first " + itemSingular + "."
                    : char.ToUpperInvariant(itemPlural[0]) + itemPlural.Substring(1) + " will appear here after they are added.";
                _action.Text = "Add " + char.ToUpperInvariant(itemSingular[0]) + itemSingular.Substring(1);
                _showAction = allowAdd;
                break;
            case CatalogEmptyKind.NoSearchResults:
                _glyph.Text = "⌕";
                _title.Text = "No " + itemPlural + " found";
                _description.Text = "Try a different " + searchFields + ".";
                _action.Text = "Clear Search";
                _showAction = true;
                break;
            default:
                _glyph.Text = "≡";
                _title.Text = "No " + itemPlural + " match your filters";
                _description.Text = "Adjust or clear the selected filters to see more " + itemPlural + ".";
                _action.Text = "Clear Filters";
                _showAction = true;
                break;
        }

        _action.Visible = _showAction;
        AccessibleDescription = _title.Text + ". " + _description.Text;
        ArrangeCard();
    }

    private void ArrangeCard()
    {
        if (IsDisposed || Disposing) return;
        int w = Math.Min(480, Math.Max(0, ClientSize.Width - 24));
        int h = Math.Min(242, Math.Max(0, ClientSize.Height - 24));
        _card.SetBounds(Math.Max(0, (ClientSize.Width - w) / 2),
            Math.Max(0, (ClientSize.Height - h) / 2), w, h);
        ArrangeContents();
    }

    private void ArrangeContents()
    {
        if (_card.ClientSize.Width < 1 || _card.ClientSize.Height < 1) return;
        int width = _card.ClientSize.Width;
        int height = _card.ClientSize.Height;
        int top = Math.Max(6, (height - (_showAction ? 202 : 152)) / 2);
        // Keep opaque child labels inside the painted card outline.
        // A full-width label obscures the left/right border in WinForms.
        _glyph.SetBounds(16, top, Math.Max(0, width - 32), 48);
        _title.SetBounds(12, top + 54, Math.Max(0, width - 24), 30);
        _description.SetBounds(16, top + 90, Math.Max(0, width - 32), 44);
        _action.SetBounds(Math.Max(0, (width - 156) / 2), top + 148, 156, 40);
        _card.Invalidate();
    }

    private void PaintCardBorder(object? sender, PaintEventArgs e)
    {
        if (_card.Width < 2 || _card.Height < 2) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Theme.Colors.Border, 1f);
        using var path = Gfx.RoundedRect(new RectangleF(0.5f, 0.5f,
            _card.Width - 1.5f, _card.Height - 1.5f), 10);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        BackColor = c.Background;
        _card.BackColor = c.Surface;
        _glyph.BackColor = c.Surface;
        _glyph.ForeColor = c.Accent;
        _glyph.Font = Theme.Display;
        _title.BackColor = c.Surface;
        _title.ForeColor = c.Text;
        _title.Font = Theme.Title;
        _description.BackColor = c.Surface;
        _description.ForeColor = c.TextMuted;
        _description.Font = Theme.Body;
        _action.BackColor = c.Accent;
        _action.ForeColor = c.OnAccent;
        _action.Font = Theme.BodyStrong;
        _action.FlatAppearance.BorderSize = 0;
        _action.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _action.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        _card.Invalidate();
    }
}
