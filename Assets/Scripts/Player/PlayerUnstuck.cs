using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUnstuck : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController controller;
    [SerializeField] private HUDController hud;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference unstuckAction;

    [Header("Destinos (gana el mas cercano, poner el spawn tambien)")]
    [SerializeField] private Transform[] safePoints;

    [Header("Reglas anti abuso")]
    [SerializeField] private float stuckSeconds = 4f;//tiempo intentando moverse sin avanzar para habilitar la tecla
    [SerializeField] private float minSpeed = 0.1f;//por debajo de esto se considera que no avanza
    [SerializeField] private float cooldown = 60f;//espera entre usos
    [SerializeField] private float enemyBlockRadius = 6f;//con un enemigo mas cerca que esto no se puede usar
    [SerializeField] private float hintRepeatSeconds = 5f;//cada cuanto se repite el aviso mientras siga atascado

    private readonly Collider[] enemyBuffer = new Collider[16];
    private Vector3 lastPosition;
    private float stuckTimer;
    private float cooldownEnd;
    private float nextHintTime;

    private void Awake()
    {
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!controller) controller = GetComponent<CharacterController>();
        if (!hud) hud = FindFirstObjectByType<HUDController>();
    }

    private void OnEnable()
    {
        unstuckAction.action.Enable();
        lastPosition = transform.position;
    }

    private void OnDisable()
    {
        unstuckAction.action.Disable();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || !CanTrack())
        {
            ResetTracking();
            return;
        }

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        float speed = delta.magnitude / Time.deltaTime;
        lastPosition = transform.position;

        bool tryingToMove = moveAction.action.ReadValue<Vector2>().sqrMagnitude > 0.01f;

        if (tryingToMove && speed < minSpeed)
            stuckTimer += Time.deltaTime;
        else
        {
            stuckTimer = 0f;
            nextHintTime = 0f;
        }

        bool available = stuckTimer >= stuckSeconds && Time.time >= cooldownEnd && !EnemyNearby();
        if (!available) return;

        if (hud != null && Time.time >= nextHintTime)
        {
            hud.ShowInteractionFeedbackMessage("ATASCADO?", "Presiona R para destrabarte.");
            nextHintTime = Time.time + hintRepeatSeconds;
        }

        if (unstuckAction.action.WasPressedThisFrame())
            Teleport();
    }

    //solo se mide el atasco cuando el player podria moverse libremente
    private bool CanTrack()
    {
        if (!playerController.enabled || playerController.MovementLocked || playerController.IsStunned || playerController.IsBusy)
            return false;

        GameProgressController progress = GameProgressController.Instance;
        if (progress != null && (progress.IsWorldPaused || progress.IsGameEnded || progress.IsFinaleActive || progress.IsFinaleLocked))
            return false;

        return true;
    }

    private bool EnemyNearby()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, enemyBlockRadius, enemyBuffer);
        for (int i = 0; i < count; i++)
            if (enemyBuffer[i].CompareTag("Enemy")) return true;

        return false;
    }

    private void Teleport()
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Transform p in safePoints)
        {
            if (p == null) continue;
            float d = (p.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = p; }
        }

        if (best == null) return;

        controller.enabled = false;
        transform.position = best.position;
        controller.enabled = true;

        cooldownEnd = Time.time + cooldown;
        ResetTracking();
    }

    private void ResetTracking()
    {
        stuckTimer = 0f;
        nextHintTime = 0f;
        lastPosition = transform.position;
    }
}