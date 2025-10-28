using UnityEngine;
using System.Collections.Generic;

public class PlayerResources : MonoBehaviour
{
    public int currency = 0;
    // simple inventario: id -> cantidad
    private Dictionary<string, int> items = new Dictionary<string, int>();

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

    // debugging
    public int GetItemCount(string id) => items.ContainsKey(id) ? items[id] : 0;
}
