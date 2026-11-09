using LiteraHub.Core.Interfaces;

namespace LiteraHub.WinForms.Forms;

// Placeholder shell for Step 3. Step 4 replaces it with the real shell (sidebar + swapping UserControls).
public sealed class ShellForm : Form
{
    private readonly IAuthService _auth;

    public bool LoggedOut { get; private set; }

    public ShellForm(ISessionContext session, IPermissionService permissions, IAuthService auth)
    {
        _auth = auth;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LiteraHub";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(24);

        var current = session.Current;
        var who = current is null ? "Not signed in" : $"Signed in as {current.Username} ({current.RoleName})";
        var granted = string.Join(Environment.NewLine, permissions.GrantedPermissions());

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

        layout.Controls.Add(new Label
        {
            Text = who,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        });
        layout.Controls.Add(new Label { Text = "Permissions for this role:", AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
        layout.Controls.Add(new Label { Text = granted, AutoSize = true, Margin = new Padding(0, 0, 0, 16) });

        var logout = new Button { Text = "Log out", AutoSize = true, Height = 36 };
        logout.Click += (_, _) =>
        {
            _auth.Logout();
            LoggedOut = true;
            Close();
        };
        layout.Controls.Add(logout);

        Controls.Add(layout);
        ResumeLayout(performLayout: true);
    }
}