using MelonLoader;

namespace VRCFT2CVR;

internal static class Config
{
    private const int CONFIG_VERSION = 1;

    public static bool IntegratedTrackingSupport => integratedTrackingSupport.Value;
    public static bool UseBinaryParameters => useBinaryParameters.Value;

    public static bool EnabledInDesktop => enabledInDesktop.Value;
    
    internal static MelonPreferences_Category preferencesCategory = MelonPreferences.CreateCategory(MainMod.MOD_NAME + " Settings");
    internal static MelonPreferences_Entry<bool> integratedTrackingSupport;
    internal static MelonPreferences_Entry<bool> useBinaryParameters;
    internal static MelonPreferences_Entry<bool> enabledInDesktop;
    private static MelonPreferences_Entry<int> configVersion;
    internal static MelonPreferences_Entry<Dictionary<string, string>> facialTrackingSettings;
    
    static Config()
    {
        integratedTrackingSupport = preferencesCategory.CreateEntry("integratedTrackingSupport", false,
            "Integrated Tracking Support", "(Experimental) Implements support for CVR's built-in Face Tracking");
        useBinaryParameters = preferencesCategory.CreateEntry("useBinaryParameters", true, "Use Binary Parameters",
            "Registers VRCFT's Binary Parameters");
        enabledInDesktop = preferencesCategory.CreateEntry("enabledInDesktop", true, "Enabled in Desktop",
            "Loads VRCFT in desktop mode");
        facialTrackingSettings = preferencesCategory.CreateEntry("facialTrackingSettings",
            new Dictionary<string, string>(), "Facial Tracking Settings", "All of the VRCFT Facial Tracking Settings");
        configVersion = preferencesCategory.CreateEntry("ConfigVersion", CONFIG_VERSION, description: "DO NOT CHANGE!");
        if (configVersion.Value == CONFIG_VERSION) return;
        configVersion.Value = CONFIG_VERSION;
        Save();
    }

    internal static void Save() => preferencesCategory.SaveToFile(false);
}