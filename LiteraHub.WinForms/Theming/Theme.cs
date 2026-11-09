
using System;
using System.Collections.Generic;
using System.Drawing;

namespace LiteraHub.WinForms.Theming;

public enum ThemeMode
{
    Light,
    Dark
}

public enum ThemePreset
{
    ClassicLight,
    MidnightLibrary,
    SepiaPaper,
    ForestScholar,
    SlateAndTeal,
    LavenderDusk,
    OceanBreeze,
    Rosewood,
    Graphite
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
    private static Color H(string hex) =>
        ColorTranslator.FromHtml(hex);

    private static Palette MakePalette(
        string background,
        string surface,
        string surfaceAlt,
        string border,
        string text,
        string muted,
        string accent,
        string onAccent,
        string accentSoft,
        bool dark)
    {
        return new Palette
        {
            Background = H(background),
            Surface = H(surface),
            SurfaceAlt = H(surfaceAlt),
            Border = H(border),
            Text = H(text),
            TextMuted = H(muted),
            Accent = H(accent),
            OnAccent = H(onAccent),
            AccentSoft = H(accentSoft),

            Danger = H(dark ? "#FF8F89" : "#B42318"),
            PillGoodBack = H(dark ? "#204B39" : "#DCFCE7"),
            PillGoodText = H(dark ? "#BDF3D2" : "#166534"),
            PillBadBack = H(dark ? "#5B2930" : "#FEE2E2"),
            PillBadText = H(dark ? "#FFD0D0" : "#991B1B")
        };
    }

    private static readonly IReadOnlyDictionary<ThemePreset, Palette>
        Palettes = new Dictionary<ThemePreset, Palette>
        {
            [ThemePreset.ClassicLight] = MakePalette(
                "#FAFAF8", "#FFFFFF", "#F0F2F5",
                "#D8DFE8", "#242B35", "#596678",
                "#536C9B", "#FFFFFF", "#E1E9F7", false),

            [ThemePreset.MidnightLibrary] = MakePalette(
                "#171C29", "#232B3B", "#2D374A",
                "#3A455A", "#E9EDF7", "#A9B6CC",
                "#8BA4F5", "#171C29", "#303D61", true),

            [ThemePreset.SepiaPaper] = MakePalette(
                "#F4E8D1", "#FAF0DD", "#E8D8BD",
                "#D0BFA5", "#483C30", "#73604C",
                "#8B6746", "#FFFFFF", "#E6D1B5", false),

            [ThemePreset.ForestScholar] = MakePalette(
                "#17231F", "#24352E", "#30463B",
                "#405B4C", "#E6F0E9", "#A9C5B4",
                "#83B89B", "#14241B", "#355746", true),

            [ThemePreset.SlateAndTeal] = MakePalette(
                "#181F28", "#263340", "#304453",
                "#405766", "#E8F0F2", "#ADC4CC",
                "#57C2BE", "#102628", "#244F55", true),

            [ThemePreset.LavenderDusk] = MakePalette(
                "#242034", "#342D48", "#413859",
                "#574B70", "#F0EAF9", "#C5B9DA",
                "#C2A9F5", "#241C36", "#4C3D70", true),

            [ThemePreset.OceanBreeze] = MakePalette(
                "#EAF4F8", "#F7FCFE", "#DCEEF5",
                "#B8D4E1", "#233E50", "#547083",
                "#287A9A", "#FFFFFF", "#CBE8F1", false),

            [ThemePreset.Rosewood] = MakePalette(
                "#2D2028", "#3D2C35", "#4C3642",
                "#664858", "#F8E9EF", "#D4B5C3",
                "#E7A3B8", "#301C27", "#614051", true),

            [ThemePreset.Graphite] = MakePalette(
                "#1C1E22", "#2A2D33", "#363A42",
                "#484E59", "#F0F2F5", "#B6C0CD",
                "#B5C0D0", "#20242B", "#414957", true)
        };

    public static ThemePreset Preset { get; private set; } =
        ThemePreset.ClassicLight;

    public static ThemeMode Mode { get; private set; } =
        ThemeMode.Light;

    public static Palette Colors { get; private set; } =
        Palettes[ThemePreset.ClassicLight];

    public static event EventHandler? Changed;

    public static IReadOnlyList<ThemePreset> AvailablePresets { get; } =
        Array.AsReadOnly(Enum.GetValues<ThemePreset>());

    public static string GetDisplayName(ThemePreset preset) =>
        preset switch
        {
            ThemePreset.ClassicLight => "Classic Light",
            ThemePreset.MidnightLibrary => "Midnight Library",
            ThemePreset.SepiaPaper => "Sepia Paper",
            ThemePreset.ForestScholar => "Forest Scholar",
            ThemePreset.SlateAndTeal => "Slate & Teal",
            ThemePreset.LavenderDusk => "Lavender Dusk",
            ThemePreset.OceanBreeze => "Ocean Breeze",
            ThemePreset.Rosewood => "Rosewood",
            ThemePreset.Graphite => "Graphite",
            _ => preset.ToString()
        };

    public static ThemeMode GetMode(ThemePreset preset) =>
        preset is ThemePreset.ClassicLight
            or ThemePreset.SepiaPaper
            or ThemePreset.OceanBreeze
            ? ThemeMode.Light
            : ThemeMode.Dark;

    public static void SetPreset(ThemePreset preset)
    {
        if (!Palettes.TryGetValue(preset, out var palette))
            throw new ArgumentOutOfRangeException(nameof(preset));

        if (Preset == preset)
            return;

        Preset = preset;
        Mode = GetMode(preset);
        Colors = palette;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    // Compatibility with existing Light/Dark callers.
    public static void SetMode(ThemeMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode));

        SetPreset(mode == ThemeMode.Dark
            ? ThemePreset.MidnightLibrary
            : ThemePreset.ClassicLight);
    }

    public static Font Caption { get; } =
        new("Segoe UI", 9f);

    public static Font Body { get; } =
        new("Segoe UI", 10f);

    public static Font BodyStrong { get; } =
        new("Segoe UI", 10f, FontStyle.Bold);

    public static Font Title { get; } =
        new("Segoe UI", 14f, FontStyle.Bold);

    public static Font Display { get; } =
        new("Segoe UI", 20f, FontStyle.Bold);
}
