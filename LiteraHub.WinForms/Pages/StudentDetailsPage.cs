using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Pages;

/// <summary>In-shell student details; existing records start in view-only mode.</summary>
public sealed class StudentDetailsPage : ThemedUserControl
{
    private readonly IStudentService _service;
    private readonly Student? _original;
    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
        Margin = Padding.Empty, Padding = Padding.Empty
    };
    private readonly TableLayoutPanel _header = new()
    {
        Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty
    };
    private readonly Label _heading = new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Button _back = new()
    {
        Text = "‹  Back", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand, Margin = new Padding(8, 5, 0, 5)
    };
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Panel _content = new() { Margin = Padding.Empty };
    private readonly TableLayoutPanel _footer = new()
    {
        Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
        Margin = Padding.Empty, Padding = new Padding(0, 10, 0, 0)
    };
    private readonly Button _save = new() { Text = "Save Student", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
    private readonly Button _cancel = new() { Text = "Back", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
    private readonly Button _archive = new() { Text = "Archive Student", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(4, 0, 0, 0) };
    private readonly Dictionary<string, ThemedTextField> _inputs = new();
    private readonly List<(string Key, Label Label, Control Editor)> _fields = new();
    private readonly List<(string Title, Label Heading, string[] Keys)> _sections = new();
    private readonly Button _birthDate = new() { FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ThemedFilterDropdown _gender = new(new[] { "Select gender", "Male", "Female", "Other", "Prefer not to say" });
    private DateTime? _selectedBirthDate;
    private bool _editing;
    private bool _busy;
    private bool _arranging;

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
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        _header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _header.Controls.Add(_heading, 0, 0);
        _header.Controls.Add(_back, 1, 0);

        AddField("StudentNo", "Student ID", 30, student?.StudentNo);
        AddField("FirstName", "First name", 100, student?.FirstName);
        AddField("MiddleName", "Middle name (optional)", 100, student?.MiddleName);
        AddField("LastName", "Last name", 100, student?.LastName);
        AddField("HouseNo", "House number", 50, student?.HouseNo);
        AddField("Street", "Street", 150, student?.Street);
        AddField("Municipality", "Municipality", 100, student?.Municipality);
        AddField("CityProvince", "City / province", 100, student?.CityProvince);
        AddField("ZipCode", "ZIP code", 10, student?.ZipCode);
        AddField("MobileNo", "Mobile number", 30, student?.MobileNo);
        AddField("LandlineNo", "Landline (optional)", 30, student?.LandlineNo);
        AddField("Email", "Email address", 150, student?.Email);
        AddSpecial("BirthDate", "Birthdate", _birthDate);
        AddSpecial("Gender", "Gender", _gender);
        AddSection("STUDENT INFORMATION", "StudentNo", "FirstName", "MiddleName", "LastName", "BirthDate", "Gender");
        AddSection("ADDRESS", "HouseNo", "Street", "Municipality", "CityProvince", "ZipCode");
        AddSection("CONTACT INFORMATION", "MobileNo", "LandlineNo", "Email");

        _selectedBirthDate = student?.BirthDate.Date ?? DateTime.Today.AddYears(-18);
        UpdateBirthDateCaption();
        _gender.SelectedIndex = GenderIndex(student?.Gender);
        _birthDate.Click += (_, _) => OpenBirthDatePicker();

        _scroll.SetContent(_content);
        _scroll.Resize += (_, _) => ArrangeForm();
        _footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 162));
        _footer.Controls.Add(_archive, 0, 0);
        _footer.Controls.Add(_cancel, 1, 0);
        _footer.Controls.Add(_save, 2, 0);
        _archive.Dock = DockStyle.Left;
        _archive.Width = 154;
        _archive.Margin = Padding.Empty;
        _layout.Controls.Add(_header, 0, 0);
        _layout.Controls.Add(_scroll, 0, 1);
        _layout.Controls.Add(_footer, 0, 2);
        Controls.Add(_layout);

        _back.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        _cancel.Click += (_, _) =>
        {
            if (_editing && _original is not null)
            {
                RestoreOriginal();
                SetEditing(false);
            }
            else BackRequested?.Invoke(this, EventArgs.Empty);
        };
        _save.Click += async (_, _) =>
        {
            if (_busy) return;
            if (!_editing) { SetEditing(true); return; }
            await SaveAsync();
        };
        _archive.Click += async (_, _) => await ToggleActiveAsync();
        Resize += (_, _) => ArrangeForm();
        SetEditing(_editing);
        ApplyTheme();
    }

    private void AddField(string key, string caption, int limit, string? value)
    {
        var input = new ThemedTextField { MaxLength = limit, Text = value ?? "" };
        _inputs.Add(key, input);
        AddSpecial(key, caption, input);
    }

    private void AddSpecial(string key, string caption, Control editor)
    {
        var label = new Label { Text = caption, TextAlign = ContentAlignment.MiddleLeft };
        _fields.Add((key, label, editor));
        _content.Controls.Add(label);
        _content.Controls.Add(editor);
    }

    private void AddSection(string caption, params string[] keys)
    {
        var heading = new Label { Text = caption, TextAlign = ContentAlignment.MiddleLeft };
        _sections.Add((caption, heading, keys));
        _content.Controls.Add(heading);
    }

    private void ArrangeForm()
    {
        if (_arranging || IsDisposed || Disposing || _scroll.ClientSize.Width < 1) return;
        _arranging = true;
        try
        {
            int width = Math.Max(1, _scroll.ClientSize.Width - 12);
            bool portrait = width < 600;
            int columns = portrait ? 1 : 2;
            int pad = portrait ? 8 : 16;
            int gap = 20;
            int inner = Math.Max(80, width - pad * 2);
            int colWidth = Math.Max(80, (inner - (columns - 1) * gap) / columns);
            int y = 4;
            foreach (var section in _sections)
            {
                section.Heading.SetBounds(pad, y, inner, 30);
                y += 38;
                int index = 0;
                foreach (string key in section.Keys)
                {
                    var field = _fields.First(f => f.Key == key);
                    int row = index / columns;
                    int col = index % columns;
                    int x = pad + col * (colWidth + gap);
                    int top = y + row * 77;
                    field.Label.SetBounds(x, top, colWidth, 21);
                    field.Editor.SetBounds(x, top + 24, colWidth, 40);
                    index++;
                }
                y += ((section.Keys.Length + columns - 1) / columns) * 77 + 9;
            }
            int height = y + 12;
            if (_content.Height != height) _content.Height = height;
            _scroll.RefreshMetrics();
            _heading.Font = portrait ? Theme.BodyStrong : Theme.Title;
            _footer.ColumnStyles[1].Width = portrait ? 90 : 142;
            _footer.ColumnStyles[2].Width = portrait ? 120 : 162;
            _archive.Width = portrait ? 78 : 154;
            _archive.Text = portrait
                ? (_original?.IsActive == false ? "Restore" : "Archive")
                : (_original?.IsActive == false ? "Restore Student" : "Archive Student");
        }
        finally { _arranging = false; }
    }

    private void OpenBirthDatePicker()
    {
        if (!_editing || _busy) return;
        using var picker = new ThemedBirthDateDialog(_selectedBirthDate ?? DateTime.Today.AddYears(-18));
        if (picker.ShowDialog(FindForm()) != DialogResult.OK) return;
        _selectedBirthDate = picker.SelectedDate;
        UpdateBirthDateCaption();
    }

    private void UpdateBirthDateCaption()
    {
        _birthDate.Text = _selectedBirthDate.HasValue
            ? "  " + _selectedBirthDate.Value.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture) + "   ▾"
            : "  Select birthdate   ▾";
    }

    private static int GenderIndex(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "male" => 1, "female" => 2, "other" => 3, "prefer not to say" => 4, _ => 0
    };

    private void SetEditing(bool editing)
    {
        _editing = editing;
        foreach (var field in _inputs.Values) field.Enabled = editing;
        _birthDate.Enabled = editing;
        _gender.Enabled = editing;
        _save.Text = editing ? "Save Student" : "Edit Student";
        _cancel.Text = editing && _original is not null ? "Cancel" : "Back";
        _archive.Visible = _original is not null && !editing;
        _archive.Text = _scroll.ClientSize.Width < 600
            ? (_original?.IsActive == false ? "Restore" : "Archive")
            : (_original?.IsActive == false ? "Restore Student" : "Archive Student");
        _heading.Text = _original is null ? "Add Student" : editing ? "Edit Student" : "Student Details";
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
            ServiceResult result = _original is null
                ? await _service.CreateAsync(Collect())
                : await _service.UpdateAsync(_original.StudentId, Collect());
            if (IsDisposed || Disposing) return;
            if (!result.Success)
            {
                ThemedMessageDialog.ShowAlert(FindForm(), "Student Validation", result.Error ?? "Unable to save student.");
                return;
            }
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "Students", ex.Message, danger: true);
        }
        finally { _busy = false; if (!IsDisposed && !Disposing) _save.Enabled = true; }
    }

    private async Task ToggleActiveAsync()
    {
        if (_original is null || _busy) return;
        bool active = !_original.IsActive;
        string action = active ? "restore" : "archive";
        if (!ThemedMessageDialog.Confirm(FindForm(), active ? "Restore Student" : "Archive Student",
                $"Are you sure you want to {action} this student?", danger: !active,
                confirmText: active ? "Restore" : "Archive")) return;
        _busy = true;
        _archive.Enabled = false;
        try
        {
            var result = await _service.SetActiveAsync(_original.StudentId, active);
            if (IsDisposed || Disposing) return;
            if (!result.Success)
            {
                ThemedMessageDialog.ShowAlert(FindForm(), "Students", result.Error ?? "Unable to change student status.");
                return;
            }
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                ThemedMessageDialog.ShowAlert(FindForm(), "Students", ex.Message, danger: true);
        }
        finally { _busy = false; if (!IsDisposed && !Disposing) _archive.Enabled = true; }
    }

    protected override void ApplyTheme()
    {
        base.ApplyTheme();
        var c = Theme.Colors;
        _layout.BackColor = c.Background;
        _header.BackColor = c.Background;
        _footer.BackColor = c.Background;
        _content.BackColor = c.Background;
        _heading.BackColor = c.Background;
        _heading.ForeColor = c.Text;
        _heading.Font = Theme.Title;
        foreach (var section in _sections)
        {
            section.Heading.ForeColor = c.Text;
            section.Heading.BackColor = c.Background;
            section.Heading.Font = Theme.BodyStrong;
        }
        foreach (var field in _fields)
        {
            field.Label.BackColor = c.Background;
            field.Label.ForeColor = c.TextMuted;
            field.Label.Font = Theme.Body;
        }
        _gender.ApplyTheme();
        _birthDate.BackColor = c.Surface;
        _birthDate.ForeColor = c.Text;
        _birthDate.Font = Theme.Body;
        _birthDate.FlatAppearance.BorderColor = c.Border;
        _birthDate.FlatAppearance.BorderSize = 1;
        _birthDate.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        _save.BackColor = c.Accent;
        _save.ForeColor = c.OnAccent;
        _save.Font = Theme.BodyStrong;
        _save.FlatAppearance.BorderSize = 0;
        foreach (var button in new[] { _cancel, _archive, _back })
        {
            button.BackColor = c.Surface;
            button.ForeColor = c.Text;
            button.Font = Theme.Body;
            button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = c.SurfaceAlt;
        }
        ArrangeForm();
    }
}
