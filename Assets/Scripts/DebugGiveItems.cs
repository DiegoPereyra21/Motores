using UnityEngine;
using UnityEngine.InputSystem;

// SOLO PARA PROBAR: con F9 agrega al inventario los items de la lista (por ejemplo una Tabla de refuerzo), sin tener que lootear y craftear.
// Con F10 agrega la bengala, para probar el final de victoria.
// Se pone en el Player. Solo funciona en el editor o en builds de desarrollo, y se puede borrar cuando ya no haga falta.
[RequireComponent(typeof(Inventory))]
public class DebugGiveItems : MonoBehaviour
{
    [SerializeField] private ItemData[] items;
    [SerializeField] private int amountPerPress = 1;

    [Header("Bengala (F10)")]
    [SerializeField] private ItemData flareItem; //asignar ScriptableObjects/Items/Crafted/flare

    private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current == null) return;

        if (Keyboard.current.f9Key.wasPressedThisFrame && items != null)
        {
            foreach (ItemData item in items)
                inventory.AddItem(item, amountPerPress);
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame && flareItem != null)
            inventory.AddItem(flareItem, 1);
#endif
    }
}