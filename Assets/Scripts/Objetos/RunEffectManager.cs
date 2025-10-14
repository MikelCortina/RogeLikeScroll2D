using System.Collections.Generic;
using UnityEngine;

public class RunEffectManager : MonoBehaviour
{
    public static RunEffectManager Instance { get; private set; }

    private readonly HashSet<ScriptableObject> activeEffects = new HashSet<ScriptableObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ActivateEffect(ScriptableObject effect)
    {
        if (effect == null) return;
        activeEffects.Add(effect);
        Debug.Log($"[RunEffectManager] Activado efecto: {effect.name}");
    }

    public bool IsEffectActive(ScriptableObject effect)
    {
        return effect != null && activeEffects.Contains(effect);
    }

    // Método para iterar/consultar los activos sin exponer la colección interna directamente
    public IEnumerable<ScriptableObject> GetActiveEffects()
    {
        foreach (var e in activeEffects) yield return e;
    }
}
