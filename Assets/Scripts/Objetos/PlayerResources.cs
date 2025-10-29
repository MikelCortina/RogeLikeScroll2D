using UnityEngine;
using System.Collections.Generic;

public class PlayerResources : MonoBehaviour
{
    public int currency = 0;
    // simple inventario: id -> cantidad
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Efectos activos: id -> true
    private HashSet<string> activeEffects = new HashSet<string>();

    public bool HasCurrency(int amount) => currency >= amount;
    public void SpendCurrency(int amount) => currency -= amount;

    public void AddItem(string id, int qty = 1)
    {
        if (!items.ContainsKey(id)) items[id] = 0;
        items[id] += qty;
    }
    public bool HasItems(List<string> ids)
    {
        foreach (var id in ids)
            if (!items.ContainsKey(id) || items[id] <= 0) return false;
        return true;
    }
    public void ConsumeItems(List<string> ids)
    {
        foreach (var id in ids)
        {
            if (items.ContainsKey(id))
            {
                items[id] = Mathf.Max(0, items[id] - 1);
            }
        }
    }

    // --- Métodos para efectos ---
    // Registrar un efecto activo (ej: al aplicar un efecto persistente)
    public void AddEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return;
        activeEffects.Add(effectId);
        Debug.Log($"[PlayerResources] Effect added: {effectId}");
    }

    // Comprobar si tiene un efecto activo
    public bool HasEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return false;
        return activeEffects.Contains(effectId);
    }

    // Comprobar varias effects (todas deben estar presentes)
    public bool HasEffects(List<string> effectIds)
    {
        if (effectIds == null || effectIds.Count == 0) return true;
        foreach (var id in effectIds)
            if (!HasEffect(id)) return false;
        return true;
    }

    // Quitar un efecto (borrar del inventario de efectos)
    public void RemoveEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return;
        if (activeEffects.Remove(effectId))
            Debug.Log($"[PlayerResources] Effect removed: {effectId}");
    }

    // Quitar varios efectos
    public void RemoveEffects(List<string> effectIds)
    {
        if (effectIds == null) return;
        foreach (var id in effectIds)
            RemoveEffect(id);
    }

    // debugging
    public int GetItemCount(string id) => items.ContainsKey(id) ? items[id] : 0;

    // debugging efectos
    public bool IsEffectActive(string id) => HasEffect(id);
}
