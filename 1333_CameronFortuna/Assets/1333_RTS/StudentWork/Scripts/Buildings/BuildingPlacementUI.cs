using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacementUI : MonoBehaviour
{
    [SerializeField] private RectTransform LayoutGroupParent;
    [SerializeField] private SelectBuildingButton ButtonPrefab;
    [SerializeField] private BuildingTypesSO BuildingData;
    

    private void Start()
    {
        foreach (BuildingData t in BuildingData.Buildings)
        {
            SelectBuildingButton Button = Instantiate(ButtonPrefab,LayoutGroupParent);
            Button.Setup(t);

        }
    }
}
