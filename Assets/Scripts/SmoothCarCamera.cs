using UnityEngine;

public class SmoothCarCamera : MonoBehaviour
{
    public Transform carTarget;
    public float distance = 6.0f;

    public float height = 2.0f;

    public float damping = 5.0f;
    public float rotationDamping = 5.0f;

    void FixedUpdate()
    {
        if(carTarget == null)
        {
            return;
        }

        Vector3 desiredPos = carTarget.position - (carTarget.forward * distance) + (Vector3.up * height);
        transform.position = Vector3.Lerp(transform.position, desiredPos, damping*Time.deltaTime);


        Quaternion desiredRotation = Quaternion.LookRotation(carTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }
}
