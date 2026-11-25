using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;
using LiteraHub.WinForms.Forms;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Coordinates themed invalid-field borders and optional inline hints.
/// Register a predicate for each field, then call Validate on Save.
/// Errors are never shown for untouched fields; they clear when corrected.
/// The owning page is responsible for allocating vertical space for hints.
/// </summary>
public sealed class CatalogValidationController : IDisposable
{
    private sealed class Field
    {
        public required Control Control { get; init; }
        public required Func<bool> IsValid { get; init; }
        public required string Message { get; init; }
        public Label? Hint { get; init; }
        public bool Invalid { get; set; }
        public EventHandler? Changed { get; set; }
    }

    private readonly List<Field> _fields = new();
    private bool _disposed;

    public CatalogValidationController()
    {
        Theme.Changed += OnThemeChanged;
    }

    /// <summary>Register a themed text field, dropdown, or selection button.</summary>
    public void Register(Control control, Func<bool> isValid, string message,
        Label? inlineHint = null)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(isValid);
        if (_disposed) throw new ObjectDisposedException(nameof(CatalogValidationController));
        if (_fields.Any(f => ReferenceEquals(f.Control, control)))
            throw new InvalidOperationException("Validation field already registered.");

        var field = new Field
        {
            Control = control,
            IsValid = isValid,
            Message = message,
            Hint = inlineHint
        };

        if (inlineHint is not null)
        {
            inlineHint.AutoSize = false;
            inlineHint.AutoEllipsis = true;
            inlineHint.TextAlign = ContentAlignment.MiddleLeft;
            inlineHint.Text = message;
            inlineHint.ForeColor = Theme.Colors.Danger;
            inlineHint.Visible = false;
            inlineHint.AccessibleDescription = message;
        }

        EventHandler changed = (_, _) =>
        {
            if (field.Invalid && !field.Control.IsDisposed && field.IsValid())
                SetInvalid(field, false);
        };
        field.Changed = changed;

        if (control is ThemedTextField textField)
            textField.TextChanged += changed;
        else if (control is ThemedFilterDropdown dropdown)
            dropdown.SelectedIndexChanged += changed;
        else if (control is ThemedAuthorPicker authors)
            authors.SelectionChanged += changed;
        else if (control is ThemedCategoryPicker categories)
            categories.SelectionChanged += changed;
        else
            control.TextChanged += changed;

        _fields.Add(field);
    }

    /// <summary>Check all registered fields; returns the error messages in registration order.</summary>
    public IReadOnlyList<string> Validate()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CatalogValidationController));
        foreach (Field field in _fields)
        {
            if (!field.Control.IsDisposed)
                SetInvalid(field, !field.IsValid());
        }
        return _fields.Where(f => f.Invalid).Select(f => f.Message).ToArray();
    }

    /// <summary>Focus the first invalid field when possible.</summary>
    public void FocusFirstInvalid()
    {
        Field? first = _fields.FirstOrDefault(f => f.Invalid && !f.Control.IsDisposed);
        if (first is null) return;
        if (first.Control.CanFocus) first.Control.Focus();
    }

    /// <summary>One themed summary dialog for an attempted invalid Save.</summary>
    public void ShowSummary(IWin32Window? owner, string recordName)
    {
        var errors = _fields.Where(f => f.Invalid).Select(f => f.Message).ToArray();
        if (errors.Length == 0) return;
        string heading = errors.Length == 1 ? "Please correct 1 field" :
            $"Please correct {errors.Length} fields";
        string body = heading + " before saving " + recordName + ".\n\n" +
            string.Join("\n", errors.Select(s => "• " + s));
        ThemedMessageDialog.ShowAlert(owner, "Check Required Fields", body, danger: true);
    }

    public void Clear()
    {
        if (_disposed) return;
        foreach (Field field in _fields) SetInvalid(field, false);
    }

    private void SetInvalid(Field field, bool invalid)
    {
        field.Invalid = invalid;
        if (field.Control is ThemedTextField textField)
            textField.HasValidationError = invalid;
        else if (field.Control is Button button)
        {
            button.FlatAppearance.BorderSize = invalid ? 2 : 1;
            button.FlatAppearance.BorderColor = invalid ? Theme.Colors.Danger : Theme.Colors.Border;
            button.ForeColor = invalid ? Theme.Colors.Danger : Theme.Colors.Text;
            button.Invalidate();
        }

        if (field.Hint is not null && !field.Hint.IsDisposed)
        {
            field.Hint.ForeColor = Theme.Colors.Danger;
            field.Hint.Visible = invalid;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        foreach (Field field in _fields)
        {
            if (!field.Invalid || field.Control.IsDisposed) continue;
            // Defer until the page's normal theme update has completed.
            if (field.Control.IsHandleCreated)
                field.Control.BeginInvoke(new Action(() =>
                {
                    if (!_disposed && !field.Control.IsDisposed && field.Invalid)
                        SetInvalid(field, true);
                }));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Clear();
        _disposed = true;
        Theme.Changed -= OnThemeChanged;
        foreach (Field field in _fields)
        {
            if (field.Changed is null || field.Control.IsDisposed) continue;
            if (field.Control is ThemedTextField textField)
                textField.TextChanged -= field.Changed;
            else if (field.Control is ThemedFilterDropdown dropdown)
                dropdown.SelectedIndexChanged -= field.Changed;
            else if (field.Control is ThemedAuthorPicker authors)
                authors.SelectionChanged -= field.Changed;
            else if (field.Control is ThemedCategoryPicker categories)
                categories.SelectionChanged -= field.Changed;
            else
                field.Control.TextChanged -= field.Changed;
        }
        _fields.Clear();
    }
}
