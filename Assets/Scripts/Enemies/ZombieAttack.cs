using UnityEngine;
//me esta volviendo loco el sonido de wwise al probar el game
[RequireComponent(typeof(ZombieControllerNavMesh))]//cambio a navmesh, la verdad tendria que haber reutilizado zombiecontroller, pero queria diferenciarlo porque eran muchos cambios
public class ZombieAttack : MonoBehaviour
{
    //config de ataque
    [SerializeField] private float damage = 10f; //daño por golpe
    [SerializeField] private float attackCooldown = 1.2f; //segundos entre golpes
    [SerializeField] private float attackWindup = 0.5f; //tiempo que "tarda" en golpear hasta que tengamos animaciones (para que el player pueda esquivar)
    [SerializeField] private float biteParalyzeTime = 0.7f; //segundos que paraliza al player al morderlo, menor q attackooldown sino deja paralizado siempre
    //privadas
    private ZombieControllerNavMesh zombieController;
    private Health myHealth; //salud propia, para chequear si ya murio
    private float nextAttackTime; //momento en que puede iniciar un nuevo golpe
    private bool isWindingUp; //true mientras esta "preparando" el golpe
    private float windupEndTime; //momento (Time.time) en que el golpe conecta

    public bool IsWindingUp => isWindingUp; //para la futura animacion

    private void Awake()
    {
            zombieController = GetComponent<ZombieControllerNavMesh>();
            myHealth = GetComponent<Health>();
    }

    private void Update()
    {
        //si el script esta desactivado o ya esta muerto, no hace nada (evita el bug de attack congelado in range)
        if (!enabled || (myHealth != null && myHealth.IsDead)) return;

        //si no hay player o ya murio, no ataca ni loguea nada mas
        if (zombieController.Player == null || zombieController.IsPlayerDead) return;

        if (zombieController.IsStunned) return;

        //si el player se aleja durante el windup, se cancela el golpe (no deberia pegar "a traves" de una esquivada)
        if (isWindingUp && !zombieController.InAttackRange)
        {
            CancelWindup();
            return;
        }

        if (isWindingUp)
        {
            //esperando a que se cumpla el tiempo de preparacion
            if (Time.time >= windupEndTime)
                LandHit();

            return;
        }

        //si esta en rango y no esta en cooldown, arranca un nuevo windup
        if (zombieController.InAttackRange && Time.time >= nextAttackTime)
        {
            StartWindup();
        }
    }

    private void StartWindup()
    {
        isWindingUp = true;
        windupEndTime = Time.time + attackWindup;


        //paraliza al player desde que empieza el windup hasta un rato despues de la mordida
        Transform target = zombieController.Player;
        if (target != null)
        {
            PlayerController playerController = target.GetComponent<PlayerController>();
            if (playerController != null) playerController.Stun(attackWindup + biteParalyzeTime);
        }

        //aca despues se puede disparar animator.SetTrigger("Attack") cuando tengan animaciones
    }

    public void CancelWindup()
    {
        isWindingUp = false;
    }

    private void LandHit()
    {
        isWindingUp = false;
        nextAttackTime = Time.time + attackCooldown;

        Transform player = zombieController.Player;
        if (player == null) return;

        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"ZOMBIE HIZO {damage} DE DAÑO");
        }
    }
}