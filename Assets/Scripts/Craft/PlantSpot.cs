using System.Collections.Generic;
using UnityEngine;

//Gameobject en interact, con boxcollider y este script
public class PlantSpot : MonoBehaviour, IInteractable
{
    [Header("Planta")]
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 2;//por si queria cambiar la cantidad de las papas, y hierbas
    [SerializeField] private GameObject model; //el q aparece y desaparece
    [SerializeField] private bool startGrown = false;//para q las hierbas solamente esten el primer dia, luego vere si dejar q todas crezcan en segundo dia, en ese caso esto se va

    [Header("Acciones")]
    [SerializeField] private List<InteractionActionData> availableActions;

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

    public bool CanOpenInteraction(GameObject interactor)
    {
        if (interactor == null || !IsGrown)
            return false;

        PlayerLootAnimation lootAnim =
            interactor.GetComponent<PlayerLootAnimation>();

        if (lootAnim != null && !lootAnim.CanLoot)
            return false;

        return true;
    }

    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        List<InteractionOption> options = new List<InteractionOption>();

        if (interactor == null || !IsGrown || availableActions == null)
            return options;

        Inventory inventory = interactor.GetComponent<Inventory>();

        foreach (InteractionActionData action in availableActions)
        {
            if (action == null || action.ActionId != "harvest")
                continue;

            bool canHarvest = inventory != null && item != null;
            string reason = canHarvest ? "" : "No se pudo acceder al inventario o la planta no tiene un recurso asignado.";

            PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();

            if (lootAnim != null && !lootAnim.CanLoot)
            {
                canHarvest = false;
                reason = "No podés recolectar en este momento.";
            }

            options.Add(new InteractionOption(action, canHarvest, reason));
        }

        return options;
    }

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        if (action == null || action.ActionId != "harvest")
            return false;

        if (interactor == null || !IsGrown || item == null)
            return false;

        PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();

        if (lootAnim != null && !lootAnim.CanLoot)
            return false;

        Inventory inventory = interactor.GetComponent<Inventory>();

        if (inventory == null)
            return false;

        inventory.AddItem(item, amount);

        if (lootAnim != null)
            lootAnim.Play(LootAnimType.Gather);

        SetGrown(false);

        return true;
    }
}