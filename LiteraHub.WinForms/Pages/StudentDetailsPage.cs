using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>In-shell, three-column student details with themed controls.</summary>
public sealed class StudentDetailsPage : ThemedUserControl
{
    private readonly IStudentService _service;
    private readonly Student? _original;
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly TableLayoutPanel _header = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly Label _heading = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _back = new() { Text = "", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(8, 4, 0, 4) };
    private readonly Label _subtitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Panel _surface = new() { Margin = Padding.Empty };
    private readonly Panel _actionFooter = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Button _save = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "" };
    private readonly Button _archive = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "" };
    private readonly Button _delete = new() { FlatStyle = FlatStyle.Flat, Text = "", Enabled = false };
    private readonly Button _cancel = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "Cancel", Visible = false };
    private readonly Button _photoButton = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "Choose Photo" };
    private readonly AvatarView _avatar = new();
    private readonly Dictionary<string, ThemedTextField> _inputs = new();
    private readonly Dictionary<string, (Label Caption, Control Control)> _fields = new();
    private readonly Label _studentHeading = new() { Text = "Student Information" };
    private readonly Label _addressHeading = new() { Text = "Address" };
    private readonly Label _contactHeading = new() { Text = "Personal & Contact" };
    private readonly Button _birthDate = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ThemedFilterDropdown _gender = new(new[] { "Select Gender", "Male", "Female", "Other", "Prefer not to say" });
    private readonly ToolTip _toolTip = new();
    private DateTime? _selectedBirthDate;
    private Image? _pendingPhoto;
    private bool _editing;
    private bool _busy;
    private bool _arranging;
    private bool _responsive;

    public event EventHandler? BackRequested;

    public StudentDetailsPage(IStudentService service, Student? student)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _original = student;
        _editing = student is null;
        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _header.Controls.Add(_heading, 0, 0);
        _header.Controls.Add(_back, 1, 0);
        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_subtitle, 0, 1);
        _layout.Controls.Add(_scroll, 0, 2);
        _layout.Controls.Add(_actionFooter, 0, 3);

        AddInput("StudentNo", "Student ID", 30, student?.StudentNo ?? MakeStudentNumber());
        AddInput("FirstName", "First name", 100, student?.FirstName);
        AddInput("MiddleName", "Middle name (optional)", 100, student?.MiddleName);
        AddInput("LastName", "Last name", 100, student?.LastName);
        AddInput("HouseNo", "House number", 50, student?.HouseNo);
        AddInput("Street", "Street", 150, student?.Street);
        AddInput("Municipality", "Municipality", 100, student?.Municipality);
        AddInput("CityProvince", "City / province", 100, student?.CityProvince);
        AddInput("ZipCode", "ZIP code", 10, student?.ZipCode);
        AddInput("MobileNo", "Mobile number", 30, student?.MobileNo);
        AddInput("LandlineNo", "Landline (optional)", 30, student?.LandlineNo);
        AddInput("Email", "Email address", 150, student?.Email);
        AddSpecial("BirthDate", "Birthdate", _birthDate);
        AddSpecial("Gender", "Gender", _gender);
        _selectedBirthDate = student?.BirthDate.Date ?? DateTime.Today.AddYears(-18);
        _gender.SelectedIndex = GenderIndex(student?.Gender);
        UpdateBirthDateCaption();
        _birthDate.Click += (_, _) => OpenBirthDatePicker();

        _surface.Controls.AddRange(new Control[] { _studentHeading, _addressHeading, _contactHeading, _avatar, _photoButton });
        _actionFooter.Controls.AddRange(new Control[] { _delete, _archive, _save, _cancel });
        _scroll.SetContent(_surface);
        Controls.Add(_layout);
        _toolTip.SetToolTip(_delete, "Permanent student deletion is not implemented in the current service; no data is deleted.");
        _inputs["StudentNo"].Enabled = false;
        _avatar.Initials = Initials(student);
        if (student is not null)
        {
            try { _avatar.Photo = StudentPhotoStore.Load(student.StudentNo); }
            catch (IOException) { /* Missing/unreadable local photo: show initials. */ }
            catch (ArgumentException) { /* Invalid image cache. */ }
            catch (UnauthorizedAccessException) { /* Local photo access denied. */ }
        }

        _back.Click += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _back.Paint += (_, e) => DrawBackButton(e);
        _save.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { SetEditing(true); return; }
            await SaveAsync();
        };
        _cancel.Click += (_, _) => { if (_busy) return; RestoreOriginal(); SetEditing(false); };
        _archive.Click += async (_, _) => await ToggleActiveAsync();
        _photoButton.Click += (_, _) => SelectPhoto();
        _save.Paint += (_, e) => DrawAction(e, _save, 0);
        _archive.Paint += (_, e) => DrawAction(e, _archive, 1);
        _delete.Paint += (_, e) => DrawAction(e, _delete, 2);
        _inputs["FirstName"].TextChanged += (_, _) => RefreshInitials();
        _inputs["LastName"].TextChanged += (_, _) => RefreshInitials();
        Resize += (_, _) => ArrangeForm();
        _scroll.Resize += (_, _) => ArrangeForm();
        _actionFooter.Resize += (_, _) => PositionFooterButtons();
        SetEditing(_editing);
        ApplyTheme();
    }

    private static string MakeStudentNumber()
    {
        // Opaque, collision-resistant candidate; database StudentNo UNIQUE is authoritative.
        return "STU-" + RandomNumberGenerator.GetInt32(100000000, 1000000000).ToString(CultureInfo.InvariantCulture);
    }

    private static string Initials(Student? s) => s is null ? "?" :
        ((s.FirstName.Length > 0 ? s.FirstName[..1] : "") + (s.LastName.Length > 0 ? s.LastName[..1] : "")).ToUpperInvariant();

    private void RefreshInitials()
    {
        string first = _inputs["FirstName"].Text.Trim();
        string last = _inputs["LastName"].Text.Trim();
        _avatar.Initials = (first.Length > 0 ? first[..1] : "") + (last.Length > 0 ? last[..1] : "");
    }

    private void AddInput(string key, string caption, int max, string? value)
    {
        var field = new ThemedTextField { Text = value ?? "", MaxLength = max };
        _inputs.Add(key, field);
        AddSpecial(key, caption, field);
    }

    private void AddSpecial(string key, string caption, Control field)
    {
        var label = new Label { Text = caption, TextAlign = ContentAlignment.MiddleLeft };
        _fields.Add(key, (label, field));
        _surface.Controls.Add(label);
        _surface.Controls.Add(field);
    }

    private void ArrangeForm()
    {
        if (_arranging || IsDisposed || Disposing || _scroll.ClientSize.Width <= 0 || _scroll.ClientSize.Height <= 0) return;
        _arranging = true;
        try
        {
            int width = Math.Max(1, _scroll.ClientSize.Width - 12);
            bool portrait = width < 690;
            _responsive = portrait;
            int pad = portrait ? 12 : 16;
            int gap = 22;
            int inner = Math.Max(80, width - pad * 2);
            int columnWidth = portrait ? inner : Math.Max(90, (inner - 2 * gap) / 3);
            int[] x = portrait ? new[] { pad, pad, pad } : new[] { pad, pad + columnWidth + gap, pad + (columnWidth + gap) * 2 };
            int startY = 14;
            int fieldHeight = portrait ? 40 : 42;
            int fieldStep = portrait ? 60 : 64;
            int y1 = startY, y2 = startY, y3 = startY;
            void Heading(Label label, int left, ref int top)
            {
                label.SetBounds(left, top, columnWidth, 26);
                top += 31;
            }
            void Field(string key, int left, ref int top)
            {
                var field = _fields[key];
                field.Caption.SetBounds(left, top, columnWidth, 17);
                field.Control.SetBounds(left, top + 19, columnWidth, fieldHeight);
                top += fieldStep;
            }
            if (portrait)
            {
                Heading(_studentHeading, x[0], ref y1);
                _avatar.SetBounds(x[0] + (columnWidth - 100) / 2, y1, 100, 100);
                y1 += 108;
                _photoButton.SetBounds(x[0], y1, columnWidth, 32);
                y1 += 43;
                foreach (var key in new[] { "StudentNo", "FirstName", "MiddleName", "LastName", "BirthDate", "Gender" }) Field(key, x[0], ref y1);
                y2 = y1 + 7;
                Heading(_addressHeading, x[0], ref y2);
                foreach (var key in new[] { "HouseNo", "Street", "Municipality", "CityProvince", "ZipCode" }) Field(key, x[0], ref y2);
                y3 = y2 + 7;
                Heading(_contactHeading, x[0], ref y3);
                foreach (var key in new[] { "MobileNo", "LandlineNo", "Email" }) Field(key, x[0], ref y3);
            }
            else
            {
                Heading(_studentHeading, x[0], ref y1);
                int avatarSize = width >= 1300 ? 148 : width >= 1150 ? 120 : 90;
                _avatar.SetBounds(x[0] + (columnWidth - avatarSize) / 2, y1, avatarSize, avatarSize);
                y1 += avatarSize + 4;
                _photoButton.SetBounds(x[0], y1, columnWidth, 30);
                y1 += 36;
                foreach (var key in new[] { "StudentNo", "FirstName", "MiddleName", "LastName" }) Field(key, x[0], ref y1);

                Heading(_addressHeading, x[1], ref y2);
                foreach (var key in new[] { "HouseNo", "Street", "Municipality", "CityProvince", "ZipCode" }) Field(key, x[1], ref y2);
                Heading(_contactHeading, x[2], ref y3);
                foreach (var key in new[] { "BirthDate", "Gender", "MobileNo", "LandlineNo", "Email" }) Field(key, x[2], ref y3);
            }
            // The content surface contains fields only. Buttons live in the
            // dedicated footer beneath it, matching the Books editor.
            int contentBottom = Math.Max(y1, Math.Max(y2, y3)) + 14;
            int targetHeight = Math.Max(contentBottom, _scroll.ClientSize.Height);
            if (_surface.Height != targetHeight) _surface.Height = targetHeight;
            _scroll.RefreshMetrics();
            PositionFooterButtons();
            _heading.Font = portrait ? Theme.BodyStrong : Theme.Title;
            _save.Invalidate(); _archive.Invalidate(); _delete.Invalidate();
        }
        finally { _arranging = false; }
    }

    private void DrawBackButton(PaintEventArgs e)
    {
        var c = Theme.Colors;
        var g = e.Graphics;
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        int cx = 18, cy = _back.Height / 2;
        using var pen = new Pen(c.Accent, 2.2f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLines(pen, new[] { new Point(cx + 4, cy - 6), new Point(cx - 2, cy), new Point(cx + 4, cy + 6) });
        TextRenderer.DrawText(g, "Back", Theme.Body,
            new Rectangle(31, 0, Math.Max(1, _back.Width - 34), _back.Height), c.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        g.SmoothingMode = old;
    }

    private void PositionFooterButtons()
    {
        if (_actionFooter.ClientSize.Width < 1) return;
        bool portrait = _actionFooter.ClientSize.Width < 690;
        const int gap = 8;
        const int height = 40;
        int y = 8;
        int right = _actionFooter.ClientSize.Width;
        int width = portrait
            ? Math.Max(70, (right - 2 * gap) / 3)
            : Math.Min(210, Math.Max(110, (right * 2 / 3 - 2 * gap) / 3));
        int left = Math.Max(0, right - 3 * width - 2 * gap);
        _delete.SetBounds(left, y, width, height);
        _archive.SetBounds(left + width + gap, y, width, height);
        _save.SetBounds(left + 2 * (width + gap), y, width, height);
        // Keep Cancel accessible while editing without shifting the actions.
        _cancel.SetBounds(0, y, Math.Min(115, Math.Max(80, left - gap)), height);
        _cancel.Visible = _original is not null && _editing;
    }

    private void SelectPhoto()
    {
        if (!_editing || _busy) return;
        using var picker = new OpenFileDialog
        {
            Title = "Select Student Photo", Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true, Multiselect = false
        };
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            using var stream = File.OpenRead(picker.FileName);
            if (stream.Length > 5_000_000) throw new InvalidOperationException("Maximum photo size is 5 MB.");
            using var image = Image.FromStream(stream, false, true);
            var bitmap = new Bitmap(256, 256);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int square = Math.Min(image.Width, image.Height);
                var crop = new Rectangle((image.Width - square) / 2, (image.Height - square) / 2, square, square);
                g.DrawImage(image, new Rectangle(0, 0, 256, 256), crop, GraphicsUnit.Pixel);
            }
            _pendingPhoto?.Dispose();
            _pendingPhoto = bitmap;
            _avatar.Photo = (Image)bitmap.Clone();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException or UnauthorizedAccessException or InvalidOperationException)
        {
            ThemedMessageDialog.ShowAlert(FindForm(), "Student Photo", "Unable to load image: " + ex.Message, danger: true);
        }
    }

    private void OpenBirthDatePicker()
    {
        if (!_editing || _busy) return;
        using var picker = new ThemedBirthDateDialog(_selectedBirthDate ?? DateTime.Today.AddYears(-18));
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        _selectedBirthDate = picker.SelectedDate;
        UpdateBirthDateCaption();
    }

    private void UpdateBirthDateCaption() => _birthDate.Text = _selectedBirthDate is DateTime date
        ? "  " + date.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture) + "  ▾" : "  Select birthdate  ▾";

    private static int GenderIndex(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "male" => 1, "female" => 2, "other" => 3, "prefer not to say" => 4, _ => 0
    };

    private void SetEditing(bool editing)
    {
        _editing = editing;
        foreach (var field in _inputs.Values) field.Enabled = editing;
        _inputs["StudentNo"].Enabled = false; // Generated student number is immutable.
        _birthDate.Enabled = editing;
        _gender.Enabled = editing;
        _photoButton.Visible = editing;
        _save.AccessibleName = editing ? "Save Student" : "Edit Student";
        _archive.Visible = _original is not null && !editing;
        _archive.AccessibleName = _original?.IsActive == false ? "Restore Student" : "Archive Student";
        _delete.Visible = _original is not null && !editing;
        _cancel.Visible = _original is not null && editing;
        _heading.Text = _original is null ? "Add Student" : editing ? "Edit Student" : "Student Details";
        _subtitle.Text = _original is null ? "Register a new student." : editing ? "Update student information." : "Viewing student record.";
        if (IsHandleCreated) { ArrangeForm(); PositionFooterButtons(); }
    }

    private void RestoreOriginal()
    {
        if (_original is null) return;
        foreach (var (key, value) in new Dictionary<string, string?>
        {
            ["StudentNo"] = _original.StudentNo, ["FirstName"] = _original.FirstName,
            ["MiddleName"] = _original.MiddleName, ["LastName"] = _original.LastName,
            ["HouseNo"] = _original.HouseNo, ["Street"] = _original.Street,
            ["Municipality"] = _original.Municipality, ["CityProvince"] = _original.CityProvince,
            ["ZipCode"] = _original.ZipCode, ["MobileNo"] = _original.MobileNo,
            ["LandlineNo"] = _original.LandlineNo, ["Email"] = _original.Email
        }) _inputs[key].Text = value ?? "";
        _selectedBirthDate = _original.BirthDate.Date;
        UpdateBirthDateCaption();
        _gender.SelectedIndex = GenderIndex(_original.Gender);
        _pendingPhoto?.Dispose(); _pendingPhoto = null;
        try { _avatar.Photo = StudentPhotoStore.Load(_original.StudentNo); }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException) { _avatar.Photo = null; }
    }

    private StudentInput Collect() => new(
        _inputs["StudentNo"].Text, _inputs["FirstName"].Text, _inputs["MiddleName"].Text,
        _inputs["LastName"].Text, _inputs["HouseNo"].Text, _inputs["Street"].Text,
        _inputs["Municipality"].Text, _inputs["CityProvince"].Text, _inputs["ZipCode"].Text,
        _selectedBirthDate!.Value.Date,
        _gender.SelectedIndex == 0 ? "" : new[] { "", "Male", "Female", "Other", "Prefer not to say" }[_gender.SelectedIndex],
        _inputs["MobileNo"].Text, _inputs["LandlineNo"].Text, _inputs["Email"].Text);

    private async Task SaveAsync()
    {
        if (_busy) return;
        if (_selectedBirthDate is null)
        {
            ThemedMessageDialog.ShowAlert(FindForm(), "Student Validation", "Select a birthdate before saving.");
            return;
        }
        _busy = true;
        _save.Enabled = false;
        try
        {
            var result = _original is null ? await _service.CreateAsync(Collect()) : await _service.UpdateAsync(_original.StudentId, Collect());
            if (IsDisposed || Disposing) return;
            if (!result.Success)
            {
                ThemedMessageDialog.ShowAlert(FindForm(), "Student Validation", result.Error ?? "Unable to save student.");
                return;
            }
            if (_pendingPhoto is not null)
            {
                try { StudentPhotoStore.Save(_inputs["StudentNo"].Text, _pendingPhoto); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    ThemedMessageDialog.ShowAlert(FindForm(), "Student Saved", "Student was saved, but the local photo could not be stored: " + ex.Message);
                }
            }
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing) ThemedMessageDialog.ShowAlert(FindForm(), "Students", ex.Message, danger: true);
        }
        finally { _busy = false; if (!IsDisposed && !Disposing) _save.Enabled = true; }
    }

    private async Task ToggleActiveAsync()
    {
        if (_original is null || _busy) return;
        bool active = !_original.IsActive;
        if (!ThemedMessageDialog.Confirm(FindForm(), active ? "Restore Student" : "Archive Student",
                $"Do you want to {(active ? "restore" : "archive")} this student?", danger: !active,
                confirmText: active ? "Restore" : "Archive")) return;
        _busy = true;
        _archive.Enabled = false;
        try
        {
            var result = await _service.SetActiveAsync(_original.StudentId, active);
            if (IsDisposed || Disposing) return;
            if (result.Success) BackRequested?.Invoke(this, EventArgs.Empty);
            else ThemedMessageDialog.ShowAlert(FindForm(), "Student Status", result.Error ?? "Operation failed.", danger: true);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) ThemedMessageDialog.ShowAlert(FindForm(), "Student Status", ex.Message, danger: true); }
        finally { _busy = false; if (!IsDisposed && !Disposing) _archive.Enabled = true; }
    }

    private void DrawAction(PaintEventArgs e, Button button, int kind)
    {
        var c = Theme.Colors;
        var g = e.Graphics;
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        string label = kind == 0 ? (_editing ? "Save Student" : "Edit Student") : kind == 1
            ? (_original?.IsActive == false ? "Restore Student" : "Archive Student") : "Delete Student";
        if (_responsive && button.Width < 130) label = kind == 0 ? (_editing ? "Save" : "Edit") : kind == 1 ? "Archive" : "Delete";
        Color ink = button.Enabled ? button.ForeColor : c.TextMuted;
        var measured = TextRenderer.MeasureText(label, button.Font);
        int cx = Math.Max(12, (button.Width - measured.Width - 24) / 2 + 8);
        int cy = button.Height / 2;
        using var pen = new Pen(ink, 2f);
        if (kind == 0)
        {
            g.DrawLine(pen, cx - 8, cy - 7, cx + 1, cy - 7);
            g.DrawLine(pen, cx - 8, cy - 7, cx - 8, cy + 8);
            g.DrawLine(pen, cx - 8, cy + 8, cx + 7, cy + 8);
            g.DrawLine(pen, cx + 7, cy + 8, cx + 7, cy + 1);
            g.DrawLine(pen, cx - 2, cy + 3, cx + 8, cy - 7);
        }
        else if (kind == 1)
        {
            g.DrawRectangle(pen, cx - 8, cy - 4, 16, 12);
            g.DrawLine(pen, cx - 10, cy - 8, cx + 10, cy - 8);
            g.DrawLine(pen, cx, cy - 2, cx, cy + 5);
        }
        else
        {
            g.DrawLine(pen, cx - 9, cy - 6, cx + 9, cy - 6);
            g.DrawLine(pen, cx - 4, cy - 9, cx + 4, cy - 9);
            g.DrawRectangle(pen, cx - 7, cy - 3, 14, 12);
            g.DrawLine(pen, cx - 3, cy, cx - 3, cy + 6);
            g.DrawLine(pen, cx + 3, cy, cx + 3, cy + 6);
        }
        TextRenderer.DrawText(g, label, button.Font, new Rectangle(cx + 16, 0, Math.Max(1, button.Width - cx - 18), button.Height), ink,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        g.SmoothingMode = old;
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;
        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;
        _subtitle.BackColor = c.Background;
        _subtitle.ForeColor = c.TextMuted;
        _subtitle.Font = Theme.Body;
        _surface.BackColor = c.Surface;
        _actionFooter.BackColor = c.Background;
        foreach (var section in new[] { _studentHeading, _addressHeading, _contactHeading })
        {
            section.BackColor = c.Surface;
            section.ForeColor = c.Text;
            section.Font = Theme.BodyStrong;
            section.TextAlign = ContentAlignment.MiddleLeft;
        }
        foreach (var field in _fields.Values)
        {
            field.Caption.BackColor = c.Surface;
            field.Caption.ForeColor = c.Text;
            field.Caption.Font = Theme.Body;
        }
        _birthDate.BackColor = c.SurfaceAlt;
        _birthDate.ForeColor = c.Text;
        _birthDate.Font = Theme.Body;
        _birthDate.FlatAppearance.BorderColor = c.Border;
        _birthDate.FlatAppearance.BorderSize = 1;
        _birthDate.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _gender.ApplyTheme();
        foreach (var button in new[] { _back, _cancel, _archive, _photoButton })
        {
            button.BackColor = c.SurfaceAlt;
            button.ForeColor = c.Text;
            button.Font = Theme.Body;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        }
        _save.BackColor = Color.FromArgb(34, 139, 86);
        _save.ForeColor = Color.White;
        _save.Font = Theme.BodyStrong;
        _save.FlatAppearance.BorderSize = 0;
        _save.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _delete.BackColor = Color.FromArgb(255, 143, 137);
        _delete.ForeColor = Color.White;
        _delete.Font = Theme.BodyStrong;
        _delete.FlatAppearance.BorderSize = 0;
        _back.Invalidate();
        _avatar.Invalidate();
        ArrangeForm();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pendingPhoto?.Dispose();
            _toolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class AvatarView : Control
    {
        private string _initials = "?";
        private Image? _photo;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Initials { get => _initials; set { _initials = string.IsNullOrWhiteSpace(value) ? "?" : value.ToUpperInvariant(); Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? Photo { get => _photo; set { if (ReferenceEquals(_photo, value)) return; var previous = _photo; _photo = value; previous?.Dispose(); Invalidate(); } }
        public AvatarView() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var c = Theme.Colors;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var clip = new GraphicsPath();
            var rect = new Rectangle(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));
            clip.AddEllipse(rect);
            using (var bg = new SolidBrush(c.AccentSoft)) g.FillEllipse(bg, rect);
            if (_photo is not null)
            {
                var state = g.Save();
                g.SetClip(clip);
                g.DrawImage(_photo, rect);
                g.Restore(state);
            }
            else
            {
                using var font = new Font(Theme.Title.FontFamily, Math.Max(12, Height * .3f), FontStyle.Bold);
                using var centered = new StringFormat
                { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center,
                  FormatFlags = StringFormatFlags.NoWrap };
                using var ink = new SolidBrush(c.Accent);
                // DrawString avoids the TextRenderer vertical overhang on '?' and initials.
                g.DrawString(_initials, font, ink, rect, centered);
            }
            using var pen = new Pen(c.Border, 2f);
            g.DrawEllipse(pen, rect);
        }
        protected override void Dispose(bool disposing) { if (disposing) _photo?.Dispose(); base.Dispose(disposing); }
    }
}

/// <summary>Local Windows profile photo cache, not a shared/database attachment.</summary>
internal static class StudentPhotoStore
{
    private static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiteraHub", "StudentPhotos");
    private static string PathFor(string studentNo)
    {
        byte[] key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(studentNo));
        return Path.Combine(DirectoryPath, Convert.ToHexString(key) + ".png");
    }
    public static Image? Load(string studentNo)
    {
        string path = PathFor(studentNo);
        if (!File.Exists(path)) return null;
        using var stream = File.OpenRead(path);
        using var decoded = Image.FromStream(stream);
        return new Bitmap(decoded);
    }
    public static void Save(string studentNo, Image photo)
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = PathFor(studentNo);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            photo.Save(temporary, System.Drawing.Imaging.ImageFormat.Png);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
