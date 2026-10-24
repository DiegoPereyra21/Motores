using UnityEngine;
using UnityEngine.AI;

public class ZombieAnimation : MonoBehaviour
{
    //referencias
    [SerializeField] private Animator animator;
    //golpe
    [SerializeField] private float hurtStunTime = 0.4f;
    [SerializeField] private float hurtCooldown = 1.2f; //luego ajustar, para q no se quede quieto siempre tampoco
    [SerializeField] private float walkAnimSpeed = 2.5f; //velocidad a la que el clip de caminar se ve natural, igual a movespeed
    //privadas parametros animator
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int AnimSpeedHash = Animator.StringToHash("LocomotionSpeed");
    //privadas
    private Health health;
    private NavMeshAgent agent;
    private ZombieControllerNavMesh zombieController;
    private ZombieAttack zombieAttack;
    private float lastNormalized = 1f;
    private float nextHurtTime;
    private bool wasWindingUp;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        zombieController = GetComponent<ZombieControllerNavMesh>();
        zombieAttack = GetComponent<ZombieAttack>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        health.onHealthChanged.AddListener(OnHealthChanged);
        health.onDied.AddListener(OnDied);
    }
    private void OnDisable()
    {
        health.onHealthChanged.RemoveListener(OnHealthChanged);
        health.onDied.RemoveListener(OnDied);
    }
    private void Update()
    {
        if (health.IsDead) return;

        float speed = agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);

        //misma animacion de caminar, mas rapida cuando va mas rapido que walkAnimSped
        animator.SetFloat(AnimSpeedHash, Mathf.Max(1f, speed / walkAnimSpeed), 0.1f, Time.deltaTime);

        bool windingUp = zombieAttack.IsWindingUp;
        if (windingUp && !wasWindingUp)
            animator.SetTrigger(AttackHash);
        wasWindingUp = windingUp;
    }

    private void OnHealthChanged(float normalized)
    {
        bool tookDamage = normalized < lastNormalized;
        lastNormalized = normalized;
        if (!tookDamage || health.IsDead || Time.time < nextHurtTime) return;

        nextHurtTime = Time.time + hurtCooldown;
        zombieAttack.CancelWindup();            //un golpe interrumpe su ataque
        zombieController.Stun(hurtStunTime);    //y luego luego lo frena
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(HurtHash);
    }

    private void OnDied()
    {
        animator.SetTrigger(DeathHash);
    }
}