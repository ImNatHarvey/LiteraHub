
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class CategoryDialog : Form
{
    private readonly ICategoryService _service;
    private readonly Category? _existing;

    private readonly Label _title = new()
    {
        AutoSize = true
    };

    private readonly Label _nameLabel = new()
    {
        Text = "Category Name *",
        AutoSize = true
    };

    private readonly ThemedTextField _name = new()
    {
        MaxLength = 100,
        Dock = DockStyle.Fill
    };

    private readonly Label _descriptionLabel = new()
    {
        Text = "Description",
        AutoSize = true
    };

    private readonly ThemedTextField _description = new()
    {
        Multiline = true,
        MaxLength = 500,
        Dock = DockStyle.Fill
    };

    private readonly Label _error = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
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

    public CategoryDialog(
        ICategoryService service,
        Category? existing = null)
    {
        _service = service ??
            throw new ArgumentNullException(nameof(service));

        _existing = existing;

        bool editing = existing is not null;

        Text = editing ? "Edit Category" : "Add Category";
        _title.Text = Text;
        _save.Text = editing ? "Save Changes" : "Save Category";

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 370);
        MinimumSize = new Size(350, 400);
        Font = Theme.Body;

        BuildLayout();

        if (existing is not null)
        {
            _name.Text = existing.Name;
            _description.Text = existing.Description ?? "";
        }

        ApplyTheme();
        Theme.Changed += OnThemeChanged;

        _save.Click += async (_, _) =>
        {
            if (!_saving)
                await SaveAsync();
        };

        _cancel.Click += (_, _) =>
        {
            if (!_saving)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        FormClosing += (_, e) =>
        {
            if (_saving) e.Cancel = true;
        };

        Shown += (_, _) => _name.FocusInput();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 7
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _title.Font = Theme.Title;
        _nameLabel.Font = Theme.Caption;
        _descriptionLabel.Font = Theme.Caption;

        _name.Margin = new Padding(0, 0, 0, 8);
        _description.Margin = new Padding(0, 0, 0, 8);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };

        buttons.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 40));

        buttons.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 60));

        buttons.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _cancel.Margin = new Padding(0, 3, 8, 3);
        _save.Margin = new Padding(0, 3, 0, 3);

        buttons.Controls.Add(_cancel, 0, 0);
        buttons.Controls.Add(_save, 1, 0);

        root.Controls.Add(_title, 0, 0);
        root.Controls.Add(_nameLabel, 0, 1);
        root.Controls.Add(_name, 0, 2);
        root.Controls.Add(_descriptionLabel, 0, 3);
        root.Controls.Add(_description, 0, 4);
        root.Controls.Add(_error, 0, 5);
        root.Controls.Add(buttons, 0, 6);

        Controls.Add(root);
    }

    private async Task SaveAsync()
    {
        string name = _name.Text.Trim();
        string description = _description.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _error.Text = "Category name is required.";
            _name.FocusInput();
            return;
        }

        _saving = true;
        _name.Enabled = false;
        _description.Enabled = false;
        _cancel.Enabled = false;
        _error.Text = string.Empty;

        try
        {
            bool saved = false;

            await _save.RunAsync(async () =>
            {
                var input = new CategoryInput(
                    name,
                    string.IsNullOrWhiteSpace(description)
                        ? null
                        : description);

                var result = _existing is null
                    ? await _service.CreateAsync(input)
                    : await _service.UpdateAsync(
                        _existing.CategoryId, input);

                if (result.Success)
                    saved = true;
                else
                    _error.Text =
                        result.Error ?? "Unable to save category.";

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
            _error.Text = "Unable to save: " + ex.Message;
        }
        finally
        {
            if (!IsDisposed)
            {
                _saving = false;
                _name.Enabled = true;
                _description.Enabled = true;
                _cancel.Enabled = true;
            }
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (!IsDisposed)
            ApplyTheme();
    }

    private void ApplyTheme()
    {
        var c = Theme.Colors;

        BackColor = c.Background;
        ForeColor = c.Text;

        _title.ForeColor = c.Text;
        _nameLabel.ForeColor = c.TextMuted;
        _descriptionLabel.ForeColor = c.TextMuted;

        _error.ForeColor = c.Danger;

        _cancel.BackColor = c.Surface;
        _cancel.ForeColor = c.Text;
        _cancel.FlatAppearance.BorderColor = c.Border;

        _name.Invalidate();
        _description.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= OnThemeChanged;

        base.Dispose(disposing);
    }
}
