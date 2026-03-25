using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PiUI(원형 메뉴)를 이용한 건물 선택 UI.
/// BuildingDatabase의 건물 목록을 슬라이스로 표시하고,
/// 선택 시 PlayerBuildingAbility에 건물 데이터를 전달한다.
/// </summary>
public class UI_BuildingPi : UIBase
{
    [Header("Pi UI")]
    [SerializeField] private PiUI _piMenu;
    [SerializeField] private BuildingDatabase _buildingDatabase;
    [SerializeField] private bool _closeOnSelect = true;
    [SerializeField] private bool _closeOnOutsideClick = true;

    [Header("Slice Colors")]
    [SerializeField] private Color _normalColor = new Color(0.18f, 0.22f, 0.48f, 1f);
    [SerializeField] private Color _highlightColor = new Color(1f, 0.82f, 0.14f, 1f);
    [SerializeField] private Color _selectedColor = new Color(0.21f, 0.55f, 0.30f, 1f);
    [SerializeField] private Color _selectedHighlightColor = new Color(0.55f, 0.84f, 0.35f, 1f);
    [SerializeField] private Color _disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);

    [Header("Open Position")]
    [SerializeField] private bool _useScreenCenterByDefault = true;
    [SerializeField] private Vector2 _fallbackScreenPosition = new Vector2(960f, 540f);
    [SerializeField] private int _defaultIconSize = 48;

    /// <summary>현재 메뉴에 표시 중인 건물 목록 (매 Rebuild 시 갱신)</summary>
    private readonly List<BuildingDataSO> _buildings = new List<BuildingDataSO>();

    private PlayerBuildingAbility _buildingAbility;
    private PlayerController _playerController;
    /// <summary>외부에서 지정한 메뉴 오픈 위치 (null이면 기본값 사용)</summary>
    private Vector2? _requestedScreenPosition;

    #region Lifecycle

    private void Awake()
    {
        // 이벤트 구독 + 이미 씬에 존재하는 Ability가 있으면 즉시 바인딩
        PlayerBuildingAbility.OnLocalPlayerReady += Bind;
        ResolveDatabase();
        Bind(FindFirstObjectByType<PlayerBuildingAbility>());
    }

    private void OnDestroy()
    {
        PlayerBuildingAbility.OnLocalPlayerReady -= Bind;

        if (_buildingAbility != null)
        {
            _buildingAbility.OnSelectedBuildingChanged -= HandleSelectedBuildingChanged;
        }
    }

    /// <summary>메뉴 영역 바깥 클릭 시 닫기 처리</summary>
    private void Update()
    {
        if (!IsOpen || !_closeOnOutsideClick || _piMenu == null) return;

        if (Input.GetMouseButtonDown(0) && !_piMenu.overMenu)
        {
            RequestClose();
        }
    }

    #endregion

    #region Public API

    /// <summary>메뉴가 열릴 스크린 좌표를 지정한다. OpenAsync 호출 전에 설정해야 한다.</summary>
    public void SetMenuPosition(Vector2 screenPosition)
    {
        _requestedScreenPosition = screenPosition;
    }

    /// <summary>현재 마우스 위치에서 메뉴를 열도록 위치를 설정한다.</summary>
    public void OpenAtMousePosition()
    {
        _requestedScreenPosition = Input.mousePosition;
    }

    #endregion

    #region UIBase Overrides

    protected override void OnOpen()
    {
        ResolveDatabase();

        // 메뉴 데이터 구축 및 표시. 데이터가 없으면 열지 않는다.
        if (!RebuildAndShowMenu()) return;

        _playerController?.EnterUIMode();
    }

    protected override void OnClose()
    {
        _playerController?.ExitUIMode();
        _requestedScreenPosition = null;
    }

    /// <summary>닫힘 애니메이션 — PiUI 자체 트랜지션에 위임한다.</summary>
    protected override UniTask OnCloseAnimation()
    {
        if (_piMenu != null)
        {
            _piMenu.CloseMenu();
        }

        return UniTask.CompletedTask;
    }

    #endregion

    #region Binding

    /// <summary>
    /// PlayerBuildingAbility와 바인딩한다.
    /// 기존 바인딩이 있으면 해제 후 새로 연결한다.
    /// </summary>
    private void Bind(PlayerBuildingAbility ability)
    {
        if (_buildingAbility == ability) return;

        // 기존 구독 해제
        if (_buildingAbility != null)
        {
            _buildingAbility.OnSelectedBuildingChanged -= HandleSelectedBuildingChanged;
        }

        _buildingAbility = ability;
        _playerController = ability != null ? ability.GetComponentInParent<PlayerController>() : null;

        if (_buildingAbility != null)
        {
            _buildingAbility.OnSelectedBuildingChanged += HandleSelectedBuildingChanged;
            // 현재 선택된 건물 정보를 즉시 받아온다
            _buildingAbility.NotifyCurrentBuildingData();
        }
    }

    /// <summary>외부에서 선택 건물이 변경되면 메뉴를 갱신한다.</summary>
    private void HandleSelectedBuildingChanged(BuildingDataSO _)
    {
        if (!IsOpen) return;

        RebuildAndShowMenu();
    }

    #endregion

    #region Menu Construction

    /// <summary>
    /// BuildingDatabase 참조가 비어 있으면 TerrainGridManager로부터 가져온다.
    /// </summary>
    private void ResolveDatabase()
    {
        if (_buildingDatabase != null) return;

        if (TerrainGridManager.Instance != null)
        {
            _buildingDatabase = TerrainGridManager.Instance.BuildingDatabase;
        }
    }

    /// <summary>
    /// 메뉴 데이터를 재구축하고 PiUI에 표시한다.
    /// OnOpen과 HandleSelectedBuildingChanged에서 공통으로 사용한다.
    /// </summary>
    /// <returns>메뉴가 정상적으로 표시되었으면 true</returns>
    private bool RebuildAndShowMenu()
    {
        RebuildMenuData();

        if (_piMenu == null || _piMenu.piData == null || _piMenu.piData.Length == 0)
        {
            return false;
        }

        Vector2 openPosition = GetOpenPosition();
        _piMenu.GeneratePi(openPosition);
        _piMenu.OpenMenu(openPosition);
        return true;
    }

    /// <summary>
    /// BuildingDatabase로부터 PiData 배열을 생성하여 _piMenu에 할당한다.
    /// 현재 선택된 건물은 별도 색상으로 표시한다.
    /// </summary>
    private void RebuildMenuData()
    {
        if (_piMenu == null)
        {
            return;
        }

        // DB에서 유효한 건물만 수집
        _buildings.Clear();

        if (_buildingDatabase != null && _buildingDatabase.Buildings != null)
        {
            for (int i = 0; i < _buildingDatabase.Buildings.Count; i++)
            {
                BuildingDataSO building = _buildingDatabase.Buildings[i];
                if (building != null)
                {
                    _buildings.Add(building);
                }
            }
        }

        // 슬라이스별 개별 색상 사용, 균등 분할
        _piMenu.syncColors = false;
        _piMenu.equalSlices = true;

        if (_buildings.Count == 0)
        {
            _piMenu.piData = System.Array.Empty<PiUI.PiData>();
            _piMenu.GeneratePi(GetOpenPosition());
            return;
        }

        PiUI.PiData[] data = new PiUI.PiData[_buildings.Count];

        for (int i = 0; i < _buildings.Count; i++)
        {
            BuildingDataSO building = _buildings[i];
            bool isSelected = _buildingAbility != null && _buildingAbility.CurrentBuildingData == building;

            // 클로저 캡처를 위해 지역 변수에 복사
            UnityEvent onPressed = new UnityEvent();
            BuildingDataSO capturedBuilding = building;
            onPressed.AddListener(() => OnSliceSelected(capturedBuilding));

            data[i] = new PiUI.PiData
            {
                angle = 360f / _buildings.Count,
                sliceLabel = GetDisplayName(building),
                icon = building.Icon,
                nonHighlightedColor = isSelected ? _selectedColor : _normalColor,
                highlightedColor = isSelected ? _selectedHighlightColor : _highlightColor,
                disabledColor = _disabledColor,
                onSlicePressed = onPressed,
                iconSize = Mathf.Clamp(_defaultIconSize, 8, 256),
                isInteractable = true,
                hoverFunctions = false,
                order = i
            };
        }

        _piMenu.piData = data;
    }

    #endregion

    #region Selection & Close

    /// <summary>
    /// 슬라이스 선택 콜백.
    /// _closeOnSelect이면 메뉴를 닫고 미리보기 모드로 진입하고,
    /// 아니면 건물 데이터만 변경한다.
    /// </summary>
    private void OnSliceSelected(BuildingDataSO buildingData)
    {
        if (_closeOnSelect)
        {
            RequestClose();
            _buildingAbility?.SelectBuildingFromUi(buildingData);
            return;
        }

        _buildingAbility?.SetBuildingData(buildingData);
    }

    /// <summary>메뉴 오픈 위치를 결정한다. 외부 지정 > 화면 중앙 > 폴백 순.</summary>
    private Vector2 GetOpenPosition()
    {
        if (_requestedScreenPosition.HasValue)
        {
            return _requestedScreenPosition.Value;
        }

        if (_useScreenCenterByDefault)
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        return _fallbackScreenPosition;
    }

    /// <summary>UIController가 있으면 그쪽을 통해, 없으면 직접 닫는다.</summary>
    private void RequestClose()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_BuildingPi>().Forget();
            return;
        }

        CloseAsync().Forget();
    }

    #endregion

    #region Utility

    /// <summary>건물의 표시 이름을 반환한다. DisplayName이 없으면 BuildingId를 사용.</summary>
    private static string GetDisplayName(BuildingDataSO buildingData)
    {
        if (buildingData == null) return string.Empty;

        return string.IsNullOrWhiteSpace(buildingData.DisplayName)
            ? buildingData.BuildingId
            : buildingData.DisplayName;
    }

    #endregion
}
