using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Eventos")]
    public UnityEvent<float> onHealthChanged;   // pasa la vida normalizada (0 a 1)
    public UnityEvent onDied;

    private float currentHealth;

    public float Current => currentHealth;
    public float Max => maxHealth;
    public float Normalized => currentHealth / maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        onHealthChanged.Invoke(Normalized);

        if (IsDead) onDied.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged.Invoke(Normalized);
    }
}