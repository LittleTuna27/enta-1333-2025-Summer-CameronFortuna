using System.Collections;
using UnityEngine;
using TMPro;

public class GameStartController : MonoBehaviour
{
    [Header("Game Start Settings")]
    [SerializeField] private BuildingData castleBuildingData; // Assign your castle BuildingData in inspector

    [Header("UI References")]
    [SerializeField] private TMP_Text instructionText; // Just the instruction text

    [Header("Countdown Settings")]
    [SerializeField] private float countdownDuration = 5f;

    public static GameStartController Instance { get; private set; }

    private bool gameHasStarted = false;
    private bool castlePlaced = false;

    // Events for other scripts to subscribe to
    public System.Action OnGameStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("GameStartController: Starting initialization...");
        InitializeGameStart();
    }

    private void InitializeGameStart()
    {
        gameHasStarted = false;
        castlePlaced = false;

        StartCoroutine(ForceBuildModeWithCastle());
    }

    private IEnumerator ForceBuildModeWithCastle()
    {
        // Wait a few frames to ensure all managers are initialized
        yield return new WaitForSeconds(0.5f);

        if (BuildModeController.Instance != null)
        {
            if (!BuildModeController.Instance.IsInBuildMode)
            {
                BuildModeController.Instance.ToggleBuildMode();
                Debug.Log("Build mode activated");
            }
        }
        else
        {
            Debug.LogError("BuildModeController.Instance is null!");
        }

        // Wait another frame
        yield return null;

            BuildingPlacementManager.Instance.SetActiveBuilding(castleBuildingData);

    }

    public void OnCastlePlacedCallback()
    {
        if (castlePlaced) return; // Prevent multiple calls

        castlePlaced = true;
        Debug.Log("Castle placed! Starting countdown...");

        // Immediately clear the active building to prevent more castle placements
        if (BuildingPlacementManager.Instance != null)
        {
            BuildingPlacementManager.Instance.SetActiveBuilding(null);
            Debug.Log("Cleared active building - no more castles can be placed");
        }

        // Update instruction text
        if (instructionText != null)
            instructionText.text = $"Castle placed! Game starting in {countdownDuration} seconds...";

        if (castlePlaced) return;
        castlePlaced = true;

        BuildingPlacementManager.Instance?.SetActiveBuilding(null);
        instructionText.text = $"Castle placed! Game starting in {countdownDuration} seconds...";
        StartCoroutine(CountdownAndStartGame());
    }

    private void StartGame()
    {
        gameHasStarted = true;
        Debug.Log("Game officially started!");

        // Hide instruction text
        if (instructionText != null)
            instructionText.gameObject.SetActive(false);

        // Exit build mode if still in it
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
        {
            BuildModeController.Instance.ToggleBuildMode();
            Debug.Log("Exited build mode");
        }

        //// Clear active building
        //if (BuildingPlacementManager.Instance != null)
        //{
        //    BuildingPlacementManager.Instance.SetActiveBuilding(null);
        //}

        // Notify other scripts that the game has started
        OnGameStarted?.Invoke();
    }
    private IEnumerator CountdownAndStartGame()
    {
        yield return new WaitForSeconds(countdownDuration);
        StartGame();
    }
    // Public methods for checking game state
    public bool HasGameStarted()
    {
        return gameHasStarted;
    }

    public bool IsCastlePlaced()
    {
        return castlePlaced;
    }

    public bool CanPlayerInteract()
    {
        return gameHasStarted; // Only allow interaction after game officially starts
    }

}