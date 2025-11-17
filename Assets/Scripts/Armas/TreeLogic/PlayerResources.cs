using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Currency")]
    private int currency;
    public delegate void CurrencyChanged(int newAmount);


    [Tooltip("Si está true, currency se resetea automáticamente en Start(). Útil si cada run recarga la escena.")]
    public bool resetCurrencyOnStart = true;

    // simple inventario: id -> cantidad
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // Efectos activos: id -> true
    private HashSet<string> activeEffects = new HashSet<string>();


    private void Start()
    {

    }
    private void Update()
    {
        currency = StatsManager.Instance.RuntimeStats.currency;
    }
    private void Awake()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            Debug.Log("Suscribiendo HUD a StatsManager: " + StatsManager.Instance);


            // Asegurar que el HUD muestra el valor actual aunque el evento ya se haya disparado antes
            OnCurrencyChanged(StatsManager.Instance.RuntimeStats.currency);
        }
    }
    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Esperar hasta que StatsManager exista
        while (StatsManager.Instance == null)
            yield return null;

        StatsManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        OnCurrencyChanged(StatsManager.Instance.RuntimeStats.currency);
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    private void OnDestroy()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
    }

    public void OnCurrencyChanged(int newAmount)
    {
        currency = newAmount;
    }

    // --- Resto de utilidades ---
    public bool HasCurrency(int amount) => StatsManager.Instance.RuntimeStats.currency >= amount;
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[PlayerResources] SpendCurrency called with non-positive amount: {amount}");
            return false;
        }

        if (StatsManager.Instance == null)
        {
            Debug.LogWarning("[PlayerResources] SpendCurrency: StatsManager.Instance es NULL");
            return false;
        }

        int before = StatsManager.Instance.RuntimeStats.currency;
        if (before < amount)
        {
            Debug.LogWarning($"[PlayerResources] SpendCurrency: no hay suficiente currency. Antes: {before}, intento gastar: {amount}");
            return false;
        }

        Debug.Log($"[PlayerResources] SpendCurrency -> antes: {before}, gastando: {amount}. StatsManager instance: {StatsManager.Instance}");
        StatsManager.Instance.AddCurrency(-amount);
        int after = StatsManager.Instance.RuntimeStats.currency;
        Debug.Log($"[PlayerResources] SpendCurrency -> después: {after}");

        return true;
    }


    public void AddItem(string id, int qty = 1)
    {
        if (!items.ContainsKey(id))
            items[id] = 0;
        items[id] += qty;
    }

    public bool HasItems(List<string> ids)
    {
        foreach (var id in ids)
            if (!items.ContainsKey(id) || items[id] <= 0)
                return false;
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
            if (!HasEffect(id))
                return false;
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
