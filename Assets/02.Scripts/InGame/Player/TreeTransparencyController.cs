using System.Collections.Generic;
using UnityEngine;

public sealed class TreeTransparencyController
{
    private readonly float _transparentAlpha;
    private readonly float _fadeSpeed;
    private readonly Dictionary<Transform, TreeFadeState> _treeStates = new();
    private readonly List<Transform> _removeBuffer = new();

    public TreeTransparencyController(float transparentAlpha, float fadeSpeed)
    {
        _transparentAlpha = transparentAlpha;
        _fadeSpeed = fadeSpeed;
    }

    public void Update(HashSet<Transform> visibleOccluders, float deltaTime)
    {
        foreach (Transform tree in visibleOccluders)
        {
            EnsureState(tree);
        }

        _removeBuffer.Clear();

        foreach (KeyValuePair<Transform, TreeFadeState> pair in _treeStates)
        {
            Transform tree = pair.Key;
            TreeFadeState state = pair.Value;

            if (tree == null)
            {
                _removeBuffer.Add(tree);
                continue;
            }

            bool isVisible = visibleOccluders.Contains(tree);
            float targetAlpha = isVisible ? _transparentAlpha : 1f;
            state.BeginFade(targetAlpha);
            state.Tick(_fadeSpeed * deltaTime);

            if (!isVisible && state.IsFullyOpaque)
            {
                _removeBuffer.Add(tree);
            }
        }

        CleanupRemovedTrees();
    }

    public void RestoreAll()
    {
        foreach (KeyValuePair<Transform, TreeFadeState> pair in _treeStates)
        {
            pair.Value.Restore();
        }
    }

    public void Dispose()
    {
        foreach (KeyValuePair<Transform, TreeFadeState> pair in _treeStates)
        {
            pair.Value.Dispose();
        }

        _treeStates.Clear();
        _removeBuffer.Clear();
    }

    private void EnsureState(Transform tree)
    {
        if (tree == null) return;
        if (_treeStates.ContainsKey(tree)) return;

        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        _treeStates.Add(tree, TreeFadeState.Create(renderers));
    }

    private void CleanupRemovedTrees()
    {
        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            Transform tree = _removeBuffer[i];
            if (tree != null && _treeStates.TryGetValue(tree, out TreeFadeState state))
            {
                state.Dispose();
            }

            _treeStates.Remove(tree);
        }

        _removeBuffer.Clear();
    }
}
