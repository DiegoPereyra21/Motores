using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MAPA"; //poner la version final 
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
        SceneManager.LoadScene(gameSceneName);
    }
    private void OnQuitClicked()//para que ande tambien en unity, sino no hace nada 
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}