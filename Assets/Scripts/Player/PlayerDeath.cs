using UnityEngine;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private Animator animator;

    private static readonly int DeathHash = Animator.StringToHash("Death");
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        health.onDied.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        health.onDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        playerController.enabled = false;
        cameraPivot.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //apaga la capa hurt para q no moleste a la anim de death
        int hurtLayer = animator.GetLayerIndex("Hurt");
        if (hurtLayer >= 0)
            animator.SetLayerWeight(hurtLayer, 0f);

        //para q la animacion termine aunque se pause
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.SetTrigger(DeathHash);

        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        GameProgressController.Instance.EndGame(GameEnding.Death);
    }
}