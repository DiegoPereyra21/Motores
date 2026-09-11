using UnityEngine;

[RequireComponent(typeof(ZombieController))]
public class ZombieAttack : MonoBehaviour
{
    //config de ataque
    [SerializeField] private float damage = 10f; //daño por golpe, esto lo podemos hacer configurable en inspector para variarlo
    [SerializeField] private float attackCooldown = 1.2f; //segundos entre golpes, abierto a modificacion

    //privadas
    private ZombieController zombieController;
    private float nextAttackTime; //momento (Time.time) en el que puede volver a atacar

    private void Awake()
    {
        zombieController = GetComponent<ZombieController>(); //cache del componente (es para no tener q buscarlo cada frame)
    }

    private void Update()
    {
        if (!zombieController.InAttackRange) return; //solo ataca si esta cerca
        if (Time.time < nextAttackTime) return; //respeta el cooldown

        TryAttackPlayer();
        nextAttackTime = Time.time + attackCooldown;
    }

    private void TryAttackPlayer()
    {
        Transform player = zombieController.Player;
        if (player == null) return;

        //busca el IDamageable en el player y le aplica daño
        IDamageable damageable = player.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Debug.Log($"{name} ataco al jugador por {damage} de daño");
        }
    }
}