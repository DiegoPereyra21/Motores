using UnityEngine;

//Spawner de zombies para la noche. Escucha eventos de DayNightCycle.cs.
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

    [SerializeField] private DayNightCycle dayNightCycle; //reloj que determina spawn/stop
    private float nextSpawnTime; //momento (Time.time) del proximo spawn permitido

    private bool nightActive; //se explica solo: true = noche, false = dia o demo terminada.

    //metodos para listeners:

    private void OnEnable()
    {
        dayNightCycle.onNightStart.AddListener(HandleNightStart);
        dayNightCycle.onDayStart.AddListener(HandleDayStart);
        dayNightCycle.onDemoEnd.AddListener(HandleDemoEnd); //la ultima noche puede terminar la demo en vez de pasar a un Dia
    }

    private void OnDisable()
    {
        dayNightCycle.onNightStart.RemoveListener(HandleNightStart);
        dayNightCycle.onDayStart.RemoveListener(HandleDayStart);
        dayNightCycle.onDemoEnd.RemoveListener(HandleDemoEnd);
    }

    //se ejecuta al arrancar cada noche (noche 1, 2 o 3): habilita el spawn, pero el primer zombie recien aparece despues de initialSpawnDelay (ver comentario en la variable).
    private void HandleNightStart()
    {
        nightActive = true;
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    //apaga el spawn al llegar el dia.
    private void HandleDayStart()
    {
        nightActive = false;
    }

    //Spawn apagado
    private void HandleDemoEnd()
    {
        nightActive = false;
    }

    private void Update()
    //condiciones de no spawn: no es de noche, no hay prefab o spawn points, o ya hay demasiados zombies vivos.
    {
        if (!nightActive) return;
        if (Time.time < nextSpawnTime) return; //con esto arregle que spawneara todos los zombies de golpe
        if (zombiePrefab == null || spawnPoints == null || spawnPoints.Length == 0) return; 

        //cuenta cuantos zombies hay vivos usando el tag Enemy
        int currentZombies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentZombies >= maxZombiesAlive) return; //maximo de zombies alcanzado (limite de spawns al mismo tiempo)

        SpawnZombie();
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    private void SpawnZombie()
    {
        //elige un spawn point al azar e instancia el prefab ahi
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Debug.Log(Time.time);
        Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
    }
}