using UnityEngine;

public class CastleGameOver : MonoBehaviour
{
    [Header("Game Over Settings")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";

    private BuildingHealth castleHealth;
    private void Start()
    {
        // Get the BuildingHealth component on this castle
        castleHealth = GetComponent<BuildingHealth>();
    }
    private void Update()
    {
        if (castleHealth.CurrentHealth <= 0)
        {
            LoadGameOverScene();
        }
    }
    private void LoadGameOverScene()
    {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
    }
}