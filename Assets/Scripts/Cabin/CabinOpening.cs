using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum OpeningType
{
    Door,
    Window
}

public enum OpeningState
{
    Open,       //sin barricada: los zombies pueden pasar (por el NavMeshLink, o por el NavMesh directo si es una puerta)
    Barricaded  //con barricada: el link apagado / el obstaculo activo, los zombies tienen que romperla
}

// Como se bloquea el paso de los zombies cuando hay barricada.
// El diagnostico del NavMesh (Zombie Cabin ▸ Barricadas ▸ Diagnosticar) mostro que las ventanas estan selladas en el bake (pasan solo por el link)
// y que las puertas estan abiertas (el NavMesh las cruza solo), por eso en Auto las puertas usan un obstaculo y las ventanas el link.
public enum NavBlockMode
{
    Auto,     //puerta = Obstacle, ventana = Link
    Link,     //el NavMeshLink se apaga con la barricada (para aberturas selladas en el NavMesh)
    Obstacle  //un NavMeshObstacle con carve se prende con la barricada (para aberturas que el NavMesh ya cruza)
}

// Una puerta o ventana de la cabaña. Se pone en un objeto aparte (no en el panel de la puerta/ventana), en la capa "Interact".
// Convencion: el transform mira (forward) hacia AFUERA de la cabaña, el right es el ancho y el up el alto.
// Lo crea OpeningsSetup (menu Zombie Cabin ▸ Barricadas) a partir de los paneles que ya existen.
public class CabinOpening : MonoBehaviour, IInteractable, IInteractionPrompt
{
    //todas las aberturas de la escena, para que los zombies sepan cuales hay
    public static readonly List<CabinOpening> All = new();
    public static event Action<CabinOpening> OnOpeningStateChanged;

    [Header("Abertura")]
    [SerializeField] private OpeningType type = OpeningType.Window;
    [SerializeField] private Vector2 size = new Vector2(2f, 1.9f); //ancho x alto
    [SerializeField] private bool requireInside = true; //solo se puede barricadar desde adentro de la cabaña
    [SerializeField] private Transform[] attackPoints; //afuera de la abertura, donde se paran los zombies a romper la barricada

    [Header("Bloqueo del NavMesh")]
    [SerializeField] private NavBlockMode navBlockMode = NavBlockMode.Auto;
    [SerializeField] private float obstacleThickness = 0.6f; //profundidad del obstaculo (solo si usa Obstacle)
    [SerializeField] private float obstacleExtraWidth = 0.4f; //ancho extra para que el obstaculo llegue hasta el marco

    [Header("Barricada")]
    [SerializeField] private ItemData barricadeItem; //el item que se gasta (Tabla de refuerzo)

    [Header("Eventos (animacion, sonido, etc.)")]
    [SerializeField] private UnityEvent onPlaceStarted;
    [SerializeField] private UnityEvent onPlaceCancelled; //lo interrumpieron (mordida, muerte): sirve para cortar la animacion
    [SerializeField] private UnityEvent onBarricaded;
    [SerializeField] private UnityEvent onBarricadeBroken;

    private OpeningState state = OpeningState.Open;
    private Barricade barricade;
    private NavMeshLink link;
    private NavMeshObstacle obstacle;
    private Coroutine placeRoutine;

    public OpeningType Type => type;
    public OpeningState State => state;
    public bool IsBarricaded => state == OpeningState.Barricaded;
    public Vector2 Size => size;
    public Barricade Barricade => barricade;
    public Transform[] AttackPoints => attackPoints;

    private void Awake()
    {
        link = GetComponent<NavMeshLink>();

        bool useObstacle = navBlockMode == NavBlockMode.Obstacle || (navBlockMode == NavBlockMode.Auto && type == OpeningType.Door);
        if (useObstacle)
            SetupObstacle();

        //la barricada vive en un hijo que arranca apagado
        barricade = GetComponentInChildren<Barricade>(true);

        if (barricade == null)
        {
            GameObject child = new GameObject("Barricade");
            child.transform.SetParent(transform, false);
            barricade = child.AddComponent<Barricade>();
        }

        barricade.gameObject.SetActive(false);
        SetNavBlocked(false);
    }

    private void OnEnable()
    {
        All.Add(this);
        barricade.OnBroken += HandleBarricadeBroken;
    }

    private void OnDisable()
    {
        All.Remove(this);

        if (barricade != null)
            barricade.OnBroken -= HandleBarricadeBroken;
    }

    // lo llama PlayerInteraction cuando el jugador apunta a la abertura y aprieta E
    public void Interact(GameObject interactor)
    {
        if (!CanPlace(interactor, out Inventory inventory, out PlaceableData data))
            return;

        placeRoutine = StartCoroutine(PlaceRoutine(interactor, inventory, data));
    }

    // texto del cartel: "(E) - Poner barricada" o "No hay barricadas disponibles"
    public bool TryGetPrompt(GameObject interactor, out string text)
    {
        text = null;

        if (state != OpeningState.Open || placeRoutine != null) return false;
        if (requireInside && !IsInsideSide(interactor.transform.position)) return false;

        Inventory inventory = interactor.GetComponent<Inventory>();
        bool hasBarricade = inventory != null && barricadeItem != null && inventory.HasItem(barricadeItem, 1);

        text = hasBarricade ? "(E) - Poner barricada" : "No hay barricadas disponibles";
        return true;
    }

    private bool CanPlace(GameObject interactor, out Inventory inventory, out PlaceableData data)
    {
        inventory = null;
        data = null;

        if (state != OpeningState.Open || placeRoutine != null) return false;
        if (requireInside && !IsInsideSide(interactor.transform.position)) return false;

        GameProgressController progress = GameProgressController.Instance;
        if (progress != null && (progress.IsGameEnded || progress.IsWorldPaused)) return false;

        PlayerController player = interactor.GetComponent<PlayerController>();
        if (player != null && (player.IsBusy || player.IsStunned)) return false;

        inventory = interactor.GetComponent<Inventory>();
        if (inventory == null || barricadeItem == null) return false;
        if (!inventory.HasItem(barricadeItem, 1)) return false;

        return barricadeItem.TryGetPlaceableData(out data);
    }

    private IEnumerator PlaceRoutine(GameObject interactor, Inventory inventory, PlaceableData data)
    {
        PlayerController player = interactor.GetComponent<PlayerController>();
        Health playerHealth = interactor.GetComponent<Health>();

        //mientras la pone no se puede mover ni atacar (misma idea que el loot)
        if (player != null) player.SetBusy(data.buildTime);
        onPlaceStarted.Invoke();

        float endTime = Time.time + data.buildTime;

        while (Time.time < endTime)
        {
            //si un zombie lo muerde a mitad de camino (o muere) se cancela y NO se gasta la tabla
            bool interrupted = (player != null && player.IsStunned) || (playerHealth != null && playerHealth.IsDead);

            if (interrupted)
            {
                if (player != null) player.SetBusy(0f);
                onPlaceCancelled.Invoke();
                placeRoutine = null;
                yield break;
            }

            yield return null;
        }

        if (inventory.RemoveItem(barricadeItem, 1))
            Build(data);

        placeRoutine = null;
    }

    private void Build(PlaceableData data)
    {
        state = OpeningState.Barricaded;
        barricade.Build(data.prefab, size, data.maxDurability);
        SetNavBlocked(true);

        onBarricaded.Invoke();
        OnOpeningStateChanged?.Invoke(this);
    }

    private void HandleBarricadeBroken()
    {
        state = OpeningState.Open;
        SetNavBlocked(false);

        onBarricadeBroken.Invoke();
        OnOpeningStateChanged?.Invoke(this);
    }

    //con la barricada nadie pasa: se apaga el link (ventanas) o se prende el obstaculo que corta el NavMesh (puertas). Sin barricada, al reves
    private void SetNavBlocked(bool blocked)
    {
        if (link != null)
            link.enabled = !blocked;

        if (obstacle != null)
            obstacle.enabled = blocked;
    }

    //obstaculo con carve del tamaño de la abertura, arranca apagado. El link de esta abertura ya no hace falta (el NavMesh cruza solo)
    private void SetupObstacle()
    {
        obstacle = GetComponent<NavMeshObstacle>();

        if (obstacle == null)
            obstacle = gameObject.AddComponent<NavMeshObstacle>();

        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = new Vector3(size.x + obstacleExtraWidth, size.y, obstacleThickness);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.enabled = false;

        if (link != null)
        {
            link.enabled = false;
            link = null;
        }
    }

    //true si el punto esta del lado de adentro de la cabaña (forward apunta para afuera)
    private bool IsInsideSide(Vector3 point)
    {
        return Vector3.Dot(point - transform.position, transform.forward) < 0f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.color = state == OpeningState.Open ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.6f, 0.1f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.05f));
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 1.2f); //hacia afuera
        Gizmos.matrix = Matrix4x4.identity;

        if (attackPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform point in attackPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, 0.25f);
        }
    }
}
