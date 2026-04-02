using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_DungeonPortal : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TMPro.TextMeshProUGUI _titleText;
    [SerializeField] private TypewriterWithWrap _typewriter;
    [SerializeField] private Transform _buttonRoot;
    [SerializeField] private GameObject _buttonPrefab;
    [SerializeField] private Button _dialoguePanelButton;

    private Coroutine _waitCoroutine;
    private Action _pendingButtonSetup;

    private void Awake()
    {
        _root.SetActive(false);

        if (_dialoguePanelButton != null)
        {
            _dialoguePanelButton.onClick.AddListener(() =>
            {
                if (_typewriter.IsTyping())
                    _typewriter.CompleteTyping();
            });
        }
    }

    public void ShowDungeonSelection(
        DungeonMapConfig[] configs,
        Action<int> onSelectDungeon,
        Action onClose)
    {
        _root.SetActive(true);
        _titleText.text = "던전 입구";

        ShowTextThenButtons("어떤 던전으로 입장하시겠습니까?", () =>
        {
            for (int i = 0; i < configs.Length; i++)
            {
                int floor = i + 1;
                string label = string.IsNullOrEmpty(configs[i].DungeonName)
                    ? $"던전 {floor}"
                    : configs[i].DungeonName;

                CreateButton(label, () => onSelectDungeon(floor));
            }
            CreateButton("사용종료", onClose);
        });
    }

    public void ShowRequirements(
        DungeonMaterialRequirement[] requirements,
        int entryCost,
        int currentGold,
        System.Func<ItemDataSO, int> getItemCount,
        Action onEnter,
        Action onCancel)
    {
        var sb = new System.Text.StringBuilder();

        if (entryCost > 0)
        {
            string goldColor = currentGold >= entryCost ? "#00CC00" : "#CC0000";
            sb.AppendLine($"입장 비용: <color={goldColor}>{entryCost}G (보유: {currentGold}G)</color>");
        }

        sb.AppendLine("필요한 재료:");

        if (requirements != null && requirements.Length > 0)
        {
            foreach (var req in requirements)
            {
                int have = getItemCount(req.Item);
                string color = have >= req.Amount ? "#00CC00" : "#CC0000";
                sb.AppendLine($"  <color={color}>{req.Item.DisplayName} x{req.Amount} (보유: {have})</color>");
            }
        }
        else
        {
            sb.AppendLine("  없음");
        }

        ShowTextThenButtons(sb.ToString(), () =>
        {
            CreateButton("입장", onEnter);
            CreateButton("취소", onCancel);
        });
    }

    public void SetDescription(string message, Action onConfirm = null)
    {
        ShowTextThenButtons(message, () =>
        {
            CreateButton("확인", onConfirm);
        });
    }

    public void Close()
    {
        StopWait();
        _root.SetActive(false);
        ClearButtons();
    }

    private void ShowTextThenButtons(string text, Action buttonSetup)
    {
        StopWait();
        ClearButtons();
        _typewriter.StartTyping(text);

        if (buttonSetup != null)
        {
            _pendingButtonSetup = buttonSetup;
            _waitCoroutine = StartCoroutine(WaitTypingThenShowButtons());
        }
    }

    private IEnumerator WaitTypingThenShowButtons()
    {
        yield return new WaitUntil(() => !_typewriter.IsTyping());

        _pendingButtonSetup?.Invoke();
        _pendingButtonSetup = null;
        _waitCoroutine = null;
    }

    private void StopWait()
    {
        if (_waitCoroutine != null)
        {
            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }
        _pendingButtonSetup = null;
    }

    private void CreateButton(string text, Action onClick)
    {
        var obj = Instantiate(_buttonPrefab, _buttonRoot);
        var btn = obj.GetComponent<UI_InteractionButton>();
        if (btn != null)
        {
            btn.Init(text, onClick);
            return;
        }

        var button = obj.GetComponent<Button>();
        var tmp = obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    private void ClearButtons()
    {
        for (int i = _buttonRoot.childCount - 1; i >= 0; i--)
            Destroy(_buttonRoot.GetChild(i).gameObject);
    }
}