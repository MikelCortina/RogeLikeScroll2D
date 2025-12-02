using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlphaTester : MonoBehaviour
{
    void OnGUI()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}