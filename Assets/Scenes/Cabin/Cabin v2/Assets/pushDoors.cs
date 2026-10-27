
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class pushDoors : MonoBehaviour
{
    [SerializeField] private float fuerzaEmpuje = 0.4f;
    [SerializeField] private float velocidadAngularMaxima = 1.5f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb == null || rb.isKinematic)
            return;

        // Solo empujar objetos que sean puertas con bisagra.
        if (!rb.TryGetComponent<HingeJoint>(out HingeJoint bisagra))
            return;

        // Evitar empujar si la puerta ya gira demasiado rápido.
        if (rb.angularVelocity.magnitude >= velocidadAngularMaxima)
            return;

        Vector3 direccion = hit.moveDirection;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        direccion.Normalize();

        rb.AddForceAtPosition(
            direccion * fuerzaEmpuje,
            hit.point,
            ForceMode.Force
        );
    }
}
