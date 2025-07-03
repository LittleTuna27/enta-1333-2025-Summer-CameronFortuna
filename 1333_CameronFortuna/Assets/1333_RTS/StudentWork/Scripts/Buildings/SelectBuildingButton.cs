using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectBuildingButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Button buttonComponent;

    private BuildingData buildingDataForButton;

    public void Setup(BuildingData buildingData)
    {
        if (buttonText == null) Debug.LogError("buttonText not assigned!");
        if (buttonImage == null) Debug.LogError("buttonImage not assigned!");
        if (buttonComponent == null) Debug.LogError("buttonComponent not assigned!");

        buildingDataForButton = buildingData;
        buttonText.text = buildingDataForButton.BuildingName;
        buttonImage.sprite = buildingDataForButton.BuildingIcon;

        buttonComponent.onClick.AddListener(() =>
        {
            BuildingPlacementManager.Instance.SetActiveBuilding(buildingDataForButton);
        });
    }
}