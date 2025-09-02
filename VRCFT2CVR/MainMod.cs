using System.Reflection;
using ABI_RC.API;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.FaceTracking;
using MelonLoader;
using UnityEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFT2CVR;
using Object = UnityEngine.Object;
using Utils = VRCFaceTracking.Core.Utils;

[assembly: MelonInfo(typeof(MainMod), MainMod.MOD_NAME, "1.4.0", "200Tigersbloxed")]
[assembly: MelonGame("ChilloutVR", "ChilloutVR")]
[assembly: MelonColor(255, 144, 242, 35)]
[assembly: MelonAuthorColor(255, 252, 100, 0)]
[assembly: MelonOptionalDependencies("BTKUILib")]
[assembly: HarmonyDontPatchAll]

namespace VRCFT2CVR;

public class MainMod : MelonMod
{
    internal const string MOD_NAME = "VRCFT2CVR";

    private static readonly ConvertedModule GlobalModule = new();
    private static ParameterDriver? parameterDriver;
    private static PlayerSetup? lastPlayerSetup;
    private static OptionalUI? optionalUI;
    
    private bool loaded;
    private bool didIntegrate;

    public override void OnUpdate()
    {
        if(!loaded) return;
        PlayerSetup playerSetup = PlayerSetup.Instance;
        if(playerSetup == null) return;
        if (lastPlayerSetup != playerSetup)
        {
            parameterDriver = new ParameterDriver(playerSetup);
            lastPlayerSetup = playerSetup;
        }
        UnifiedTrackingData data = UnifiedTracking.Data;
        parameterDriver?.Update(data);
    }

    public override void OnLateUpdate()
    {
        if(loaded) return;
        Player? localPlayer = GetLocalPlayer();
        if(FaceTrackingManager.Instance == null || localPlayer == null) return;
        if (Runner.Instance == null)
        {
            GameObject runner = new GameObject("VRCFT2CVRRunner");
            runner.AddComponent<Runner>();
            Object.DontDestroyOnLoad(runner);
        }
        string gameDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modulesPath = Path.Combine(gameDirectory, "VRCFTModules");
        string persistentData = Path.Combine(gameDirectory, "UserData", "VRCFTData");
        if (!Directory.Exists(modulesPath)) Directory.CreateDirectory(modulesPath);
        if (!Directory.Exists(persistentData)) Directory.CreateDirectory(persistentData);
        Utils.CustomLibsDirectory = modulesPath;
        Utils.PersistentDataDirectory = persistentData;
        VRCFTParameters.UseBinary = Config.UseBinaryParameters;
        Hypernex.ExtendedTracking.FaceTrackingManager.Init(localPlayer, LoggerInstance);
        try
        {
            if(!Config.IntegratedTrackingSupport) return;
            Type faceTrackingType = typeof(FaceTrackingManager);
            FaceTrackingManager.Instance.RegisterEyeModule(GlobalModule);
            FaceTrackingManager.Instance.RegisterMouthModule(GlobalModule);
            // Find the index on the Tracking Modules
            List<IEyeTrackingModule> _eyeTrackingModules =
                (List<IEyeTrackingModule>) faceTrackingType.GetField("_eyeTrackingModules",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(FaceTrackingManager.Instance);
            List<IMouthTrackingModule> _mouthTrackingModules =
                (List<IMouthTrackingModule>) faceTrackingType.GetField("_mouthTrackingModules",
                    BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(FaceTrackingManager.Instance);
            int eyeIndex = _eyeTrackingModules.FindIndex(x => x == GlobalModule) + 1;
            int mouthIndex = _mouthTrackingModules.FindIndex(x => x == GlobalModule) + 1;
            // Update the index
            SetEyeTrackingModuleIndex(eyeIndex);
            SetEyeTrackingModuleIndex(mouthIndex);
            // awesomeness
            ForceActiveModule(faceTrackingType);
        }
        catch (Exception e)
        {
            MelonLogger.Error("Failed to load IntegratedTracking! " + e);
            MelonLogger.Msg("IntegratedTracking will be unavailable. Falling back to VRCFT Parameters.");
            NoIntegratedLoad();
        }
        finally
        {
            if(!Config.IntegratedTrackingSupport) NoIntegratedLoad();
            MelonLogger.Msg("Loaded VRCFT2CVR!");
            loaded = true;
            optionalUI = new OptionalUI();
        }
    }

    public override void OnApplicationQuit() => Hypernex.ExtendedTracking.FaceTrackingManager.Destroy();

    private void SetEyeTrackingModuleIndex(int index)
    {
        /*faceTrackingType.GetMethod("SetActiveEyeModule", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            FaceTrackingManager.Instance, new object[1] {index});*/
        MetaPort.Instance.settings.SetSettingsInt("ImplementationFaceTrackingEyeModule", index);
    }

    private void SetMouthTrackingModuleIndex(int index)
    {
        /*faceTrackingType.GetMethod("SetActiveMouthModule", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            FaceTrackingManager.Instance, new object[1] {index});*/
        MetaPort.Instance.settings.SetSettingsInt("ImplementationFaceTrackingMouthModule", index);
    }

    private void ForceActiveModule(Type faceTrackingType)
    {
        PropertyInfo eyeProperty = faceTrackingType.GetProperties().First(x => x.Name == "ActiveEyeModule");
        PropertyInfo mouthProperty = faceTrackingType.GetProperties().First(x => x.Name == "ActiveMouthModule");
        MethodInfo eyeMethod = eyeProperty.GetSetMethod(true);
        MethodInfo mouthMethod = mouthProperty.GetSetMethod(true);
        eyeMethod.Invoke(FaceTrackingManager.Instance, new IEyeTrackingModule[1] {GlobalModule});
        mouthMethod.Invoke(FaceTrackingManager.Instance, new IMouthTrackingModule[1] {GlobalModule});
    }

    private void NoIntegratedLoad()
    {
        SetEyeTrackingModuleIndex(0);
        SetMouthTrackingModuleIndex(0);
    }

    private Player? GetLocalPlayer() => PlayerAPI.LocalPlayerInternal;
}