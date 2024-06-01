using System.Text.RegularExpressions;
using ViveSR.anipal.Eye;
using ViveSR.anipal.Lip;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Params.Expressions;
using Vector3 = UnityEngine.Vector3;

namespace VRCFT2CVR;

internal static class VRCFTTools
{
    private static CombinedEyeData GetCombined(UnifiedEyeData eyeData)
    {
        UnifiedSingleEyeData combined = eyeData.Combined();
        return new()
        {
            eye_data = new SingleEyeData
            {
                eye_openness = combined.Openness,
                gaze_direction_normalized = Vector3.Normalize(new Vector3(combined.Gaze.x, combined.Gaze.y, 3f)),
                gaze_origin_mm = new Vector3(combined.Gaze.x, combined.Gaze.y),
                pupil_diameter_mm = combined.PupilDiameter_MM
            }
        };
    }

    internal static EyeData_v2 GetEyeData(UnifiedEyeData eyeData) => new()
    {
        expression_data = new EyeExpression
        {
            left = new SingleEyeExpression
            {
                eye_wide = eyeData.Left.Openness
            },
            right = new SingleEyeExpression
            {
                eye_wide = eyeData.Right.Openness
            }
        },
        verbose_data = new VerboseData
        {
            left = new SingleEyeData
            {
                eye_openness = eyeData.Left.Openness,
                gaze_direction_normalized = Vector3.Normalize(new Vector3(eyeData.Left.Gaze.x, eyeData.Left.Gaze.y, 3f)),
                gaze_origin_mm = new Vector3(eyeData.Left.Gaze.x, eyeData.Left.Gaze.y, 0),
                pupil_diameter_mm = eyeData.Left.PupilDiameter_MM
            },
            right = new SingleEyeData
            {
                eye_openness = eyeData.Right.Openness,
                gaze_direction_normalized = Vector3.Normalize(new Vector3(eyeData.Right.Gaze.x, eyeData.Right.Gaze.y, 3f)),
                gaze_origin_mm = new Vector3(eyeData.Right.Gaze.x, eyeData.Right.Gaze.y, 0),
                pupil_diameter_mm = eyeData.Right.PupilDiameter_MM
            },
            combined = GetCombined(eyeData)
        }
    };

    private static float[] GetFaceWeights(UnifiedTrackingData trackingData)
    {
        float[] weights = new float[37];
        SRanipal_LipShape_v2[] srValues = Enum.GetValues(typeof(SRanipal_LipShape_v2)).Cast<SRanipal_LipShape_v2>().ToArray();
        for (int i = 0; i < srValues.Length; i++)
        {
            SRanipal_LipShape_v2 shape = srValues[i];
            if(shape >= SRanipal_LipShape_v2.Max) continue;
            try
            {
                weights[i] = UnifiedSRanMapper.GetTransformedShape(shape, trackingData);
            }
            catch(Exception){}
        }
        return weights;
    }

    internal static LipData_v2 GetLipData(UnifiedTrackingData trackingData) => new()
    {
        prediction_data = new PredictionData_v2
        {
            blend_shape_weight = GetFaceWeights(trackingData)
        }
    };

    #region VRCFaceTracking
    /*
     * VRCFaceTracking is licensed under Apache-2.0
     * You can view the license here: https://github.com/benaclejames/VRCFaceTracking/blob/master/LICENSE
     * No changed to code below were made
     */

    public static Regex GetParameterRegex(string parameterName) =>
        new(@"(?<!(v\d+))(/" + parameterName + ")$|^(" + parameterName + ")$");
    
    internal enum SRanipal_LipShape_v2
    {
        //None = -1,
        JawRight = 0, // +JawX
        JawLeft = 1, // -JawX
        JawForward = 2,
        JawOpen = 3,
        MouthApeShape = 4,
        MouthUpperRight = 5, // +MouthUpper
        MouthUpperLeft = 6, // -MouthUpper
        MouthLowerRight = 7, // +MouthLower
        MouthLowerLeft = 8, // -MouthLower
        MouthUpperOverturn = 9,
        MouthLowerOverturn = 10,
        MouthPout = 11,
        MouthSmileRight = 12, // +SmileSadRight
        MouthSmileLeft = 13, // +SmileSadLeft
        MouthSadRight = 14, // -SmileSadRight
        MouthSadLeft = 15, // -SmileSadLeft
        CheekPuffRight = 16,
        CheekPuffLeft = 17,
        CheekSuck = 18,
        MouthUpperUpRight = 19,
        MouthUpperUpLeft = 20,
        MouthLowerDownRight = 21,
        MouthLowerDownLeft = 22,
        MouthUpperInside = 23,
        MouthLowerInside = 24,
        MouthLowerOverlay = 25,
        TongueLongStep1 = 26,
        TongueLongStep2 = 32,
        TongueDown = 30, // -TongueY
        TongueUp = 29, // +TongueY
        TongueRight = 28, // +TongueX
        TongueLeft = 27, // -TongueX
        TongueRoll = 31,
        TongueUpLeftMorph = 34,
        TongueUpRightMorph = 33,
        TongueDownLeftMorph = 36,
        TongueDownRightMorph = 35,
        Max = 37,
    }
    
    internal static class UnifiedSRanMapper
    {
        // This map will transform the robust tracking data from Unified and transform it to track as closely to the SRanipal standard tracking (as of SRanipal 1.3.6.x) as possible.
        // Some shapes are used to gate other shapes from tracking, some shapes are directly mappable, and some shapes are a combination of many Unified shapes.
        // If an interface properly interfaces to Unified's tracking standard then this SRanipal emulator will work well.
        private static Dictionary<SRanipal_LipShape_v2, Func<UnifiedTrackingData, float>> ShapeMap = new Dictionary<SRanipal_LipShape_v2, Func<UnifiedTrackingData, float>>
        {
            { SRanipal_LipShape_v2.JawRight, exp => exp.Shapes[(int)UnifiedExpressions.JawRight].Weight },
            { SRanipal_LipShape_v2.JawLeft, exp => exp.Shapes[(int)UnifiedExpressions.JawLeft].Weight },
            { SRanipal_LipShape_v2.JawForward, exp => exp.Shapes[(int)UnifiedExpressions.JawForward].Weight },
            { SRanipal_LipShape_v2.JawOpen, exp => Math.Min(1, Math.Max(0, exp.Shapes[(int)UnifiedExpressions.JawOpen].Weight - exp.Shapes[(int)UnifiedExpressions.MouthClosed].Weight)) },
            { SRanipal_LipShape_v2.MouthApeShape, exp => exp.Shapes[(int)UnifiedExpressions.MouthClosed].Weight},

            { SRanipal_LipShape_v2.MouthUpperRight, exp => exp.Shapes[(int)UnifiedExpressions.MouthUpperRight].Weight },
            { SRanipal_LipShape_v2.MouthUpperLeft, exp => exp.Shapes[(int)UnifiedExpressions.MouthUpperLeft].Weight },
            { SRanipal_LipShape_v2.MouthLowerRight, exp => exp.Shapes[(int)UnifiedExpressions.MouthLowerRight].Weight },
            { SRanipal_LipShape_v2.MouthLowerLeft, exp => exp.Shapes[(int)UnifiedExpressions.MouthLowerLeft].Weight },

            { SRanipal_LipShape_v2.MouthUpperOverturn, exp => (exp.Shapes[(int)UnifiedExpressions.LipFunnelUpperLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipFunnelUpperRight].Weight) / 2.0f },
            { SRanipal_LipShape_v2.MouthLowerOverturn, exp => (exp.Shapes[(int)UnifiedExpressions.LipFunnelLowerLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipFunnelLowerRight].Weight) / 2.0f },

            { SRanipal_LipShape_v2.MouthPout, exp => 
                (exp.Shapes[(int)UnifiedExpressions.LipPuckerUpperLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipPuckerUpperRight].Weight +
                exp.Shapes[(int)UnifiedExpressions.LipPuckerLowerLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipPuckerLowerRight].Weight) / 4.0f },

            { SRanipal_LipShape_v2.MouthSmileRight, exp =>
                GetSimpleShape(exp, UnifiedSimpleExpressions.MouthSmileRight) > exp.Shapes[(int)UnifiedExpressions.MouthDimpleRight].Weight ?
                GetSimpleShape(exp, UnifiedSimpleExpressions.MouthSmileRight) : exp.Shapes[(int)UnifiedExpressions.MouthDimpleRight].Weight },
            { SRanipal_LipShape_v2.MouthSmileLeft, exp =>
                GetSimpleShape(exp, UnifiedSimpleExpressions.MouthSmileLeft) > exp.Shapes[(int)UnifiedExpressions.MouthDimpleLeft].Weight ?
                GetSimpleShape(exp, UnifiedSimpleExpressions.MouthSmileLeft) : exp.Shapes[(int)UnifiedExpressions.MouthDimpleLeft].Weight },
            { SRanipal_LipShape_v2.MouthSadRight, exp =>
                Math.Max(0, (((exp.Shapes[(int)UnifiedExpressions.MouthFrownRight].Weight + exp.Shapes[(int)UnifiedExpressions.MouthFrownLeft].Weight) / 2.0f >
                (exp.Shapes[(int)UnifiedExpressions.MouthStretchRight].Weight) ?
                    (exp.Shapes[(int)UnifiedExpressions.MouthFrownRight].Weight + exp.Shapes[(int)UnifiedExpressions.MouthFrownLeft].Weight) / 2.0f :
                    (exp.Shapes[(int)UnifiedExpressions.MouthStretchRight].Weight)) -
                (GetTransformedShape(SRanipal_LipShape_v2.MouthSmileRight, exp))))},
            { SRanipal_LipShape_v2.MouthSadLeft, exp =>
                Math.Max(0, (((exp.Shapes[(int)UnifiedExpressions.MouthFrownRight].Weight + exp.Shapes[(int)UnifiedExpressions.MouthFrownLeft].Weight) / 2.0f >
                (exp.Shapes[(int)UnifiedExpressions.MouthStretchLeft].Weight) ?
                    (exp.Shapes[(int)UnifiedExpressions.MouthFrownRight].Weight + exp.Shapes[(int)UnifiedExpressions.MouthFrownLeft].Weight) / 2.0f :
                    (exp.Shapes[(int)UnifiedExpressions.MouthStretchLeft].Weight)) -
                (GetTransformedShape(SRanipal_LipShape_v2.MouthSmileLeft, exp))))},

            { SRanipal_LipShape_v2.CheekPuffLeft, exp => exp.Shapes[(int)UnifiedExpressions.CheekPuffLeft].Weight },
            { SRanipal_LipShape_v2.CheekPuffRight, exp => exp.Shapes[(int)UnifiedExpressions.CheekPuffRight].Weight },
            { SRanipal_LipShape_v2.CheekSuck, exp => (exp.Shapes[(int)UnifiedExpressions.CheekSuckLeft].Weight + exp.Shapes[(int)UnifiedExpressions.CheekSuckRight].Weight) / 2.0f },

            { SRanipal_LipShape_v2.MouthUpperUpRight, exp =>
                (float)Math.Max(0,
                    exp.Shapes[(int)UnifiedExpressions.MouthUpperUpRight].Weight +
                    (1f - exp.Shapes[(int)UnifiedExpressions.LipPuckerUpperRight].Weight) * exp.Shapes[(int)UnifiedExpressions.LipFunnelUpperRight].Weight)},
            { SRanipal_LipShape_v2.MouthUpperUpLeft, exp =>
                (float)Math.Max(0,
                    exp.Shapes[(int)UnifiedExpressions.MouthUpperUpLeft].Weight +
                    (1f - exp.Shapes[(int)UnifiedExpressions.LipPuckerUpperLeft].Weight) * exp.Shapes[(int)UnifiedExpressions.LipFunnelUpperLeft].Weight)},
            { SRanipal_LipShape_v2.MouthLowerDownRight, exp =>
                Math.Max(0,
                    exp.Shapes[(int)UnifiedExpressions.MouthLowerDownRight].Weight +
                    (1f - exp.Shapes[(int)UnifiedExpressions.LipPuckerLowerRight].Weight) * exp.Shapes[(int)UnifiedExpressions.LipFunnelLowerRight].Weight)},
            { SRanipal_LipShape_v2.MouthLowerDownLeft, exp =>
                Math.Max(0,
                    exp.Shapes[(int)UnifiedExpressions.MouthLowerDownLeft].Weight +
                    (1f - exp.Shapes[(int)UnifiedExpressions.LipPuckerLowerLeft].Weight) * exp.Shapes[(int)UnifiedExpressions.LipFunnelLowerLeft].Weight)},

            { SRanipal_LipShape_v2.MouthUpperInside, exp =>
                (float)Math.Max(0, (exp.Shapes[(int)UnifiedExpressions.LipSuckUpperLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipSuckUpperRight].Weight) / 2.0f)},
            { SRanipal_LipShape_v2.MouthLowerInside, exp =>
                (float)Math.Max(0, (exp.Shapes[(int)UnifiedExpressions.LipSuckLowerLeft].Weight + exp.Shapes[(int)UnifiedExpressions.LipSuckLowerRight].Weight) / 2.0f)},

            { SRanipal_LipShape_v2.MouthLowerOverlay, exp => exp.Shapes[(int)UnifiedExpressions.MouthRaiserLower].Weight },

            { SRanipal_LipShape_v2.TongueLongStep1, exp => Math.Min(1f, exp.Shapes[(int)UnifiedExpressions.TongueOut].Weight * 2.0f)},
            { SRanipal_LipShape_v2.TongueLongStep2, exp => Math.Min(1f, Math.Max(0, (exp.Shapes[(int)UnifiedExpressions.TongueOut].Weight * 2.0f) - 1f)) },

            { SRanipal_LipShape_v2.TongueDown, exp => exp.Shapes[(int)UnifiedExpressions.TongueDown].Weight},
            { SRanipal_LipShape_v2.TongueUp, exp => exp.Shapes[(int)UnifiedExpressions.TongueUp].Weight},
            { SRanipal_LipShape_v2.TongueRight, exp => exp.Shapes[(int)UnifiedExpressions.TongueRight].Weight},
            { SRanipal_LipShape_v2.TongueLeft, exp => exp.Shapes[(int)UnifiedExpressions.TongueLeft].Weight},
            { SRanipal_LipShape_v2.TongueRoll, exp => exp.Shapes[(int)UnifiedExpressions.TongueRoll].Weight},

            { SRanipal_LipShape_v2.TongueUpLeftMorph, exp => 0.0f},
            { SRanipal_LipShape_v2.TongueUpRightMorph, exp => 0.0f},
            { SRanipal_LipShape_v2.TongueDownLeftMorph, exp => 0.0f},
            { SRanipal_LipShape_v2.TongueDownRightMorph, exp => 0.0f},

            { SRanipal_LipShape_v2.Max, exp => 0.0f},
        };

        public static float GetTransformedShape(SRanipal_LipShape_v2 sr_shape, UnifiedTrackingData expData) =>
            ShapeMap[sr_shape].Invoke(expData);
        private static float GetSimpleShape(UnifiedTrackingData data, UnifiedSimpleExpressions expression) => UnifiedSimplifier.ExpressionMap[expression].Invoke(data);
    }
    #endregion
}