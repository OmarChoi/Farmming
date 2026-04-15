using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public struct HelperUpgradeCostGroup
{
    [SerializeField] private EHelperGrade _fromGrade;
    [SerializeField] private HelperUpgradeCostEntry[] _costs;

    public EHelperGrade FromGrade => _fromGrade;
    public IReadOnlyList<HelperUpgradeCostEntry> Costs => _costs;
}
