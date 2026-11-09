namespace LiteraHub.WinForms.Theming;

public enum ThemeMode
{
    Light,
    Dark
}

public sealed class Palette
{
    public required Color Background { get; init; }
    public required Color Surface { get; init; }
    public required Color SurfaceAlt { get; init; }
    public required Color Border { get; init; }
    public required Color Text { get; init; }
    public required Color TextMuted { get; init; }
    public required Color Accent { get; init; }
    public required Color OnAccent { get; init; }
    public required Color AccentSoft { get; init; }
    public required Color Danger { get; init; }
    public required Color PillGoodBack { get; init; }
    public required Color PillGoodText { get; init; }
    public required Color PillBadBack { get; init; }
    public required Color PillBadText { get; init; }
}

// Spacing scale in design pixels at 96 DPI.
public static class Space
{
    public const int XS = 4;
    public const int S = 8;
    public const int M = 12;
    public const int L = 16;
    public const int XL = 24;
}

public static class Radius
{
    public const int Small = 6;
    public const int Medium = 10;
}

public static class Theme
{
    private static Color H(string hex) => ColorTranslator.FromHtml(hex);

    private static readonly Palette Light = new()
    {
        Background = H("#FAF8F4"),
        Surface = H("#FFFFFF"),
        SurfaceAlt = H("#F1EEE8"),
        Border = H("#E4E0D8"),
        Text = H("#1F2328"),
        TextMuted = H("#5F6670"),
        Accent = H("#B45309"),
        OnAccent = H("#FFFFFF"),
        AccentSoft = H("#FBEBD7"),
        Danger = H("#B42318"),
        PillGoodBack = H("#DCFCE7"),
        PillGoodText = H("#166534"),
        PillBadBack = H("#FEE2E2"),
        PillBadText = H("#991B1B")
    };

    private static readonly Palette Dark = new()
    {
        Background = H("#16181D"),
        Surface = H("#1E2127"),
        SurfaceAlt = H("#2A2E36"),
        Border = H("#353A44"),
        Text = H("#E8E6E1"),
        TextMuted = H("#A0A6B0"),
        Accent = H("#F59E0B"),
        OnAccent = H("#1A1A1A"),
        AccentSoft = H("#3A2E17"),
        Danger = H("#F97066"),
        PillGoodBack = H("#14532D"),
        PillGoodText = H("#BBF7D0"),
        PillBadBack = H("#7F1D1D"),
        PillBadText = H("#FECACA")
    };

    public static ThemeMode Mode { get; private set; } = ThemeMode.Light;

    public static Palette Colors { get; private set; } = Light;

    // Raised on the UI thread after the mode changes.
    public static event EventHandler? Changed;

    public static void SetMode(ThemeMode mode)
    {
        if (mode == Mode) return;
        Mode = mode;
        Colors = mode == ThemeMode.Dark ? Dark : Light;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    // Type scale: a small fixed set, shared and never disposed.
    public static Font Caption { get; } = new("Segoe UI", 9f);
    public static Font Body { get; } = new("Segoe UI", 10f);
    public static Font BodyStrong { get; } = new("Segoe UI", 10f, FontStyle.Bold);
    public static Font Title { get; } = new("Segoe UI", 14f, FontStyle.Bold);
    public static Font Display { get; } = new("Segoe UI", 20f, FontStyle.Bold);
}