using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI; // LayoutRebuilder

[RequireComponent(typeof(RectTransform))]
public class TooltipView : MonoBehaviour
{
    [Header("UI Elements")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Prerequisites list")]
    public RectTransform prereqContainer;      // contenedor vertical dentro del tooltip
    public GameObject requiredNodeEntryPrefab; // prefab RequiredNodeEntry (Image + TMP)

    [Header("Optional")]
    public CanvasGroup canvasGroup;

    // keep track to destroy entries later
    private List<GameObject> spawnedEntries = new List<GameObject>();

    public void SetData(ItemNode node, SkillTreeUI treeUI)
    {
        // --- datos principales ---
        if (iconImage != null) iconImage.sprite = node?.icon;
        if (titleText != null) titleText.text = node != null ? (string.IsNullOrEmpty(node.displayName) ? node.nodeId : node.displayName) : "";
        if (descriptionText != null) descriptionText.text = node != null ? node.description ?? "" : "";

        // <-- COSTE: usar costes runtime si treeUI está disponible, si no fallback a node.cost o vacío -->
        int effectiveCost = 0;
        if (node != null)
        {
            if (treeUI != null)
                effectiveCost = Mathf.Max(0, treeUI.GetNodeEffectiveCost(node));
            else
                effectiveCost = node.cost > 0 ? node.cost : 0;
        }
        if (costText != null) costText.text = effectiveCost > 0 ? $"Cost: {effectiveCost}" : "";

        // --- limpiar entradas previas ---
        foreach (var go in spawnedEntries) if (go != null) Destroy(go);
        spawnedEntries.Clear();

        if (prereqContainer != null)
        {
            for (int i = prereqContainer.childCount - 1; i >= 0; i--)
            {
                var c = prereqContainer.GetChild(i);
                if (c != null) Destroy(c.gameObject);
            }
        }

        if (node == null || node.prerequisiteNodeIds == null || node.prerequisiteNodeIds.Count == 0)
        {
            if (prereqContainer != null) prereqContainer.gameObject.SetActive(false);
            Debug.Log("[TooltipView] No prerequisites to show.");
            return;
        }

        if (prereqContainer != null)
            prereqContainer.gameObject.SetActive(true);

        Debug.Log($"[TooltipView] Creating {node.prerequisiteNodeIds.Count} prereq entries for node '{node.nodeId}'");

        var layout = prereqContainer != null ? prereqContainer.GetComponent<UnityEngine.UI.LayoutGroup>() : null;
        float fallbackHeight = 28f;
        float spacing = 2f;
        if (layout is UnityEngine.UI.VerticalLayoutGroup vlg) spacing = vlg.spacing;
        else if (layout is UnityEngine.UI.GridLayoutGroup glg) spacing = glg.spacing.y;

        int created = 0;
        foreach (var reqId in node.prerequisiteNodeIds)
        {
            GameObject inst = null;

            if (requiredNodeEntryPrefab == null || prereqContainer == null)
            {
                inst = new GameObject("prereq_" + reqId, typeof(RectTransform));
                inst.transform.SetParent(prereqContainer, false);
                var txt = inst.AddComponent<TextMeshProUGUI>();
                txt.text = reqId;
                txt.raycastTarget = false;
                spawnedEntries.Add(inst);
                created++;
                Debug.LogWarning($"[TooltipView] requiredNodeEntryPrefab or prereqContainer is null. Created fallback text for {reqId}");
                continue;
            }

            inst = Instantiate(requiredNodeEntryPrefab, prereqContainer, false);
            if (inst == null)
            {
                Debug.LogError($"[TooltipView] Instantiate returned null for prefab while creating prereq {reqId}");
                continue;
            }

            inst.name = "prereq_" + reqId;
            inst.SetActive(true);
            inst.transform.SetParent(prereqContainer, false);
            inst.transform.localScale = Vector3.one;

            var rt = inst.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                if (rt.sizeDelta.y <= 0f || rt.sizeDelta.x <= 0f) rt.sizeDelta = new Vector2(rt.sizeDelta.x <= 0f ? 100f : rt.sizeDelta.x, fallbackHeight);
            }

            var le = inst.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null) le = inst.AddComponent<UnityEngine.UI.LayoutElement>();
            if (le != null && le.preferredHeight <= 0f) le.preferredHeight = fallbackHeight;

            var icon = inst.transform.Find("Icon")?.GetComponent<Image>();

            TextMeshProUGUI chosenTMP = null;
            var nameTransform = inst.transform.Find("Name");
            if (nameTransform != null) chosenTMP = nameTransform.GetComponent<TextMeshProUGUI>();

            if (chosenTMP == null)
            {
                var tmps = inst.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmps != null && tmps.Length > 0)
                {
                    for (int i = 0; i < tmps.Length; i++)
                    {
                        var lower = tmps[i].transform.name.ToLower();
                        if (lower.Contains("name") || lower.Contains("label") || lower.Contains("title"))
                        {
                            chosenTMP = tmps[i];
                            break;
                        }
                    }
                    if (chosenTMP == null) chosenTMP = tmps[0];
                }
            }

            if (chosenTMP == null)
            {
                var go = new GameObject("Name", typeof(RectTransform));
                go.transform.SetParent(inst.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.raycastTarget = false;
                chosenTMP = txt;
                Debug.LogWarning($"[TooltipView] No TextMeshProUGUI found in prefab; created fallback TMP for {reqId}");
            }

            var allTmpsInInst = inst.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (allTmpsInInst != null && allTmpsInInst.Length > 1)
            {
                for (int i = 0; i < allTmpsInInst.Length; i++)
                {
                    if (allTmpsInInst[i] == chosenTMP) continue;
                    try
                    {
                        allTmpsInInst[i].text = "";
                        allTmpsInInst[i].gameObject.SetActive(false);
                    }
                    catch { }
                }
                Debug.Log($"[TooltipView] Cleared {allTmpsInInst.Length - 1} extra TMP(s) in instance '{inst.name}' to avoid duplicates.");
            }

            ItemNode reqNode = FindItemNodeById(reqId, treeUI);
            if (reqNode != null)
            {
                if (icon != null) { icon.enabled = true; icon.sprite = reqNode.icon; }
                string display = string.IsNullOrEmpty(reqNode.displayName) ? reqNode.nodeId : reqNode.displayName;

                // obtener coste efectivo del requisito también
                int reqCost = 0;
                if (treeUI != null) reqCost = Mathf.Max(0, treeUI.GetNodeEffectiveCost(reqNode));
                else reqCost = reqNode.cost > 0 ? reqNode.cost : 0;

                if (reqCost > 0) display += $" (Cost: {reqCost})";

                if (chosenTMP != null) chosenTMP.text = display;

                bool unlocked = treeUI != null && treeUI.IsUnlocked(reqNode.nodeId);
                var cg = chosenTMP.GetComponent<CanvasGroup>();
                if (unlocked)
                {
                    if (cg != null) cg.alpha = 1f;
                }
                else
                {
                    if (cg != null) cg.alpha = 0.6f;
                }
            }
            else
            {
                if (icon != null) icon.enabled = false;
                if (chosenTMP != null) chosenTMP.text = reqId;
            }

            inst.transform.SetSiblingIndex(created);
            spawnedEntries.Add(inst);
            created++;

            Debug.Log($"[TooltipView] Instantiated prereq '{reqId}' -> instanceName='{inst.name}' childCountNow={prereqContainer.childCount}");
        }

        if (prereqContainer != null)
        {
            if (layout != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(prereqContainer);
                Debug.Log("[TooltipView] Forced LayoutRebuilder (LayoutGroup present).");
            }
            else
            {
                Debug.Log("[TooltipView] No LayoutGroup found. Applying manual fallback positioning.");
                float yOffset = 0f;
                for (int i = 0; i < prereqContainer.childCount; i++)
                {
                    var child = prereqContainer.GetChild(i);
                    if (child == null) continue;
                    var rtChild = child.GetComponent<RectTransform>();
                    if (rtChild == null) continue;
                    rtChild.anchoredPosition = new Vector2(rtChild.anchoredPosition.x, -yOffset);
                    float h = rtChild.sizeDelta.y > 0f ? rtChild.sizeDelta.y : fallbackHeight;
                    yOffset += h + spacing;
                }
                var rtCont = prereqContainer.GetComponent<RectTransform>();
                if (rtCont != null) rtCont.sizeDelta = new Vector2(rtCont.sizeDelta.x, yOffset);
            }
        }

        if (prereqContainer != null)
        {
            Debug.Log($"[TooltipView] Final prereqContainer childCount = {prereqContainer.childCount}");
            for (int i = 0; i < prereqContainer.childCount; i++)
            {
                var child = prereqContainer.GetChild(i);
                if (child == null) continue;
                var rt = child.GetComponent<RectTransform>();
                string pos = rt != null ? $"anchored={rt.anchoredPosition} size={rt.sizeDelta} scale={child.localScale}" : "no-rect";
                Debug.Log($"[TooltipView] child[{i}] name={child.name} active={child.gameObject.activeSelf} {pos}");
            }
        }
    }


    // intenta resolver ItemNode desde treeUI.skillFamilies (busca por nodeId)
    private ItemNode FindItemNodeById(string id, SkillTreeUI treeUI)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (treeUI == null || treeUI.skillFamilies == null) return null;

        foreach (var fam in treeUI.skillFamilies)
        {
            if (fam == null || fam.nodes == null) continue;
            foreach (var n in fam.nodes)
            {
                if (n != null && n.nodeId == id) return n;
            }
        }
        return null;
    }
}
