using System.Drawing.Drawing2D;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.WinForms.Controls;
using LiteraHub.WinForms.Forms;
using LiteraHub.WinForms.Theming;
using FontAwesome.Sharp;

namespace LiteraHub.WinForms.Pages;

public sealed class RoleDetailsPage : ThemedUserControl
{
    private readonly IRoleService _service;
    private readonly IPermissionService _permissions;
    private readonly HashSet<RolePermission> _grantable;
    private readonly bool _canManageTarget;
    private readonly RoleDefinition? _role;
    private readonly HashSet<RolePermission> _original;
    private readonly Dictionary<RolePermission, CheckBox> _checks = new();
    private readonly List<(ThemedSectionCard Panel, Label Heading, CheckBox[] Checks)> _groups = new();
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
    private readonly DetailsPageHeader _header = new() { Dock = DockStyle.Fill };
    private readonly DetailsActionFooter _footer = new() { Dock = DockStyle.Fill, UseStudentSizing = true };
    private readonly ScrollHost _scroll = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Panel _content = new();
    private readonly ThemedSectionCard _info;
    private readonly ThemedSectionCard _descriptionCard;
    private readonly Label _infoHeading = new() { Text = "Role Information" };
    private readonly Label _nameLabel = new() { Text = "Role name" };
    private readonly Label _descriptionLabel = new() { Text = "Description" };
    private readonly Label _type = new();
    private readonly Label _notice = new() { AutoEllipsis = true };
    private readonly Label _permissionHeading = new() { Text = "Access Permissions" };
    private readonly Label _count = new();
    private readonly ThemedTextField _name = new() { MaxLength = 50 };
    private readonly ThemedTextField _description = new() { MaxLength = 500, Multiline = true };
    private readonly Label _viewName = ViewField();
    private readonly Label _viewDescription = ViewField();
    private readonly IconButton _all = SelectionButton("Select All");
    private readonly IconButton _clear = SelectionButton("Clear");
    private readonly IconButton _cancel = ActionButton("Cancel");
    private readonly IconButton _primary = ActionButton("Save Role");
    private readonly IconButton _archive = ActionButton("Archive Role");
    private readonly IconButton _delete = ActionButton("Delete Role");
    private readonly CatalogValidationController _validation = new();
    private bool _editing, _busy, _setting, _arranging;
    public event EventHandler? BackRequested;

    public RoleDetailsPage(IRoleService service, RoleDefinition? role, IEnumerable<RolePermission> permissions, IPermissionService authorization, string actorRole)
    {
        _service = service; _role = role; _original = permissions.ToHashSet();
        _permissions = authorization;
        _grantable = authorization.GrantedRolePermissions().ToHashSet();
        bool admin = string.Equals(actorRole, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
        _canManageTarget = role is null || admin || (!role.IsSystem &&
            !string.Equals(role.Name, actorRole, StringComparison.OrdinalIgnoreCase) && _original.IsSubsetOf(_grantable));
        Dock = DockStyle.Fill; Padding = new Padding(Space.L, Space.L, 0, Space.L);
        _header.AlignBackToRightEdge = true;
        _root.ColumnCount = 2;
        _root.Margin = Padding.Empty; _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Space.L));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        _root.Controls.Add(_header, 0, 0); _root.Controls.Add(_scroll, 0, 1); _root.Controls.Add(_footer, 0, 2);
        _root.SetColumnSpan(_scroll, 2);
        Controls.Add(_root);
        _scroll.ReservedRightGutter = Space.L;
        _scroll.SetContent(_content);
        _infoHeading.TextAlign = ContentAlignment.MiddleLeft;
        _descriptionLabel.TextAlign = ContentAlignment.MiddleLeft;
        _info = new ThemedSectionCard(_infoHeading, IconChar.LayerGroup);
        _descriptionCard = new ThemedSectionCard(_descriptionLabel, IconChar.FileAlt);
        _viewDescription.TextAlign = ContentAlignment.TopLeft;
        _content.Controls.AddRange(new Control[] { _info, _descriptionCard, _permissionHeading, _all, _clear, _count });
        _info.AddFields(_nameLabel, _name, _viewName, _type, _notice);
        _descriptionCard.AddFields(_description, _viewDescription);
        _footer.Controls.AddRange(new Control[] { _cancel, _primary, _archive, _delete });
        _footer.Configure(_delete, _archive, _primary, _cancel, () => _role is null);
        _primary.Paint += (_, e) => PaintActionIcon(e, _primary, 0);
        _archive.Paint += (_, e) => PaintActionIcon(e, _archive, 1);
        _delete.Paint += (_, e) => PaintActionIcon(e, _delete, 2);
        foreach (var group in new[] { ("Books", "Books"), ("Authors", "Authors"), ("Categories", "Categories"), ("Students", "Students"), ("Users", "Users"), ("Roles", "Roles & Permissions"), ("BorrowReturn", "Borrow & Return"), ("Reports", "Reports"), ("ActivityLog", "Activity Log") })
        {
            var heading = new Label { Text = group.Item2, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft };
            var panel = new ThemedSectionCard(heading, IconChar.Lock); var boxes = new List<CheckBox>();
            foreach (var permission in Enum.GetValues<RolePermission>().Where(p => p.ToString().StartsWith(group.Item1, StringComparison.Ordinal)))
            {
                var suffix = permission.ToString()[group.Item1.Length..];
                var box = new PermissionCheckBox { Text = suffix == "ArchiveRestore" ? "Archive / Restore" : suffix, AutoSize = false, AccessibleName = group.Item2 + " " + suffix };
                _checks.Add(permission, box); boxes.Add(box); panel.Controls.Add(box);
                box.CheckedChanged += (_, _) => PermissionChanged(permission, group.Item1);
            }
            _groups.Add((panel, heading, boxes.ToArray())); _content.Controls.Add(panel);
        }
        _validation.Register(_name, () => !string.IsNullOrWhiteSpace(_name.Text), "Role name is required.");
        _name.TextChanged += (_, _) => RefreshCaption();
        _all.Click += (_, _) => SetAll(true); _clear.Click += (_, _) => SetAll(false);
        _header.BackRequested += (_, _) => { if (!_busy) BackRequested?.Invoke(this, EventArgs.Empty); };
        _cancel.Click += (_, _) => { if (_busy) return; if (_role is null) BackRequested?.Invoke(this, EventArgs.Empty); else { Restore(); SetEditing(false); } };
        _primary.Click += async (_, _) => { if (_busy) return; if (!_editing) { if (_role is not null) SetEditing(true); } else await SaveAsync(); };
        _archive.Click += async (_, _) => await ChangeStatusAsync();
        _delete.Click += async (_, _) => await DeleteAsync();
        _content.Resize += (_, _) => Arrange();
        _scroll.Resize += (_, _) => Arrange();
        Disposed += (_, _) => _validation.Dispose();
        Restore(); SetEditing(role is null); ApplyTheme(); Arrange();
    }

    private static CenteredIconButton SelectionButton(string text) => new() { Text = text, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = Padding.Empty, IconSize = 16 };

    private static IconButton ActionButton(string text) => new() { Text = text, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = Padding.Empty, TextImageRelation = TextImageRelation.ImageBeforeText, IconSize = 16, ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.MiddleCenter };
    private static Label ViewField()
    {
        var label = new Label { AutoEllipsis = true, UseMnemonic = false, Padding = new Padding(12, 4, 12, 4), TextAlign = ContentAlignment.MiddleLeft };
        label.Paint += (_, e) => { using var pen = new Pen(Theme.Colors.Border); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, label.Width - 1), Math.Max(0, label.Height - 1)); };
        return label;
    }
    private void Restore()
    {
        _name.Text = _role?.Name ?? ""; _description.Text = _role?.Description ?? "";
        _viewName.Text = _name.Text; _viewDescription.Text = string.IsNullOrWhiteSpace(_description.Text) ? "—" : _description.Text;
        _setting = true;
        try { foreach (var pair in _checks) pair.Value.Checked = _original.Contains(pair.Key); }
        finally { _setting = false; }
        _validation.Clear(); RefreshCaption(); UpdateCount();
    }
    private void SetEditing(bool editing)
    {
        _editing = editing;
        bool editPermissions = editing && _role?.IsSystem != true;
        _name.Visible = editPermissions; _description.Visible = editing;
        _viewName.Visible = !editPermissions; _viewDescription.Visible = !editing;
        foreach (var pair in _checks) { pair.Value.Enabled = editPermissions && !_busy && _grantable.Contains(pair.Key); pair.Value.TabStop = pair.Value.Enabled; }
        _all.Visible = _clear.Visible = editPermissions;
        _primary.Visible = _canManageTarget && _permissions.Can(_role is null ? RolePermission.RolesAdd : RolePermission.RolesEdit);
        _archive.Visible = _canManageTarget && _role is { IsSystem: false } && _permissions.Can(RolePermission.RolesArchiveRestore);
        _delete.Visible = _canManageTarget && _role is { IsSystem: false } && _permissions.Can(RolePermission.RolesDelete);
        _cancel.Visible = editing;
        _primary.Text = _archive.Text = _delete.Text = "";
        _primary.AccessibleName = editing ? "Save Role" : "Edit Role";
        _archive.AccessibleName = _role?.IsActive == false ? "Restore Role" : "Archive Role";
        _delete.AccessibleName = "Delete Role";
        _header.HeadingText = _role is null ? "Add Role" : editing ? "Edit Role" : "Role Details";
        _header.SubtitleText = _role is null ? "Create a custom role and select its permissions." : $"Role ID: {_role.RoleId}";
        _type.Text = _role is null ? "Custom · Active on creation" : $"{(_role.IsSystem ? "System" : "Custom")} · {(_role.IsActive ? "Active" : "Archived")}";
        _notice.Text = _role?.IsSystem == true ? "Only the description of a built-in role can be edited." : _canManageTarget ? "Only permissions you hold may be granted." : "This role is protected or has permissions beyond your own.";
        _footer.ArrangeActions(); ApplyTheme();
    }
    private void PermissionChanged(RolePermission permission, string prefix)
    {
        if (_setting || !_editing || _role?.IsSystem == true || !_grantable.Contains(permission)) return;
        _setting = true;
        try
        {
            var key = permission.ToString();
            var view = _checks.First(p => p.Key.ToString() == prefix + "View").Value;
            if (key == prefix + "View")
            {
                if (!view.Checked)
                {
                    foreach (var pair in _checks.Where(p => p.Key.ToString().StartsWith(prefix, StringComparison.Ordinal)))
                        if (pair.Key != permission) pair.Value.Checked = false;
                }
            }
            else if (_checks[permission].Checked)
            {
                view.Checked = true;
            }
        }
        finally { _setting = false; }
        UpdateCount();
    }
    private void SetAll(bool value)
    {
        if (!_editing || _busy || _role?.IsSystem == true) return;
        _setting = true;
        try { foreach (var pair in _checks) pair.Value.Checked = value && _grantable.Contains(pair.Key); }
        finally { _setting = false; }
        UpdateCount();
    }
    private void UpdateCount() => _count.Text = $"{_checks.Values.Count(b => b.Checked)} of {_checks.Count} permissions selected";
    private void RefreshCaption() { _nameLabel.Text = _name.HasValidationError ? "Role name • Required" : "Role name"; _nameLabel.ForeColor = _name.HasValidationError ? Theme.Colors.Danger : Theme.Colors.Text; }
    private void SetBusy(bool busy)
    {
        _busy = busy; _header.BackEnabled = !busy;
        foreach (var button in new[] { _primary, _archive, _delete, _cancel }) button.Enabled = !busy;
        _name.Enabled = _description.Enabled = !busy; _all.Enabled = _clear.Enabled = !busy;
        foreach (var pair in _checks) pair.Value.Enabled = _editing && !busy && _role?.IsSystem != true && _grantable.Contains(pair.Key);
    }
    private async Task SaveAsync()
    {
        if (_busy || !_editing) return;
        var errors = _validation.Validate(); RefreshCaption();
        if (errors.Count > 0) { _validation.ShowSummary(FindForm(), "this role"); _name.FocusInput(); return; }
        var input = new RoleInput(_name.Text, _description.Text, _checks.Where(p => p.Value.Checked).Select(p => p.Key).ToArray());
        await RunMutationAsync(() => _role is null ? _service.CreateAsync(input) : _service.UpdateAsync(_role.RoleId, input), _role is null ? "Role added successfully." : "Role updated successfully.");
    }
    private async Task ChangeStatusAsync()
    {
        if (_busy || _role is null || _role.IsSystem) return;
        bool desired = !_role.IsActive; string action = desired ? "Restore" : "Archive";
        if (!ThemedMessageDialog.Confirm(FindForm(), action + " Role", $"{action} '{_role.Name}'? Unsaved changes will be discarded.", !desired, action)) return;
        await RunMutationAsync(() => _service.SetActiveAsync(_role.RoleId, desired), desired ? "Role restored successfully." : "Role archived successfully.");
    }
    private async Task DeleteAsync()
    {
        if (_busy || _role is null || _role.IsSystem) return;
        if (_role.IsActive) { ThemedMessageDialog.ShowAlert(FindForm(), "Delete Role", "Archive this role before permanently deleting it.", true); return; }
        if (!ThemedMessageDialog.Confirm(FindForm(), "Delete Role", $"Permanently delete '{_role.Name}'? This cannot be undone.", true, "Delete")) return;
        await RunMutationAsync(() => _service.DeleteAsync(_role.RoleId), "Role deleted successfully.");
    }
    private async Task RunMutationAsync(Func<Task<ServiceResult>> operation, string success)
    {
        SetBusy(true); bool completed = false;
        try
        {
            var result = await operation(); if (IsDisposed || Disposing) return;
            if (!result.Success) { ThemedMessageDialog.ShowAlert(FindForm(), "Roles & Permissions", result.Error ?? "Unable to save changes.", true); return; }
            CatalogActionFeedback.Notify(this, success); completed = true;
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (!IsDisposed) ThemedMessageDialog.ShowAlert(FindForm(), "Roles & Permissions", "Unable to complete this action.", true); }
        finally { if (!IsDisposed) SetBusy(false); }
        if (completed && !IsDisposed) BackRequested?.Invoke(this, EventArgs.Empty);
    }
    private void Arrange()
    {
        if (_arranging || _content.Width < 1) return;
        _arranging = true;
        try
        {
            int w = Math.Max(1, _scroll.ClientSize.Width - Space.L);
            var layout = new ResponsiveSectionLayout(w);
            int gap = layout.Gap, pad = layout.Padding;
            bool stacked = w < 650;
            int first = stacked ? w : (w - gap) / 2;
            int second = stacked ? w : w - gap - first;
            int topHeight = pad * 2 + 164;
            _info.SetBounds(0, 0, first, topHeight);
            _descriptionCard.SetBounds(stacked ? 0 : first + gap, stacked ? topHeight + gap : 0, second, topHeight);
            int fy = _info.ArrangeHeading(first, pad);
            int fw = Math.Max(1, first - 2 * pad);
            _nameLabel.SetBounds(pad, fy, fw, 22);
            _name.SetBounds(pad, fy + 24, fw, 40); _viewName.Bounds = _name.Bounds;
            _type.SetBounds(pad, fy + 68, fw, 28);
            _notice.SetBounds(pad, fy + 98, fw, 34);
            int dy = _descriptionCard.ArrangeHeading(second, pad);
            _description.SetBounds(pad, dy, Math.Max(1, second - 2 * pad), topHeight - dy - pad);
            _viewDescription.Bounds = _description.Bounds;
            int toolbarY = (stacked ? topHeight * 2 + gap : topHeight) + gap;
            _permissionHeading.SetBounds(0, toolbarY, Math.Max(1, w - 254), 48);
            _all.SetBounds(w - 248, toolbarY, 128, 48); _clear.SetBounds(w - 112, toolbarY, 112, 48);
            _count.SetBounds(0, toolbarY + 48, w, 28);
            int columns = w >= 1200 ? 3 : w >= 660 ? 2 : 1;
            int usable = w - (columns - 1) * gap;
            int y = toolbarY + 80;
            for (int i = 0; i < _groups.Count; i += columns)
            {
                int height = pad * 2 + 32;
                foreach (var group in _groups.Skip(i).Take(columns)) height = Math.Max(height, pad * 2 + 32 + ((group.Checks.Length + 1) / 2) * 34);
                for (int j = 0; j < columns && i + j < _groups.Count; j++)
                {
                    int gx = j * gap + j * usable / columns;
                    int gw = (j + 1) * usable / columns - j * usable / columns;
                    var group = _groups[i + j]; group.Panel.SetBounds(gx, y, gw, height);
                    int gy = group.Panel.ArrangeHeading(gw, pad);
                    int cw = Math.Max(1, (gw - 2 * pad) / 2);
                    for (int k = 0; k < group.Checks.Length; k++) group.Checks[k].SetBounds(pad + (k % 2) * cw, gy + k / 2 * 34, cw, 32);
                }
                y += height + gap;
            }
            _content.Height = Math.Max(1, y - gap + 2); _scroll.RefreshMetrics();
        }
        finally { _arranging = false; }
    }
    protected override void ApplyTheme()
    {
        base.ApplyTheme(); var c = Theme.Colors;
        _root.BackColor = _footer.BackColor = c.Background; _content.BackColor = c.Background; _info.BackColor = c.Surface;
        foreach (var label in new[] { _infoHeading, _nameLabel, _descriptionLabel, _type, _notice, _permissionHeading, _count }) { label.ForeColor = c.Text; label.Font = Theme.Body; }
        _infoHeading.Font = _descriptionLabel.Font = _permissionHeading.Font = Theme.BodyStrong; _notice.ForeColor = _count.ForeColor = c.TextMuted;
        foreach (var label in new[] { _viewName, _viewDescription }) { label.BackColor = c.SurfaceAlt; label.ForeColor = c.Text; label.Font = Theme.BodyStrong; label.Invalidate(); }
        foreach (var group in _groups) { group.Panel.BackColor = c.Surface; group.Heading.ForeColor = c.Text; group.Heading.Font = Theme.BodyStrong; foreach (var box in group.Checks) { box.BackColor = c.Surface; box.ForeColor = c.Text; box.Font = Theme.Body; box.Invalidate(); } }
        foreach (var button in new Button[] { _primary, _archive, _delete, _cancel, _all, _clear })
        {
            button.Font = Theme.BodyStrong; button.BackColor = c.Surface; button.ForeColor = c.Text;
            button.FlatAppearance.BorderSize = 1; button.FlatAppearance.BorderColor = c.Border;
            button.FlatAppearance.MouseOverBackColor = c.SurfaceAlt; button.FlatAppearance.MouseDownBackColor = c.AccentSoft;
        }
        _all.IconChar = IconChar.CheckSquare; _clear.IconChar = IconChar.Eraser;
        _all.IconColor = _clear.IconColor = c.Accent;
        _cancel.BackColor = c.SurfaceAlt; _cancel.FlatAppearance.MouseOverBackColor = c.AccentSoft;
        _primary.BackColor = Color.FromArgb(34, 139, 86); _primary.ForeColor = Color.White;
        _primary.FlatAppearance.BorderSize = 0;
        _primary.FlatAppearance.MouseOverBackColor = ControlPaint.Light(_primary.BackColor, 0.08f);
        _primary.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(_primary.BackColor, 0.08f);
        _delete.BackColor = Color.FromArgb(255, 143, 137); _delete.ForeColor = Color.White;
        _delete.FlatAppearance.BorderSize = 0;
        _delete.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(_delete.BackColor, 0.12f);
        _delete.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(_delete.BackColor, 0.23f);
        _primary.Invalidate(); _archive.Invalidate(); _delete.Invalidate();
        _header.ApplyTheme(); RefreshCaption();
    }
    private void PaintActionIcon(PaintEventArgs e, Button button, int kind)
    {
        string label = kind == 0 ? (_editing ? "Save Role" : "Edit Role")
            : kind == 2 ? "Delete Role"
            : (_role?.IsActive == false ? "Restore Role" : "Archive Role");
        if (button.Width < 130)
            label = kind == 0 ? (_editing ? "Save" : "Edit")
                : kind == 2 ? "Delete" : (_role?.IsActive == false ? "Restore" : "Archive");

        IconButtonContent.DrawAction(e, button, kind, label, _role?.IsActive == false);
    }

    // Native checkbox semantics and keyboard interaction, with the application's own colors.
    private sealed class PermissionCheckBox : CheckBox
    {
        public PermissionCheckBox() { SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true); CheckedChanged += (_, _) => Invalidate(); EnabledChanged += (_, _) => Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var c = Theme.Colors; e.Graphics.Clear(BackColor);
            var rect = new Rectangle(2, Math.Max(0, (Height - 18) / 2), 18, 18);
            using var fill = new SolidBrush(Checked ? c.Accent : c.Surface);
            using var border = new Pen(Checked ? c.Accent : c.Border);
            e.Graphics.FillRectangle(fill, rect); e.Graphics.DrawRectangle(border, rect);
            if (Checked) { using var pen = new Pen(c.OnAccent, 2); e.Graphics.DrawLines(pen, new[] { new Point(rect.X + 4, rect.Y + 9), new Point(rect.X + 8, rect.Y + 13), new Point(rect.X + 15, rect.Y + 5) }); }
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(28, 0, Math.Max(0, Width - 30), Height), c.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            if (Focused && Enabled && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }
}
