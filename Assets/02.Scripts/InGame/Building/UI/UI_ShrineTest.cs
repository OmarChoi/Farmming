using System;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ShrineTest : UIBase
{
    private RectTransform _panel;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _bodyText;
    private TextMeshProUGUI _effectText;
    private RectTransform _buttonRoot;
    private ShrineBuilding _shrine;
    private Action _onClose;
    private bool _hierarchyBuilt;
    private bool _suppressCloseNotify;

    private void Awake()
    {
        BuildHierarchyIfNeeded();
    }

    private void OnDestroy()
    {
        WorldEffectEvents.OnEffectsChanged -= RefreshEffectText;
    }

    public void Configure(ShrineBuilding shrine, Action onClose)
    {
        _shrine = shrine;
        _onClose = onClose;
    }

    public void CloseFromOwner()
    {
        if (!IsOpen)
        {
            ClearContext();
            return;
        }

        _suppressCloseNotify = true;
        RequestClose();
    }

    protected override void OnOpen()
    {
        BuildHierarchyIfNeeded();

        WorldEffectEvents.OnEffectsChanged -= RefreshEffectText;
        WorldEffectEvents.OnEffectsChanged += RefreshEffectText;
        ShowQuestDescription();
    }

    protected override void OnClose()
    {
        WorldEffectEvents.OnEffectsChanged -= RefreshEffectText;
        ClearButtons();

        Action onClose = _onClose;
        bool notify = !_suppressCloseNotify;
        ClearContext();
        _suppressCloseNotify = false;

        if (notify)
        {
            onClose?.Invoke();
        }
    }

    private void ClearContext()
    {
        _onClose = null;
        _shrine = null;
    }

    private void ShowQuestDescription()
    {
        _titleText.text = _shrine != null ? _shrine.QuestTitle : "제단 의뢰";
        _bodyText.text = _shrine != null
            ? _shrine.QuestDescription
            : "제단 의뢰 정보를 찾을 수 없습니다.";

        RefreshEffectText();
        ClearButtons();
        AddButton("퀘스트 완료 요청", ShowCompletionRequest);
        AddButton("닫기", RequestClose);
    }

    private void ShowCompletionRequest()
    {
        _titleText.text = "퀘스트 완료 요청";
        _bodyText.text = _shrine != null
            ? _shrine.CompletionRequestText
            : "제단 의뢰 정보를 찾을 수 없습니다.";

        RefreshEffectText();
        ClearButtons();
        AddButton("성공", () => ApplyResult(true));
        AddButton("실패", () => ApplyResult(false));
        AddButton("뒤로", ShowQuestDescription);
    }

    private void ApplyResult(bool isSuccess)
    {
        _titleText.text = isSuccess ? "성공" : "실패";
        _bodyText.text = _shrine != null
            ? _shrine.ApplyTestQuestResult(isSuccess)
            : "제단 의뢰 정보를 찾을 수 없어 결과를 적용하지 못했습니다.";

        RefreshEffectText();
        ClearButtons();
        AddButton("설명으로", ShowQuestDescription);
        AddButton("닫기", RequestClose);
    }

    private void RefreshEffectText()
    {
        if (_effectText == null) return;

        WorldEffectManager manager = WorldEffectManager.Instance;
        if (manager == null)
        {
            _effectText.text = "활성 월드 효과: WorldEffectManager 없음";
            return;
        }

        if (manager.ActiveEffects == null || manager.ActiveEffects.Count == 0)
        {
            _effectText.text = "활성 월드 효과: 없음";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("활성 월드 효과:");

        for (int i = 0; i < manager.ActiveEffects.Count; i++)
        {
            WorldEffectEntry entry = manager.ActiveEffects[i];
            string kind = ((EWorldEffectKind)entry.Kind).ToString();
            string remaining = entry.RemainingDays < 0 ? "영구" : $"{entry.RemainingDays}일";
            builder.AppendLine($"- {entry.EffectId} ({kind}, {remaining})");
        }

        _effectText.text = builder.ToString();
    }

    private void RequestClose()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.CloseAsync<UI_ShrineTest>().Forget();
            return;
        }

        CloseAsync().Forget();
    }

    private void BuildHierarchyIfNeeded()
    {
        if (_hierarchyBuilt) return;
        _hierarchyBuilt = true;

        RectTransform root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform dim = CreateRect("Dim", root);
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = Vector2.zero;
        dim.offsetMax = Vector2.zero;
        Image dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);

        _panel = CreateRect("Panel", dim);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.pivot = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(720f, 520f);
        _panel.anchoredPosition = Vector2.zero;

        Image panelImage = _panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

        VerticalLayoutGroup layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        _titleText = CreateText("Title", _panel, 32, TextAlignmentOptions.Center, FontStyles.Bold);
        AddLayout(_titleText.gameObject, 54f);

        _bodyText = CreateText("Body", _panel, 24, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        AddLayout(_bodyText.gameObject, 170f);

        _effectText = CreateText("Effects", _panel, 20, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        AddLayout(_effectText.gameObject, 130f);

        _buttonRoot = CreateRect("Buttons", _panel);
        HorizontalLayoutGroup buttonLayout = _buttonRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 12f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;
        AddLayout(_buttonRoot.gameObject, 58f);
    }

    private void AddButton(string label, Action onClick)
    {
        RectTransform buttonRect = CreateRect(label, _buttonRoot);
        buttonRect.sizeDelta = new Vector2(180f, 48f);

        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.78f, 0.68f, 0.42f, 1f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());

        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.preferredHeight = 48f;

        TextMeshProUGUI labelText = CreateText("Label", buttonRect, 21, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform labelRect = (RectTransform)labelText.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelText.color = Color.black;
        labelText.text = label;
    }

    private void ClearButtons()
    {
        if (_buttonRoot == null) return;

        for (int i = _buttonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_buttonRoot.GetChild(i).gameObject);
        }
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float fontSize,
        TextAlignmentOptions alignment,
        FontStyles fontStyle)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = Color.white;
        return text;
    }

    private static void AddLayout(GameObject obj, float preferredHeight)
    {
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.minHeight = preferredHeight;
    }
}
