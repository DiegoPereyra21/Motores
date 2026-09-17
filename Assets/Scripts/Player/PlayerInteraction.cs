using UnityEngine;
using UnityEngine.InputSystem;

//detecto si algo es interactuable con E
public class PlayerInteraction : MonoBehaviour
{
    //inputs
    [SerializeField] private InputActionReference interactAction;

    //forma de detectar items cercanos(ver si conviene otra forma segun la camara)
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    private void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();
    }
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    //raycast desde la camara para internat interactuar
    private void TryInteract()
    {
        Ray ray = new Ray(cameraPivot.position, cameraPivot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(gameObject);
            }
        }
    }
    //para previsualizar el rango, luego se puede quitar cuando tengamos un rango q nos guste
    private void OnDrawGizmosSelected()
    {
        if (cameraPivot == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraPivot.position, cameraPivot.forward * interactRange);
    }
}