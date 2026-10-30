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

    //privadas
    private static readonly int PickUpHash = Animator.StringToHash("PickUp");
    private static readonly int LiftHash = Animator.StringToHash("Lift");

    //no puede lootear si ya esta haciendo otra cosa o lo tiene agarrado un zombie
    public bool CanLoot => playerController != null && !playerController.IsBusy && !playerController.IsStunned;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
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
                break;
            default:
                animator.SetTrigger(PickUpHash);
                playerController.SetBusy(objectTime);
                break;
        }
    }
}