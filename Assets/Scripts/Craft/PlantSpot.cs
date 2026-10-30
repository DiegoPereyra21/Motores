using UnityEngine;

//Gameobject en interact, con boxcollider y este script
public class PlantSpot : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 2;//por si queria cambiar la cantidad de las papas, y hierbas
    [SerializeField] private GameObject model; //el q aparece y desaparece
    [SerializeField] private bool startGrown = false;//para q las hierbas solamente esten el primer dia, luego vere si dejar q todas crezcan en segundo dia, en ese caso esto se va
    //privadas
    private Collider spotCollider;
    //publicas
    public bool IsGrown { get; private set; }

    private void Awake()
    {
        spotCollider = GetComponent<Collider>();
        SetGrown(startGrown);
    }

    public void Interact(GameObject interactor)
    {
        if (!IsGrown) return;

        PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();
        if (lootAnim != null && !lootAnim.CanLoot) return;

        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null) return;

        inventory.AddItem(item, amount);

        if (lootAnim != null) lootAnim.Play(LootAnimType.Gather);

        SetGrown(false);
    }

    //lo usa el garden manager para respawnearla
    public void SetGrown(bool grown)
    {
        IsGrown = grown;

        //obvio necesita collider
        if (spotCollider != null) spotCollider.enabled = grown;

        if (model != null) model.SetActive(grown);
    }
}