using FontAwesome.Sharp;
using LiteraHub.WinForms.Theming;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LiteraHub.WinForms.Controls;

public sealed record NavItem(string Key, string Title, IconChar Icon);

public sealed class SidebarNav : ThemedUserControl
{
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
    private readonly Label _brand = new() { Dock = DockStyle.Fill, Text = "LiteraHub" };
    private readonly TableLayoutPanel _brandArea = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly BrandMorphToggle _brandToggle = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _items = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        Padding = new Padding(Space.S, Space.S, Space.S, 0)
    };
    private readonly TableLayoutPanel _footer = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1,
        Padding = new Padding(Space.S)
    };
    private readonly Label _user = new() { Dock = DockStyle.Fill, Height = 44, TextAlign = ContentAlignment.MiddleLeft };
    private readonly IconButton _logout = new();
    private readonly ToolTip _tip = new();
    private readonly List<(NavItem Item, IconButton Button)> _buttons = new();

    private string? _selectedKey;
    private bool _compact;

    public event EventHandler<string>? ItemSelected;

    public event EventHandler? LogoutRequested;

    /// <summary>The logo/hamburger toggle was activated.</summary>
    public event EventHandler? ToggleRequested;

    public SidebarNav()
    {
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        _brandArea.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        _brandArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _brandArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _brandArea.Controls.Add(_brandToggle, 0, 0);
        _brandArea.Controls.Add(_brand, 1, 0);
        _brandToggle.ToggleRequested += (_, _) => ToggleRequested?.Invoke(this, EventArgs.Empty);
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _items.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _user.Padding = new Padding(Space.M, 0, 0, 0);
        _logout.Dock = DockStyle.Fill;
        _logout.Height = 40;
        _logout.Margin = new Padding(0, 0, 0, Space.XS);
        _logout.IconChar = AppIcons.Logout;
        _logout.IconSize = 20;
        _logout.Cursor = Cursors.Hand;
        _logout.TextImageRelation = TextImageRelation.ImageBeforeText;
        _logout.Click += (_, _) => LogoutRequested?.Invoke(this, EventArgs.Empty);

        _footer.RowCount = 3;
        _footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _footer.Controls.Add(_user, 0, 0);
        _footer.Controls.Add(_logout, 0, 2);

        _root.Controls.Add(_brandArea, 0, 0);
        _root.Controls.Add(_items, 0, 1);
        _root.Controls.Add(_footer, 0, 2);
        Controls.Add(_root);

        ApplyCompact();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Compact
    {
        get => _compact;
        set
        {
            if (_compact == value) return;
            _compact = value;
            ApplyCompact();
        }
    }

    public void SetUser(string username, string role)
    {
        _user.Text = $"{username}{Environment.NewLine}{role}";
    }

    public void SetItems(IEnumerable<NavItem> items)
    {
        _items.SuspendLayout();

        foreach (var (_, existing) in _buttons)
        {
            existing.Parent?.Controls.Remove(existing);
            existing.Dispose();
        }
        _buttons.Clear();

        foreach (var item in items)
        {
            var button = new IconButton
            {
                Dock = DockStyle.Fill,
                Height = 40,
                Margin = new Padding(0, 0, 0, Space.XS),
                IconChar = item.Icon,
                IconSize = 20,
                Cursor = Cursors.Hand
            };
            button.Click += (_, _) =>
            {
                SelectItem(item.Key);
                ItemSelected?.Invoke(this, item.Key);
            };
            if (item.Key == "profile") _footer.Controls.Add(button, 0, 1);
            else _items.Controls.Add(button);
            _buttons.Add((item, button));
        }

        ApplyCompact();
        if (IsHandleCreated) ApplyTheme();
        _items.ResumeLayout(true);
    }

    public void SelectItem(string key)
    {
        _selectedKey = key;
        foreach (var (item, button) in _buttons)
            StyleButton(button, item.Key == key);
    }

    private void ApplyCompact()
    {
        _brand.Text = "LiteraHub";
        _brand.Visible = !_compact;
        _brand.TextAlign = ContentAlignment.MiddleLeft;
        _brand.Padding = Padding.Empty;
        _tip.SetToolTip(_brandToggle, _compact ? "Expand navigation" : "Collapse navigation");
        _brandToggle.AccessibleName = _compact ? "Expand navigation" : "Collapse navigation";

        foreach (var (item, button) in _buttons)
        {
            button.Text = _compact ? "" : "  " + item.Title;
            button.TextImageRelation = _compact ? TextImageRelation.Overlay : TextImageRelation.ImageBeforeText;
            button.ImageAlign = _compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.Padding = _compact ? Padding.Empty : new Padding(6, 0, 0, 0);
            _tip.SetToolTip(button, _compact ? item.Title.Replace("&&", "&") : "");
        }

        _logout.Text = _compact ? "" : "  Log out";
        _logout.TextImageRelation = _compact ? TextImageRelation.Overlay : TextImageRelation.ImageBeforeText;
        _logout.ImageAlign = _compact ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft;
        _logout.TextAlign = ContentAlignment.MiddleLeft;
        _logout.Padding = _compact ? Padding.Empty : new Padding(6, 0, 0, 0);
        _tip.SetToolTip(_logout, _compact ? "Log out" : "");
        _user.Visible = !_compact;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;

        BackColor = c.Surface;
        _root.BackColor = c.Surface;
        _brandArea.BackColor = c.Surface;
        _brandToggle.SetColors(c.Accent, c.Surface);
        _items.BackColor = c.Surface;
        _footer.BackColor = c.Surface;

        _brand.BackColor = c.Surface;
        _brand.ForeColor = c.Accent;
        _brand.Font = Theme.Title;

        _user.BackColor = c.Surface;
        _user.ForeColor = c.TextMuted;
        _user.Font = Theme.Caption;

        StyleButton(_logout, selected: false);
        foreach (var (item, button) in _buttons)
            StyleButton(button, item.Key == _selectedKey);
    }

    private static void StyleButton(IconButton button, bool selected)
    {
        var c = Theme.Colors;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        button.FlatAppearance.MouseDownBackColor = c.SurfaceAlt;
        button.BackColor = selected ? c.AccentSoft : c.Surface;
        button.ForeColor = selected ? c.Text : c.TextMuted;
        button.IconColor = selected ? c.Accent : c.TextMuted;
        button.Font = selected ? Theme.BodyStrong : Theme.Body;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// One focusable, owner-drawn hover surface so the logo and hamburger cannot
/// flicker due to pointer movement between two overlapping child controls.
/// Draws the user-supplied white PNG tinted from Theme.Colors.Accent.
/// Uses the installed FontAwesome.Sharp IconChar.Bars for the hamburger.
/// </summary>
internal sealed class BrandMorphToggle : Control
{
    private readonly System.Windows.Forms.Timer _animation = new() { Interval = 16 };
    private readonly Bitmap _hamburger;
    private readonly Bitmap _fallbackBook;
    private Bitmap? _logo;
    private float _hoverAmount;
    private float _targetAmount;
    private Color _accent = Color.White;

    public event EventHandler? ToggleRequested;

    public BrandMorphToggle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = "Collapse navigation";

        // The application's existing FontAwesome.Sharp library, not a new PNG.
        using (Bitmap bars = IconChar.Bars.ToBitmap(IconFont.Auto, 96, Color.White))
            _hamburger = TrimTransparentPadding(bars);
        using (Bitmap book = IconChar.BookOpen.ToBitmap(IconFont.Auto, 96, Color.White))
            _fallbackBook = TrimTransparentPadding(book);
        LoadBrandLogo();

        _animation.Tick += (_, _) =>
        {
            float delta = _targetAmount - _hoverAmount;
            if (Math.Abs(delta) <= 0.12f)
            {
                _hoverAmount = _targetAmount;
                _animation.Stop();
            }
            else
                _hoverAmount += Math.Sign(delta) * 0.12f;

            Invalidate();
        };
    }

    private void LoadBrandLogo()
    {
        // The user-supplied PNG is embedded at build time, so it keeps
        // working after publish/Debug/Release and on another computer.
        // Keep a file fallback for older builds without the resource.
        try
        {
            using Stream? embedded = typeof(BrandMorphToggle).Assembly
                .GetManifestResourceStream("LiteraHub.Brand.Logo");
            if (embedded is not null)
            {
                using var image = Image.FromStream(embedded);
                _logo = TrimTransparentPadding(image);
                return;
            }
        }
        catch (ArgumentException) { /* Invalid image: try copied file. */ }
        catch (IOException) { /* Invalid stream: try copied file. */ }

        string file = Path.Combine(AppContext.BaseDirectory,
            "Assets", "Brand", "LiteraHub-Logo.png");
        if (!File.Exists(file)) return;
        try
        {
            using var stream = File.OpenRead(file);
            using var image = Image.FromStream(stream);
            _logo = TrimTransparentPadding(image);
        }
        catch (IOException) { }
        catch (ArgumentException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void SetColors(Color accent, Color background)
    {
        _accent = accent;
        BackColor = background;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        AnimateTo(1f);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        AnimateTo(0f);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        ToggleRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            OnClick(EventArgs.Empty);
            return;
        }
        base.OnKeyDown(e);
    }

    private void AnimateTo(float amount)
    {
        _targetAmount = amount;
        if (!_animation.Enabled) _animation.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // 56px header icon column: center the visible glyph at x=32,
        // matching the collapsed 64px navigation rail and module icons.
        // The logo image has transparent margins, so both assets were trimmed
        // before they reach this common 26px visual frame.
        int size = Math.Min(26, Math.Min(Width - 10, Height - 10));
        if (size <= 0) return;
        var rect = new Rectangle((Width - size) / 2 + 4, (Height - size) / 2, size, size);

        DrawTinted(e.Graphics, _logo ?? _fallbackBook, rect, 1f - _hoverAmount);
        DrawTinted(e.Graphics, _hamburger, rect, _hoverAmount);

        if (Focused && ShowFocusCues)
        {
            var focusRect = Rectangle.Inflate(rect, 5, 5);
            ControlPaint.DrawFocusRectangle(e.Graphics, focusRect, _accent, BackColor);
        }
    }

    private static Rectangle FitToVisualBounds(Image source, Rectangle frame)
    {
        float scale = Math.Min((float)frame.Width / source.Width,
            (float)frame.Height / source.Height);
        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        return new Rectangle(
            frame.Left + (frame.Width - width) / 2,
            frame.Top + (frame.Height - height) / 2,
            width, height);
    }

    /// <summary>
    /// Remove invisible margins from the supplied logo and FontAwesome glyphs.
    /// Otherwise the original 1254px PNG renders visibly smaller/lower than
    /// the 64px FontAwesome hamburger even when both use the same destination.
    /// This only processes the images once when the navigation control loads.
    /// </summary>
    private static Bitmap TrimTransparentPadding(Image source)
    {
        const int previewSize = 128;
        using var preview = new Bitmap(previewSize, previewSize,
            PixelFormat.Format32bppArgb);
        using (Graphics previewGraphics = Graphics.FromImage(preview))
        {
            previewGraphics.Clear(Color.Transparent);
            previewGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            previewGraphics.DrawImage(source, 0, 0, previewSize, previewSize);
        }

        int minX = previewSize, minY = previewSize;
        int maxX = -1, maxY = -1;
        for (int y = 0; y < previewSize; y++)
        {
            for (int x = 0; x < previewSize; x++)
            {
                if (preview.GetPixel(x, y).A < 20) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
            return new Bitmap(source);

        // Keep a one-preview-pixel margin so antialiased edges are not clipped.
        float scaleX = (float)source.Width / previewSize;
        float scaleY = (float)source.Height / previewSize;
        int left = Math.Max(0, (int)Math.Floor((minX - 1) * scaleX));
        int top = Math.Max(0, (int)Math.Floor((minY - 1) * scaleY));
        int right = Math.Min(source.Width, (int)Math.Ceiling((maxX + 2) * scaleX));
        int bottom = Math.Min(source.Height, (int)Math.Ceiling((maxY + 2) * scaleY));
        var sourceBounds = Rectangle.FromLTRB(left, top, right, bottom);

        var trimmed = new Bitmap(sourceBounds.Width, sourceBounds.Height,
            PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(trimmed))
        {
            graphics.Clear(Color.Transparent);
            graphics.DrawImage(source,
                new Rectangle(0, 0, trimmed.Width, trimmed.Height),
                sourceBounds, GraphicsUnit.Pixel);
        }
        return trimmed;
    }

    private void DrawTinted(Graphics canvas, Image source, Rectangle destination, float opacity)
    {
        if (opacity <= 0.001f) return;
        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix
        {
            Matrix00 = 0f,
            Matrix11 = 0f,
            Matrix22 = 0f,
            Matrix33 = opacity,
            Matrix40 = _accent.R / 255f,
            Matrix41 = _accent.G / 255f,
            Matrix42 = _accent.B / 255f
        };
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        Rectangle fitted = FitToVisualBounds(source, destination);
        canvas.DrawImage(source, fitted,
            0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animation.Stop();
            _animation.Dispose();
            _logo?.Dispose();
            _hamburger.Dispose();
            _fallbackBook.Dispose();
        }
        base.Dispose(disposing);
    }
}
