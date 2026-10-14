namespace Distribution.WinForms.Theme;

public static class Theme
{
    public static readonly Color Sidebar = ColorTranslator.FromHtml("#172429");
    public static readonly Color SidebarGradientTop = ColorTranslator.FromHtml("#14262C");
    public static readonly Color SidebarGradientBottom = ColorTranslator.FromHtml("#0D1B20");
    public static readonly Color Primary = ColorTranslator.FromHtml("#0F5C63");
    public static readonly Color PrimaryDark = ColorTranslator.FromHtml("#0A4247");
    public static readonly Color Background = ColorTranslator.FromHtml("#F7F6F2");
    public static readonly Color Surface = ColorTranslator.FromHtml("#FFFFFF");
    public static readonly Color Border = ColorTranslator.FromHtml("#DEDDD6");
    public static readonly Color Text = ColorTranslator.FromHtml("#1C1F24");
    public static readonly Color Muted = ColorTranslator.FromHtml("#5A5F66");
    public static readonly Color Warning = ColorTranslator.FromHtml("#B45309");
    public static readonly Color Danger = ColorTranslator.FromHtml("#B42318");

    public static readonly Color HeadingNavy = ColorTranslator.FromHtml("#16233D");
    public static readonly Color IconBadgeBackground = ColorTranslator.FromHtml("#E3F6F2");
    public static readonly Color IconColor = ColorTranslator.FromHtml("#0F5C63");
    public static readonly Color InputBackground = ColorTranslator.FromHtml("#F4F6F8");
    public static readonly Color InputBorderFocus = ColorTranslator.FromHtml("#0F5C63");

    public static readonly Color PastelGreen = ColorTranslator.FromHtml("#D7F3E3");
    public static readonly Color PastelPink = ColorTranslator.FromHtml("#FBDCE0");
    public static readonly Color PastelBlue = ColorTranslator.FromHtml("#D9E8FB");
    public static readonly Color PastelOrange = ColorTranslator.FromHtml("#FBE4CF");
    public static readonly Color AccentGreen = ColorTranslator.FromHtml("#17824F");
    public static readonly Color AccentPink = ColorTranslator.FromHtml("#C23B55");
    public static readonly Color AccentBlue = ColorTranslator.FromHtml("#1C5FB0");
    public static readonly Color AccentOrange = ColorTranslator.FromHtml("#C2701C");
    public static readonly Color PastelPurple = ColorTranslator.FromHtml("#E6DEFB");
    public static readonly Color AccentPurple = ColorTranslator.FromHtml("#6B3FC2");

    public const string FontFamily = "Segoe UI";

    public static Font BaseFont { get; } = new(FontFamily, 10f);
    public static Font HeadingFont { get; } = new(FontFamily, 16f, FontStyle.Bold);
    public static Font KpiFont { get; } = new(FontFamily, 18f, FontStyle.Bold);
    public static Font MutedFont { get; } = new(FontFamily, 9f);
    public static Font SectionTitleFont { get; } = new(FontFamily, 12f, FontStyle.Bold);

    private static readonly Dictionary<float, Font> BoldFontCache = new();

    /// <summary>Returns a shared, cached bold Font for the given size. Never dispose the result — it's reused across every paint call.</summary>
    public static Font GetBoldFont(float size)
    {
        if (!BoldFontCache.TryGetValue(size, out var font))
        {
            font = new Font(FontFamily, size, FontStyle.Bold);
            BoldFontCache[size] = font;
        }
        return font;
    }

    /// <summary>The colour actually visible behind a control: walks up past transparent parents.</summary>
    public static Color EffectiveBackColor(Control? control)
    {
        while (control is not null && control.BackColor.A < 255)
        {
            control = control.Parent;
        }
        return control?.BackColor ?? Surface;
    }

    private static readonly Dictionary<Color, SolidBrush> BrushCache = new();

    /// <summary>Returns a shared, cached SolidBrush for the given color. Never dispose the result — it's reused across every paint call.</summary>
    public static SolidBrush GetBrush(Color color)
    {
        if (!BrushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidBrush(color);
            BrushCache[color] = brush;
        }
        return brush;
    }

    private static readonly Dictionary<(Color Color, float Width), Pen> PenCache = new();

    /// <summary>Returns a shared, cached Pen for the given color/width. Never dispose the result — it's reused across every paint call.</summary>
    public static Pen GetPen(Color color, float width = 1f)
    {
        var key = (color, width);
        if (!PenCache.TryGetValue(key, out var pen))
        {
            pen = new Pen(color, width);
            PenCache[key] = pen;
        }
        return pen;
    }
}
