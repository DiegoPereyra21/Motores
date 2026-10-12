using UnityEngine;

//para q se anime el recibir daño, es mas complejo pero hace q el torso solamente reaccione y no quede raro
[RequireComponent(typeof(Health))]//necesita si o si health
public class PlayerHurt : MonoBehaviour
{
    [SerializeField] private Animator animator;
    //privadas
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private Health health;
    private PlayerAttack playerAttack;
    private float lastNormalized = 1f;

    private void Awake()
    {
        health = GetComponent<Health>();
        playerAttack = GetComponent<PlayerAttack>();
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable() => health.onHealthChanged.AddListener(OnHealthChanged);
    private void OnDisable() => health.onHealthChanged.RemoveListener(OnHealthChanged);

    private void OnHealthChanged(float normalized)
    {
        bool tookDamage = normalized < lastNormalized;
        lastNormalized = normalized;
        //para q al curarse no salte ni tampoco si murio
        if (!tookDamage || health.IsDead) return;

        //mientras golpeas no se corta, sino quedaria raro (luego ver si sacar esto, depende del diseño)
        if (playerAttack != null && playerAttack.IsAttacking) return;

        animator.SetTrigger(HurtHash);
    }
}