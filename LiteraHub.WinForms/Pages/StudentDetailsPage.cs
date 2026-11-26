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
using LiteraHub.Core.Security;
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
    private readonly bool _canHardDelete;
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = Padding.Empty, Padding = Padding.Empty };
    private readonly DetailsPageHeader _detailsHeader = new() { Dock = DockStyle.Fill };
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Panel _surface = new() { Margin = Padding.Empty };
    private readonly DetailsActionFooter _actionFooter = new() { Dock = DockStyle.Fill, Margin = Padding.Empty, UseStudentSizing = true };
    private readonly Button _save = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "" };
    private readonly Button _archive = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "" };
    private readonly Button _delete = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "" };
    private readonly Button _cancel = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "Cancel", Visible = false };
    private readonly Button _photoButton = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Text = "Choose Photo" };
    private readonly AvatarView _avatar = new();
    private readonly Dictionary<string, ThemedTextField> _inputs = new();
    private readonly Dictionary<string, Label> _viewInputs = new();
    private readonly Dictionary<string, (Label Caption, Control Control)> _fields = new();
    private readonly Label _studentHeading = new() { Text = "Student Information" };
    private readonly Label _addressHeading = new() { Text = "Address" };
    private readonly Label _contactHeading = new() { Text = "Personal & Contact" };
    private readonly Button _birthDate = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ThemedFilterDropdown _gender = new(new[] { "Select Gender", "Male", "Female", "Other", "Prefer not to say" })
    {
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 8, 0)
    };
    private readonly ToolTip _toolTip = new();
    private readonly Label _viewBirthDate = new() { TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
    private readonly Label _viewGender = new() { TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0) };
    private DateTime? _selectedBirthDate;
    private Image? _pendingPhoto;
    private bool _editing;
    private bool _busy;
    private readonly CatalogValidationController _validation = new();
    private bool _validationAttempted;
    private bool _arranging;
    private bool _responsive;

    public event EventHandler? BackRequested;

    private readonly IPermissionService _permissions;

    public StudentDetailsPage(IStudentService service, Student? student, bool canHardDelete, IPermissionService permissions)
    {
        _permissions = permissions;
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _canHardDelete = canHardDelete;
        _original = student;
        _editing = student is null;
        Dock = DockStyle.Fill;
        Padding = new Padding(Space.L);
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // Shared header occupies the same 56px title row + 42px subtitle row.
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _layout.Controls.Add(_detailsHeader, 0, 0);
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
        foreach (var view in new[] { _viewBirthDate, _viewGender })
        {
            view.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Colors.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, view.Width - 1), Math.Max(0, view.Height - 1));
            };
        }
        _selectedBirthDate = student?.BirthDate.Date ?? DateTime.Today.AddYears(-18);
        _gender.SelectedIndex = GenderIndex(student?.Gender);
        UpdateBirthDateCaption();
        _birthDate.Click += (_, _) => OpenBirthDatePicker();

        _surface.Controls.AddRange(new Control[] { _studentHeading, _addressHeading, _contactHeading, _avatar, _photoButton, _viewBirthDate, _viewGender });
        _actionFooter.Controls.AddRange(new Control[] { _delete, _archive, _save, _cancel });
        _actionFooter.Configure(_delete, _archive, _save, _cancel, () => false);
        _scroll.SetContent(_surface);
        Controls.Add(_layout);
        _toolTip.SetToolTip(_delete, _canHardDelete ? "Permanently delete this unreferenced student" : "Only administrators with HardDelete permission can delete students.");
        _delete.Enabled = _canHardDelete;
        _inputs["StudentNo"].Enabled = false;
        _avatar.Initials = Initials(student);
        if (student is not null)
        {
            try { _avatar.Photo = StudentPhotoStore.Load(student.StudentNo); }
            catch (IOException) { /* Missing/unreadable local photo: show initials. */ }
            catch (ArgumentException) { /* Invalid image cache. */ }
            catch (UnauthorizedAccessException) { /* Local photo access denied. */ }
        }

        _detailsHeader.BackRequested += (_, _) =>
        {
            if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty);
        };

        _save.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { SetEditing(true); return; }
            await SaveAsync();
        };
        _cancel.Click += (_, _) => { if (_busy) return; _validation.Clear(); _validationAttempted = false; ResetValidationCaptions(); RestoreOriginal(); SetEditing(false); };
        _archive.Click += async (_, _) => await ToggleActiveAsync();
        _delete.Click += async (_, _) => await DeleteStudentAsync();
        _photoButton.Click += (_, _) => SelectPhoto();
        _save.Paint += (_, e) => DrawAction(e, _save, 0);
        _archive.Paint += (_, e) => DrawAction(e, _archive, 1);
        _delete.Paint += (_, e) => DrawAction(e, _delete, 2);
        _inputs["FirstName"].TextChanged += (_, _) => RefreshInitials();
        _inputs["LastName"].TextChanged += (_, _) => RefreshInitials();
        Resize += (_, _) => ArrangeForm();
        _scroll.Resize += (_, _) => ArrangeForm();
        _actionFooter.Resize += (_, _) => PositionFooterButtons();
        ConfigureValidation();
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
        var view = new Label
        {
            AutoSize = false,
            TabStop = false,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 3, 12, 3),
            Visible = false,
            BorderStyle = BorderStyle.None,
            AutoEllipsis = false
        };
        view.Paint += (_, e) =>
        {
            using var border = new Pen(Theme.Colors.Border);
            e.Graphics.DrawRectangle(border, 0, 0,
                Math.Max(0, view.Width - 1), Math.Max(0, view.Height - 1));
        };
        _viewInputs.Add(key, view);
        _surface.Controls.Add(view);
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
            int fieldHeight = 40;
            // At 1280x720, the four Student Information fields plus a large
            // avatar used to exceed the viewport, forcing an unnecessary bar.
            // Retain full desktop spacing whenever there is room.
            int fieldStep = portrait ? 74 : (_scroll.ClientSize.Height < 600 ? 70 : 72);
            int y1 = startY, y2 = startY, y3 = startY;
            void Heading(Label label, int left, ref int top)
            {
                label.SetBounds(left, top, columnWidth, 26);
                top += 31;
            }
            void Field(string key, int left, ref int top)
            {
                var field = _fields[key];
                field.Caption.SetBounds(left, top, columnWidth, 19);
                field.Control.SetBounds(left, top + 27, columnWidth, fieldHeight);
                if (_viewInputs.TryGetValue(key, out var view))
                {
                    view.Bounds = field.Control.Bounds;
                    if (!_editing || key == "StudentNo") view.BringToFront();
                }
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
                int avatarSize = width >= 1300 ? 148 : width >= 1150 ? 120 : 78;
                // Budget space for heading, photo button, four inputs and the
                // bottom padding. The ScrollHost still scrolls if the actual
                // available viewport is too short (e.g. high-DPI small display).
                // Photo button is hidden in View mode; do not reserve its
                // 33px row there. That unused space caused the 1280px scrollbar.
                int photoRow = _editing ? 33 : 0;
                int maxAvatarThatFits = _scroll.ClientSize.Height -
                    (startY + 31 + 4 + photoRow + 4 * fieldStep + 14);
                avatarSize = Math.Min(avatarSize, Math.Max(78, maxAvatarThatFits));
                _avatar.SetBounds(x[0] + (columnWidth - avatarSize) / 2, y1, avatarSize, avatarSize);
                y1 += avatarSize + 4;
                if (_editing)
                {
                    _photoButton.SetBounds(x[0], y1, columnWidth, 28);
                    y1 += 33;
                }
                else
                {
                    _photoButton.Bounds = Rectangle.Empty;
                }
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
            _viewBirthDate.Bounds = _birthDate.Bounds;
            _viewGender.Bounds = _gender.Bounds;
            _viewBirthDate.BringToFront();
            _viewGender.BringToFront();
            PositionFooterButtons();
            // The shared DetailsPageHeader owns title font and theme styling.
            _save.Invalidate(); _archive.Invalidate(); _delete.Invalidate();
        }
        finally { _arranging = false; }
    }

    private void PositionFooterButtons()
    {
        _actionFooter.ArrangeActions();
        // Preserve existing Student edit-mode visibility semantics.
        _cancel.Visible = _original is not null && _editing;
    }

    private void SelectPhoto()
    {
        if (!_editing || _busy) return;
        using var picker = new OpenFileDialog
        {
            Title = "Select Student Photo",
            Filter = "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
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
        "male" => 1,
        "female" => 2,
        "other" => 3,
        "prefer not to say" => 4,
        _ => 0
    };

    private void SetEditing(bool editing)
    {
        _editing = editing;

        // EXACTLY like Book View: the original editor control is hidden and
        // a non-focusable themed value label occupies its place. Never set
        // ThemedTextField.ReadOnly in View mode; its own internal display label
        // obscures the border and caused the broken corner-only rectangles.
        foreach (var (key, field) in _inputs)
        {
            bool displayOnly = !editing || key == "StudentNo";
            field.ReadOnly = false;
            field.Enabled = true;
            field.TabStop = !displayOnly;
            field.Visible = !displayOnly;

            var view = _viewInputs[key];
            view.Text = field.Text;
            view.Visible = displayOnly;
            view.TabStop = false;
            if (displayOnly) view.BringToFront();
        }

        _viewBirthDate.Text = _selectedBirthDate?.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture) ?? "Not specified";
        _viewGender.Text = _gender.SelectedIndex > 0
            ? new[] { "", "Male", "Female", "Other", "Prefer not to say" }[_gender.SelectedIndex]
            : "Not specified";
        _viewBirthDate.Visible = !editing;
        _viewGender.Visible = !editing;
        _birthDate.Visible = editing;
        _gender.Visible = editing;
        _birthDate.Enabled = editing;
        _gender.Enabled = editing;
        if (!editing)
        {
            _viewBirthDate.BringToFront();
            _viewGender.BringToFront();
        }

        _photoButton.Visible = editing;
        _save.AccessibleName = editing ? "Save Student" : "Edit Student";
        // Match Book Editor: the three action buttons retain their positions
        // and remain visible for an existing record in View and Edit.
        _save.Visible = _permissions.Can(_original is null ? RolePermission.StudentsAdd : RolePermission.StudentsEdit);
        _archive.Visible = _original is not null && _permissions.Can(RolePermission.StudentsArchiveRestore);
        _archive.AccessibleName = _original?.IsActive == false ? "Restore Student" : "Archive Student";
        _delete.Visible = _original is not null && _canHardDelete;
        _delete.Enabled = _canHardDelete && !_busy;
        _cancel.Visible = _original is not null && editing;
        _detailsHeader.HeadingText = _original is null ? "Add Student" : editing ? "Edit Student" : "Student Details";
        _detailsHeader.SubtitleText = _original is null ? "Register a new student." : editing ? "Update student information." : "Viewing student record.";
        _detailsHeader.BackEnabled = !_busy;
        if (IsHandleCreated)
        {
            ArrangeForm();
            PositionFooterButtons();
        }
        _save.Invalidate();
        _archive.Invalidate();
        _delete.Invalidate();
    }

    private void RestoreOriginal()
    {
        if (_original is null) return;
        foreach (var (key, value) in new Dictionary<string, string?>
        {
            ["StudentNo"] = _original.StudentNo,
            ["FirstName"] = _original.FirstName,
            ["MiddleName"] = _original.MiddleName,
            ["LastName"] = _original.LastName,
            ["HouseNo"] = _original.HouseNo,
            ["Street"] = _original.Street,
            ["Municipality"] = _original.Municipality,
            ["CityProvince"] = _original.CityProvince,
            ["ZipCode"] = _original.ZipCode,
            ["MobileNo"] = _original.MobileNo,
            ["LandlineNo"] = _original.LandlineNo,
            ["Email"] = _original.Email
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

    private void ConfigureValidation()
    {
        // Mirror StudentService.Validate required fields. Existing caption rows
        // carry inline warnings without increasing form or horizontal width.
        RegisterText("StudentNo", () => !string.IsNullOrWhiteSpace(_inputs["StudentNo"].Text),
            "Student ID is required.");
        RegisterText("FirstName", () => !string.IsNullOrWhiteSpace(_inputs["FirstName"].Text),
            "First name is required.");
        RegisterText("LastName", () => !string.IsNullOrWhiteSpace(_inputs["LastName"].Text),
            "Last name is required.");

        RegisterText("HouseNo", () => !string.IsNullOrWhiteSpace(_inputs["HouseNo"].Text),
            "House number is required.");
        RegisterText("Street", () => !string.IsNullOrWhiteSpace(_inputs["Street"].Text),
            "Street is required.");
        RegisterText("Municipality", () => !string.IsNullOrWhiteSpace(_inputs["Municipality"].Text),
            "Municipality is required.");
        RegisterText("CityProvince", () => !string.IsNullOrWhiteSpace(_inputs["CityProvince"].Text),
            "City/province is required.");
        RegisterText("ZipCode", () => !string.IsNullOrWhiteSpace(_inputs["ZipCode"].Text),
            "ZIP code is required.");

        _validation.Register(_birthDate,
            () => _selectedBirthDate.HasValue &&
                  _selectedBirthDate.Value.Date >= new DateTime(1900, 1, 1) &&
                  _selectedBirthDate.Value.Date <= DateTime.Today,
            "Select a birthdate between 1900 and today.");
        _birthDate.TextChanged += (_, _) => RefreshValidationCaptions();

        _validation.Register(_gender,
            () => _gender.SelectedIndex > 0,
            "Select a gender.");
        _gender.SelectedIndexChanged += (_, _) => RefreshValidationCaptions();

        RegisterText("MobileNo", () => !string.IsNullOrWhiteSpace(_inputs["MobileNo"].Text),
            "Mobile number is required.");
        RegisterText("Email", () =>
        {
            string email = _inputs["Email"].Text.Trim();
            return email.Length > 0 &&
                System.Net.Mail.MailAddress.TryCreate(email, out var parsed) &&
                string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
        }, "Enter a valid email address (required).");
        // Middle name and landline remain optional, per StudentService.
    }

    private void RegisterText(string key, Func<bool> predicate, string error)
    {
        var control = _inputs[key];
        _validation.Register(control, predicate, error);
        control.TextChanged += (_, _) => RefreshValidationCaptions();
    }

    private void RefreshValidationCaptions()
    {
        if (!_validationAttempted || IsDisposed || Disposing) return;
        foreach (var (key, label) in new (string Key, string Caption)[]
        {
            ("StudentNo", "Student ID"),
            ("FirstName", "First name"),
            ("LastName", "Last name"),
            ("HouseNo", "House number"),
            ("Street", "Street"),
            ("Municipality", "Municipality"),
            ("CityProvince", "City / province"),
            ("ZipCode", "ZIP code"),
            ("MobileNo", "Mobile number"),
            ("Email", "Email address")
        })
        {
            bool invalid = _inputs[key].HasValidationError;
            string warning = key == "Email" ? "Invalid email" : "Required";
            UpdateValidationCaption(key, label, invalid, warning);
        }

        bool birthInvalid = !_selectedBirthDate.HasValue ||
                            _selectedBirthDate.Value.Date < new DateTime(1900, 1, 1) ||
                            _selectedBirthDate.Value.Date > DateTime.Today;
        UpdateValidationCaption("BirthDate", "Birthdate", birthInvalid, "Invalid date");
        UpdateValidationCaption("Gender", "Gender", _gender.SelectedIndex == 0, "Required");
    }

    private void UpdateValidationCaption(string key, string name, bool invalid, string warning)
    {
        if (!_fields.TryGetValue(key, out var field)) return;
        field.Caption.Text = invalid ? name + " • " + warning : name;
        field.Caption.ForeColor = invalid ? Theme.Colors.Danger : Theme.Colors.Text;
        field.Caption.AutoEllipsis = true;
    }

    private void ResetValidationCaptions()
    {
        foreach (var (key, field) in _fields)
        {
            // Retain the original captions, including optional-field labels.
            string caption = key switch
            {
                "StudentNo" => "Student ID",
                "FirstName" => "First name",
                "MiddleName" => "Middle name (optional)",
                "LastName" => "Last name",
                "HouseNo" => "House number",
                "Street" => "Street",
                "Municipality" => "Municipality",
                "CityProvince" => "City / province",
                "ZipCode" => "ZIP code",
                "BirthDate" => "Birthdate",
                "Gender" => "Gender",
                "MobileNo" => "Mobile number",
                "LandlineNo" => "Landline (optional)",
                "Email" => "Email address",
                _ => field.Caption.Text
            };
            field.Caption.Text = caption;
            field.Caption.ForeColor = Theme.Colors.Text;
        }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;
        _validationAttempted = true;
        if (_validation.Validate().Count > 0)
        {
            RefreshValidationCaptions();
            _validation.ShowSummary(FindForm(), "this student");
            if (!IsDisposed && !Disposing) _validation.FocusFirstInvalid();
            return;
        }
        bool creating = _original is null;
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
            _validation.Clear();
            _validationAttempted = false;
            ResetValidationCaptions();
            CatalogActionFeedback.Notify(this,
                creating ? "Student added successfully." : "Student updated successfully.");
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
            if (result.Success)
            {
                CatalogActionFeedback.Notify(this,
                    active ? "Student restored successfully." : "Student archived successfully.");
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            else ThemedMessageDialog.ShowAlert(FindForm(), "Student Status", result.Error ?? "Operation failed.", danger: true);
        }
        catch (Exception ex) { if (!IsDisposed && !Disposing) ThemedMessageDialog.ShowAlert(FindForm(), "Student Status", ex.Message, danger: true); }
        finally { _busy = false; if (!IsDisposed && !Disposing) _archive.Enabled = true; }
    }


    private async Task DeleteStudentAsync()
    {
        if (_original is null || _busy || !_canHardDelete) return;
        if (!ThemedMessageDialog.Confirm(FindForm(), "Permanently Delete Student",
                $"Permanently delete '{_original.FirstName} {_original.LastName}'?\n\n" +
                "This cannot be undone. Students with borrowing history or linked accounts cannot be deleted.",
                danger: true, confirmText: "Delete")) return;
        _busy = true;
        _delete.Enabled = false;
        _save.Enabled = false;
        _archive.Enabled = false;
        try
        {
            var result = await _service.DeleteAsync(_original.StudentId);
            if (IsDisposed || Disposing) return;
            if (result.Success)
            {
                // Photo cache is intentionally left untouched if local deletion fails;
                // database deletion is authoritative and always happens first.
                StudentPhotoStore.TryDelete(_original.StudentNo);
                CatalogActionFeedback.Notify(this, "Student deleted successfully.");
                BackRequested?.Invoke(this, EventArgs.Empty);
            }
            else
                ThemedMessageDialog.ShowAlert(FindForm(), "Delete Student",
                    result.Error ?? "Unable to delete student.", danger: true);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "Delete Student", ex.Message, danger: true);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed && !Disposing)
            {
                _delete.Enabled = _canHardDelete;
                _save.Enabled = true;
                _archive.Enabled = true;
            }
        }
    }

    // Same button label grouping, colors and icon geometry as BookEditorPage.
    private void DrawAction(PaintEventArgs e, Button button, int kind)
    {
        string label = kind == 0 ? (_editing ? "Save Student" : "Edit Student")
            : kind == 2 ? "Delete Student"
            : (_original?.IsActive == false ? "Restore Student" : "Archive Student");
        if (_responsive && button.Width < 130)
            label = kind == 0 ? (_editing ? "Save" : "Edit")
                : kind == 2 ? "Delete" : (_original?.IsActive == false ? "Restore" : "Archive");

        IconButtonContent.DrawAction(e, button, kind, label, _original?.IsActive == false);
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _detailsHeader.ApplyTheme();
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
        foreach (var value in _viewInputs.Values)
        {
            value.BackColor = c.SurfaceAlt;
            value.ForeColor = c.Text;
            value.Font = Theme.BodyStrong;
            value.Invalidate();
        }
        foreach (var value in new[] { _viewBirthDate, _viewGender })
        {
            value.BackColor = c.SurfaceAlt;
            value.ForeColor = c.Text;
            value.Font = Theme.BodyStrong;
        }
        _birthDate.BackColor = c.SurfaceAlt;
        _birthDate.ForeColor = c.Text;
        _birthDate.Font = Theme.Body;
        _birthDate.FlatAppearance.BorderColor = c.Border;
        _birthDate.FlatAppearance.BorderSize = 1;
        _birthDate.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _gender.ApplyTheme();
        foreach (var button in new[] { _cancel, _archive, _photoButton })
        {
            button.BackColor = c.SurfaceAlt;
            button.ForeColor = c.Text;
            button.Font = Theme.Body;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        }
        // Match BookEditorPage's three action styles and interactive states.
        _save.BackColor = Color.FromArgb(34, 139, 86);
        _save.ForeColor = Color.White;
        _save.Font = Theme.BodyStrong;
        _save.FlatAppearance.BorderSize = 0;
        _save.FlatAppearance.MouseOverBackColor = Color.Empty;
        _save.FlatAppearance.MouseDownBackColor = Color.Empty;

        _archive.BackColor = c.Surface;
        _archive.ForeColor = c.Text;
        _archive.Font = Theme.BodyStrong;
        _archive.FlatAppearance.BorderColor = c.Border;
        _archive.FlatAppearance.BorderSize = 1;
        _archive.FlatAppearance.MouseOverBackColor = Color.Empty;
        _archive.FlatAppearance.MouseDownBackColor = Color.Empty;

        Color deleteRed = Color.FromArgb(255, 143, 137);
        _delete.BackColor = deleteRed;
        _delete.ForeColor = Color.White;
        _delete.Font = Theme.BodyStrong;
        _delete.FlatAppearance.BorderSize = 0;
        _delete.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(deleteRed, 0.12f);
        _delete.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(deleteRed, 0.23f);
        // Shared DetailsPageHeader refreshes itself when the theme changes.
        _avatar.Invalidate();
        ArrangeForm();
        RefreshValidationCaptions();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pendingPhoto?.Dispose();
            _toolTip.Dispose();
            _validation.Dispose();
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
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
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
    public static void TryDelete(string studentNo)
    {
        try { string file = PathFor(studentNo); if (File.Exists(file)) File.Delete(file); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
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
