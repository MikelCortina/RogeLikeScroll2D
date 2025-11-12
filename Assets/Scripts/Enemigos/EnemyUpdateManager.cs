using System.Collections.Generic;
using UnityEngine;

public class EnemyUpdateManager : MonoBehaviour
{
    private static readonly List<EnemyBase> enemies = new();

    public static void Register(EnemyBase e) => enemies.Add(e);
    public static void Unregister(EnemyBase e) => enemies.Remove(e);

    private void Update()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (i % 3 != Time.frameCount % 3) continue; // actualiza 1/3 por frame
            enemies[i].Tick(); // método más ligero en vez de Update()
        }
    }
}
