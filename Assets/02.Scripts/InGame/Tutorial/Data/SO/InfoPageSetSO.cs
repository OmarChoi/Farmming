using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InfoPageSetSO", menuName = "Scriptable Objects/InfoPageSetSO")]
public class InfoPageSetSO : ScriptableObject
{
    [SerializeField] private List<InfoPageData> _pages = new();

    public IReadOnlyList<InfoPageData> Pages => _pages;

    public int PageCount => _pages?.Count ?? 0;

    public bool HasPages()
    {
        return PageCount > 0;
    }
}
