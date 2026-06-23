using UnityEngine;

public class TestMovement : MonoBehaviour
{
    void Start()
    {
        Debug.Log("✅ Script cargado en: " + gameObject.name);
    }

    void Update()
    {
        // Mover SIN rigidbody, solo transform directo
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(x, 0, z) * Time.deltaTime * 5f);

        if (Input.GetKeyDown(KeyCode.W)) Debug.Log("W presionada");
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log("A presionada");
        if (Input.GetKeyDown(KeyCode.S)) Debug.Log("S presionada");
        if (Input.GetKeyDown(KeyCode.D)) Debug.Log("D presionada");
    }
}
