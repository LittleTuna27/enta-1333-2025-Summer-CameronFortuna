using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitType", menuName = "Game/Unit Type")]

public class UnitType : ScriptableObject
{
    [SerializeField] private int _maxHp;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int _damage;
    [SerializeField] private int _defence;
    [SerializeField] private AttackType _attackType;
    [SerializeField] private int _range;
    [SerializeField] private int _width;
    [SerializeField] private int _height;

    public int Width => _width;
    public int Height => _height;

    public int MaxHp => _maxHp;
    public float MoveSpeed => _moveSpeed;
    public int Damage => _damage;
    public int Defence => _defence;
    public AttackType AttackType => _attackType;
    public int Range => _range;
}


