using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;

public class FullAutoReset : MonoBehaviour
{
    [ContextMenu("Reset Game Completely")]
    public void ResetGame()
    {
        // 1. Destruir todos los objetos DontDestroyOnLoad
        DestroyDontDestroyOnLoad();

        // 2. Resetear todos los statics automáticamente
        ResetAllStatics();

        // 3. Recargar la escena activa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DestroyDontDestroyOnLoad()
    {
        // Crear un objeto temporal para forzar Unity a buscar root objects fuera de la escena
        GameObject temp = new GameObject("TempDestroyer");
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.scene.rootCount == 0) // objetos que no pertenecen a ninguna escena activa
            {
                DestroyImmediate(obj);
            }
        }
        DestroyImmediate(temp);
    }

    private void ResetAllStatics()
    {
        // Buscar todas las clases del ensamblado de la ejecución
        Assembly assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                // Ignorar constantes
                if (field.IsLiteral) continue;

                // Reiniciar al valor por defecto de su tipo
                object defaultValue = field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
                field.SetValue(null, defaultValue);
            }
        }
    }
}
