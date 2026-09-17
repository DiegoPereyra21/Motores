using UnityEngine;
using UnityEngine.Events;

//el objeto el cual es looteable
public class Lootable : MonoBehaviour, IInteractable
{
    [SerializeField] private LootTable lootTable;
    [SerializeField] private bool requireZombieDeath = false; //por si esto esta pegado en un zombie
    [SerializeField] private UnityEvent onLooted; //para avisar a la ui, abrir cajon, destruir valla, etc
    //´rivadas
    private bool isLooted = false;

    public void Interact(GameObject interactor)
    {
        //chequeo si ya fue saqueado antes
        if (isLooted)
        {
            return;
        }

        //busca el health en caso de necesitar saber si el zombie esta muerto
        if (requireZombieDeath)
        {
            Health health = GetComponentInParent<Health>();

            if (health == null || !health.IsDead)
            {
                Debug.Log($"[Lootable] {gameObject.name} todavia no se puede saquear.");
                return;
            }
        }

        //buscamos el inventario del player
        Inventory inventory = interactor.GetComponent<Inventory>();

        if (inventory == null)//medio innecesario, pero x las dudas
        {
            return;
        }

        //si hay tabla de loot, se sortean los items
        if (lootTable != null)
        {
            // check para el calculo de loot
            var drops = requireZombieDeath
                ? lootTable.RollRandomLoot()
                : lootTable.RollGuaranteedLoot();

            foreach (var stack in drops)
            {
                inventory.AddItem(stack.item, stack.amount);
            }
        }

        //marco como saqueado para que no se pueda repetir
        isLooted = true;
        //EVENTO PARA SONIDO, ANIMACION
        onLooted.Invoke();
    }
}