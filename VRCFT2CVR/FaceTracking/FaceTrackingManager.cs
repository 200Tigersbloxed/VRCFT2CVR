/*
 * Hypernex.Unity is Licensed under GPL 3.0
 * You can view the license here: https://github.com/TigersUniverse/Hypernex.Unity/blob/main/LICENSE
 * Changes:
 *   + Made private fields nullable
 *   + Init uses ChilloutVR player and MelonLogger
 *   + Init no longer Initializes Utils
 *   + Init registers mutations
 *   + Removed some helper methods
 *   + Use Runner
 */

using ABI_RC.API;
using MelonLoader;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Models;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Services;
using VRCFT2CVR;
using Utils = VRCFaceTracking.Core.Utils;

namespace Hypernex.ExtendedTracking
{
    public static class FaceTrackingManager
    {
        public static bool HasInitialized { get; private set; }
        public static bool EyeTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingEye && x.Active) > 0;
        public static bool LipTracking => libManager?.LoadedModulesMetadata.Count(x => x.UsingExpression && x.Active) > 0;
        public static Action<UnifiedTrackingData> OnTrackingUpdated = data => { };

        public static List<ICustomFaceExpression> CustomFaceExpressions = new();
        
        private static FaceTrackingServices.FTSettings? settings;
        private static FaceTrackingServices.FTLoggerFactory? loggerFactory;
        private static FaceTrackingServices.FTDispatcher? dispatcher;
        private static ILogger<ModuleDataService>? moduleDataServiceLogger;
        private static IModuleDataService? moduleDataService;
        private static ILibManager? libManager;
        private static ILogger<UnifiedTrackingMutator>? mutatorLogger;
        private static UnifiedTrackingMutator? mutator;
        private static MainIntegrated? mainIntegrated;

        public static async void Init(Player player, MelonLogger.Instance instance)
        {
            if (HasInitialized)
                return;
            /*Utils.PersistentDataDirectory = Path.Combine(persistentData, "VRCFaceTracking");
            if (!Directory.Exists(Utils.PersistentDataDirectory))
                Directory.CreateDirectory(Utils.PersistentDataDirectory);
            Utils.CustomLibsDirectory = persistentData + "\\VRCFTModules";*/
            if (!Directory.Exists(Utils.CustomLibsDirectory))
                Directory.CreateDirectory(Utils.CustomLibsDirectory);
            settings = new FaceTrackingServices.FTSettings();
            loggerFactory = new FaceTrackingServices.FTLoggerFactory(instance);
            dispatcher = new FaceTrackingServices.FTDispatcher();
            moduleDataServiceLogger = loggerFactory.CreateLogger<ModuleDataService>();
            mutatorLogger = loggerFactory.CreateLogger<UnifiedTrackingMutator>();
            
            Dictionary<string, string> v = Config.facialTrackingSettings.Value;
            if (!v.ContainsKey("Mutations"))
                SetSettings("Mutations", new UnifiedMutationConfig());
            
            moduleDataService =
                new ModuleDataService(new FaceTrackingServices.ChilloutVRIdentity(player), moduleDataServiceLogger);
            libManager = new UnifiedLibManager(loggerFactory, dispatcher, moduleDataService);
            mutator = new UnifiedTrackingMutator(mutatorLogger, settings);
            mainIntegrated = new MainIntegrated(loggerFactory, libManager, mutator);
            await mainIntegrated.InitializeAsync();
            CustomFaceExpressions.Clear();
            UnifiedTracking.OnUnifiedDataUpdated +=
                data => Runner.InvokeActionOnMainThread(OnTrackingUpdated, data);
            HasInitialized = true;
        }
        
        public static T GetSettings<T>(string key) => settings!.ReadSettingAsync<T>(key).Result;
        public static void SetSettings<T>(string key, T value) => settings!.SaveSettingAsync(key, value);

        public static void Restart()
        {
            if(!HasInitialized || mainIntegrated == null) return;
            HasInitialized = false;
            mainIntegrated.Teardown();
            mainIntegrated.InitializeAsync();
            HasInitialized = true;
        }

        public static void Destroy()
        {
            if(HasInitialized)
            {
                mainIntegrated?.Teardown();
                HasInitialized = false;
            }
        }
    }
}