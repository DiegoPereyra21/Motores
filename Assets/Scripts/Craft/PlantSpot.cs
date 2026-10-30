using UnityEngine;

//un lugar fijo donde crece una planta. tiene que estar en la capa "Interact" y con un collider en el mismo objeto
public class PlantSpot : MonoBehaviour, IInteractable
{
    [Header("Recompensa")]
    [SerializeField] private ItemData item;
    [SerializeField, Min(1)] private int amount = 2;

    [Header("Visual (hijos con los modelos, se activa 1 al azar)")]
    [SerializeField] private GameObject[] variants;
    [SerializeField] private bool randomYRotation = true;

    [Header("Estado inicial")]
    [SerializeField] private bool startGrown = false;

    private Collider spotCollider;
    private Quaternion[] baseRotations;

    public bool IsGrown { get; private set; }

    private void Awake()
    {
        spotCollider = GetComponent<Collider>();

        baseRotations = new Quaternion[variants.Length];
        for (int i = 0; i < variants.Length; i++)
            baseRotations[i] = variants[i].transform.localRotation;

        SetGrown(startGrown);
    }

    public void Interact(GameObject interactor)
    {
        if (!IsGrown) return;

        //si el player esta ocupado no puede recolectar
        PlayerLootAnimation lootAnim = interactor.GetComponent<PlayerLootAnimation>();
        if (lootAnim != null && !lootAnim.CanLoot) return;

        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null) return;

        inventory.AddItem(item, amount);

        if (lootAnim != null) lootAnim.Play(LootAnimType.Gather);

        SetGrown(false);
    }

    //lo usa tambien el GardenManager para hacerla crecer
    public void SetGrown(bool grown)
    {
        IsGrown = grown;

        //sin collider no se puede apuntar a un lugar vacio
        if (spotCollider != null) spotCollider.enabled = grown;

        int chosen = (grown && variants.Length > 0) ? Random.Range(0, variants.Length) : -1;

        for (int i = 0; i < variants.Length; i++)
        {
            bool active = i == chosen;
            variants[i].SetActive(active);

            if (active && randomYRotation)
                variants[i].transform.localRotation = baseRotations[i] * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
    }
}