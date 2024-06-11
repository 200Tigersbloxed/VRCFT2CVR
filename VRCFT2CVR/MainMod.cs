using System.Reflection;
using System.Reflection.Emit;
using ABI_RC.Core.Player;
using ABI_RC.Core.Player.EyeMovement;
using ABI_RC.Core.Savior;
using ABI_RC.Systems.FaceTracking;
using HarmonyLib;
using MelonLoader;
using Tobii.Gaming;
using Tobii.XR;
using Unity.XR.OpenVR;
using UnityEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFT2CVR;
using Utils = VRCFaceTracking.Core.Utils;
using Vector2 = VRCFaceTracking.Core.Types.Vector2;

[assembly: MelonInfo(typeof(MainMod), MainMod.MOD_NAME, "1.0.2", "200Tigersbloxed")]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonOptionalDependencies("BTKUILib")]
[assembly: HarmonyDontPatchAll]

namespace VRCFT2CVR;

public class MainMod : MelonMod
{
    internal const string MOD_NAME = "VRCFT2CVR";
    
    private static ConvertedModule? customEyeModule;
    private static ParameterDriver? parameterDriver;
    private static PlayerSetup? lastPlayerSetup;
    private static List<ConvertedModule> convertedModules = new();
    private static OptionalUI? optionalUI;
    
    private string? modulesPath;
    private bool loaded;
    private bool didIntegrate;

    public override void OnEarlyInitializeMelon() => HarmonyInstance.Patch(
        typeof(OpenVRHelpers).GetMethod("IsUsingSteamVRInput"),
        new HarmonyMethod(typeof(OpenVR_IsUsingSteamVRInputPatch).GetMethod("Prefix",
            BindingFlags.Static | BindingFlags.NonPublic)));

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
        if(FaceTrackingManager.Instance == null || EyeTrackingManager.Instance == null) return;
        modulesPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(MelonAssembly.Location)!)!.FullName,
            "VRCFTModules");
        string persistentData =
            Path.Combine(Directory.GetParent(Path.GetDirectoryName(MelonAssembly.Location)!)!.FullName, "UserData",
                "VRCFTData");
        if (!Directory.Exists(modulesPath)) Directory.CreateDirectory(modulesPath);
        if (!Directory.Exists(persistentData)) Directory.CreateDirectory(persistentData);
        Utils.CustomLibsDirectory = modulesPath;
        Utils.PersistentDataDirectory = persistentData;
        foreach (string module in Directory.GetFiles(modulesPath))
        {
            string fileExt = Path.GetExtension(module);
            if(fileExt != ".dll") continue;
            try
            {
                Assembly assembly = Assembly.LoadFile(module);
                Type[] moduleTypes =
                    assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ExtTrackingModule))).ToArray();
                foreach (Type moduleType in moduleTypes)
                {
                    ExtTrackingModule extTrackingModule = (ExtTrackingModule) Activator.CreateInstance(moduleType)!;
                    ConvertedModule convertedModule = new ConvertedModule(extTrackingModule);
                    FaceTrackingManager.Instance.RegisterModule(convertedModule);
                    convertedModules.Add(convertedModule);
                    MelonLogger.Msg("Loaded " + (string.IsNullOrEmpty(extTrackingModule.ModuleInformation.Name)
                        ? Path.GetFileNameWithoutExtension(module)
                        : extTrackingModule.ModuleInformation.Name));
                }
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to load module at " + module + "!", e);
            }
        }
        ParameterDriver.UseBinary = Config.UseBinaryParameters;
        try
        {
            if (!Config.IntegratedTrackingSupport) return;
            MelonLogger.Warning("ITrackingModule and IEyeTrackingProvider implementation is experimental!");
            HarmonyInstance.Patch(typeof(TobiiXR_Settings).GetMethod("GetProvider"),
                new HarmonyMethod(typeof(TobiiAPI_GetProviderPatch).GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.NonPublic)));
            Type faceTrackingManagerType = typeof(FaceTrackingManager);
            FieldInfo activeEyeField = faceTrackingManagerType.GetField("_activeEyeModule",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            FieldInfo activeLipField = faceTrackingManagerType.GetField("_activeLipModule",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            if (activeEyeField.GetValue(FaceTrackingManager.Instance) != null ||
                activeLipField.GetValue(FaceTrackingManager.Instance) != null)
            {
                MetaPort.Instance.settings.SetSettingsBool("ImplementationVRViveFaceTracking", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationDesktopViveFaceTracking", false);
                throw new Exception(
                    "You have the Face Tracking enabled in settings! Please restart to use IntegratedTracking.");
            }
            if (EyeTrackingManager.Instance._settingBlinkingEnabled ||
                EyeTrackingManager.Instance._settingTrackingEnabled)
            {
                MetaPort.Instance.settings.SetSettingsBool("ImplementationDesktopTobiiEyeTracking", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationDesktopTobiiEyeBlinking", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationVRTobiiEyeTracking", false);
                MetaPort.Instance.settings.SetSettingsBool("ImplementationVRTobiiEyeBlinking", false);
                throw new Exception(
                    "You have the Face Tracking enabled in settings! Please restart to use IntegratedTracking.");
            }
            // Remove non-converted modules, as they can cause issues (i.e. SRanipal)
            FieldInfo registeredModulesField = faceTrackingManagerType.GetField("_registeredTrackingModules", BindingFlags.Instance | BindingFlags.NonPublic)!;
            List<ITrackingModule> registeredModules =
                (List<ITrackingModule>) registeredModulesField.GetValue(FaceTrackingManager.Instance);
            registeredModules.RemoveAll(x => !convertedModules.Contains(x));
            registeredModulesField.SetValue(FaceTrackingManager.Instance, registeredModules);
            // Start the Face Tracking
            FaceTrackingManager.Instance.Initialize();
            faceTrackingManagerType.GetProperty("_settingEnabled")!.GetSetMethod(true)
                .Invoke(FaceTrackingManager.Instance, new object[1] {true});
            // Eye Tracking
            object activeEyeModule = activeEyeField.GetValue(FaceTrackingManager.Instance);
            if (activeEyeModule is ConvertedModule)
            {
                customEyeModule = (ConvertedModule) activeEyeModule;
                object _tobii =
                    typeof(EyeTrackingManager).GetField("_tobii", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .GetValue(
                            EyeTrackingManager.Instance);
                object Settings = _tobii.GetType().GetField("Settings").GetValue(_tobii);
                Settings.GetType().GetField("_eyeTrackingProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(Settings, customEyeModule);
            }
            typeof(EyeTrackingManager).GetProperty("_settingTrackingEnabled")!.GetSetMethod(true)
                .Invoke(EyeTrackingManager.Instance, new object[1] {true});
            typeof(EyeTrackingManager).GetProperty("_settingBlinkingEnabled")!.GetSetMethod(true)
                .Invoke(EyeTrackingManager.Instance, new object[1] {true});
            typeof(EyeTrackingManager).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(
                    EyeTrackingManager.Instance, Array.Empty<object>());
            HarmonyInstance.PatchAll();
            didIntegrate = true;
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

    public override void OnApplicationQuit()
    {
        if(!didIntegrate) return;
        convertedModules.ForEach(module =>
        {
            try
            {
                module.Shutdown();
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to Shutdown module " + module.Name + " for reason " + e);
            }
        });
    }

    private void NoIntegratedLoad()
    {
        typeof(FaceTrackingManager).GetProperty("_settingEnabled")!.GetSetMethod(true)
            .Invoke(FaceTrackingManager.Instance, new object[1] {false});
        typeof(EyeTrackingManager).GetProperty("_settingTrackingEnabled")!.GetSetMethod(true)
            .Invoke(EyeTrackingManager.Instance, new object[1] {false});
        typeof(EyeTrackingManager).GetProperty("_settingBlinkingEnabled")!.GetSetMethod(true)
            .Invoke(EyeTrackingManager.Instance, new object[1] {false});
        convertedModules.ForEach(module =>
        {
            try
            {
                (bool, bool) r = module.Initialize(true, true);
                if(r.Item1) MelonLogger.Msg("Loaded EyeTracking for module " + module.Name);
                if(r.Item2) MelonLogger.Msg("Loaded LipTracking for module " + module.Name);
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to load module " + module.Name + " for reason " + e);
            }
        });
    }

    [HarmonyPatch(typeof(TobiiAPI))]
    [HarmonyPatch(nameof(TobiiAPI.IsConnected), MethodType.Getter)]
    private class TobiiAPI_getIsConnectedPatch
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(TobiiAPI), nameof(TobiiAPI.GetGazePoint), new Type[0])]
    private class TobiiAPI_GetGazePointPatch
    {
        private static float ConvertRange(float x) => x < -1 || x > 1
            ? throw new ArgumentOutOfRangeException(nameof(x), "Input value must be in the range -1 to 1.")
            : (x + 1) / 2.00f;
        
        static bool Prefix(ref GazePoint __result)
        {
            if (customEyeModule == null) return true;
            Vector2 gaze = UnifiedTracking.Data.Eye.Combined().Gaze;
            __result = new GazePoint(new UnityEngine.Vector2(ConvertRange(gaze.x), ConvertRange(gaze.y)),
                Time.unscaledTime / 1000000f, DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond / 1000));
            return false;
        }
    }

    [HarmonyPatch(typeof(TobiiXR), nameof(TobiiXR.GetEyeTrackingData), new Type[1]{typeof(TobiiXR_TrackingSpace)})]
    private class TobiiXR_GetEyeTrackingDataPatch
    {
        static bool Prefix(ref TobiiXR_EyeTrackingData __result, TobiiXR_TrackingSpace trackingSpace)
        {
            if (customEyeModule == null) return true;
            TobiiXR_EyeTrackingData eyeTrackingData = customEyeModule.EyeTrackingDataLocal;
            eyeTrackingData.GazeRay.IsValid = false;
            __result = eyeTrackingData;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(EyeMovementController), "ProcessTobiiEyeTracking")]
    private class EyeMovementController_ProcessTobiiEyeTrackingPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld &&
                    codes[i].operand is FieldInfo fieldInfo &&
                    fieldInfo.Name == "isUsingVr" &&
                    fieldInfo.DeclaringType!.Name == "MetaPort")
                {
                    codes.RemoveAt(i - 1);
                    codes.Insert(i - 1, new CodeInstruction(OpCodes.Nop));
                    codes[i] = new CodeInstruction(OpCodes.Ldc_I4_1);
                }
            }
            return codes.AsEnumerable();
        }
    }
    
    #region TemporaryFixes
    
    /*
     * These are temporary fixes!
     * Currently, because of all of the libraries that VRCFaceTracking needs, some don't 'register' correctly.
     * Whenever Assemblies in the same AppDomain attempt to access types for this mod's classes,
     * an exception for a missing type is thrown (which it is correct, it is missing).
     * I am currently stumped on how to resolve all of these libraries without error, but it has proven difficult.
     * 
     * Here's a list of things I've currently tried to fix this mystery issue:
     * 1) Merging all VRCFaceTracking libraries with ILRepack
     *   + System.ComponentModel.DataAnnotations won't let this work
     * 2) Merging all VRCFaceTracking libraries with ILMerge
     *   + System.ComponentModel.DataAnnotations won't let this work
     * 3) Dumping all the libraries in UserLibs
     *   + System.ComponentModel.DataAnnotations is still missing, even with its direct reference dll added
     *
     * Here's some proposed solutions
     * 1) Remove all VRCFaceTracking.Core and its libraries and only access required dependencies with
     *    AppDomain.AssemblyResolve
     *   + This would require a LOT of reflection and might not even work
     *   + This is not a good solution
     * 2) Load modules and their necessary dependencies into a separate AppDomain
     *   + This would prevent Assembly.GetTypes() from failing in the main AppDomain
     *   + This is currently the best solution
     * 3) Remove the Community.Mvvm dependency (relies on System.ComponentModel.Annotations a.k.a. the big problem)
     *   + This would require an entire rewrite of VRCFaceTracking.Core (not fun)
     * 4) Remove the System.ComponentModel.Annotations dependency
     *   + This would require a partial rewrite of the Community.Mvvm library (also not fun)
     *
     * Currently, these two patches prevent the following from happening:
     * 1) Prevent the TobiiAPI from getting the wrong provider
     *   + This is good and will probably stay
     * 2) Prevent the TobiiAPI from breaking itself (because of us) by getting all of the Assembly's types
     * 3) Prevent OpenVR from breaking itself (again, because of us) by getting all of the Assembly's types
     */
    
    [HarmonyPatch(typeof(TobiiXR_Settings), nameof(TobiiXR_Settings.GetProvider), new Type[0])]
    private class TobiiAPI_GetProviderPatch
    {
        static bool Prefix(ref IEyeTrackingProvider __result)
        {
            if (customEyeModule == null) return true;
            __result = customEyeModule;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(OpenVRHelpers), nameof(OpenVRHelpers.IsUsingSteamVRInput), new Type[0])]
    private class OpenVR_IsUsingSteamVRInputPatch
    {
        static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
    
    #endregion
}