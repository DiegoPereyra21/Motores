using UnityEngine;
using UnityEngine.AI;

// Cruce de los NavMeshLink (las ventanas de la cabaña): en vez de que el agente se deslice solo de un lado al otro,
// el zombie "trepa" el marco con un arco y a una velocidad propia, y de paso dispara la animacion de trepar si el Animator la tiene.
// Lo maneja ZombieControllerNavMesh (llama a Control() en su Update), por eso no tiene Update propio.
// Si se rompe algo (agente apagado, muerto), suelta el link y el agente sigue normal: nunca se queda colgado en el aire.
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieLinkTraversal : MonoBehaviour
{
    [Header("Cruce")]
    [SerializeField] private float climbSpeed = 1.5f; //m/s a los que avanza de un lado al otro del link
    [SerializeField] private float minDuration = 0.8f; //aunque el link sea corto, tarda al menos esto
    [SerializeField] private float arcHeight = 0.4f; //cuanto sube sobre el marco de la ventana en la mitad del cruce
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animacion (opcional)")]
    [SerializeField] private string climbTrigger = "Climb"; //si el Animator no tiene este parametro, no pasa nada

    [Header("Debug")]
    [SerializeField] private bool debugLog = false; //prenderlo para depurar

    private NavMeshAgent agent;
    private ZombieControllerNavMesh controller;
    private Animator animator;
    private int climbHash;
    private bool hasClimbParam;

    private bool traversing;
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float elapsed;

    //para que ZombieAnimation sepa a que velocidad "camina" mientras cruza (el agente no se mueve solo en el link)
    public bool IsTraversing => traversing;
    public float Speed => traversing ? climbSpeed : 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        controller = GetComponent<ZombieControllerNavMesh>();
        animator = GetComponentInChildren<Animator>();

        climbHash = Animator.StringToHash(climbTrigger);
        hasClimbParam = HasParameter(climbTrigger);
    }

    private void OnEnable()
    {
        //el cruce lo hacemos nosotros: sin esto el agente cruza el link solo
        agent.autoTraverseOffMeshLink = false;
    }

    private void OnDisable()
    {
        traversing = false;

        //apagando el componente el agente vuelve a cruzar los links solo
        if (agent != null)
            agent.autoTraverseOffMeshLink = true;
    }

    // Lo llama ZombieControllerNavMesh cada frame. Devuelve true si el zombie esta cruzando un link este frame
    // (y el controller no tiene que perseguir ni atacar), false si no.
    public bool Control()
    {
        if (!enabled) return false;

        if (!traversing)
        {
            if (!agent.isOnOffMeshLink) return false;

            Begin();
        }

        //aturdido: se queda a mitad de camino hasta que termine el stun
        if (controller != null && controller.IsStunned) return true;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        Vector3 position = Vector3.Lerp(startPos, endPos, t);
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = position;

        Vector3 direction = endPos - startPos;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (t >= 1f)
            Finish();

        return true;
    }

    private void Begin()
    {
        OffMeshLinkData data = agent.currentOffMeshLinkData;

        startPos = transform.position;
        endPos = data.endPos + Vector3.up * agent.baseOffset;
        duration = Mathf.Max(minDuration, Vector3.Distance(startPos, endPos) / climbSpeed);
        elapsed = 0f;
        traversing = true;

        if (animator != null && hasClimbParam)
            animator.SetTrigger(climbHash);

        Log($"empieza a cruzar el link ({duration:0.0}s)");
    }

    private void Finish()
    {
        traversing = false;
        transform.position = endPos;

        if (agent.isOnOffMeshLink)
            agent.CompleteOffMeshLink();

        Log("termino de cruzar el link");
    }

    private bool HasParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[Traversal] {name}: {message}", this);
    }
}
