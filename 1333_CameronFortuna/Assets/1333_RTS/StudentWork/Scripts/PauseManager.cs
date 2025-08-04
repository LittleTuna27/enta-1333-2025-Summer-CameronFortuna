using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Management")]
    [SerializeField] private SimpleSceneChanger sceneChanger;
    [SerializeField] private string mainMenuSceneName = "TitleScrene"; // Set this to your main menu scene name

    private bool isPaused = false;

    public bool IsPaused => isPaused;

    void Start()
    {
        // Find SimpleSceneChanger if not assigned
        if (sceneChanger == null)
            sceneChanger = FindObjectOfType<SimpleSceneChanger>();

        // Make sure pause menu starts hidden
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Hook up button events
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void Update()
    {
        // Check for Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze the game

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        // Show cursor if it was hidden during gameplay
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume normal time

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Debug.Log("Game Resumed");
    }

    private void OpenSettings()
    {
        // You can implement settings menu later
        Debug.Log("Settings button pressed - implement settings menu here");
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Reset time scale before changing scenes

        if (sceneChanger != null)
        {
            sceneChanger.LoadSceneByName(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("SimpleSceneChanger not found!");
        }
    }

    private void QuitGame()
    {
        if (sceneChanger != null)
        {
            sceneChanger.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }
}