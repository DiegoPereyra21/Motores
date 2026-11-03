using UnityEngine;

[RequireComponent(typeof(Health))]//aunque se sobreentienda q el player debe tener health, por las dudas
public class PlayerDeath : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private Animator animator;

    //audio (el AudioSource del player tiene que tener Output en el grupo SFX del MainMixer)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip fallClip;//el cuerpo cae al piso
    [SerializeField] private float fallSoundDelay = 0.7f;//segundos desde que arranca la anim de muerte hasta que suena (el clip tiene ~0.27s de silencio al inicio, ajustar a ojo)
    [SerializeField, Range(0f, 1f)] private float fallVolume = 0.4f;//el clip es muy fuerte, esto lo baja sin tocar el volumen de los demas sonidos del player

    private static readonly int DeathHash = Animator.StringToHash("Death");
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!animator) animator = GetComponent<Animator>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
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

        StartCoroutine(PlayFallSound());
        StartCoroutine(DeathSequence());
    }

    //tiempo SIN escalar: el fin de partida pausa el mundo (timeScale 0) y la anim de muerte sigue por unscaled time
    private System.Collections.IEnumerator PlayFallSound()
    {
        yield return new WaitForSecondsRealtime(fallSoundDelay);

        if (sfxSource != null && fallClip != null)
            sfxSource.PlayOneShot(fallClip, fallVolume);
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        GameProgressController.Instance.EndGame(GameEnding.Death);
    }
}