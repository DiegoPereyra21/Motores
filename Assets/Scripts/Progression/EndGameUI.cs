using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("HUD")]
    [SerializeField] private HUDController hudController;

    [Header("Cinematica de final (opcional)")]
    [SerializeField] private EndingCinematic endingCinematic; //si esta asignada, victoria/derrota esperan a q termine la cinematica

    [Header("Camara de muerte")]
    [SerializeField] private GameObject deathCamera;

    [Header("Ambientacion de muerte")]
    [SerializeField] private GameObject deathPresentation;

    [Header("Fade de muerte")]
    [SerializeField] private float deathFadeDuration = 2f;

    [Header("Fade de victoria/derrota")]
    [SerializeField] private float endingPanelFadeDuration = 1.5f;

    [Header("Fade de restart")]
    [SerializeField] private float fadeToBlackDuration = 1f;

    [Header("Animacion del titulo de muerte")]
    [SerializeField] private float titleStartSize = 120f;
    [SerializeField] private float titleEndSize = 170f;

    private GameProgressController gameProgressController;
    private bool isSubscribed;
    private Coroutine panelFadeCoroutine;

    private VisualElement endingRoot;
    private VisualElement victoryPanel;
    private VisualElement defeatPanel;
    private VisualElement deathPanel;
    private VisualElement blackScreen;

    private Label deathTitle;

    private float originalAmbientIntensity;
    private bool ambientIntensitySaved;
    private bool deathPresentationActive;

    private Button deathRestartButton;
    private Button deathMainMenuButton;
    private Button victoryMainMenuButton;
    private Button defeatRetryButton;
    private Button defeatMainMenuButton;

    //audio (el AudioSource tiene que tener Output en el grupo SFX del MainMixer, si esta vacio se busca en este mismo objeto)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float mainMenuDelay = 0.3f; //espera antes de volver al menu para que el click se escuche (el reintentar ya tiene su fade)

    private void Awake()
    {
        // se guarda la iluminacion original
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        ambientIntensitySaved = true;

        if (uiDocument != null)
        {
            VisualElement root = uiDocument.rootVisualElement;

            endingRoot = root.Q<VisualElement>("EndingRoot");
            victoryPanel = root.Q<VisualElement>("VictoryPanel");
            defeatPanel = root.Q<VisualElement>("DefeatPanel");

            deathPanel = root.Q<VisualElement>("DeathPanel");
            deathTitle = root.Q<Label>("DeathTitle");

            deathRestartButton = root.Q<Button>("DeathCheckpointButton");
            deathMainMenuButton = root.Q<Button>("DeathMainMenuButton");
            victoryMainMenuButton = root.Q<Button>("VictoryMainMenuButton");
            defeatRetryButton = root.Q<Button>("DefeatRetryButton");
            defeatMainMenuButton = root.Q<Button>("DefeatMainMenuButton");

            blackScreen = root.Q<VisualElement>("BlackScreen");

            if (deathRestartButton != null) deathRestartButton.clicked += RestartGame;
            if (deathMainMenuButton != null) deathMainMenuButton.clicked += ReturnToMainMenu;
            if (victoryMainMenuButton != null) victoryMainMenuButton.clicked += ReturnToMainMenu;
            if (defeatRetryButton != null) defeatRetryButton.clicked += RestartGame;
            if (defeatMainMenuButton != null) defeatMainMenuButton.clicked += ReturnToMainMenu;

            HideAllPanels();
        }
        else
        {
            Debug.LogError("[EndGameUI] No se asigno UIDocument.", this);
        }
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();
    }

    private void OnEnable()
    {
        TryConnectToGameProgress();
    }

    private void OnDisable()
    {
        if (gameProgressController != null && isSubscribed)
            gameProgressController.OnGameEnded -= HandleGameEnded;

        gameProgressController = null;
        isSubscribed = false;

        RestoreDeathPresentation();
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgressController = GameProgressController.Instance;

        if (gameProgressController == null)
            return;

        gameProgressController.OnGameEnded += HandleGameEnded;
        isSubscribed = true;
    }

    private void HandleGameEnded(GameEnding ending)
    {
        HideAllPanels();

        switch (ending)
        {
            case GameEnding.Victory:
            case GameEnding.Defeat:
                //con cinematica, ella llama a ShowEndingPanel cuando termina
                if (endingCinematic != null && endingCinematic.isActiveAndEnabled)
                    return;

                ShowEndingPanel(ending);
                break;

            case GameEnding.Death:
                if (hudController != null)
                    hudController.HideHUD();

                if (deathPanel != null)
                {
                    if (deathCamera != null)
                        deathCamera.SetActive(true);

                    StartPanelFade(deathPanel, deathFadeDuration, true);
                }

                ShowDeathPresentation();

                Debug.Log("Mostrar pantalla de muerte");
                break;
        }
    }

    //lo llama EndingCinematic al terminar (o HandleGameEnded si no hay cinematica)
    public void ShowEndingPanel(GameEnding ending)
    {
        if (hudController != null)
            hudController.HideHUD();

        VisualElement panel = ending == GameEnding.Victory ? victoryPanel : defeatPanel;

        if (panel == null)
            return;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        StartPanelFade(panel, endingPanelFadeDuration, false);
    }

    //fade de la pantalla negra, se usa en la cinematica y en el restart. Al terminar en 0 se oculta para q no tape los botones
    public IEnumerator FadeBlackScreen(float from, float to, float duration)
    {
        if (blackScreen == null)
            yield break;

        blackScreen.style.display = DisplayStyle.Flex;
        blackScreen.style.opacity = from;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackScreen.style.opacity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        blackScreen.style.opacity = to;

        if (to <= 0f)
            blackScreen.style.display = DisplayStyle.None;
    }

    private void HideAllPanels()
    {
        if (victoryPanel != null)
            victoryPanel.style.display = DisplayStyle.None;

        if (defeatPanel != null)
            defeatPanel.style.display = DisplayStyle.None;

        if (deathPanel != null)
            deathPanel.style.display = DisplayStyle.None;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (deathRestartButton != null) deathRestartButton.SetEnabled(enabled);
        if (deathMainMenuButton != null) deathMainMenuButton.SetEnabled(enabled);
        if (victoryMainMenuButton != null) victoryMainMenuButton.SetEnabled(enabled);
        if (defeatRetryButton != null) defeatRetryButton.SetEnabled(enabled);
        if (defeatMainMenuButton != null) defeatMainMenuButton.SetEnabled(enabled);
    }

    private void RestartGame()
    {
        if (blackScreen == null)
        {
            Debug.LogError("[EndGameUI] No se encontro BlackScreen.", this);
            return;
        }

        PlayClick();
        SetButtonsEnabled(false);
        StartCoroutine(RestartGameSequence());
    }

    private void ReturnToMainMenu()
    {
        PlayClick();
        SetButtonsEnabled(false);
        StartCoroutine(ReturnToMainMenuSequence());
    }

    private IEnumerator ReturnToMainMenuSequence()
    {
        //tiempo sin escalar: en la muerte el mundo esta pausado
        if (clickClip != null && mainMenuDelay > 0f)
            yield return new WaitForSecondsRealtime(mainMenuDelay);

        Time.timeScale = 1f;

        RestoreDeathPresentation();

        SceneLoader.Load("MainMenu", 0f);
    }

    private void PlayClick()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    private void ShowDeathPresentation()
    {
        if (deathPresentation != null)
        {
            deathPresentation.SetActive(true);
            deathPresentationActive = true;
        }

        RenderSettings.ambientIntensity = 0f;
    }

    private void RestoreDeathPresentation()
    {
        if (deathPresentationActive && deathPresentation != null)
        {
            deathPresentation.SetActive(false);
            deathPresentationActive = false;
        }

        if (ambientIntensitySaved)
        {
            RenderSettings.ambientIntensity = originalAmbientIntensity;
        }
    }

    private void StartPanelFade(VisualElement panel, float duration, bool animateDeathTitle)
    {
        if (panelFadeCoroutine != null)
            StopCoroutine(panelFadeCoroutine);

        panelFadeCoroutine = StartCoroutine(FadeInPanel(panel, duration, animateDeathTitle));
    }

    private IEnumerator FadeInPanel(VisualElement panel, float duration, bool animateDeathTitle)
    {
        panel.style.display = DisplayStyle.Flex;
        panel.style.opacity = 0f;

        bool animateTitle = animateDeathTitle && deathTitle != null;

        if (animateTitle)
            deathTitle.style.fontSize = titleStartSize;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            panel.style.opacity = progress;

            if (animateTitle)
                deathTitle.style.fontSize = Mathf.Lerp(titleStartSize, titleEndSize, progress);

            yield return null;
        }

        panel.style.opacity = 1f;

        if (animateTitle)
            deathTitle.style.fontSize = titleEndSize;

        panelFadeCoroutine = null;
    }

    private IEnumerator RestartGameSequence()
    {
        yield return FadeBlackScreen(0f, 1f, fadeToBlackDuration);

        Time.timeScale = 1f;

        RestoreDeathPresentation();

        if (GameProgressController.Instance != null)
        {
            GameProgressController.Instance.ResetGameProgress();
        }

        SceneLoader.Load(SceneManager.GetActiveScene().name);
    }
}