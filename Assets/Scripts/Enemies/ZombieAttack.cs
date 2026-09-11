using UnityEngine;

[RequireComponent(typeof(ZombieController))]
public class ZombieAttack : MonoBehaviour
{
    //config de ataque
    [SerializeField] private float damage = 10f; //daño por golpe
    [SerializeField] private float attackCooldown = 1.2f; //segundos entre golpes
    [SerializeField] private float attackWindup = 0.5f; //tiempo que "tarda" en golpear, simula la animacion
    private ZombieController zombieController;
    private float nextAttackTime; //momento en que puede iniciar un nuevo golpe
    private bool isWindingUp; //true mientras esta "preparando" el golpe
    private float windupEndTime; //momento (Time.time) en que el golpe conecta

    public bool IsWindingUp => isWindingUp; //para animaciones, saber si esta en windup

    private void Awake()
    {
        zombieController = GetComponent<ZombieController>();
    }

    private void Update()
    {
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
        //dejo animator.SetTrigger("Attack") cuando pongamos animaciones
    }

    private void CancelWindup()
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
            Debug.Log($"{name} conecto un golpe al jugador por {damage} de daño");
        }
    }
}