/*
 * Hypernex.Unity is Licensed under GPL 3.0
 * You can view the license here: https://github.com/TigersUniverse/Hypernex.Unity/blob/main/LICENSE
 */

using VRCFaceTracking.Core.Params.Data;

namespace Hypernex.ExtendedTracking
{
    public interface ICustomFaceExpression
    {
        public string Name { get; }
        public float GetWeight(UnifiedTrackingData unifiedTrackingData);
        bool IsMatch(string parameterName);
    }
}