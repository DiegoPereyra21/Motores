using UnityEngine;

//Spawner de zombies para la noche, limpieza de zombies para el dia. Escucha eventos de DayNightCycle.cs.
//Poner tag "Enemy" a los zombies (para deteccion de golpe del player y para la cuenta de zombies vivos).

public class ZombieSpawner : MonoBehaviour
{
    [Header("Config de spawn")]
    [SerializeField] private GameObject zombiePrefab; //prefab a instanciar
    [SerializeField] private Transform[] spawnPoints; //puntos posibles de aparicion
    [SerializeField] private float timeBetweenSpawns = 4f; //segundos entre spawns
    [SerializeField] private int maxZombiesAlive = 10; //limite de zombies vivos a la vez (podemos ampliarlo para que varie segun la oleada)

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

    //se ejecuta al arrancar cada noche (Noche 1, 2 o 3): habilita el spawn y hace que el primer zombie aparezca de inmediato en vez de esperar timeBetweenSpawns.
    private void HandleNightStart()
    {
        nightActive = true;
        nextSpawnTime = Time.time;
    }

    //apaga el spawn y limpia todos los zombies vivos de la escena. Se ejecuta al llegar el dia
    private void HandleDayStart()
    {
        nightActive = false;
        ClearAllZombies();
    }

    //Esto apaga el spawn pero no limpia los zombies porque aca termina la demo.
    private void HandleDemoEnd()
    {
        nightActive = false;
    }

    //Destruye gameobjects con tag "Enemy".
    private void ClearAllZombies()
    {
        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Destroy(zombie);
        }
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