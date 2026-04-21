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
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            ClearAll();
            Instance = null;
        }
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

    // Open 직전/Close 직후 훅을 한 번에 등록. 콜백은 1회성이며 CloseAsync 완료 시 UIBase가 자동 해제한다.
    public async UniTask<T> OpenAsync<T>(UILifecycleActions<T> actions = default) where T : UIBase
    {
        string key = AssetKey.UI.GetKey<T>();
        T ui = await GetOrCreateAsync<T>(key);

        if (ui == null) return null;

        if (ui.IsOpen)
        {
            // 이미 열려 있으면 새 사이클이 아니므로 콜백을 덮어쓰지 않는다. 기존 구독자 보호.
            BringToFront(ui);
            return ui;
        }

        // 생성된 인스턴스를 캡처해 Action<T> → Action 으로 바인딩.
        Action onOpen = actions.OnOpen != null ? () => actions.OnOpen(ui) : null;
        Action onClose = actions.OnClose != null ? () => actions.OnClose(ui) : null;
        ui.SetLifecycleActions(onOpen, onClose);

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

    public T GetInstance<T>() where T : UIBase
    {
        string key = typeof(T).Name;
        return _instances.TryGetValue(key, out UIBase ui) ? ui as T : null;
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
            OpenPauseAsync().Forget();
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

    private UniTask<UI_Pause> OpenPauseAsync()
    {
        PlayerController localPlayer = PlayerController.Local;

        return OpenAsync<UI_Pause>
        (
            new UILifecycleActions<UI_Pause>
            {
                OnOpen = ui => ui.SetOwnerPlayer(localPlayer)
            }
        );
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
    #endregion
}
