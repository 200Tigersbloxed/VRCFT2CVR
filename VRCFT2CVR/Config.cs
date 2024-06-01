using MelonLoader;

namespace VRCFT2CVR;

internal static class Config
{
    private const int CONFIG_VERSION = 1;

    public static bool IntegratedTrackingSupport => integratedTrackingSupport.Value;
    public static bool UseBinaryParameters => useBinaryParameters.Value;
    
    internal static MelonPreferences_Category preferencesCategory = MelonPreferences.CreateCategory(MainMod.MOD_NAME + " Settings");
    internal static MelonPreferences_Entry<bool> integratedTrackingSupport;
    internal static MelonPreferences_Entry<bool> useBinaryParameters;
    private static MelonPreferences_Entry<int> configVersion;

    static Config()
    {
        integratedTrackingSupport = preferencesCategory.CreateEntry("integratedTrackingSupport", false,
            "Integrated Tracking Support", "(Experimental) Implements support for CVR's built-in Face Tracking");
        useBinaryParameters = preferencesCategory.CreateEntry("useBinaryParameters", true, "Use Binary Parameters",
            "Registers VRCFT's Binary Parameters");
        configVersion = preferencesCategory.CreateEntry("ConfigVersion", CONFIG_VERSION, description: "DO NOT CHANGE!");
        if (configVersion.Value == CONFIG_VERSION) return;
        configVersion.Value = CONFIG_VERSION;
        Save();
    }

    internal static void Save() => preferencesCategory.SaveToFile(false);
}