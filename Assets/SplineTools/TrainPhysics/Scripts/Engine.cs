using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Engine : MonoBehaviour
{
    private Rigidbody rb;

    [SerializeField] private float accelerateForce = 30f;
    [SerializeField] private float brakeForce = 40f;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            Throttle(accelerateForce);
        }

        if (Input.GetKey(KeyCode.S))
        {
            Throttle(-brakeForce);
        }
    }

    private void Throttle(float power)
    {
        Vector3 dir = power * transform.forward;
        rb.AddForce(dir);
    }
}
