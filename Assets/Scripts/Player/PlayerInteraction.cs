using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

//detecto si algo es interactuable con E
public class PlayerInteraction : MonoBehaviour
{
    //inputs
    [SerializeField] private InputActionReference interactAction;
    [SerializeField] private InputActionReference cancelAction;

    //forma de detectar items cercanos(ver si conviene otra forma segun la camara)
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    //lo que el player esta mirando ahora (null si nada). Lo usa el cartel de interaccion para mostrar el mensaje
    public IInteractable CurrentTarget { get; private set; }

    // estado del menu de interaccion
    public bool IsMenuOpen { get; private set; }

    // eventos para InteractionUI
    public event Action<IInteractable> OnTargetChanged;
    public event Action<IReadOnlyList<InteractionOption>> OnMenuOpened;
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
        if (IsMenuOpen)
            return;

        if (CurrentTarget == null)
            return;

        OpenMenu();
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (IsMenuOpen)
            CloseMenu();
    }

    private void OpenMenu()
    {
        if (!CanInteract() || CurrentTarget == null)
            return;

        IReadOnlyList<InteractionOption> actions = CurrentTarget.GetActions(gameObject);

        if (actions == null || actions.Count == 0)
            return;

        IsMenuOpen = true;
        OnMenuOpened?.Invoke(actions);
    }

    private void CloseMenu()
    {
        if (!IsMenuOpen)
            return;

        IsMenuOpen = false;
        OnMenuClosed?.Invoke();
    }

    public void ExecuteAction(InteractionActionData action)
    {
        if (!IsMenuOpen || CurrentTarget == null || action == null || !CanInteract())
            return;

        IReadOnlyList<InteractionOption> options = CurrentTarget.GetActions(gameObject);

        if (options == null)
            return;

        foreach (InteractionOption option in options)
        {
            if (option == null || option.Data == null || option.Data != action)
                continue;

            if (!option.IsAvailable)
                return;

            if (CurrentTarget.Interact(action, gameObject))
            {
                CloseMenu();
            }

            return;
        }
    }

    //el raycast de siempre, separado para usarlo tanto al apretar E como para el cartel de interaccion
    private IInteractable FindTarget()
    {
        if (cameraPivot == null)
            return null;

        Ray ray = new Ray(cameraPivot.position, cameraPivot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                return interactable;
            }
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

    //para previsualizar el rango, luego se puede quitar cuando tengamos un rango q nos guste
    private void OnDrawGizmosSelected()
    {
        if (cameraPivot == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraPivot.position, cameraPivot.forward * interactRange);
    }
}