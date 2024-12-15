using ABI_RC.Core.Player;
using ABI_RC.Systems.FaceTracking;
using Tobii.XR;
using UnityEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using FaceTrackingManager = Hypernex.ExtendedTracking.FaceTrackingManager;

namespace VRCFT2CVR;

public class ConvertedModule : ITrackingModule, IEyeTrackingProvider
{
    public string Name => "VRCFTModule";

    public ConvertedModule() => FaceTrackingManager.OnTrackingUpdated += data =>
    {
        if(IsEyeDataAvailable())
            ABI_RC.Systems.FaceTracking.FaceTrackingManager.Instance.SubmitNewEyeData(
                VRCFTTools.GetEyeData(data.Eye));
        if(IsLipDataAvailable())
            ABI_RC.Systems.FaceTracking.FaceTrackingManager.Instance.SubmitNewFacialData(
                VRCFTTools.GetLipData(data));
    };

    public (bool, bool) Initialize(bool useEye, bool useLip) => (true, true);

    public void Shutdown(){}

    public bool IsEyeDataAvailable() => FaceTrackingManager.EyeTracking;
    public bool IsLipDataAvailable() => FaceTrackingManager.LipTracking;
    public bool Initialize() => true;

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