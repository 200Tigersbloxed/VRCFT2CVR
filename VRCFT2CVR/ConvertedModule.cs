using ABI_RC.Core.Player;
using ABI_RC.Systems.FaceTracking;
using MelonLoader;
using Microsoft.Extensions.Logging;
using Tobii.XR;
using UnityEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Data;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace VRCFT2CVR;

public class ConvertedModule : ITrackingModule, IEyeTrackingProvider
{
    public string Name => string.IsNullOrEmpty(extTrackingModule.ModuleInformation.Name)
        ? extTrackingModule.GetType().Name
        : extTrackingModule.ModuleInformation.Name;
    
    private ExtTrackingModule extTrackingModule;
    private Thread? updateThread;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private bool didInit;
    private (bool, bool) status;

    public ConvertedModule(ExtTrackingModule extTrackingModule)
    {
        this.extTrackingModule = extTrackingModule;
        extTrackingModule.Logger = new ConvertedModuleLogger(extTrackingModule.GetType().Name);
    }
    
    private Thread? InternalInit(bool useEye = true, bool useLip = true)
    {
        if(didInit) return null;
        try
        {
            Thread t = new Thread(() =>
            {
                status = extTrackingModule.Initialize(useEye, useLip);
                extTrackingModule.ModuleInformation.OnActiveChange = state =>
                    extTrackingModule.Status = state ? ModuleState.Active : ModuleState.Idle;
                Type moduleInformationType = extTrackingModule.ModuleInformation.GetType();
                moduleInformationType.GetProperty("Active")!.GetSetMethod(true)
                    .Invoke(extTrackingModule.ModuleInformation, new object[1] {true});
                moduleInformationType.GetProperty("UsingEye")!.GetSetMethod(true)
                    .Invoke(extTrackingModule.ModuleInformation, new object[1] {status.Item1});
                moduleInformationType.GetProperty("UsingExpression")!.GetSetMethod(true)
                    .Invoke(extTrackingModule.ModuleInformation, new object[1] {status.Item2});
            });
            updateThread = new Thread(Update);
            return t;
        }
        catch (MissingMethodException)
        {
            MelonLogger.Error(
                $"{extTrackingModule.GetType().Name} does not properly implement ExtTrackingModule. Skipping.");
            status = (false, false);
        }
        catch (Exception e)
        {
            MelonLogger.Error($"Exception initializing {extTrackingModule.GetType().Name}. Skipping. {e}");
            status = (false, false);
        }
        finally
        {
            didInit = true;
        }
        return null;
    }

    public (bool, bool) Initialize(bool useEye, bool useLip)
    {
        if (didInit) return status;
        // Eye is always off, we want that on for now!
        Thread? startThread = InternalInit(true, useLip);
        if (startThread == null) return status;
        startThread.Start();
        startThread.Join();
        updateThread!.Start();
        return status;
    }

    public void Shutdown()
    {
        extTrackingModule.Teardown();
        cancellationTokenSource.Cancel();
    }

    public bool IsEyeDataAvailable() => status.Item1;
    public bool IsLipDataAvailable() => status.Item2;

    private void Update()
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            extTrackingModule.Update();
            if (FaceTrackingManager.Instance != null)
            {
                if(status.Item1)
                    FaceTrackingManager.Instance.SubmitNewEyeData(VRCFTTools.GetEyeData(UnifiedTracking.Data.Eye));
                if(status.Item2)
                    FaceTrackingManager.Instance.SubmitNewFacialData(VRCFTTools.GetLipData(UnifiedTracking.Data));
            }
        }
    }

    public bool Initialize()
    {
        if (didInit) return status.Item1;
        return false;
    }

    public void Tick(){}
    public void Destroy(){}

    public TobiiXR_EyeTrackingData EyeTrackingDataLocal
    {
        get
        {
            UnifiedSingleEyeData combinedEye = UnifiedTracking.Data.Eye.Combined();
            /*Camera playerCamera = PlayerSetup.Instance == null
                ? Camera.current
                : PlayerSetup.Instance.GetActiveCamera().GetComponent<Camera>();
            // GetActiveCamera() can be null for one frame
            if (playerCamera == null)
            {
                return new TobiiXR_EyeTrackingData
                {
                    GazeRay = new TobiiXR_GazeRay
                    {
                        Direction = Vector3.zero,
                        Origin = Vector3.zero,
                        IsValid = false
                    },
                    IsLeftEyeBlinking = false,
                    IsRightEyeBlinking = false
                };
            }*/
            //Vector3 dir = playerCamera.ViewportToWorldPoint();
            TobiiXR_EyeTrackingData trackingData = new()
            {
                GazeRay = new TobiiXR_GazeRay
                {
                    Origin = new Vector3(combinedEye.Gaze.x, combinedEye.Gaze.y),
                    Direction = new Vector3(combinedEye.Gaze.x, combinedEye.Gaze.y, 3f).normalized,
                    IsValid = true
                },
                IsLeftEyeBlinking = UnifiedTracking.Data.Eye.Left.Openness < 0.2f,
                IsRightEyeBlinking = UnifiedTracking.Data.Eye.Right.Openness < 0.2f,
            };
            return trackingData;
        }
    }

    public Matrix4x4 LocalToWorldMatrix
    {
        get
        {
            if (PlayerSetup.Instance == null) return Matrix4x4.identity;
            return PlayerSetup.Instance.GetActiveCamera().transform.localToWorldMatrix;
        }
    }
}

public class ConvertedModuleLogger : ILogger
{
    private readonly string p;
    
    public ConvertedModuleLogger(string c) => p = $"[{c}] ";
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Information:
                MelonLogger.Msg(p + state);
                break;
            case LogLevel.Warning:
                MelonLogger.Warning(p + state);
                break;
            case LogLevel.Error:
                MelonLogger.Error(p + state);
                break;
            case LogLevel.Critical:
                MelonLogger.Error(p + state, exception);
                break;
            default:
                MelonLogger.Msg(p + state);
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new _();
    
    private class _ : IDisposable{public void Dispose(){}}
}