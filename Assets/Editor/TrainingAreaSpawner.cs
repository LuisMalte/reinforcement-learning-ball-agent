using UnityEngine;
using UnityEditor;

public class TrainingAreaSpawner : EditorWindow
{
    GameObject areaPrefab;
    int cantidad = 20;
    int cols = 5;
    float spacingX = 50f;
    float spacingZ = 55f;
    string parentName = "TrainingAreas";

    [MenuItem("ML-Agents/Spawner de Áreas")]
    public static void ShowWindow() => GetWindow<TrainingAreaSpawner>("Spawner de Áreas");

    void OnGUI()
    {
        GUILayout.Label("Configuración", EditorStyles.boldLabel);

        areaPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab (TrainingEnvironment2)", areaPrefab, typeof(GameObject), false);

        cantidad   = EditorGUILayout.IntField("Cantidad total", cantidad);
        cols       = EditorGUILayout.IntField("Columnas",       cols);
        spacingX   = EditorGUILayout.FloatField("Espacio X",    spacingX);
        spacingZ   = EditorGUILayout.FloatField("Espacio Z",    spacingZ);

        int rows = Mathf.CeilToInt((float)cantidad / cols);
        EditorGUILayout.HelpBox(
            $"Se crearán {cantidad} áreas en {rows} filas x {cols} columnas",
            MessageType.Info);

        EditorGUILayout.Space(10);

        GUI.enabled = areaPrefab != null;
        if (GUILayout.Button("✦ Generar áreas", GUILayout.Height(35)))
            Spawn();
        GUI.enabled = true;

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Borrar todas", GUILayout.Height(25)))
            DeleteAll();
    }

    void Spawn()
    {
        DeleteAll();

        GameObject parent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parent, "Spawn áreas");

        int count = 0;
        int rows = Mathf.CeilToInt((float)cantidad / cols);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (count >= cantidad) break;

                Vector3 pos = new Vector3(c * spacingX, 0f, r * spacingZ);
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(areaPrefab);
                inst.transform.position = pos;
                inst.transform.SetParent(parent.transform);
                inst.name = $"Area_{count:00}";
                Undo.RegisterCreatedObjectUndo(inst, "Spawn área");
                count++;
            }
        }

        Selection.activeGameObject = parent;
        Debug.Log($"[Spawner] {count} áreas generadas.");
    }

    void DeleteAll()
    {
        var existing = GameObject.Find(parentName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }
}
