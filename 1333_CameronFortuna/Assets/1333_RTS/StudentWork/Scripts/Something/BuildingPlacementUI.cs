using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacementUI : MonoBehaviour
{
    [SerializeField] private RectTransform LayoutGroupParent;
    [SerializeField] private SelectBuiildingButton ButtonPrefab;
    [SerializeField] private BuildingTypesSO BuildingData;
    

    private void Start()
    {
        foreach (BuildingData t in BuildingData.Buildings)
        {
            SelectBuiildingButton Button = Instantiate(ButtonPrefab,LayoutGroupParent);
            Button.Setup(t);

        }
    }



}
