using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    //input
    [SerializeField] private InputActionReference attackAction;

    //config del golpe
    [SerializeField] private Transform attackPoint; //si se deja vacio, usa la posicion del player + su forward
    [SerializeField] private float attackRange = 1.2f; //radio del golpe
    [SerializeField] private float attackDamage = 25f; //daño por golpe
    [SerializeField] private float attackCooldown = 0.6f; //segundos entre golpes

    //privadas
    private float nextAttackTime; //momento (Time.time) en que puede volver a golpear

    private void OnEnable()
    {
        attackAction.action.Enable();
    }

    private void OnDisable()
    {
        attackAction.action.Disable();
    }

    private void Update()
    {
        //WasPressedThisFrame evita que mantener apretado siga golpeando cada frame (full auto de golpes)
        if (attackAction.action.WasPressedThisFrame() && Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void PerformAttack()
    {
        //punto desde donde se centra el golpe: attackPoint si se asigno, sino un poco adelante del player (esto va a modificarse despues con las animaciones y armas)
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position + transform.forward;

        //detecta todos los colliders dentro del radio de golpe
        Collider[] hits = Physics.OverlapSphere(origin, attackRange);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue; //solo golpea lo que tenga tag Enemy

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                Debug.Log($"El player golpeo a {hit.name} por {attackDamage} de daño");
            }
        }
    }

    //dibuja el radio de golpe en la escena (solo visible en el editor, para ajustarlo a ojo)
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position + transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}