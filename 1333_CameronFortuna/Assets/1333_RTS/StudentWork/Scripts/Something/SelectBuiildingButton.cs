using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectBuiildingButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Button text;

    private BuildingData buildingDataForButton;
    public void Setup(BuildingData buildingData)
    {
        buildingDataForButton = buildingData;

        buttonText.text = buildingDataForButton.BuildingName;

        //buttonImage.sprite = buildingDataForBUtton.buildingIcon;
    }
}
