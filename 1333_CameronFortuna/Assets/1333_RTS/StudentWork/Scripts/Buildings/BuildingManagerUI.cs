using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button buildingMenuToggleButton;
    [SerializeField] private RectTransform buildingMenuPanel;
    [SerializeField] private BuildingPlacementUI buildingPlacementUI;

    [Header("Animation Settings")]
    [SerializeField] private float slideAnimationSpeed = 0.3f;
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Menu Positions")]
    [SerializeField] private Vector2 menuHiddenPosition = new Vector2(-300f, 0f);
    [SerializeField] private Vector2 menuVisiblePosition = new Vector2(0f, 0f);

    private bool isMenuVisible = false;
    private Coroutine currentAnimation;

    [SerializeField] private BuildModeController buildModeController;

    private void Start()
    {
        // Initialize menu in hidden position
        buildingMenuPanel.anchoredPosition = menuHiddenPosition;
        isMenuVisible = false;

        // Setup button listener
        if (buildingMenuToggleButton != null)
        {
            buildingMenuToggleButton.onClick.AddListener(ToggleBuildingMenu);
        }

        // Make sure build mode starts disabled
        if (BuildModeController.Instance != null)
        {
            // If build mode is somehow enabled, disable it
            if (BuildModeController.Instance.IsInBuildMode)
            {
                // We can't directly call ToggleBuildMode, so we'll simulate the B key press
                // Or you could make ToggleBuildMode public in BuildModeController
            }
        }
    }

    private void Update()
    {
        // Close menu with Escape key
        if (Input.GetKeyDown(KeyCode.Escape) && isMenuVisible)
        {
            CloseBuildingMenu();
        }
    }

    public void ToggleBuildingMenu()
    {
        if (isMenuVisible)
        {
            CloseBuildingMenu();
        }
        else
        {
            OpenBuildingMenu();
        }
    }

    public void OpenBuildingMenu()
    {
        if (isMenuVisible) return;

        // Enter build mode
        if (BuildModeController.Instance != null && !BuildModeController.Instance.IsInBuildMode)
        {
            buildModeController.ToggleBuildMode();
        }

        // Animate menu in
        AnimateMenu(menuVisiblePosition, true);
    }

    public void CloseBuildingMenu()
    {
        if (!isMenuVisible) return;

        // Exit build mode
        if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
        {
            // Same as above - you'll need to make this accessible
            SendMessage("ToggleBuildMode", SendMessageOptions.DontRequireReceiver);
        }

        // Clear any selected building
        if (BuildingPlacementManager.Instance != null)
        {
            BuildingPlacementManager.Instance.SetActiveBuilding(null);
        }

        // Animate menu out
        AnimateMenu(menuHiddenPosition, false);
    }

    private void AnimateMenu(Vector2 targetPosition, bool willBeVisible)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(SlideMenuCoroutine(targetPosition, willBeVisible));
    }

    private IEnumerator SlideMenuCoroutine(Vector2 targetPosition, bool willBeVisible)
    {
        Vector2 startPosition = buildingMenuPanel.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < slideAnimationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / slideAnimationSpeed;
            float easedProgress = slideEase.Evaluate(progress);

            buildingMenuPanel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        buildingMenuPanel.anchoredPosition = targetPosition;
        isMenuVisible = willBeVisible;
        currentAnimation = null;
    }

    // Optional: Method to close menu when a building is successfully placed
    public void OnBuildingPlaced()
    {
        // You could choose to keep the menu open or close it after placement
        // CloseBuildingMenu();
    }
}