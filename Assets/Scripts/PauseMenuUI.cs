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
    [SerializeField, Min(1f)] private float timeLimitSeconds = 90f;

    private UIDocument document;
    private CheckpointSequence checkpointSequence;
    private Label checkpointCounter;
    private Label timerLabel;
    private Label bestTimeLabel;
    private VisualElement pauseOverlay;
    private VisualElement gameOverOverlay;
    private VisualElement chapterCompleteOverlay;
    private float previousTimeScale = 1f;
    private bool isPaused;
    private bool isGameOver;
    private bool isChapterComplete;
    private bool gameOverPending;
    private float remainingTime;
    private Coroutine gameOverRoutine;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        checkpointSequence = FindFirstObjectByType<CheckpointSequence>();
        remainingTime = timeLimitSeconds;
        BuildMenu();
    }

    private void OnDisable()
    {
        if (isPaused || isGameOver || isChapterComplete)
            Time.timeScale = previousTimeScale;
    }

    private void Update()
    {
        UpdateCheckpointCounter();
        UpdateGameTimer();

        if (!isGameOver && !isChapterComplete && Input.GetKeyDown(KeyCode.Escape))
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

        checkpointCounter = new Label();
        checkpointCounter.AddToClassList("checkpoint-counter");
        root.Add(checkpointCounter);
        UpdateCheckpointCounter();

        timerLabel = new Label();
        timerLabel.AddToClassList("game-timer");
        root.Add(timerLabel);
        UpdateGameTimerLabel();

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
        BuildChapterCompleteOverlay(root);
    }

    private void BuildChapterCompleteOverlay(VisualElement root)
    {
        chapterCompleteOverlay = new VisualElement();
        chapterCompleteOverlay.AddToClassList("chapter-complete-overlay");
        root.Add(chapterCompleteOverlay);

        VisualElement panel = new VisualElement();
        panel.AddToClassList("chapter-complete-panel");
        chapterCompleteOverlay.Add(panel);

        Label title = new Label("CHAPTER COMPLETE!");
        title.AddToClassList("chapter-complete-title");
        panel.Add(title);

        Label message = new Label("Congratulations! The kitchen chapter is completed. The drawing room chapter is coming soon");
        message.AddToClassList("chapter-complete-message");
        panel.Add(message);

        bestTimeLabel = new Label();
        bestTimeLabel.AddToClassList("chapter-best-time");
        panel.Add(bestTimeLabel);

        Button mainMenuButton = new Button(LoadMainMenu) { text = "MAIN MENU" };
        mainMenuButton.AddToClassList("pause-button");
        mainMenuButton.AddToClassList("pause-button-primary");
        mainMenuButton.AddToClassList("chapter-complete-menu-button");
        panel.Add(mainMenuButton);

        chapterCompleteOverlay.style.display = DisplayStyle.None;
    }

    public void ShowChapterComplete()
    {
        if (isGameOver || isChapterComplete)
            return;

        float completionTime = Mathf.Max(0f, timeLimitSeconds - remainingTime);
        float bestTime = GameScore.RecordCompletionTime(completionTime);
        bestTimeLabel.text = $"BEST TIME  {GameScore.FormatTime(bestTime)}";

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = false;
        isChapterComplete = true;
        pauseOverlay.style.display = DisplayStyle.None;
        gameOverOverlay.style.display = DisplayStyle.None;
        chapterCompleteOverlay.style.display = DisplayStyle.Flex;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    }

    private void UpdateGameTimer()
    {
        if (isGameOver || isChapterComplete || gameOverPending)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        UpdateGameTimerLabel();

        if (remainingTime <= 0f)
            TriggerGameOverImmediately();
    }

    private void UpdateGameTimerLabel()
    {
        if (timerLabel == null)
            return;

        int secondsLeft = Mathf.CeilToInt(remainingTime);
        timerLabel.text = $"TIME  {GameScore.FormatTime(secondsLeft)}";
    }

    private void UpdateCheckpointCounter()
    {
        if (checkpointCounter == null)
            return;

        if (checkpointSequence == null)
        {
            checkpointCounter.style.display = DisplayStyle.None;
            return;
        }

        checkpointCounter.text = $"CHECKPOINTS   {checkpointSequence.CompletedCheckpoints} / {checkpointSequence.TotalCheckpoints}";
        checkpointCounter.style.display = DisplayStyle.Flex;
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
        if (isGameOver || isChapterComplete || gameOverPending)
            return;

        gameOverPending = true;
        gameOverRoutine = StartCoroutine(ShowGameOverAfterDelay());
    }

    private System.Collections.IEnumerator ShowGameOverAfterDelay()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        gameOverRoutine = null;
        DisplayGameOver();
    }

    private void TriggerGameOverImmediately()
    {
        if (isGameOver || isChapterComplete)
            return;

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
            gameOverRoutine = null;
        }

        gameOverPending = false;
        DisplayGameOver();
    }

    private void DisplayGameOver()
    {
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
        isChapterComplete = false;
        gameOverPending = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;
        isChapterComplete = false;
        gameOverPending = false;

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
