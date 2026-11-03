using UnityEngine;

public enum LootAnimType { Object, Corpse, Gather }

//anim de loot, y bloquea lo demas mientra
public class PlayerLootAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float objectTime = 1.5f; //picking up naim
    [SerializeField] private float corpseTime = 2f;   //lifting anim

    //para las plantas

    [SerializeField] private float gatherTime = 2.5f; //working on device anim
    private static readonly int GatherHash = Animator.StringToHash("Gather");

    //audio (el AudioSource del player tiene que tener Output en el grupo SFX del MainMixer)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip[] gatherClips;//variantes del sonido de cosechar, no repite la misma dos veces seguidas
    private int lastGatherClip = -1;

    //privadas
    private static readonly int PickUpHash = Animator.StringToHash("PickUp");
    private static readonly int LiftHash = Animator.StringToHash("Lift");

    //no puede lootear si ya esta haciendo otra cosa o lo tiene agarrado un zombie
    public bool CanLoot => playerController != null && !playerController.IsBusy && !playerController.IsStunned;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
    }

    public void Play(LootAnimType type)
    {
        switch (type)
        {
            case LootAnimType.Corpse:
                animator.SetTrigger(LiftHash);
                playerController.SetBusy(corpseTime);
                break;
            case LootAnimType.Gather:
                animator.SetTrigger(GatherHash);
                playerController.SetBusy(gatherTime);
                PlayRandomGatherSound();
                break;
            default:
                animator.SetTrigger(PickUpHash);
                playerController.SetBusy(objectTime);
                break;
        }
    }

    //elige una variante al azar pero nunca la misma que sono la vez anterior (con una sola variante suena siempre esa)
    private void PlayRandomGatherSound()
    {
        if (sfxSource == null || gatherClips == null || gatherClips.Length == 0) return;

        int index = Random.Range(0, gatherClips.Length);

        if (gatherClips.Length > 1 && index == lastGatherClip)
            index = (index + Random.Range(1, gatherClips.Length)) % gatherClips.Length;

        lastGatherClip = index;

        if (gatherClips[index] != null)
            sfxSource.PlayOneShot(gatherClips[index]);
    }
}