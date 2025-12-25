using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunItemData
{
    public ScriptableObject item;
    public int quantity;

    public RunItemData(ScriptableObject item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

public class RunInventory : MonoBehaviour
{
    public static RunInventory Instance { get; private set; }

    // Inventario actual de la run
    private Dictionary<ScriptableObject, RunItemData> inventory =
        new Dictionary<ScriptableObject, RunItemData>();

    private void Awake()
    {
    

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);    // Vive hasta que la run termine
    }

    public void AddItem(ScriptableObject item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (inventory.TryGetValue(item, out RunItemData data))
        {
            data.quantity += amount;
        }
        else
        {
            inventory[item] = new RunItemData(item, amount);
        }

       // Debug.Log($"[RunInventory] Añadido {amount} de {item.name}. Total: {inventory[item].quantity}");
    }

    public bool RemoveItem(ScriptableObject item, int amount = 1)
    {
        if (!inventory.TryGetValue(item, out RunItemData data)) return false;
        if (data.quantity < amount) return false;

        data.quantity -= amount;
        if (data.quantity == 0)
            inventory.Remove(item);

        return true;
    }

    public int GetQuantity(ScriptableObject item)
    {
        return inventory.TryGetValue(item, out RunItemData data) ? data.quantity : 0;
    }

    public IEnumerable<RunItemData> GetAllItems()
    {
        foreach (var i in inventory.Values)
            yield return i;
    }

    // Limpieza automática: se usa cuando empieza una nueva partida/run
    public void ResetInventory()
    {
        inventory.Clear();
       // Debug.Log("[RunInventory] Inventario reseteado para nueva run");
    }
}
