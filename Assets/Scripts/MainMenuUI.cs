using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Builds a UI Toolkit main menu at runtime. Attach to a GameObject with a
/// UIDocument and assign the gameplay scene name in the Inspector.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private StyleSheet menuStyle;
    [SerializeField] private Texture2D gameLogo;

    private UIDocument document;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        BuildMenu();
    }

    private void BuildMenu()
    {
        VisualElement root = document.rootVisualElement;
        root.Clear();
        root.AddToClassList("menu-root");

        if (menuStyle != null && !root.styleSheets.Contains(menuStyle))
            root.styleSheets.Add(menuStyle);

        VisualElement panel = new VisualElement();
        panel.AddToClassList("menu-panel");
        root.Add(panel);

        if (gameLogo != null)
        {
            Image logo = new Image
            {
                image = gameLogo,
                scaleMode = ScaleMode.ScaleToFit
            };
            logo.AddToClassList("menu-logo");
            panel.Add(logo);
        }

        VisualElement content = new VisualElement();
        content.AddToClassList("menu-content");
        panel.Add(content);

        Label subtitle = new Label("Tiny wheels. Big adventures.");
        subtitle.AddToClassList("menu-subtitle");
        content.Add(subtitle);

        VisualElement buttons = new VisualElement();
        buttons.AddToClassList("menu-buttons");
        content.Add(buttons);

        Button startButton = CreateButton("START GAME", "menu-button", StartGame);
        startButton.AddToClassList("menu-button-primary");
        buttons.Add(startButton);
        buttons.Add(CreateButton("OPTIONS", "menu-button", ShowOptions));
        buttons.Add(CreateButton("QUIT", "menu-button", QuitGame));

        Label footer = new Label("W A S D   TO DRIVE     •     MOUSE TO LOOK");
        footer.AddToClassList("menu-footer");
        content.Add(footer);
    }

    private static Button CreateButton(string text, string className, System.Action onClick)
    {
        Button button = new Button(onClick) { text = text };
        button.AddToClassList(className);
        return button;
    }

    private void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Set Game Scene Name on MainMenuUI to the name of a gameplay scene in Build Profiles.", this);
            return;
        }

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameSceneName);
        if (sceneIndex < 0)
        {
            // Also accept a scene name rather than a full path.
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == gameSceneName)
                {
                    sceneIndex = i;
                    break;
                }
            }
        }

        if (sceneIndex < 0)
        {
            Debug.LogError($"Gameplay scene '{gameSceneName}' is not in Build Profiles > Scene List. Add it and set its name on MainMenuUI.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void ShowOptions()
    {
        Debug.Log("Options menu is not implemented yet.", this);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
