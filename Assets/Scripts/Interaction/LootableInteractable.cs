using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//el objeto el cual es looteable
public class LootableInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private LootTable lootTable;
    [SerializeField] private bool requireZombieDeath = false; // para loot proveniente de zombies
    [SerializeField] private UnityEvent onLooted; //para avisar a la ui, abrir cajon, destruir valla, etc

    [Header("Acciones")]
    [SerializeField] private List<InteractionActionData> availableActions;

    private bool isLooted = false;
    public bool RequiresZombieDeath => requireZombieDeath;

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        if (action == null || isLooted || interactor == null)
            return false;

        // si el player esta ocupado o lo tiene agarrado un zombie no puede lootear
        PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();
        if (lootAnim != null && !lootAnim.CanLoot)
            return false;

        // si el loot pertenece a un zombie, comprobamos que este muerto
        if (requireZombieDeath)
        {
            Health health = GetComponentInParent<Health>();

            if (health == null || !health.IsDead)
            {
                Debug.Log($"[Lootable] {gameObject.name} todavia no se puede saquear.");
                return false;
            }
        }

        // buscamos el inventario del jugador
        Inventory inventory = interactor.GetComponent<Inventory>();

        if (inventory == null)
            return false;

        // se sortean los items
        if (lootTable != null)
        {
            var drops = requireZombieDeath ? lootTable.RollRandomLoot() : lootTable.RollGuaranteedLoot();

            foreach (var stack in drops)
            {
                inventory.AddItem(stack.item, stack.amount);
            }
        }

        if (lootAnim != null)
        {
            lootAnim.Play(requireZombieDeath ? LootAnimType.Corpse : LootAnimType.Object);
        }

        isLooted = true;

        //EVENTO PARA SONIDO, ANIMACION (RECORDARLOO)
        onLooted.Invoke();

        return true;
    }

    // revisa que el player pueda abrir el menu de interaccion
    public bool CanOpenInteraction(GameObject interactor)
    {
        if (interactor == null)
            return false;

        if (requireZombieDeath)
        {
            Health health = GetComponentInParent<Health>();

            if (health == null || !health.IsDead)
                return false;
        }

        PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();

        if (lootAnim != null && !lootAnim.CanLoot)
            return false;

        return true;
    }

    // construye las opciones de interaccion
    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        List<InteractionOption> options = new List<InteractionOption>();

        if (availableActions == null)
            return options;

        foreach (InteractionActionData action in availableActions)
        {
            if (action == null)
                continue;

            // validaciones especificas de cada accion
            if (action.ActionId == "loot")
            {
                bool canLoot = !isLooted && interactor != null && interactor.GetComponent<Inventory>() != null;

                string reason = isLooted ? "No queda nada util." : canLoot ? "" : "No se encontro el inventario.";

                options.Add(new InteractionOption(action, canLoot, reason));
            }
            else
            {
                // Las demas acciones se agregan sin restricciones especificas
                options.Add(new InteractionOption(action, true, ""));
            }
        }

        return options;
    }
}