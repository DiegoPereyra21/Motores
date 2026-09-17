using UnityEngine;
using UnityEngine.SceneManagement;

//Condicion de victoria si el player sobrevive toda la demo
public class DemoEndUI : MonoBehaviour
{
    [SerializeField] private DayNightCycle dayNightCycle; //el "reloj" del que escucha el evento de fin de demo
    [SerializeField] private GameObject demoEndUI; //texto/panel de "Fin de la demo", desactivado por defecto en la escena
    //para volver al menu luego de ganar (luego ver si poner boton)
    private string mainMenuSceneName = "MainMenu"; //nombre de la escena del menu principal
    [SerializeField] private float secondsBeforeMainMenu = 3f; //cuanto se queda el panel antes de volver

    private void OnEnable()
    {
        dayNightCycle.onDemoEnd.AddListener(HandleDemoEnd);
    }

    private void OnDisable()
    {
        dayNightCycle.onDemoEnd.RemoveListener(HandleDemoEnd);
    }

    //se ejecuta una sola vez, cuando DayNightCycle.cs dispara onDemoEnd (justo al terminar la noche 3).
    private void HandleDemoEnd()
    {
        if (demoEndUI != null)
            demoEndUI.SetActive(true);

        //misma pausa total que usa PlayerDeath.cs al morir el jugador: congela zombies, fisica y
        //cualquier timer basado en Time.deltaTime/Time.time, incluido el propio DayNightCycle
        Time.timeScale = 0f;

        Debug.Log("Demo completed: the player survived all nights");

        //waitforsecondsrealtime ignora el timescale, por eso funciona con el juego pausado
        StartCoroutine(GoToMainMenuAfterDelay());
    }

    private System.Collections.IEnumerator GoToMainMenuAfterDelay()
    {
        yield return new WaitForSecondsRealtime(secondsBeforeMainMenu);

        //reactivo el timeScale antes de cargar, sino el menu tambien queda congelado
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}