using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HelperDatabase", menuName = "Scriptable Objects/HelperDatabase")]
public class HelperDatabase : ScriptableObject
{
    [SerializeField] private List<HelperDataSO> _helpers;
    private Dictionary<string, HelperDataSO> _dictionary;

    public HelperDataSO GetById(string helperId)
    {
        EnsureDictionary();
        _dictionary.TryGetValue(helperId, out HelperDataSO helper);
        return helper;
    }

    private void EnsureDictionary()
    {
        if (_dictionary != null && _dictionary.Count > 0) return;

        _dictionary = new Dictionary<string, HelperDataSO>();
        foreach (var helper in _helpers)
        {
            if (helper != null && !string.IsNullOrEmpty(helper.HelperId))
                _dictionary[helper.HelperId] = helper;
        }
    }
}