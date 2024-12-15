using ABI_RC.Core.Player;
using ABI_RC.Core.Util.AnimatorManager;
using UnityEngine;
using VRCFaceTracking.Core.Params.Data;

namespace VRCFT2CVR;

public class ParameterDriver
{
    private PlayerSetup playerSetup;
    private List<CVRAnimatorManager.ParamDef>? parameters;

    public ParameterDriver(PlayerSetup playerSetup)
    {
        this.playerSetup = playerSetup;
        UpdateParameters();
    }

    private void UpdateParameters()
    {
        parameters = new Dictionary<string, CVRAnimatorManager.ParamDef>(playerSetup.animatorManager.Parameters).Values
            .ToList();
        VRCFTParameters.UpdateParameters(parameters);
    }

    private bool CheckForParameterUpdate()
    {
        if (parameters == null) return true;
        List<CVRAnimatorManager.ParamDef> newParams =
            new Dictionary<string, CVRAnimatorManager.ParamDef>(playerSetup.animatorManager.Parameters).Values.ToList();
        if (parameters.Count != newParams.Count) return true;
        for (int i = 0; i < newParams.Count; i++)
        {
            CVRAnimatorManager.ParamDef old = parameters.ElementAt(i);
            CVRAnimatorManager.ParamDef n = parameters.ElementAt(i);
            if (old.name != n.name) return true;
        }
        return false;
    }

    public void Update(UnifiedTrackingData data)
    {
        if(playerSetup.animatorManager == null) return;
        if(CheckForParameterUpdate()) UpdateParameters();
        parameters!.Where(x => x.type != AnimatorControllerParameterType.Trigger).ToList().ForEach(parameter =>
        {
            VRCFTParameters.CustomFaceExpressions.ForEach(expression =>
            {
                if(!expression.IsMatch(parameter.name)) return;
                float weight = expression.GetWeight(data);
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        playerSetup.animatorManager.SetParameter(parameter.name, weight);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        playerSetup.animatorManager.SetParameter(parameter.name, weight > 0.5f);
                        break;
                    case AnimatorControllerParameterType.Int:
                        playerSetup.animatorManager.SetParameter(parameter.name, Mathf.RoundToInt(weight));
                        break;
                }
            });
        });
    }
}