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

    //initialize menu state and set up toggle button
    private void Start()
    {
        buildingMenuPanel.anchoredPosition = menuHiddenPosition;
        isMenuVisible = false;

        if (buildingMenuToggleButton != null)
        {
            buildingMenuToggleButton.onClick.AddListener(ToggleBuildingMenu);
        }
    }
    //checks for escape key to close the menu
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isMenuVisible)
        {
            CloseBuildingMenu();
        }
    }
    //toggles the building menu open or closed
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
    //opens the building menu and enters build mode
    public void OpenBuildingMenu()
    {
        if (isMenuVisible) return;

        if (BuildModeController.Instance != null && !BuildModeController.Instance.IsInBuildMode)
        {
            buildModeController.ToggleBuildMode();
        }

        AnimateMenu(menuVisiblePosition, true);
    }
    //closes the building menu and exits build mode
    public void CloseBuildingMenu()
    {
        if (!isMenuVisible) return;

        if (BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
        {
            SendMessage("ToggleBuildMode", SendMessageOptions.DontRequireReceiver);
        }

        if (BuildingPlacementManager.Instance != null)
        {
            BuildingPlacementManager.Instance.SetActiveBuilding(null);
        }

        AnimateMenu(menuHiddenPosition, false);
    }
    //starts the menu slide animation
    private void AnimateMenu(Vector2 targetPosition, bool willBeVisible)
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(SlideMenuCoroutine(targetPosition, willBeVisible));
    }
    //animates the menu sliding in or out
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
}
