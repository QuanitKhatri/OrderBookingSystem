namespace Distribution.WinForms.Theme;

public static class Icons
{
    private static readonly Lazy<string> ResolvedFamily = new(ResolveFontFamily);

    public static string FontFamily => ResolvedFamily.Value;

    private static readonly Dictionary<float, Font> FontCache = new();

    /// <summary>Returns a shared, cached icon-glyph Font for the given size. Never dispose the result — it's reused across every paint call.</summary>
    public static Font GetFont(float size)
    {
        if (!FontCache.TryGetValue(size, out var font))
        {
            font = new Font(FontFamily, size);
            FontCache[size] = font;
        }
        return font;
    }

    private static string ResolveFontFamily()
    {
        using var installed = new System.Drawing.Text.InstalledFontCollection();
        var names = installed.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (names.Contains("Segoe Fluent Icons")) return "Segoe Fluent Icons";
        if (names.Contains("Segoe MDL2 Assets")) return "Segoe MDL2 Assets";
        return "Segoe UI";
    }

    public const string Save = "";
    public const string Delete = "";
    public const string Cancel = "";
    public const string Refresh = "";
    public const string Search = "";
    public const string ChevronUp = "";
    public const string ChevronDown = "";
    public const string Upload = "";
    public const string Download = "";
    public const string Edit = "";
    public const string Filter = "";
    public const string Calendar = "";
    public const string Tag = "";
    public const string Shop = "";
    public const string Page = "";
    public const string Library = "";
    public const string Sort = "";
    public const string Add = "";
    public const string Contact = "";
    public const string Package = "";
    public const string Scan = "";
    public const string ViewAll = "";
    public const string Car = "";
    public const string Up = "";
    public const string PaymentCard = "";
    public const string Contact2 = "";
    public const string Admin = "";
    public const string Home = "";
    public const string ReportDocument = "";
    public const string Phone = "";
    public const string MapPin = "";
    public const string Mail = "";
    public const string Lock = "";
    public const string Eye = "";
    public const string EyeOff = "";
    public const string Import = "";
}
