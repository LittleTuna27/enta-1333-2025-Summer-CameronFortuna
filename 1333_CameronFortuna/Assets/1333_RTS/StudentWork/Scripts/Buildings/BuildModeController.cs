using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Needed for TextMeshPro UI text

public class BuildModeController : MonoBehaviour
{
    public static BuildModeController Instance { get; private set; }
    public bool IsInBuildMode { get; private set; } = false;
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
    }
    public void ToggleBuildMode()
    {
        IsInBuildMode = !IsInBuildMode;
        Debug.Log("Build Mode: " + (IsInBuildMode ? "Enabled" : "Disabled"));
       
    }
}
