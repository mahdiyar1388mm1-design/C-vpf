using System.Windows;
using System.Windows.Media;
namespace NexusControlCenter.Services;
public sealed class SettingsService
{
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexusControlCenter", "settings.json");
    public bool IsLightTheme { get; private set; }
    private bool _notificationsEnabled = true;
    public bool NotificationsEnabled { get => _notificationsEnabled; set { _notificationsEnabled = value; Save(); } }
    public event EventHandler? ThemeChanged;
    public SettingsService()
    {
        try { if (File.Exists(_settingsPath)) { using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(_settingsPath)); var root = document.RootElement; IsLightTheme = root.TryGetProperty("lightTheme", out var light) && light.GetBoolean(); if (root.TryGetProperty("notificationsEnabled", out var notifications)) _notificationsEnabled = notifications.GetBoolean(); } } catch { IsLightTheme = false; }
        ApplyTheme();
    }
    public void ToggleTheme() { IsLightTheme = !IsLightTheme; ApplyTheme(); Save(); }
    private void Save() { try { Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!); File.WriteAllText(_settingsPath, System.Text.Json.JsonSerializer.Serialize(new { lightTheme = IsLightTheme, notificationsEnabled = NotificationsEnabled })); } catch { } }
    private void ApplyTheme()
    {
        var r = Application.Current.Resources;
        var colors = IsLightTheme ? (Page: "#F2F5FA", Panel: "#E6FFFFFF", Alt: "#E6E8EDF5", Text: "#172033", Muted: "#66738B", Border: "#D9E1ED") : (Page: "#0B1020", Panel: "#E5121A2D", Alt: "#E6172238", Text: "#F2F5FC", Muted: "#8491AA", Border: "#26334B");
        r["PageColor"] = ColorConverter.ConvertFromString(colors.Page)!; r["PanelColor"] = ColorConverter.ConvertFromString(colors.Panel)!; r["PanelAltColor"] = ColorConverter.ConvertFromString(colors.Alt)!; r["TextColor"] = ColorConverter.ConvertFromString(colors.Text)!; r["MutedColor"] = ColorConverter.ConvertFromString(colors.Muted)!; r["BorderColor"] = ColorConverter.ConvertFromString(colors.Border)!;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
