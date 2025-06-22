using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [SerializeField] protected UnitType unitType;

    public virtual int Width => unitType != null ? unitType.Width : 1;
    public virtual int Height => unitType != null ? unitType.Height : 1;


    // called to assign a target node to move toward
    public abstract void MoveTo(GridNode targetNode);

    // handles single-frame/tick movement update
    public abstract void DoMove();


}