using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPlacementUI : MonoBehaviour
{
    [SerializeField] private RectTransform LayoutGroupParent;
    [SerializeField] private SelectBuildingButton ButtonPrefab;
    [SerializeField] private BuildingTypesSO BuildingData;
    [SerializeField] private int SoundNumber = 1;
    private void Start()
    {
        foreach (BuildingData t in BuildingData.Buildings)
        {
            SelectBuildingButton Button = Instantiate(ButtonPrefab, LayoutGroupParent);
            Button.Setup(t);

        }
     }
}
