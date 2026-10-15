using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Camara de muerte")]
    [SerializeField] private GameObject deathCamera;

    [Header("Ambientacion de muerte")]
    [SerializeField] private GameObject deathPresentation;

    [Header("Fade de muerte")]
    [SerializeField] private float deathFadeDuration = 2f;

    [Header("Fade de restart")]
    [SerializeField] private float fadeToBlackDuration = 1f;

    [Header("Animacion del titulo de muerte")]
    [SerializeField] private float titleStartSize = 120f;
    [SerializeField] private float titleEndSize = 170f;

    private GameProgressController gameProgressController;
    private Coroutine deathFadeCoroutine;

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

            blackScreen = root.Q<VisualElement>("BlackScreen");

            if (deathRestartButton != null)
                deathRestartButton.clicked += RestartGame;

            if (deathMainMenuButton != null)
                deathMainMenuButton.clicked += ReturnToMainMenu;

            HideAllPanels();
        }
        else
        {
            Debug.LogError("[EndGameUI] No se asigno UIDocument.", this);
        }
    }

    private void OnEnable()
    {
        gameProgressController = GameProgressController.Instance;

        if (gameProgressController == null)
        {
            Debug.LogError("[EndGameUI] No se encontró GameProgressController.", this);
            return;
        }

        gameProgressController.OnGameEnded += HandleGameEnded;
    }

    private void OnDisable()
    {
        if (gameProgressController != null)
        {
            gameProgressController.OnGameEnded -= HandleGameEnded;
        }

        RestoreDeathPresentation();
    }

    private void HandleGameEnded(GameEnding ending)
    {
        HideAllPanels();

        switch (ending)
        {
            case GameEnding.Victory:
                if (victoryPanel != null)
                    victoryPanel.style.display = DisplayStyle.Flex;

                Debug.Log("Mostrar pantalla de victoria");
                break;

            case GameEnding.Defeat:
                if (defeatPanel != null)
                    defeatPanel.style.display = DisplayStyle.Flex;

                Debug.Log("Mostrar pantalla de derrota");
                break;

            case GameEnding.Death:
                if (deathPanel != null)
                {
                    deathPanel.style.display = DisplayStyle.Flex;
                    deathPanel.style.opacity = 0f;

                    if (deathFadeCoroutine != null)
                        StopCoroutine(deathFadeCoroutine);

                    if (deathCamera != null)
                    {
                        deathCamera.SetActive(true);
                    }

                    deathFadeCoroutine = StartCoroutine(FadeInDeathPanel());
                }

                ShowDeathPresentation();

                Debug.Log("Mostrar pantalla de muerte");
                break;
        }
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

    private void RestartGame()
    {
        if (blackScreen == null)
        {
            Debug.LogError("[EndGameUI] No se encontró BlackScreen.", this);
            return;
        }

        if (deathRestartButton != null)
            deathRestartButton.SetEnabled(false);

        if (deathMainMenuButton != null)
            deathMainMenuButton.SetEnabled(false);

        StartCoroutine(RestartGameSequence());
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        RestoreDeathPresentation();

        SceneManager.LoadScene("MainMenu");
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

    private System.Collections.IEnumerator FadeInDeathPanel()
    {
        if (deathPanel == null)
            yield break;

        deathPanel.style.opacity = 0f;

        if (deathTitle != null)
            deathTitle.style.fontSize = titleStartSize;

        float elapsed = 0f;

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / deathFadeDuration);
            deathPanel.style.opacity = progress;

            if (deathTitle != null)
            {
                float currentSize = Mathf.Lerp(titleStartSize, titleEndSize, progress);

                deathTitle.style.fontSize = currentSize;
            }

            yield return null;
        }

        deathPanel.style.opacity = 1f;

        if (deathTitle != null)
            deathTitle.style.fontSize = titleEndSize;

        deathFadeCoroutine = null;
    }

    private IEnumerator RestartGameSequence()
    {
        blackScreen.style.display = DisplayStyle.Flex;
        blackScreen.style.opacity = 0f;

        float elapsed = 0f;

        while (elapsed < fadeToBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeToBlackDuration);

            blackScreen.style.opacity = progress;

            yield return null;
        }

        blackScreen.style.opacity = 1f;

        Time.timeScale = 1f;

        RestoreDeathPresentation();

        if (GameProgressController.Instance != null)
        {
            GameProgressController.Instance.ResetGameProgress();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}