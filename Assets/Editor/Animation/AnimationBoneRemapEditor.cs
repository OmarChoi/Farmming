using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnimationBoneRemapEditor : EditorWindow
{
    private AnimationClip _sourceClip;
    private GameObject _targetModel;
    private Vector2 _scrollPos;

    // 애니메이션 바인딩의 원본 경로 → 새 경로 매핑
    private List<BindingEntry> _entries = new();
    private string[] _targetPaths;

    [MenuItem("Tools/Animation Bone Remapper")]
    public static void Open()
    {
        GetWindow<AnimationBoneRemapEditor>("Bone Remapper");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Animation Bone Remapper", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 1. 애니메이션 클립
        var newClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", _sourceClip, typeof(AnimationClip), false);

        // 2. 타겟 모델 (뼈 구조 참조용)
        var newModel = (GameObject)EditorGUILayout.ObjectField(
            "Target Model", _targetModel, typeof(GameObject), true);

        // 입력 변경 시 바인딩 새로 읽기
        if (newClip != _sourceClip || newModel != _targetModel)
        {
            _sourceClip = newClip;
            _targetModel = newModel;
            RefreshBindings();
        }

        if (_entries.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Animation Clip과 Target Model을 지정하면\n바인딩 목록이 표시됩니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"바인딩 수: {_entries.Count}", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        // 3. 바인딩 리스트
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        float halfWidth = (position.width - 30f) * 0.5f;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("원본 경로", EditorStyles.boldLabel, GUILayout.Width(halfWidth));
        EditorGUILayout.LabelField("→", GUILayout.Width(20));
        EditorGUILayout.LabelField("새 경로 (모델 뼈 선택)", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            EditorGUILayout.BeginHorizontal();

            // 매칭 안 된 항목은 노란색 표시
            if (entry.NewPathIndex == 0)
                GUI.color = new Color(1f, 1f, 0.6f);

            EditorGUILayout.TextField(entry.OriginalPath, EditorStyles.label, GUILayout.Width(halfWidth));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));

            entry.NewPathIndex = EditorGUILayout.Popup(entry.NewPathIndex, _targetPaths);

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 4. 자동 매칭 버튼
        if (GUILayout.Button("Auto Match (이름 기반 자동 매칭)", GUILayout.Height(25)))
        {
            AutoMatch();
        }

        EditorGUILayout.Space(3);

        // 5. 새 클립 생성 버튼
        GUI.enabled = _sourceClip != null;
        if (GUILayout.Button("Create Remapped Clip", GUILayout.Height(30)))
        {
            CreateRemappedClip();
        }
        GUI.enabled = true;
    }

    private void RefreshBindings()
    {
        _entries.Clear();
        _targetPaths = new[] { "(none)" };

        if (_sourceClip == null) return;

        // 타겟 모델의 모든 Transform 경로 수집
        if (_targetModel != null)
        {
            var paths = new List<string> { "(none)" };
            CollectPaths(_targetModel.transform, "", paths);
            _targetPaths = paths.ToArray();
        }

        // 애니메이션 바인딩 읽기
        var bindings = AnimationUtility.GetCurveBindings(_sourceClip);
        var seen = new HashSet<string>();

        foreach (var binding in bindings)
        {
            if (seen.Contains(binding.path)) continue;
            seen.Add(binding.path);

            var entry = new BindingEntry { OriginalPath = binding.path };

            // 타겟 모델이 있으면 자동 매칭 시도
            if (_targetModel != null)
            {
                string boneName = GetLastBoneName(binding.path);
                for (int i = 1; i < _targetPaths.Length; i++)
                {
                    if (GetLastBoneName(_targetPaths[i]) == boneName)
                    {
                        entry.NewPathIndex = i;
                        break;
                    }
                }
            }

            _entries.Add(entry);
        }
    }

    private void CollectPaths(Transform t, string parentPath, List<string> paths)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            string path = string.IsNullOrEmpty(parentPath)
                ? child.name
                : parentPath + "/" + child.name;

            paths.Add(path);
            CollectPaths(child, path, paths);
        }
    }

    [SerializeField] private int _minMatchLength = 3;

    private void AutoMatch()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            string source = NormalizeName(GetLastBoneName(entry.OriginalPath));

            int bestIndex = 0;
            int bestLength = 0;

            for (int j = 1; j < _targetPaths.Length; j++)
            {
                string target = NormalizeName(GetLastBoneName(_targetPaths[j]));

                int matchLen = 0;
                if (source == target)
                    matchLen = source.Length + 100; // 완전 일치 최우선
                else if (target.Contains(source))
                    matchLen = source.Length;
                else if (source.Contains(target))
                    matchLen = target.Length;

                if (matchLen >= _minMatchLength && matchLen > bestLength)
                {
                    bestLength = matchLen;
                    bestIndex = j;
                }
            }

            entry.NewPathIndex = bestIndex;
        }

        Repaint();
    }

    // "upperarm.r" → "lupperarm" / "Bip001 R UpperArm" → "rupperarm"
    // L/R 접미사를 접두사로 통일 + 구분자 제거
    private string NormalizeName(string name)
    {
        name = name.ToLower()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace(".", "")
            .Replace("-", "");

        // 동의어 치환
        name = name.Replace("lower", "fore");

        // 끝에 붙은 l/r을 앞으로 이동 (upperarmr → rupperarm)
        if (name.Length > 1 && (name.EndsWith("l") || name.EndsWith("r")))
        {
            char side = name[name.Length - 1];
            string body = name.Substring(0, name.Length - 1);

            // 이미 앞에 l/r이 있으면 건드리지 않음
            if (!body.StartsWith("l") && !body.StartsWith("r"))
                name = side + body;
        }

        return name;
    }

    private void CreateRemappedClip()
    {
        // 원본 → 새 경로 딕셔너리
        var pathMap = new Dictionary<string, string>();
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.NewPathIndex > 0)
                pathMap[entry.OriginalPath] = _targetPaths[entry.NewPathIndex];
        }

        // 새 클립 생성
        var newClip = new AnimationClip();
        newClip.frameRate = _sourceClip.frameRate;

        // 일반 커브
        var bindings = AnimationUtility.GetCurveBindings(_sourceClip);
        foreach (var binding in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(_sourceClip, binding);
            var newBinding = binding;

            if (pathMap.TryGetValue(binding.path, out var newPath))
                newBinding.path = newPath;

            AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
        }

        // Object Reference 커브
        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(_sourceClip);
        foreach (var binding in objBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(_sourceClip, binding);
            var newBinding = binding;

            if (pathMap.TryGetValue(binding.path, out var newPath))
                newBinding.path = newPath;

            AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, keyframes);
        }

        // 애니메이션 이벤트 복사
        var events = AnimationUtility.GetAnimationEvents(_sourceClip);
        AnimationUtility.SetAnimationEvents(newClip, events);

        // Loop 설정 복사
        var settings = AnimationUtility.GetAnimationClipSettings(_sourceClip);
        AnimationUtility.SetAnimationClipSettings(newClip, settings);

        // 저장
        string sourcePath = AssetDatabase.GetAssetPath(_sourceClip);
        string directory = string.IsNullOrEmpty(sourcePath)
            ? "Assets"
            : System.IO.Path.GetDirectoryName(sourcePath);
        string fileName = _sourceClip.name + "_Remapped.anim";
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Remapped Clip", fileName, "anim", "저장 위치를 선택하세요", directory);

        if (string.IsNullOrEmpty(savePath)) return;

        AssetDatabase.CreateAsset(newClip, savePath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("완료",
            $"리매핑된 클립이 저장되었습니다.\n{savePath}", "OK");

        Selection.activeObject = newClip;
        EditorGUIUtility.PingObject(newClip);
    }

    private string GetLastBoneName(string path)
    {
        int idx = path.LastIndexOf('/');
        return idx >= 0 ? path.Substring(idx + 1) : path;
    }

    private class BindingEntry
    {
        public string OriginalPath;
        public int NewPathIndex;
    }
}