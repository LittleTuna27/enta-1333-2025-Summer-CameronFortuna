using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTileBehaviour : MonoBehaviour
{
    public GridNode nodeData;

    public void Initialize(GridNode node)
    {
        nodeData = node;

       
        var renderer = GetComponent<Renderer>();
        if (renderer != null && node.terrainType != null)
        {
            renderer.material.color = node.terrainType.GizmoColour;
        }
    }
}
