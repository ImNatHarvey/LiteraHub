
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LiteraHub.WinForms.Theming;

/// <summary>
/// Applies the current LiteraHub palette to standard WinForms controls.
/// Call Apply() when constructing a page and after theme changes.
/// </summary>
public static class NativeTheme
{
    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        ApplyControl(root);

        foreach (Control child in root.Controls)
        {
            Apply(child);
        }
    }

    private static void ApplyControl(Control control)
    {
        var c = Theme.Colors;

        switch (control)
        {
            case TextBoxBase textBox:
                textBox.BackColor = c.Surface;
                textBox.ForeColor = c.Text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = c.Surface;
                comboBox.ForeColor = c.Text;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;

            case NumericUpDown numeric:
                numeric.BackColor = c.Surface;
                numeric.ForeColor = c.Text;
                numeric.BorderStyle = BorderStyle.FixedSingle;
                break;

            case DateTimePicker datePicker:
                datePicker.CalendarMonthBackground = c.Surface;
                datePicker.CalendarForeColor = c.Text;
                datePicker.CalendarTitleBackColor = c.Accent;
                datePicker.CalendarTitleForeColor = c.OnAccent;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = c.Background;
                checkBox.ForeColor = c.Text;
                break;

            case RadioButton radioButton:
                radioButton.BackColor = c.Background;
                radioButton.ForeColor = c.Text;
                break;

            case Label label:
                label.ForeColor = c.Text;
                break;
        }
    }
}
