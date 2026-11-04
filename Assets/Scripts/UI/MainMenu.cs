using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MAPA"; //poner la version final
    //audio (el AudioSource tiene que tener Output en el grupo SFX del MainMixer, si esta vacio se busca en este mismo objeto)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float actionDelay = 0.3f; //espera antes de cargar la escena o salir, para que el click se escuche (si no se corta al cambiar de escena)
    //privados
    private Button playButton;
    private Button quitButton;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        playButton = root.Q<Button>("ButtonPlay");
        quitButton = root.Q<Button>("ButtonQuit");
        playButton.clicked += OnPlayClicked;
        quitButton.clicked += OnQuitClicked;
        //desactivar cursor cuando entre en play
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= OnPlayClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
    }
    private void OnPlayClicked()
    {
        if (SceneLoader.IsLoading) return;//evita doble click mientras carga
        PlayClick();
        StartCoroutine(RunAfterDelay(() => SceneLoader.Load(gameSceneName)));
    }

    private void OnQuitClicked()
    {
        PlayClick();
        StartCoroutine(RunAfterDelay(QuitGame));
    }

    private void PlayClick()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    //sin clip no espera nada (queda como antes). Tiempo sin escalar por si el menu se abre con timeScale en 0
    private System.Collections.IEnumerator RunAfterDelay(System.Action action)
    {
        if (clickClip != null && actionDelay > 0f)
            yield return new WaitForSecondsRealtime(actionDelay);

        action();
    }

    private void QuitGame()//para que ande tambien en unity, sino no hace nada
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}