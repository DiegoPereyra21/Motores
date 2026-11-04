using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//detecto si algo es interactuable con E
public class PlayerInteraction : MonoBehaviour
{
    //inputs
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference cancelAction;

    //forma de detectar items cercanos(ver si conviene otra forma segun la camara)
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    //lo que el player esta mirando ahora (null si nada). Lo usa el cartel de interaccion para mostrar el mensaje
    public IInteractable CurrentTarget { get; private set; }

    // estado del menu de interaccion
    public bool IsMenuOpen { get; private set; }

    // eventos para InteractionUI
    public event Action<IInteractable> OnTargetChanged;
    public event Action<InteractionOption> OnUnavailableActionAttempted;
    public event Action<IReadOnlyList<InteractionOption>> OnMenuOpened;
    public event Action<InteractionOption> OnActionExecuted;
    public event Action OnMenuClosed;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateTarget();

        if (IsMenuOpen && !CanInteract())
            CloseMenu();
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPerformed;
        }

        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelPerformed;
        }
    }

    private void OnDisable()
    {
        if (IsMenuOpen)
            CloseMenu();

        // limpiar el objetivo y avisar a la UI para que oculte el cartel
        if (CurrentTarget != null)
        {
            CurrentTarget = null;
            OnTargetChanged?.Invoke(null);
        }

        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
            cancelAction.action.Disable();
        }
    }

    private void UpdateTarget()
    {
        IInteractable newTarget = FindTarget();

        // durante el final solo se puede usar la radio
        GameProgressController progress = GameProgressController.Instance;
        if (progress != null && progress.IsFinaleActive && newTarget is not RadioInteractable)
            newTarget = null;

        // si cambio el objetivo, cerramos el menu anterior
        if (!ReferenceEquals(CurrentTarget, newTarget))
        {
            if (IsMenuOpen)
                CloseMenu();

            CurrentTarget = newTarget;
            OnTargetChanged?.Invoke(CurrentTarget);
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (IsMenuOpen || CurrentTarget == null || !CanInteract())
            return;

        IReadOnlyList<InteractionOption> actions = CurrentTarget.GetActions(gameObject);

        if (actions == null)
            return;

        List<InteractionOption> validActions = GetValidActions(actions);

        if (validActions.Count == 0)
            return;

        // si solo tenemos una accion, la ejecutamos directamente
        if (validActions.Count == 1)
        {
            ExecuteActionInternal(validActions[0].Data);
            return;
        }

        // si tenemos varias acciones se abre el menu
        OpenMenu(validActions);
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (IsMenuOpen)
            CloseMenu();
    }

    private void OpenMenu(IReadOnlyList<InteractionOption> actions)
    {
        if (!CanInteract() || CurrentTarget == null || actions == null || actions.Count == 0)
            return;

        IsMenuOpen = true;

        if (cameraPivot != null)
            cameraPivot.SetLookEnabled(false);

        OnMenuOpened?.Invoke(actions);
    }

    private void CloseMenu()
    {
        if (!IsMenuOpen)
            return;

        IsMenuOpen = false;

        if (cameraPivot != null)
            cameraPivot.SetLookEnabled(true);

        OnMenuClosed?.Invoke();
    }

    public void ExecuteAction(InteractionActionData action)
    {
        if (!IsMenuOpen)
            return;

        ExecuteActionInternal(action);
    }

    //el raycast de siempre, separado para usarlo tanto al apretar E como para el cartel de interaccion
    private IInteractable FindTarget()
    {
        if (cameraPivot == null)
            return null;

        Ray ray = new Ray(cameraPivot.transform.position, cameraPivot.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanOpenInteraction(gameObject))
                return interactable;
        }

        return null;
    }

    // condensamos la logica para validar si el jugador puede o no interactuar en ese momento
    private bool CanInteract()
    {
        GameProgressController progress = GameProgressController.Instance;

        if (progress != null && (progress.IsGameEnded || progress.IsWorldPaused))
        {
            return false;
        }

        if (playerController != null && (playerController.IsBusy || playerController.IsStunned))
        {
            return false;
        }

        return true;
    }

    // logica compartida para acciones directas y acciones del menu
    private void ExecuteActionInternal(InteractionActionData action)
    {
        if (CurrentTarget == null || action == null || !CanInteract())
            return;

        IReadOnlyList<InteractionOption> options = CurrentTarget.GetActions(gameObject);

        if (options == null)
            return;

        foreach (InteractionOption option in options)
        {
            if (option == null || option.Data == null || option.Data != action)
                continue;

            if (!option.IsAvailable)
            {
                OnUnavailableActionAttempted?.Invoke(option);
                return;
            }

            if (CurrentTarget.Interact(action, gameObject))
            {
                OnActionExecuted?.Invoke(option);
                CloseMenu();
            }

            return;
        }
    }

    // devuelve las opciones validas, ignorando referencias nulas
    private List<InteractionOption> GetValidActions(IReadOnlyList<InteractionOption> actions)
    {
        List<InteractionOption> validActions = new List<InteractionOption>();

        foreach (InteractionOption option in actions)
        {
            if (option != null && option.Data != null)
                validActions.Add(option);
        }

        return validActions;
    }

    // texto que muestra el prompt segun el interactuable
    public string GetPromptText()
    {
        if (CurrentTarget == null)
            return "";

        IReadOnlyList<InteractionOption> actions = CurrentTarget.GetActions(gameObject);

        if (actions == null)
            return "";

        List<InteractionOption> validActions = GetValidActions(actions);

        if (validActions.Count == 0)
            return "";

        if (validActions.Count == 1)
            return validActions[0].DisplayName;

        return "Interactuar";
    }

    //para previsualizar el rango, luego se puede quitar cuando tengamos un rango q nos guste
    private void OnDrawGizmosSelected()
    {
        if (cameraPivot == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraPivot.transform.position, cameraPivot.transform.forward * interactRange);
    }
}