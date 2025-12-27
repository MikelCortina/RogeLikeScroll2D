using UnityEngine;

public class ShaderVariableManager : MonoBehaviour
{
    static readonly int UnscaledTimeID = Shader.PropertyToID("_UnscaledTime");
    static readonly int UnscaledDeltaTimeID = Shader.PropertyToID("_UnscaledDeltaTime");

    void Update()
    {
        Shader.SetGlobalFloat(UnscaledTimeID, Time.unscaledTime);
        Shader.SetGlobalFloat(UnscaledDeltaTimeID, Time.unscaledDeltaTime);
    }
}
