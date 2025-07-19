using UnityEngine;

public class CastleGameOver : MonoBehaviour
{
    [Header("Game Over Settings")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private float gameOverDelay = 2f; // Delay before switching to game over
    [SerializeField] private bool useSceneLoader = false; // If you want to use your fancy SceneLoader with fade

    [Header("Optional References")]
    [SerializeField] private SceneLoader sceneLoader; // Reference to your SceneLoader if you want fade effect
    [SerializeField] private SimpleSceneChanger simpleSceneChanger; // Or use the simple one

    private BuildingHealth castleHealth;
    private bool gameOverTriggered = false;

    private void Start()
    {
        // Get the BuildingHealth component on this castle
        castleHealth = GetComponent<BuildingHealth>();

        if (castleHealth == null)
        {
            Debug.LogError($"CastleGameOver: No BuildingHealth component found on {gameObject.name}!");
            return;
        }

        // Subscribe to the building destruction event
        castleHealth.OnBuildingDestroyed.AddListener(TriggerGameOver);

        Debug.Log($"CastleGameOver: Monitoring castle {gameObject.name} for destruction");
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered) return; // Prevent multiple triggers

        gameOverTriggered = true;

        Debug.Log("CASTLE DESTROYED! Triggering Game Over...");

        // Add any game over effects here (sound, particles, etc.)

        // Switch to game over scene after delay
        Invoke(nameof(LoadGameOverScene), gameOverDelay);
    }

    private void LoadGameOverScene()
    {
        if (useSceneLoader && sceneLoader != null)
        {
            // Use the fancy SceneLoader with fade effect
            sceneLoader.sceneName = gameOverSceneName;
            sceneLoader.LoadSceneWithFade();
        }
        else if (simpleSceneChanger != null)
        {
            // Use the simple scene changer
            simpleSceneChanger.LoadSceneByName(gameOverSceneName);
        }
        else
        {
            // Fallback to direct scene loading
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }

        Debug.Log($"Loading Game Over Scene: {gameOverSceneName}");
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (castleHealth != null)
        {
            castleHealth.OnBuildingDestroyed.RemoveListener(TriggerGameOver);
        }
    }
}