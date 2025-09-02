using ABI_RC.Systems.FaceTracking;
using FaceTrackingManager = Hypernex.ExtendedTracking.FaceTrackingManager;

namespace VRCFT2CVR;

public class ConvertedModule : IEyeTrackingModule, IMouthTrackingModule
{
    public string Name => "VRCFTModule";

    private EyeTrackingData eyeTrackingData;
    private MouthTrackingData mouthTrackingData;

    public ConvertedModule() => FaceTrackingManager.OnTrackingUpdated += data =>
    {
        if (FaceTrackingManager.EyeTracking)
            eyeTrackingData = VRCFTTools.GetEyeData(data.Eye, data);
        if (FaceTrackingManager.LipTracking)
            mouthTrackingData = VRCFTTools.GetLipData(data);
    };

    bool IEyeTrackingModule.Start(bool vr) => true;
    void IMouthTrackingModule.Stop(){}

    void IMouthTrackingModule.Update(){}

    void IMouthTrackingModule.Dispose(){}

    bool IMouthTrackingModule.IsRunning() => FaceTrackingManager.HasInitialized;

    bool IMouthTrackingModule.IsDataAvailable() => FaceTrackingManager.LipTracking;

    MouthTrackingData IMouthTrackingModule.GetTrackingData() => mouthTrackingData;

    bool IMouthTrackingModule.Start(bool vr) => true;

    void IEyeTrackingModule.Stop(){}

    void IEyeTrackingModule.Update(){}

    void IEyeTrackingModule.Dispose(){}

    bool IEyeTrackingModule.IsRunning() => FaceTrackingManager.HasInitialized;

    bool IEyeTrackingModule.IsDataAvailable() => FaceTrackingManager.EyeTracking;

    EyeTrackingData IEyeTrackingModule.GetTrackingData() => eyeTrackingData;
}