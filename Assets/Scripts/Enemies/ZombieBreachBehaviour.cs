using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Comportamiento del zombie cuando el player esta ADENTRO de la cabaña y el zombie todavia esta afuera:
// en vez de seguir el camino mas corto al player, va a la abertura (puerta o ventana) que tiene mas cerca.
//  - si tiene barricada, se para en un punto de ataque y la golpea hasta romperla
//  - si esta libre, llega hasta ahi y despues la cruza persiguiendo al player como siempre
// Si el player esta afuera (aunque sea pegado a la cabaña) o el zombie ya entro, no hace nada y el zombie persigue normal.
// Lo maneja ZombieControllerNavMesh (llama a Control() en su Update), por eso no tiene Update propio.
// Si algo sale mal (no llega a la abertura, se traba), la abandona un rato y vuelve a perseguir normal: nunca se queda quieto.
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieBreachBehaviour : MonoBehaviour
{
    [Header("Eleccion de abertura")]
    [SerializeField] private float barricadePenalty = 0f; //metros extra que "cuesta" una abertura con barricada. 0 = va a la mas cercana sin importar si tiene barricada
    [SerializeField] private float arriveDistance = 0.8f; //a esta distancia del punto de ataque se considera que llego
    [SerializeField] private float retargetInterval = 0.5f; //cada cuanto reintenta elegir abertura si no encontro ninguna

    [Header("Ataque a la barricada")]
    [SerializeField] private float barricadeDamage = 10f; //daño por golpe (igual que el golpe al player en Zombie1)
    [SerializeField] private float attackWindup = 0.2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Seguridad (que no se quede trabado)")]
    [SerializeField] private float stuckTime = 3f; //si en este tiempo no se acerca nada al punto, abandona esa abertura
    [SerializeField] private float stuckMinProgress = 0.2f; //cuanto tiene que acercarse para contar como progreso
    [SerializeField] private float blockedTime = 15f; //cuanto tiempo ignora una abertura despues de abandonarla

    [Header("Debug")]
    [SerializeField] private bool debugLog = true; //avisa en la Consola cuando elige, llega, se traba, etc. (apagar cuando ya funcione)

    private enum Phase
    {
        None,
        Approaching, //yendo al punto de ataque
        Breaking,    //golpeando la barricada
        Crossing     //abertura libre: ya llego y ahora la cruza persiguiendo normal
    }

    private NavMeshAgent agent;
    private Animator animator;
    private Health health;
    private NavMeshPath pathCache; //se crea en Awake: Unity no deja crear un NavMeshPath en un inicializador de campo
    private readonly Dictionary<CabinOpening, float> blockedUntil = new(); //aberturas abandonadas y hasta cuando

    private Phase phase = Phase.None;
    private CabinOpening target;
    private Transform targetPoint;
    private float nextTargetTime;
    private float nextNoTargetLogTime;

    private float bestDistance; //lo mas cerca que estuvo del punto de ataque
    private float lastProgressTime;

    private bool windingUp;
    private float windupEndTime;
    private float nextAttackTime;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        health = GetComponent<Health>();
        pathCache = new NavMeshPath();
    }

    private void OnEnable()
    {
        if (health != null)
            health.onDied.AddListener(ResetState);
    }

    private void OnDisable()
    {
        if (health != null)
            health.onDied.RemoveListener(ResetState);

        ResetState();
    }

    // Lo llama ZombieControllerNavMesh cada frame. Devuelve true si este comportamiento maneja al zombie este frame
    // (y el controller no tiene que perseguir al player), false si el zombie tiene que perseguir normal.
    public bool Control(Transform player)
    {
        if (!ShouldUseOpenings(player))
        {
            ResetState();
            return false;
        }

        if (target == null)
        {
            if (Time.time < nextTargetTime) return false;

            if (!ChooseTarget())
            {
                nextTargetTime = Time.time + retargetInterval;
                return false;
            }
        }

        //la abertura se barricada de nuevo mientras esperaba para cruzar
        if (phase == Phase.Crossing && target.IsBarricaded)
            phase = Phase.Approaching;

        if (phase == Phase.Crossing)
            return false;

        if (!HasArrived())
        {
            phase = Phase.Approaching;

            if (IsStuck())
            {
                Abandon("no se acerca al punto de ataque");
                return false;
            }

            MoveToPoint();
            return true;
        }

        //llego: si la abertura esta libre la cruza, si tiene barricada la golpea
        if (!target.IsBarricaded)
        {
            Log($"llego a {target.name} (libre), la cruza persiguiendo al player");
            phase = Phase.Crossing;
            windingUp = false;
            return false;
        }

        if (phase != Phase.Breaking)
            Log($"llego a {target.name} (barricada), empieza a golpearla");

        phase = Phase.Breaking;
        BreakBarricade();
        return true;
    }

    //solo aplica con el player adentro de la cabaña y el zombie afuera
    private bool ShouldUseOpenings(Transform player)
    {
        if (player == null || !CabinInterior.Exists) return false;

        return CabinInterior.Contains(player.position) && !CabinInterior.Contains(transform.position);
    }

    // elige la abertura con menor "puntaje" = metros caminando hasta su punto de ataque (+ penalidad si tiene barricada)
    private bool ChooseTarget()
    {
        CabinOpening best = null;
        float bestScore = float.MaxValue;
        int noFreePoint = 0;
        int noPath = 0;
        int blocked = 0;

        foreach (CabinOpening opening in CabinOpening.All)
        {
            if (IsBlocked(opening)) { blocked++; continue; }

            Transform point = opening.PeekFreeAttackPoint(transform.position, this);
            if (point == null) { noFreePoint++; continue; }
            if (!TryGetPathLength(point.position, out float length)) { noPath++; continue; }

            float score = length + (opening.IsBarricaded ? barricadePenalty : 0f);

            if (score < bestScore)
            {
                bestScore = score;
                best = opening;
            }
        }

        if (best == null || !best.TryReserveAttackPoint(this, transform.position, out Transform reserved))
        {
            //se avisa de vez en cuando, no cada reintento
            if (Time.time >= nextNoTargetLogTime)
            {
                nextNoTargetLogTime = Time.time + 5f;
                Log($"no encontro abertura (total {CabinOpening.All.Count}: sin punto libre {noFreePoint}, sin camino {noPath}, abandonadas {blocked}). Persigue normal");
            }

            return false;
        }

        target = best;
        targetPoint = reserved;
        phase = Phase.Approaching;
        windingUp = false;
        bestDistance = PlanarDistanceToPoint();
        lastProgressTime = Time.time;

        Log($"elige {target.name} ({(target.IsBarricaded ? "barricada" : "libre")}, puntaje {bestScore:0.0})");
        return true;
    }

    //largo del camino por el NavMesh hasta el punto (false si no se puede llegar)
    private bool TryGetPathLength(Vector3 destination, out float length)
    {
        length = 0f;

        if (!agent.isOnNavMesh) return false;
        if (!NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, pathCache)) return false;
        if (pathCache.status != NavMeshPathStatus.PathComplete) return false;

        Vector3[] corners = pathCache.corners;
        for (int i = 1; i < corners.Length; i++)
            length += Vector3.Distance(corners[i - 1], corners[i]);

        return true;
    }

    private float PlanarDistanceToPoint()
    {
        Vector3 offset = targetPoint.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool HasArrived()
    {
        return PlanarDistanceToPoint() <= arriveDistance;
    }

    //true si hace stuckTime segundos que no se acerca (el punto puede estar mal ubicado o el camino cortado)
    private bool IsStuck()
    {
        float distance = PlanarDistanceToPoint();

        if (distance < bestDistance - stuckMinProgress)
        {
            bestDistance = distance;
            lastProgressTime = Time.time;
        }

        return Time.time - lastProgressTime > stuckTime;
    }

    private void MoveToPoint()
    {
        agent.isStopped = false;
        agent.SetDestination(targetPoint.position);
        Rotate(agent.desiredVelocity);
    }

    private void BreakBarricade()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Rotate(target.transform.position - transform.position);

        Barricade barricade = target.Barricade;
        if (barricade == null || barricade.IsBroken) return;

        if (!windingUp)
        {
            if (Time.time < nextAttackTime) return;

            windingUp = true;
            windupEndTime = Time.time + attackWindup;

            if (animator != null)
                animator.SetTrigger(AttackHash);

            return;
        }

        if (Time.time < windupEndTime) return;

        windingUp = false;
        nextAttackTime = Time.time + attackCooldown;

        //si el golpe se "congelo" (el zombie estuvo aturdido) no pega de golpe al volver
        if (Time.time - windupEndTime < 0.3f)
            barricade.TakeDamage(barricadeDamage);
    }

    private void Rotate(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    //deja de intentar con esta abertura un rato y vuelve a perseguir al player normal
    private void Abandon(string reason)
    {
        Log($"abandona {target.name}: {reason}");
        blockedUntil[target] = Time.time + blockedTime;
        ResetState();
        nextTargetTime = Time.time + retargetInterval;
    }

    private bool IsBlocked(CabinOpening opening)
    {
        return blockedUntil.TryGetValue(opening, out float until) && Time.time < until;
    }

    //libera el punto de ataque y vuelve al estado normal (el zombie persigue al player)
    private void ResetState()
    {
        if (target != null)
            target.ReleaseAttackPoint(this);

        target = null;
        targetPoint = null;
        phase = Phase.None;
        windingUp = false;
    }

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[Breach] {name}: {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (targetPoint == null) return;

        Gizmos.color = phase == Phase.Breaking ? Color.red : Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up, targetPoint.position);
    }
}
