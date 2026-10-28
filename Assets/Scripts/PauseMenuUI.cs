using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>Escape-key pause overlay for the gameplay scene.</summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private StyleSheet pauseStyle;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Min(0f)] private float gameOverDelay = 1f;

    private UIDocument document;
    private VisualElement pauseOverlay;
    private VisualElement gameOverOverlay;
    private float previousTimeScale = 1f;
    private bool isPaused;
    private bool isGameOver;
    private bool gameOverPending;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        BuildMenu();
    }

    private void OnDisable()
    {
        if (isPaused || isGameOver)
            Time.timeScale = previousTimeScale;
    }

    private void Update()
    {
        if (!isGameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    private void BuildMenu()
    {
        VisualElement root = document.rootVisualElement;
        root.Clear();
        if (pauseStyle != null && !root.styleSheets.Contains(pauseStyle))
            root.styleSheets.Add(pauseStyle);

        pauseOverlay = new VisualElement();
        pauseOverlay.AddToClassList("pause-overlay");
        root.Add(pauseOverlay);

        VisualElement panel = new VisualElement();
        panel.AddToClassList("pause-panel");
        pauseOverlay.Add(panel);

        Label title = new Label("PAUSED");
        title.AddToClassList("pause-title");
        panel.Add(title);

        Button resumeButton = new Button(ResumeGame) { text = "RESUME" };
        resumeButton.AddToClassList("pause-button");
        resumeButton.AddToClassList("pause-button-primary");
        panel.Add(resumeButton);

        Button restartButton = new Button(RestartGame) { text = "RESTART" };
        restartButton.AddToClassList("pause-button");
        panel.Add(restartButton);

        Label volumeLabel = new Label("VOLUME");
        volumeLabel.AddToClassList("pause-volume-label");
        panel.Add(volumeLabel);

        Slider volumeSlider = new Slider(0f, 1f)
        {
            value = AudioListener.volume,
            showInputField = false
        };
        volumeSlider.AddToClassList("pause-volume-slider");
        volumeSlider.RegisterValueChangedCallback(evt => AudioListener.volume = evt.newValue);
        panel.Add(volumeSlider);

        pauseOverlay.style.display = DisplayStyle.None;

        BuildGameOverOverlay(root);
    }

    private void BuildGameOverOverlay(VisualElement root)
    {
        gameOverOverlay = new VisualElement();
        gameOverOverlay.AddToClassList("gameover-overlay");
        root.Add(gameOverOverlay);

        VisualElement panel = new VisualElement();
        panel.AddToClassList("gameover-panel");
        gameOverOverlay.Add(panel);

        Label title = new Label("GAME OVER");
        title.AddToClassList("gameover-title");
        panel.Add(title);

        Button restartButton = new Button(RestartGame) { text = "RESTART" };
        restartButton.AddToClassList("pause-button");
        restartButton.AddToClassList("pause-button-primary");
        panel.Add(restartButton);

        Button mainMenuButton = new Button(LoadMainMenu) { text = "MAIN MENU" };
        mainMenuButton.AddToClassList("pause-button");
        panel.Add(mainMenuButton);

        gameOverOverlay.style.display = DisplayStyle.None;
    }

    public void TriggerGameOver()
    {
        if (isGameOver || gameOverPending)
            return;

        gameOverPending = true;
        StartCoroutine(ShowGameOverAfterDelay());
    }

    private System.Collections.IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = false;
        isGameOver = true;
        gameOverPending = false;
        pauseOverlay.style.display = DisplayStyle.None;
        gameOverOverlay.style.display = DisplayStyle.Flex;
        gameOverOverlay.Q<Button>().Focus();
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    }

    private void PauseGame()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
        pauseOverlay.style.display = DisplayStyle.Flex;
        pauseOverlay.Q<Button>().Focus();
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    }

    private void ResumeGame()
    {
        Time.timeScale = previousTimeScale;
        isPaused = false;
        pauseOverlay.style.display = DisplayStyle.None;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(mainMenuSceneName);
        if (sceneIndex < 0)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == mainMenuSceneName)
                {
                    sceneIndex = i;
                    break;
                }
            }
        }

        if (sceneIndex < 0)
        {
            Debug.LogError($"Main menu scene '{mainMenuSceneName}' is not in Build Profiles > Scene List.", this);
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}
