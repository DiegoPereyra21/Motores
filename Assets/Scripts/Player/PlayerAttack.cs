using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    //input(ver si seguir usando el mismo o crear uno de 0)
    [SerializeField] private InputActionReference attackAction;
    //referencias
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Health health;
    //config del golpe
    [SerializeField] private Transform attackPoint;//en el centro del arma o en la punta, ya verse
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private bool faceCameraOnAttack = true;//hace demasiado cambio, preguntar a furia luego
    //privadas
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly System.Collections.Generic.HashSet<IDamageable> alreadyHit = new();
    //publicas
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        //x si no se
        if (!animator) animator = GetComponent<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!health) health = GetComponent<Health>();
    }
    private void OnEnable() => attackAction.action.Enable();

    private void OnDisable()
    {
        attackAction.action.Disable();
        if (playerController) playerController.MovementLocked = false;
    }

    private void Update()
    {
        if (health && health.IsDead) return;//si esta muerto ni intenta revisar si ataco on no

        if (attackAction.action.WasPressedThisFrame() && !IsAttacking)
            StartAttack();
    }
    private void StartAttack()
    {
        IsAttacking = true;

        if (faceCameraOnAttack && cameraPivot)//quitar facecameraonattack si es q queremos q ataque hacia donde este el cuerpo
            transform.rotation = Quaternion.Euler(0f, cameraPivot.eulerAngles.y, 0f);

        if (playerController)
            playerController.MovementLocked = true;//para q no se deslice mientras ataca, osea se quede quieto mientras ataca

        animator.SetTrigger(AttackHash);
    }
    //en el frame donde golpea en la animacion, hace esto. si es clip de mixamo hay q hacerlo desde el fbx
    public void AttackHit()
    {
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Collider[] hits = Physics.OverlapSphere(origin, attackRange, ~0, QueryTriggerInteraction.Ignore);
        alreadyHit.Clear();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();//clasica verificacion si es q tiene la interfaz de idamageable
            if (damageable == null || !alreadyHit.Add(damageable)) continue;

            damageable.TakeDamage(attackDamage);
            Debug.Log($"HICISTE {attackDamage} DE DAÑO");
        }
    }
    //para q no tenga q hacerlo a ojo
    public void AttackEnd()
    {
        IsAttacking = false;
        if (playerController) playerController.MovementLocked = false;
    }
    //luego se puede borrar, es para ubicar bien
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}