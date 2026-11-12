
using System;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class AddAuthorDialog : Form
{
    private readonly IAuthorCreationService _service;
    private readonly AuthorRecord? _existing;

    private readonly Label _title = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly ThemedTextField _first = new()
    {
        Dock = DockStyle.Fill,
        MaxLength = 100,
        Margin = Padding.Empty
    };

    private readonly ThemedTextField _middle = new()
    {
        Dock = DockStyle.Fill,
        MaxLength = 100,
        Margin = Padding.Empty
    };

    private readonly ThemedTextField _last = new()
    {
        Dock = DockStyle.Fill,
        MaxLength = 100,
        Margin = Padding.Empty
    };

    private readonly ThemedFilterDropdown _gender =
        new(new[] { "Unknown", "Male", "Female", "Other" })
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

    private readonly ThemedTextField _birthDate = new()
    {
        Dock = DockStyle.Fill,
        MaxLength = 10,
        Margin = Padding.Empty
    };

    private readonly Button _calendar = new()
    {
        Text = "▦",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat,
        Cursor = Cursors.Hand,
        Margin = new Padding(6, 0, 0, 0)
    };

    private readonly Label _error = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };

    private readonly BusyButton _save = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly Button _cancel = new()
    {
        Text = "Cancel",
        Dock = DockStyle.Fill,
        FlatStyle = FlatStyle.Flat
    };

    private bool _saving;

    public AddAuthorDialog(
        IAuthorCreationService service,
        AuthorRecord? existing = null)
    {
        _service = service ??
            throw new ArgumentNullException(nameof(service));

        _existing = existing;

        bool editing = existing is not null;

        Text = editing ? "Edit Author" : "Add Author";
        _title.Text = Text;
        _save.Text = editing ? "Save Changes" : "Save Author";

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        ClientSize = new Size(440, 545);
        MinimumSize = new Size(350, 575);
        Font = Theme.Body;

        BuildLayout();

        if (existing is not null)
        {
            _first.Text = existing.FirstName;
            _middle.Text = existing.MiddleName ?? "";
            _last.Text = existing.LastName;

            _gender.SelectedIndex = existing.Gender switch
            {
                "Male" => 1,
                "Female" => 2,
                "Other" => 3,
                _ => 0
            };

            _birthDate.Text = existing.BirthDate?
                .ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture) ?? "";
        }

        ApplyTheme();
        Theme.Changed += OnThemeChanged;

        _calendar.Click += (_, _) => OpenCalendar();

        _save.Click += async (_, _) =>
        {
            if (!_saving)
                await SaveAsync();
        };

        _cancel.Click += (_, _) =>
        {
            if (_saving)
                return;

            DialogResult = DialogResult.Cancel;
            Close();
        };

        FormClosing += (_, e) =>
        {
            if (_saving)
                e.Cancel = true;
        };

        Shown += (_, _) => _first.FocusInput();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 14,
            Padding = new Padding(24, 18, 24, 20),
            Margin = Padding.Empty
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));

        for (int i = 0; i < 5; i++)
        {
            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 32));

            root.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 40));
        }

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 46));

        root.Controls.Add(_title, 0, 0);

        root.Controls.Add(
            FieldLabel("First Name *"), 0, 1);
        root.Controls.Add(_first, 0, 2);

        root.Controls.Add(
            FieldLabel("Middle Name"), 0, 3);
        root.Controls.Add(_middle, 0, 4);

        root.Controls.Add(
            FieldLabel("Last Name *"), 0, 5);
        root.Controls.Add(_last, 0, 6);

        root.Controls.Add(
            FieldLabel("Gender *"), 0, 7);
        root.Controls.Add(_gender, 0, 8);

        root.Controls.Add(
            FieldLabel("Birth Date (optional) — YYYY-MM-DD"),
            0, 9);

        var dateRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };

        dateRow.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        dateRow.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 48));

        dateRow.Controls.Add(_birthDate, 0, 0);
        dateRow.Controls.Add(_calendar, 1, 0);

        root.Controls.Add(dateRow, 0, 10);
        root.Controls.Add(_error, 0, 12);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };

        footer.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 40));

        footer.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 60));

        _cancel.Margin = new Padding(0, 3, 8, 3);
        _save.Margin = new Padding(0, 3, 0, 3);

        footer.Controls.Add(_cancel, 0, 0);
        footer.Controls.Add(_save, 1, 0);

        root.Controls.Add(footer, 0, 13);

        Controls.Add(root);

        AcceptButton = _save;
        CancelButton = _cancel;
    }

    private static Label FieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Caption,
            ForeColor = Theme.Colors.TextMuted
        };
    }

    private void OpenCalendar()
    {
        if (_saving)
            return;

        DateTime initial = DateTime.Today;

        if (DateTime.TryParseExact(
            _birthDate.Text.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var existing))
        {
            initial = existing;
        }

        using var dialog =
            new ThemedBirthDateDialog(initial);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _birthDate.Text = dialog.SelectedDate?
                .ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture) ?? "";
        }
    }

    private async Task SaveAsync()
    {
        string first = _first.Text.Trim();
        string middle = _middle.Text.Trim();
        string last = _last.Text.Trim();

        if (string.IsNullOrWhiteSpace(first))
        {
            _error.Text = "First name is required.";
            _first.FocusInput();
            return;
        }

        if (string.IsNullOrWhiteSpace(last))
        {
            _error.Text = "Last name is required.";
            _last.FocusInput();
            return;
        }

        DateTime? birthDate = null;
        string dateText = _birthDate.Text.Trim();

        if (dateText.Length > 0)
        {
            if (!DateTime.TryParseExact(
                dateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
            {
                _error.Text =
                    "Use YYYY-MM-DD for the birth date.";

                _birthDate.FocusInput();
                return;
            }

            if (parsed > DateTime.Today)
            {
                _error.Text =
                    "Birth date cannot be in the future.";
                return;
            }

            birthDate = parsed.Date;
        }

        var input = new AuthorInput(
            first,
            middle.Length == 0 ? null : middle,
            last,
            _gender.SelectedIndex switch
            {
                1 => "Male",
                2 => "Female",
                3 => "Other",
                _ => "Unknown"
            },
            birthDate);

        _saving = true;
        _error.Text = "";

        SetFieldsEnabled(false);

        try
        {
            bool saved = false;

            await _save.RunAsync(async () =>
            {
                if (_existing is null)
                {
                    var result =
                        await _service.CreateAsync(input);

                    saved = result.Success;

                    if (!saved)
                        _error.Text = result.Error ??
                            "Unable to create author.";
                }
                else
                {
                    var result =
                        await _service.UpdateAsync(
                            _existing.AuthorId,
                            input);

                    saved = result.Success;

                    if (!saved)
                        _error.Text = result.Error ??
                            "Unable to update author.";
                }
            }, "Saving...");

            if (saved)
            {
                _saving = false;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed && !Disposing)
                _error.Text =
                    "Unable to save: " + ex.Message;
        }
        finally
        {
            _saving = false;

            if (!IsDisposed && !Disposing)
                SetFieldsEnabled(true);
        }
    }

    private void SetFieldsEnabled(bool enabled)
    {
        _first.Enabled = enabled;
        _middle.Enabled = enabled;
        _last.Enabled = enabled;
        _gender.Enabled = enabled;
        _birthDate.Enabled = enabled;
        _calendar.Enabled = enabled;
        _cancel.Enabled = enabled;
    }

    private void OnThemeChanged(
        object? sender,
        EventArgs e)
    {
        if (!IsDisposed)
            ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;

        _title.Font = Theme.Title;
        _title.ForeColor = c.Text;

        _cancel.BackColor = c.Surface;
        _cancel.ForeColor = c.Text;
        _cancel.FlatAppearance.BorderColor = c.Border;

        _calendar.BackColor = c.Surface;
        _calendar.ForeColor = c.Text;
        _calendar.FlatAppearance.BorderColor = c.Border;

        _gender.ApplyTheme();

        foreach (Control control in Controls)
            UpdateLabelColors(control);

        _error.ForeColor = c.Danger;
    }

    private static void UpdateLabelColors(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is Label label &&
                label.Text != "Add Author" &&
                label.Text != "Edit Author")
            {
                label.ForeColor =
                    Theme.Colors.TextMuted;
            }

            UpdateLabelColors(child);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= OnThemeChanged;

        base.Dispose(disposing);
    }
}
