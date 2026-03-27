using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    
    [SerializeField] private KeyCode _escapeKey = KeyCode.Escape;
    
    [Header("Canvas 참조")]
    [SerializeField] private Transform _hudCanvas;
    [SerializeField] private Transform _popupCanvas;
    [SerializeField] private Transform _overlayCanvas;

    private readonly Dictionary<EUILayer, Transform> _canvases = new Dictionary<EUILayer, Transform>();
    private readonly Dictionary<string, UIBase> _instances = new Dictionary<string, UIBase>();
    private readonly HashSet<string> _loadingKeys = new HashSet<string>();
    private readonly List<UIBase> _activeInstance = new List<UIBase>();

    #region LifeCycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _canvases.Add(EUILayer.HUD, _hudCanvas);
        _canvases.Add(EUILayer.Popup, _popupCanvas);
        _canvases.Add(EUILayer.Overlay, _overlayCanvas);

        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(_escapeKey))
        {
            CloseTopAsync().Forget();
        }
    }

    #endregion

    #region Public API

    public async UniTask<T> OpenAsync<T>(Action<T> onBeforeOpen = null) where T : UIBase
    {
        string key = AssetKey.UI.GetKey<T>();
        T ui = await GetOrCreateAsync<T>(key);

        if (ui == null) return null;

        if (ui.IsOpen)
        {
            BringToFront(ui);
            return ui;
        }

        onBeforeOpen?.Invoke(ui);
        await ui.OpenAsync();
        PushToStack(ui);
        return ui;
    }

    public async UniTask CloseAsync<T>() where T : UIBase
    {
        string key = typeof(T).Name;

        if (_instances.TryGetValue(key, out UIBase ui) && ui.IsOpen)
        {
            await ui.CloseAsync();
            RemoveFromStack(ui);
        }
    }

    public void OnClickUI<T>() where T : UIBase
    {
        if (!_instances.TryGetValue(typeof(T).Name, out UIBase ui) || !ui.IsOpen) return;
        if (_activeInstance[^1] == ui) return;
        BringToFront(ui);
    }
    
    #endregion

    #region Active Instance 관리

    private async UniTask CloseTopAsync()
    {
        if (_activeInstance.Count == 0)
        {
            // todo: 일시정지 UI Popup 표시
            return;
        }
        for (int i = _activeInstance.Count - 1; i >= 0; i--)
        {
            UIBase ui = _activeInstance[i];

            if (ui == null || ui.Config.Layer == EUILayer.HUD) continue;

            await ui.CloseAsync();
            _activeInstance.RemoveAt(i);
            return;
        }
    }

    private void PushToStack(UIBase ui)
    {
        _activeInstance.Add(ui);
        ui.transform.SetAsLastSibling();
    }

    private void RemoveFromStack(UIBase ui)
    {
        _activeInstance.Remove(ui);
    }
    
    private void BringToFront(UIBase ui)
    {
        if (ui == null || !ui.IsOpen) return;

        _activeInstance.Remove(ui);
        _activeInstance.Add(ui);
        ui.transform.SetAsLastSibling();
    }

    #endregion

    #region Resource 관리

    private async UniTask<T> GetOrCreateAsync<T>(string key) where T : UIBase
    {
        if (_instances.TryGetValue(key, out UIBase cached)) return cached as T;

        if (!_loadingKeys.Add(key)) return null;

        GameObject prefab = await ResourceManager.Instance.LoadAsync<GameObject>(key);

        if (prefab == null)
        {
            Debug.LogError($"[UIController] UI 프리팹 로드 실패: {key}");
            _loadingKeys.Remove(key);
            return null;
        }

        T prefabComponent = prefab.GetComponent<T>();

        if (prefabComponent == null)
        {
            Debug.LogError($"[UIController] 프리팹에 {typeof(T).Name} 컴포넌트 없음: {key}");
            _loadingKeys.Remove(key);
            return null;
        }

        Transform parent = GetCanvasForLayer(prefabComponent.Config.Layer);
        GameObject instance = Instantiate(prefab, parent);
        instance.SetActive(false);

        T component = instance.GetComponent<T>();
        _instances[key] = component;
        _loadingKeys.Remove(key);

        return component;
    }

    private Transform GetCanvasForLayer(EUILayer layer)
    {
        return _canvases.GetValueOrDefault(layer, _popupCanvas);
    }

    private void ClearAll()
    {
        // todo. Clear All이 아닌 현재 Scene과 상황에 따른 Clear 할 UI 설정
        _activeInstance.Clear();
        _loadingKeys.Clear();

        foreach (KeyValuePair<string, UIBase> kvp in _instances)
        {
            if (kvp.Value != null)
            {
                if (kvp.Value.IsOpen) kvp.Value.CloseAsync().Forget();
                Destroy(kvp.Value.gameObject);
            }

            ResourceManager.Instance.Release(kvp.Key);
        }

        _instances.Clear();
    }

    private void OnSceneUnloaded(Scene scene)
    {
        ClearAll();
    }

    #endregion
}