using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_BuildingList : UIBase
{
    public event Action OnClosed;
    [Header("Slots")]
    [SerializeField] private List<BuildingListSlot> _slots = new List<BuildingListSlot>();

    [Header("Layout")]
    [SerializeField] private float _innerRadius = 150f;
    [SerializeField] private float _outerRadius = 400f;
    [Range(0f, 1f)]
    [SerializeField] private float _iconPosition = 0.7f;
    [Range(0f, 100f)]
    [SerializeField] private float _iconYOffset = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float _iconScale = 0.3f;

    [Header("Preview")]
    [SerializeField] private Image _previewImage;
    [SerializeField] private TextMeshProUGUI _previewLabel;

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new Color(0.08f, 0.08f, 0.12f, 0.85f);
    [SerializeField] private Color _highlightColor = new Color(0.1f, 0.6f, 0.9f, 0.9f);

    private IReadOnlyList<BuildingDataSO> _buildings;
    private Action<BuildingDataSO> _onSelected;

    private int _hoveredIndex = -1;
    private int _activeCount;
    private Vector2 _mouseDelta;
    private CursorLockMode _prevLockMode;
    private bool _prevCursorVisible;
    private const float MaxDeltaMagnitude = 50f;
    private static readonly Vector2 InitialDirection = new Vector2(1f, 1f);

    public void Initialize(IReadOnlyList<BuildingDataSO> buildings, Action<BuildingDataSO> onSelected)
    {
        _buildings = buildings;
        _onSelected = onSelected;
    }

    protected override void OnOpen() => BindData();
    protected override void OnClose() => ResetVisuals();
    protected override UniTask OnCloseAnimation() => UniTask.CompletedTask;

    // 슬롯 레이아웃 데이터 생성
    private BuildingListSlot.LayoutData CreateLayoutData(int index, int count)
    {
        float angleStep = 360f / count;
        float sliceThickness = _outerRadius - _innerRadius;

        return new BuildingListSlot.LayoutData
        {
            Rotation = angleStep * index,
            FillAmount = 1f / count + 0.002f,
            IconAngle = 90f - angleStep * index - angleStep * 0.5f,
            IconDist = Mathf.Lerp(_innerRadius, _outerRadius, _iconPosition),
            IconSize = sliceThickness * _iconScale,
            IconYOffset = _iconYOffset,
        };
    }

    // 건물 데이터를 슬롯에 바인딩 + 슬라이스 fill/rotation 설정
    private void BindData()
    {
        if (_buildings == null) return;

        _activeCount = Mathf.Min(_buildings.Count, _slots.Count);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < _activeCount)
            {
                // 슬라이스 fill/rotation + 아이콘 위치/크기 설정
                _slots[i].SetLayout(CreateLayoutData(i, _activeCount));
                _slots[i].SetData(_buildings[i].Icon, _buildings[i].DisplayName);
                _slots[i].SetColor(_normalColor);
                _slots[i].gameObject.SetActive(true);
            }
            else
            {
                _slots[i].gameObject.SetActive(false);
            }
        }

        // 커서 잠금
        _prevLockMode = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // (1, 1) 방향 슬롯이 초기 선택되도록 설정
        _mouseDelta = InitialDirection;
        _hoveredIndex = -1;
        UpdatePreview(-1);
    }

    // 슬롯 색상 초기화 + 커서 복원
    private void ResetVisuals()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].SetColor(_normalColor);
        }

        // 커서 상태 복원
        Cursor.lockState = _prevLockMode;
        Cursor.visible = _prevCursorVisible;

        _hoveredIndex = -1;
        _mouseDelta = Vector2.zero;
        UpdatePreview(-1);

        OnClosed?.Invoke();
    }

    // 매 프레임 호버 판정 및 입력 처리
    private void Update()
    {
        if (!IsOpen || _activeCount == 0) return;

        int prevHover = _hoveredIndex;
        UpdateHover();
        
        // 호버 슬롯이 바뀐 경우, 이전/현재 슬롯만 색상 갱신
        if (prevHover != _hoveredIndex)
        {
            if (prevHover >= 0) _slots[prevHover].SetColor(_normalColor);
            if (_hoveredIndex >= 0) _slots[_hoveredIndex].SetColor(_highlightColor);
            UpdatePreview(_hoveredIndex);
        }

        if (Input.GetMouseButtonDown(0) && _hoveredIndex >= 0)
        {
            SelectSlot(_hoveredIndex);
        }
    }

    // 마우스 방향 기반 호버 슬롯 판정 (커서 잠금 상태에서 delta 누적으로 방향 판정)
    private void UpdateHover()
    {
        _hoveredIndex = -1;

        _mouseDelta += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        _mouseDelta = Vector2.ClampMagnitude(_mouseDelta, MaxDeltaMagnitude);

        float angle = Mathf.Atan2(_mouseDelta.y, _mouseDelta.x) * Mathf.Rad2Deg;
        float normalized = (90f - angle + 360f) % 360f;
        float angleStep = 360f / _activeCount;

        int index = Mathf.FloorToInt(normalized / angleStep);
        if (index >= 0 && index < _activeCount)
        {
            _hoveredIndex = index;
        }
    }

    // 중앙 프리뷰에 호버된 건물 표시
    private void UpdatePreview(int index)
    {
        if (_previewImage == null) return;

        bool valid = index >= 0 && _buildings != null && index < _buildings.Count;

        _previewImage.sprite = valid ? _buildings[index].Icon : null;
        _previewImage.color = valid ? Color.white : Color.clear;

        if (_previewLabel != null)
        {
            _previewLabel.text = valid ? _buildings[index].DisplayName : string.Empty;
        }
    }

    // 건물 선택 처리
    private void SelectSlot(int index)
    {
        if (_buildings == null || index < 0 || index >= _buildings.Count) return;
        _onSelected?.Invoke(_buildings[index]);
        RequestClose();
    }

    private void RequestClose()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_BuildingList>().Forget();
            return;
        }
        CloseAsync().Forget();
    }
}