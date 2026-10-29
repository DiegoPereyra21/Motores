using UnityEngine;
using UnityEngine.InputSystem;

// SOLO PARA PROBAR: con F9 agrega al inventario los items de la lista (por ejemplo una Tabla de refuerzo), sin tener que lootear y craftear.
// Se pone en el Player. Solo funciona en el editor o en builds de desarrollo, y se puede borrar cuando ya no haga falta.
[RequireComponent(typeof(Inventory))]
public class DebugGiveItems : MonoBehaviour
{
    [SerializeField] private ItemData[] items;
    [SerializeField] private int amountPerPress = 1;

    private Inventory inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (items == null || Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame) return;

        foreach (ItemData item in items)
            inventory.AddItem(item, amountPerPress);
#endif
    }
}
