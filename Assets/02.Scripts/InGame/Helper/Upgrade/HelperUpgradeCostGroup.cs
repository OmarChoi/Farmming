using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct HelperUpgradeCostGroup
{
    [SerializeField] private EHelperGrade _fromGrade;
    [SerializeField] private int _goldCost;
    [SerializeField] private HelperUpgradeCostEntry[] _costs;

    public EHelperGrade FromGrade => _fromGrade;
    public int GoldCost => _goldCost;
    public IReadOnlyList<HelperUpgradeCostEntry> Costs => _costs;
}
