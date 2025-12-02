using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;                     // panel principal
    public GameObject itemLinePrefab;            // Prefab que muestra nombre/icono/cantidad
    public Transform contentParent;              // vertical layout

    [Header("Options")]
    public bool startVisible = false;
    public bool pauseOnOpen = true;
    public bool showCursorOnOpen = true;

    private readonly List<GameObject> spawnedLines = new List<GameObject>();

    private void Awake()
    {
        if (panel == null || itemLinePrefab == null || contentParent == null)
        {
            Debug.LogWarning("[InventoryPanelController] Falta asignar panel / itemLinePrefab / contentParent.");
            enabled = false;
            return;
        }

        panel.SetActive(startVisible);
        ApplyGameState(startVisible);
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool newState = !panel.activeSelf;
            panel.SetActive(newState);

            ApplyGameState(newState);

            if (newState)
            {
              
                RefreshUI();
            }
        }
    }

    private void ApplyGameState(bool isPanelOpen)
    {
        if (pauseOnOpen) Time.timeScale = isPanelOpen ? 0f : 1f;

        if (showCursorOnOpen)
        {
            Cursor.visible = isPanelOpen;
            Cursor.lockState = isPanelOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    public void RefreshUI()
    {
        Clear();

        foreach (RunItemData data in RunInventory.Instance.GetAllItems())
        {
            IObjetos item = data.item as IObjetos;
            if (item == null) continue;

            // Crear linea UI
            GameObject go = Instantiate(itemLinePrefab, contentParent);
            go.name = "ItemLine_" + item.name;

            // Asignar datos
            InventoryItemUI ui = go.GetComponent<InventoryItemUI>();
            ui.Setup(item.icon, item.name, data.quantity);

            go.SetActive(true);

            spawnedLines.Add(go);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void Clear()
    {
        for (int i = spawnedLines.Count - 1; i >= 0; i--)
        {
            if (spawnedLines[i] != null) Destroy(spawnedLines[i]);
        }
        spawnedLines.Clear();
    }
}
