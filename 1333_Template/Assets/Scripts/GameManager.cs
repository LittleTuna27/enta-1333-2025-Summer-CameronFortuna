using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private PathFinder pathFinder;

    private void Awake()
    {
        gridManager.InitializeGrid();
        pathFinder.ResetFeild();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            gridManager.InitializeGrid();
            pathFinder.ResetFeild();
        }
    }
}
