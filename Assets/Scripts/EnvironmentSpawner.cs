using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("Configuración de Instancias")]
    public GameObject environmentPrefab;
    public int numberOfInstances = 12;

    [Header("Desplazamiento Espacial")]
    public float offsetZ = 150f;

    // Este atributo crea un botón en el menú del script en Unity
    [ContextMenu("Generar Circuitos Permanentemente")]
    public void GenerarEntornosEditor()
    {
#if UNITY_EDITOR
        if (environmentPrefab == null)
        {
            Debug.LogError("[Spawner] Error Crítico: Prefab no asignado en el Inspector.");
            return;
        }

        // Generación del grid en línea recta
        for (int i = 0; i < numberOfInstances; i++)
        {
            Vector3 spawnPosition = new Vector3(0f, 0f, i * offsetZ);

            // Instanciamos manteniendo la conexión directa con el Prefab original
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(environmentPrefab);

            // Asignamos las coordenadas
            instance.transform.position = spawnPosition;
            instance.transform.rotation = Quaternion.identity;

            // Empaquetamos dentro de este objeto controlador para no ensuciar la Hierarchy
            instance.transform.parent = this.transform;
            instance.name = $"Circuito_Instancia_{i}";
        }

        Debug.Log($"[Arquitectura] Despliegue permanente completado: {numberOfInstances} entornos generados.");
#else
        Debug.LogWarning("Esta función solo opera dentro del Editor de Unity.");
#endif
    }
}