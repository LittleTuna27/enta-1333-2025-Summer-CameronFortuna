using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Needed for TextMeshPro UI text

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }

    public bool IsInBuildMode { get; private set; } = false;

    [Header("Optional UI Reference")]
    [SerializeField] private TextMeshProUGUI buildModeText;

    private void Awake()
    {
        // Singleton check
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Make sure build mode starts off
        IsInBuildMode = false;
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBuildMode();
        }
    }

    private void ToggleBuildMode()
    {
        IsInBuildMode = !IsInBuildMode;
        Debug.Log("Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled"));
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (buildModeText != null)
        {
            buildModeText.text = "Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled");
        }
    }
}
