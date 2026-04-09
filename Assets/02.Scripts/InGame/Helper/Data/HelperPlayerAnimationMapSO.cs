using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HelperPlayerAnimationMapSO", menuName = "Scriptable Objects/HelperPlayerAnimationMapSO")]
public class HelperPlayerAnimationMapSO : ScriptableObject
{
    [SerializeField] private List<Entry> _entries = new();

    public bool TryGetTriggers(HelperDataSO helperData, out string primaryTrigger, out string secondaryTrigger)
    {
        primaryTrigger = null;
        secondaryTrigger = null;

        if (helperData == null)
            return false;

        foreach (var entry in _entries)
        {
            if (entry == null || entry.Helper != helperData)
                continue;

            primaryTrigger = entry.PrimaryTrigger;
            secondaryTrigger = entry.SecondaryTrigger;
            return true;
        }

        return false;
    }

    [Serializable]
    public class Entry
    {
        public HelperDataSO Helper;
        public string PrimaryTrigger;
        public string SecondaryTrigger;
    }
}
