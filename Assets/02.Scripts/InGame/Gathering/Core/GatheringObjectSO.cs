using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GatheringObject", menuName = "Gathering/GatheringObjectSO")]
public class GatheringObjectSO : ScriptableObject
{
    [System.Serializable]
    public struct ModelDropOverride
    {
        [SerializeField] private DropEntry[] _drops;

        public DropEntry[] Drops => _drops ?? Array.Empty<DropEntry>();
        public bool HasDrops => _drops != null && _drops.Length > 0;
    }

    [Header("기본 정보")]
    [SerializeField] private string _objectName;
    [SerializeField] private int _maxHealth = 100;

    [Header("나무 등급")]
    [SerializeField] private EHelperGrade _requiredLevel = EHelperGrade.Normal;

    [Header("모델 (랜덤 선택)")]
    [SerializeField] private GameObject[] _modelPrefabs;

    [Header("공통 보상")]
    [SerializeField] private DropEntry[] _drops;

    [Header("보상 설정")]
    [SerializeField] private ModelDropOverride[] _modelDropOverrides;

    public string ObjectName => _objectName;
    public int MaxHealth => _maxHealth;
    public EHelperGrade RequiredLevel => _requiredLevel;

    public GameObject GetRandomModel()
    {
        if (_modelPrefabs == null || _modelPrefabs.Length == 0)
            return null;

        return _modelPrefabs[UnityEngine.Random.Range(0, _modelPrefabs.Length)];
    }

    public GameObject GetModelByIndex(int index)
    {
        if (_modelPrefabs == null || _modelPrefabs.Length == 0)
            return null;

        return _modelPrefabs[Mathf.Clamp(index, 0, _modelPrefabs.Length - 1)];
    }

    public int ModelCount => _modelPrefabs != null ? _modelPrefabs.Length : 0;
    public DropEntry[] Drops => _drops ?? Array.Empty<DropEntry>();

    public DropEntry[] GetModelOverrideDropsForIndex(int index)
    {
        if (_modelDropOverrides != null
            && index >= 0
            && index < _modelDropOverrides.Length
            && _modelDropOverrides[index].HasDrops)
        {
            return _modelDropOverrides[index].Drops;
        }

        return Array.Empty<DropEntry>();
    }

    public DropEntry[] GetDropsForModelIndex(int index)
    {
        DropEntry[] modelDrops = GetModelOverrideDropsForIndex(index);

        return modelDrops.Length > 0 ? modelDrops : Drops;
    }
}
