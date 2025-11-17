using System.Collections;
using TMPro;
using UnityEngine;

public class PickupEffectHelper : MonoBehaviour
{
    public static PickupEffectHelper Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void ShowEffectCoroutine(ScriptableObject[] effectList, ScriptableObject effectToActivate,
                                    TextMeshProUGUI effectTextUI, GameObject textPanel, float displayDuration, float interval)
    {
        StartCoroutine(ShowEffectNamesAndActivate(effectList, effectToActivate, effectTextUI, textPanel, displayDuration, interval));
    }

    private IEnumerator ShowEffectNamesAndActivate(ScriptableObject[] effectList, ScriptableObject effectToActivate,
                                                    TextMeshProUGUI effectTextUI, GameObject textPanel, float displayDuration, float interval)
    {
        float elapsed = 0f;
        int index = Random.Range(0, effectList.Length);

        while (elapsed < displayDuration)
        {
            textPanel.SetActive(true);
            effectTextUI.text = effectList[index].name;
            index = (index + 1) % effectList.Length;
            yield return new WaitForSecondsRealtime(interval);
            elapsed += interval;
        }

        textPanel.SetActive(false);
        RunEffectManager.Instance.ActivateEffect(effectToActivate);
    }
}
