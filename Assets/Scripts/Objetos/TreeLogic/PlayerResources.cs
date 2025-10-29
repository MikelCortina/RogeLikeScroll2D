using UnityEngine;
using System.Collections.Generic;

public class PlayerResources : MonoBehaviour
{
    [Header("Currency")]
    public int currency = 0;

    // Mantengo el delegate original para compatibilidad
    public delegate void CurrencyChanged(int newAmount);
    public event CurrencyChanged OnCurrencyChanged;

    [Tooltip("Si está true, currency se resetea automáticamente en Start(). Útil si cada run recarga la escena.")]
    public bool resetCurrencyOnStart = true;

    // simple inventario: id -> cantidad
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Efectos activos: id -> true
    private HashSet<string> activeEffects = new HashSet<string>();

    private void Awake()
    {
        // Asegura que currency tiene un valor consistente al Awake
        // (no dispararemos evento aquí para evitar "doble evento" en algunos flujos,
        // el evento se dispara en ResetCurrency/Start cuando corresponda).
    }

    private void Start()
    {
        if (resetCurrencyOnStart)
            ResetCurrency();
    }

    // Añade currency y notifica
    public void AddCurrency(int amount)
    {
        if (amount == 0) return;
        currency += amount;
        Debug.Log($"[PlayerResources] AddCurrency: {amount} -> {currency}");
        OnCurrencyChanged?.Invoke(currency);
    }

    // Resta currency (si lo deseas, puedes añadir comprobación HasCurrency antes)
    public void SpendCurrency(int amount)
    {
        currency -= amount;
        currency = Mathf.Max(0, currency);
        OnCurrencyChanged?.Invoke(currency);
    }

    // Resetea currency y notifica (IMPORTANTE para que el HUD se actualice al iniciar)
    public void ResetCurrency()
    {
        currency = 0;
        Debug.Log("[PlayerResources] Currency reseteada a 0");
        OnCurrencyChanged?.Invoke(currency);
    }

    /// <summary>
    /// Método público pensado para llamar al iniciar una nueva run.
    /// Actualmente solo resetea currency (como pediste).
    /// </summary>
    public void ResetForNewRun()
    {
        ResetCurrency();
        // items.Clear();
        // activeEffects.Clear();
    }

    [ContextMenu("Reset Currency (editor)")]
    private void ContextResetCurrency()
    {
        ResetCurrency();
    }

    // --- Resto de utilidades ---
    public bool HasCurrency(int amount) => currency >= amount;

    public void AddItem(string id, int qty = 1)
    {
        if (string.IsNullOrEmpty(id) || qty <= 0) return;
        if (!items.ContainsKey(id)) items[id] = 0;
        items[id] += qty;
    }

    public int GetItemCount(string id) => items.ContainsKey(id) ? items[id] : 0;

    // --- Métodos para efectos ---
    public void AddEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return;
        activeEffects.Add(effectId);
        Debug.Log($"[PlayerResources] Effect added: {effectId}");
    }

    public bool HasEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return false;
        return activeEffects.Contains(effectId);
    }

    public void RemoveEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return;
        if (activeEffects.Remove(effectId))
            Debug.Log($"[PlayerResources] Effect removed: {effectId}");
    }
}
