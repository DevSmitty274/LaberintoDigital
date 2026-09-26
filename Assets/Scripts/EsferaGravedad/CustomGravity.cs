using UnityEngine;
// Opción simple: multiplicador de gravedad personalizado
public class CustomGravity : MonoBehaviour
{
    public float gravityMultiplier = 3f; // prueba 2-5
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // desactivamos la gravedad automática
    }

    void FixedUpdate()
    {
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }
}