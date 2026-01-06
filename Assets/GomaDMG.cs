using UnityEngine;

public class GomaDMG : MonoBehaviour
{
   

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("enemigo"))
        {
            EnemyBase enemy = collision.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {

                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg, false);
               // ConsoleManager.Instance.Log($"Rubber-coated bullets damage dealed: {dmg}");
                Debug.Log($"Rubber-coated bullets damage dealed: {dmg}");
            }
        }
    }
}
