
using System;
using System.Drawing;
using System.Windows.Forms;
using LiteraHub.WinForms.Theming;

namespace LiteraHub.WinForms.Forms;

public sealed class ThemedMessageDialog : Form
{
    private readonly Label _titleLabel;
    private readonly Label _messageLabel;
    private readonly Label _symbol;
    private readonly Button _confirm;
    private readonly Button _cancel;

    private readonly bool _danger;
    private readonly bool _confirmation;

    private ThemedMessageDialog(
        string title,
        string message,
        bool confirmation,
        bool danger,
        string confirmText)
    {
        _danger = danger;
        _confirmation = confirmation;

        Text = title;
        Font = Theme.Body;

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;

        ClientSize = new Size(450, 230);
        MinimumSize = new Size(390, 265);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = new Padding(24, 18, 24, 18)
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));

        _titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Title,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, 12, 0, 0)
        };

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 42));

        content.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        content.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        _symbol = new Label
        {
            Text = danger ? "!" : "?",
            Dock = DockStyle.Top,
            Height = 26,
            Font = Theme.Title,
            TextAlign = ContentAlignment.TopCenter,
            Margin = new Padding(0, 0, 10, 0)
        };

        _messageLabel = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = Theme.Body,
            AutoEllipsis = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 0)
        };

        content.Controls.Add(_symbol, 0, 0);
        content.Controls.Add(_messageLabel, 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _confirm = new Button
        {
            Text = confirmText,
            Size = new Size(120, 38),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK,
            Margin = new Padding(8, 4, 0, 0)
        };

        _cancel = new Button
        {
            Text = "Cancel",
            Size = new Size(105, 38),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 4, 0, 0),
            Visible = confirmation
        };

        footer.Controls.Add(_confirm);

        if (confirmation)
            footer.Controls.Add(_cancel);

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);

        // Destructive confirmations must not be accepted
        // accidentally by pressing Enter.
        AcceptButton = confirmation && danger
            ? null
            : _confirm;

        CancelButton = confirmation
            ? _cancel
            : _confirm;

        ApplyTheme();

        Theme.Changed += HandleThemeChanged;
    }

    public static bool Confirm(
        IWin32Window? owner,
        string title,
        string message,
        bool danger = false,
        string confirmText = "Confirm")
    {
        using var dialog = new ThemedMessageDialog(
            title,
            message,
            true,
            danger,
            confirmText);

        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    public static void ShowAlert(
        IWin32Window? owner,
        string title,
        string message,
        bool danger = false)
    {
        using var dialog = new ThemedMessageDialog(
            title,
            message,
            false,
            danger,
            "OK");

        dialog.ShowDialog(owner);
    }

    private void HandleThemeChanged(
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

        _titleLabel.BackColor = c.Background;
        _titleLabel.ForeColor = c.Text;

        _messageLabel.BackColor = c.Background;
        _messageLabel.ForeColor = c.Text;

        // No colored square behind the symbol.
        _symbol.BackColor = c.Background;
        _symbol.ForeColor = _danger
            ? c.Danger
            : c.Accent;

        _confirm.BackColor = _danger
            ? c.Danger
            : c.Accent;

        _confirm.ForeColor = _danger
            ? Color.White
            : c.OnAccent;

        _confirm.FlatAppearance.BorderSize = 0;

        _cancel.BackColor = c.Surface;
        _cancel.ForeColor = c.Text;
        _cancel.FlatAppearance.BorderColor = c.Border;
        _cancel.FlatAppearance.BorderSize = 1;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Theme.Changed -= HandleThemeChanged;

        base.Dispose(disposing);
    }
}
