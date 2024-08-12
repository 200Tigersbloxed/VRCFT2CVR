#define NOIMAGE
using System.Reflection;
using MelonLoader;

namespace VRCFT2CVR;

internal class OptionalUI
{
    private const string QUICKMENUAPI_NAME = "BTKUILib.QuickMenuAPI";
    private const string PAGE_TYPE_NAME = "BTKUILib.UIObjects.Page";
    private const string CATEGORY_TYPE_NAME = "BTKUILib.UIObjects.Category";
    private const string TOGGLEBUTTON_TYPE_NAME = "BTKUILib.UIObjects.Components.ToggleButton";
    
    private static object? rootPage;
    private static object? rootCategory;
    
    internal OptionalUI()
    {
        Type? quickMenuAPIType = null;
        Type? pageType = null;
        Type? categoryType = null;
        Type? toggleType = null;
        foreach (MelonAssembly melonAssembly in MelonAssembly.LoadedAssemblies)
        {
            quickMenuAPIType ??= melonAssembly.Assembly.GetType(QUICKMENUAPI_NAME);
            pageType ??= melonAssembly.Assembly.GetType(PAGE_TYPE_NAME);
            categoryType ??= melonAssembly.Assembly.GetType(CATEGORY_TYPE_NAME);
            toggleType ??= melonAssembly.Assembly.GetType(TOGGLEBUTTON_TYPE_NAME);
        }
        if (quickMenuAPIType == null ||pageType == null || categoryType == null || toggleType == null)
        {
            MelonLogger.Warning(
                "BTKUI was not detected! You must set settings manually through the MelonPreferences file.");
            return;
        }
#if !NOIMAGE
        // Prepare the icon
        quickMenuAPIType.GetMethod("PrepareIcon")!.Invoke(null, new object[3]
        {
            MainMod.MOD_NAME,
            "VRCFTIcon",
            Assembly.GetExecutingAssembly().GetManifestResourceStream("VRCFT2CVR.FaceTrackingIcon3.png")
        });
        // Create the BTKUI Page
        rootPage = Activator.CreateInstance(pageType, MainMod.MOD_NAME, MainMod.MOD_NAME + " Settings", true,
            "VRCFTIcon", null, false);
        pageType.GetProperty("MenuTitle")!.SetValue(rootPage, MainMod.MOD_NAME + "Settings");
        pageType.GetProperty("MenuSubtitle")!.SetValue(rootPage, "Edit Settings for " + MainMod.MOD_NAME);
#else
        rootPage = quickMenuAPIType.GetProperty("MiscTabPage")!.GetValue(null);
#endif
        // Add the Category
        rootCategory =
            pageType.GetMethod("AddCategory", new Type[1] {typeof(string)})!.Invoke(rootPage,
                new object[1] {MainMod.MOD_NAME + " Settings"});
        // Toggles
        CreateToggle(Config.integratedTrackingSupport, categoryType, toggleType,
            new object[3]
            {
                Config.integratedTrackingSupport.DisplayName, Config.integratedTrackingSupport.Description,
                Config.integratedTrackingSupport.Value
            });
        CreateToggle(Config.useBinaryParameters, categoryType, toggleType,
            new object[3]
            {
                Config.useBinaryParameters.DisplayName, Config.useBinaryParameters.Description,
                Config.useBinaryParameters.Value
            });
    }

    private static void CreateToggle(MelonPreferences_Entry<bool> p, Type categoryType, Type toggleType, object[] pa)
    {
        object toggleObject = categoryType.GetMethod("AddToggle")!.Invoke(rootCategory, pa);
        FieldInfo fieldInfo = toggleType.GetField("OnValueUpdated");
        Action<bool> toggleAction = (Action<bool>) fieldInfo.GetValue(toggleObject);
        toggleAction += b => ToggleValueUpdated(p, b);
        fieldInfo.SetValue(toggleObject, toggleAction);
    }
    
    private static void ToggleValueUpdated(MelonPreferences_Entry<bool> p, bool b)
    {
        p.Value = b;
        Config.Save();
    }
}