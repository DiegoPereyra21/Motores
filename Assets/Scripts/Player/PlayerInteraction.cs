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

    //lo que el player esta mirando ahora (null si nada). Lo usa el cartel de interaccion para mostrar el mensaje
    public IInteractable CurrentTarget { get; private set; }

    private void Update()
    {
        CurrentTarget = FindTarget();
    }

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
        IInteractable interactable = FindTarget();

        if (interactable != null)
        {
            interactable.Interact(gameObject);
        }
    }

    //el raycast de siempre, separado para usarlo tanto al apretar E como para el cartel de interaccion
    private IInteractable FindTarget()
    {
        if (cameraPivot == null) return null;

        Ray ray = new Ray(cameraPivot.position, cameraPivot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            return hit.collider.GetComponentInParent<IInteractable>();

        return null;
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