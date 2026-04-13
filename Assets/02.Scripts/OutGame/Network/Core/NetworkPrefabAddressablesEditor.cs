#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// 네트워크 프리팹 Addressables 그룹을 AssetKey.NetworkPrefab 기준으로 동기화하고 검증한다.
/// </summary>
public static class NetworkPrefabAddressablesEditor
{
    private const string GroupName = "NetworkPrefabs";
    private const string LabelName = "NetworkPrefab";

    // 네트워크 프리팹은 Resources 이중 번들링을 피하기 위해 전용 Prefabs 폴더를 기준 경로로 사용한다.
    private const string NetworkPrefabPathFormat = "Assets/03.Prefabs/NetworkPrefabs/{0}.prefab";

    /// <summary>
    /// 에디터 메뉴에서 네트워크 프리팹 Addressables 그룹을 생성 또는 갱신한다.
    /// </summary>
    [MenuItem("Tools/Farmming/Addressables/Sync Network Prefabs")]
    public static void SyncNetworkPrefabs()
    {
        SyncNetworkPrefabsOrThrow();
        Debug.Log("[NetworkPrefabAddressablesEditor] Synced network prefabs to the NetworkPrefabs local Addressables group.");
    }

    /// <summary>
    /// 에디터 메뉴에서 네트워크 프리팹 주소와 PhotonView 구성을 검증한다.
    /// </summary>
    [MenuItem("Tools/Farmming/Addressables/Validate Network Prefabs")]
    public static void ValidateNetworkPrefabs()
    {
        ValidateNetworkPrefabsOrThrow();
        Debug.Log("[NetworkPrefabAddressablesEditor] Network prefab Addressables validation passed.");
    }

    /// <summary>
    /// NetworkPrefabs 로컬 그룹을 준비하고 AssetKey.NetworkPrefab.All의 모든 프리팹을 등록한다.
    /// </summary>
    public static void SyncNetworkPrefabsOrThrow()
    {
        // Addressables 설정과 AssetKey 목록을 먼저 확인해 잘못된 그룹 생성을 방지한다.
        AddressableAssetSettings settings = GetSettings();
        ValidateAssetKeyDuplicates();

        // 그룹이 없으면 로컬 Addressables 그룹으로 새로 만든다.
        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                GroupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
        }

        ConfigureLocalGroup(settings, group);
        settings.AddLabel(LabelName);

        // AssetKey를 소스 오브 트루스로 삼아 주소와 라벨을 강제로 맞춘다.
        foreach (string key in AssetKey.NetworkPrefab.All)
        {
            // AssetKey 상수값과 동일한 파일명을 새 네트워크 프리팹 폴더에서 찾는다.
            string assetPath = string.Format(NetworkPrefabPathFormat, key);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Network prefab asset is missing: {assetPath}");

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = key;
            entry.SetLabel(LabelName, true, true);
        }

        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(group);
        AssetDatabase.SaveAssets();

        // 저장 직후 다시 검증해 주소 누락이나 중복을 즉시 잡는다.
        ValidateNetworkPrefabsOrThrow();
    }

    /// <summary>
    /// AssetKey.NetworkPrefab.All과 실제 Addressables 그룹/주소가 일치하는지 검증한다.
    /// </summary>
    public static void ValidateNetworkPrefabsOrThrow()
    {
        // 전역 주소 중복을 먼저 확인해 런타임 로드가 다른 에셋으로 해석되는 일을 막는다.
        AddressableAssetSettings settings = GetSettings();
        ValidateAssetKeyDuplicates();
        ValidateUniqueAddresses(settings);

        // 네트워크 프리팹은 v1에서 전용 로컬 그룹에만 존재해야 한다.
        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
            throw new InvalidOperationException($"Addressables group is missing: {GroupName}");

        foreach (string key in AssetKey.NetworkPrefab.All)
        {
            // 파일, Addressables entry, 주소, PhotonView를 순서대로 검증한다.
            // 검증 기준 경로도 sync와 같은 전용 네트워크 프리팹 폴더를 사용한다.
            string assetPath = string.Format(NetworkPrefabPathFormat, key);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Network prefab asset is missing: {assetPath}");

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
                throw new InvalidOperationException($"Network prefab is not Addressable: {assetPath}");

            if (entry.parentGroup != group)
                throw new InvalidOperationException($"Network prefab is in the wrong Addressables group: {key}");

            if (entry.address != key)
                throw new InvalidOperationException($"Network prefab address mismatch. Expected '{key}', actual '{entry.address}'.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                throw new InvalidOperationException($"Network prefab failed to load in editor: {assetPath}");

            if (prefab.GetComponentInChildren<Photon.Pun.PhotonView>(true) == null)
                throw new InvalidOperationException($"Network prefab has no PhotonView: {assetPath}");
        }
    }

    /// <summary>
    /// 프로젝트의 기본 Addressables 설정 에셋을 반환한다.
    /// </summary>
    private static AddressableAssetSettings GetSettings()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            throw new InvalidOperationException("AddressableAssetSettings could not be found.");

        return settings;
    }

    /// <summary>
    /// NetworkPrefabs 그룹을 로컬 빌드/로드 경로를 사용하는 번들 그룹으로 설정한다.
    /// </summary>
    private static void ConfigureLocalGroup(AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        // 필요한 Addressables schema가 없으면 추가해서 그룹 설정을 완성한다.
        BundledAssetGroupSchema bundled = group.GetSchema<BundledAssetGroupSchema>();
        if (bundled == null)
            bundled = group.AddSchema<BundledAssetGroupSchema>();

        ContentUpdateGroupSchema contentUpdate = group.GetSchema<ContentUpdateGroupSchema>();
        if (contentUpdate == null)
            contentUpdate = group.AddSchema<ContentUpdateGroupSchema>();

        bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
        bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);

        EditorUtility.SetDirty(bundled);
        EditorUtility.SetDirty(contentUpdate);
    }

    /// <summary>
    /// AssetKey.NetworkPrefab.All에 빈 값이나 중복 키가 없는지 검증한다.
    /// </summary>
    private static void ValidateAssetKeyDuplicates()
    {
        // 문자열 상수 오타는 Addressables 주소와 1:1 매핑되므로 에디터에서 조기에 실패시킨다.
        var seen = new HashSet<string>();
        foreach (string key in AssetKey.NetworkPrefab.All)
        {
            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("AssetKey.NetworkPrefab.All contains a null or empty key.");

            if (!seen.Add(key))
                throw new InvalidOperationException($"AssetKey.NetworkPrefab.All contains a duplicate key: {key}");
        }
    }

    /// <summary>
    /// 전체 Addressables 설정에서 같은 주소가 서로 다른 에셋에 중복으로 쓰이지 않는지 검증한다.
    /// </summary>
    private static void ValidateUniqueAddresses(AddressableAssetSettings settings)
    {
        // Addressables는 주소 문자열로 로드하므로 중복 주소를 허용하지 않는다.
        var addresses = new Dictionary<string, AddressableAssetEntry>();

        foreach (AddressableAssetGroup group in settings.groups)
        {
            if (group == null) continue;

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.address)) continue;

                if (!addresses.TryGetValue(entry.address, out AddressableAssetEntry existing))
                {
                    addresses[entry.address] = entry;
                    continue;
                }

                if (existing.guid != entry.guid)
                    throw new InvalidOperationException($"Duplicate Addressables address '{entry.address}' exists in multiple entries.");
            }
        }
    }
}
#endif
