using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [SerializeField] protected UnitType _unitType;

    public virtual int Width => _unitType != null ? _unitType.Width : 1;

    public virtual int Height => _unitType != null ? _unitType.Height : 1;

    public abstract void MoveTo(GridNode targetNode);
    public abstract void DoMove();


}
