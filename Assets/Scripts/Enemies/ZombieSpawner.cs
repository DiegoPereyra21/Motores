using UnityEngine;

//Spawner de zombies para la noche. Escucha eventos de GameProgressController.cs.
//IMPORTANTE QUE LOS ZOMBIES TENGAN EL TAG DE ENEMY!!!

public class ZombieSpawner : MonoBehaviour
{
    [Header("Config de spawn")]
    [SerializeField] private GameObject zombiePrefab; //prefab a instanciar
    [SerializeField] private Transform[] spawnPoints; //puntos posibles de aparicion
    [SerializeField] private float timeBetweenSpawns = 4f; //segundos entre spawns
    [SerializeField] private int maxZombiesAlive = 10; //limite de zombies vivos a la vez (podemos ampliarlo para que varie segun la oleada)
    [SerializeField] private float initialSpawnDelay = 15f; //segundos de espera antes del primer spawn de cada noche. Al arrancar la Noche todavia se ve como atardecer (la rotacion del sol arranca donde termino el Dia anterior), asi que no tiene sentido que aparezcan zombies de una.

    [Header("Ciclo dia/noche")]
    [SerializeField] private GameProgressController gameProgressController; //controla el progreso general y el tiempo
    private float nextSpawnTime; //momento (Time.time) del proximo spawn permitido

    private bool nightActive; //se explica solo: true = noche, false = dia o demo terminada.

    //condiciones de no spawn: no es de noche, no hay prefab o spawn points, o ya hay demasiados zombies vivos.
    private void Update()
    {
        if (!nightActive) return;

        if (zombiePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (Time.time < nextSpawnTime)
            return;

        int currentZombies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (currentZombies >= maxZombiesAlive)
        {
            nextSpawnTime = Time.time + timeBetweenSpawns;
            return;
        }

        SpawnZombie();
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    private void OnEnable()
    {
        if (gameProgressController == null)
        {
            Debug.LogError("[ZombieSpawner] No se asigno GameProgressController.", this);
            return;
        }

        gameProgressController.OnPhaseChanged += HandlePhaseChanged;
        gameProgressController.OnRunFinished += StopEnemiesSpawn;

        // Comprobar el estado actual por si el spawner se activa durante la noche.
        HandlePhaseChanged(gameProgressController.CurrentPhase);

        // Comprobar si la partida ya terminó.
        if (gameProgressController.IsRunFinished)
        {
            StopEnemiesSpawn();
        }
    }

    private void OnDisable()
    {
        if (gameProgressController == null)
            return;

        gameProgressController.OnPhaseChanged -= HandlePhaseChanged;
        gameProgressController.OnRunFinished -= StopEnemiesSpawn; // detiene el spawn de enemigos al terminar la partida
    }

    // revisa la fase en la que nos encontramos
    private void HandlePhaseChanged(DayNightPhase phase)
    {
        if (phase == DayNightPhase.Night)
        {
            StartEnemiesSpawn();
        }
        else
        {
            StopEnemiesSpawn();
        }
    }

    //se ejecuta al arrancar cada noche (noche 1, 2 o 3): habilita el spawn, pero el primer zombie recien aparece despues de initialSpawnDelay (ver comentario en la variable).
    private void StartEnemiesSpawn()
    {
        nightActive = true;
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    // desactiva los spawns
    private void StopEnemiesSpawn()
    {
        nightActive = false;
    }

    private void SpawnZombie()
    {
        //elige un spawn point al azar e instancia el prefab ahi
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Debug.Log(Time.time);
        Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
    }
}