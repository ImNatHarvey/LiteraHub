
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LiteraHub.WinForms.Theming;

public static class NativeTheme
{
    [DllImport("uxtheme.dll",
        CharSet = CharSet.Unicode,
        ExactSpelling = true)]
    private static extern int SetWindowTheme(
        IntPtr hwnd,
        string? subAppName,
        string? subIdList);

    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        ApplyControl(root);

        foreach (Control child in root.Controls)
            Apply(child);
    }

    public static void ApplyScrollBarTheme(Control control)
    {
        if (control.IsDisposed || !control.IsHandleCreated)
            return;

        try
        {
            string? theme = Theme.Mode == ThemeMode.Dark
                ? "DarkMode_Explorer"
                : "Explorer";

            SetWindowTheme(control.Handle, theme, null);

            // DataGridView creates its own scrollbar controls.
            // Apply the same theme to those child handles.
            if (control is DataGridView grid)
            {
                foreach (Control child in grid.Controls)
                {
                    if (child is ScrollBar && child.IsHandleCreated)
                    {
                        SetWindowTheme(
                            child.Handle,
                            theme,
                            null);
                    }
                }
            }

            control.Invalidate();
        }
        catch (DllNotFoundException)
        {
            // Keep native scrolling available.
        }
        catch (EntryPointNotFoundException)
        {
            // Unsupported Windows implementation.
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

            case DateTimePicker picker:
                picker.CalendarMonthBackground = c.Surface;
                picker.CalendarForeColor = c.Text;
                picker.CalendarTitleBackColor = c.Accent;
                picker.CalendarTitleForeColor = c.OnAccent;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = c.Background;
                checkBox.ForeColor = c.Text;
                break;

            case RadioButton radio:
                radio.BackColor = c.Background;
                radio.ForeColor = c.Text;
                break;

            case Label label:
                label.ForeColor = c.Text;
                break;
        }

        if (control is DataGridView or ScrollBar)
            ApplyScrollBarTheme(control);
    }
}
