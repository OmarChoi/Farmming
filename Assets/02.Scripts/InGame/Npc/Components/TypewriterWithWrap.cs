using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class TypewriterWithWrap : MonoBehaviour
{
    private TextMeshProUGUI _tmp;

    [SerializeField] private float _typingSpeed = 0.07f;

    private bool _isTyping;
    private string _fullText;

    private Coroutine _typingCoroutine;
    private Action<char> _onChar;
    private WaitForSeconds _typingWait;
    private float _cachedTypingSpeed = -1f;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    public void StartTyping(string input, Action<char> onChar = null)
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        string wrapped = WrapText(input);
        _fullText = wrapped;
        _onChar = onChar;

        // _typingSpeed가 런타임에 바뀔 수 있으므로 값이 달라질 때만 WaitForSeconds를 재생성한다.
        if (_typingWait == null || !Mathf.Approximately(_cachedTypingSpeed, _typingSpeed))
        {
            _typingWait = new WaitForSeconds(_typingSpeed);
            _cachedTypingSpeed = _typingSpeed;
        }

        _typingCoroutine = StartCoroutine(TypingTextCoroutine(wrapped));
    }

    private IEnumerator TypingTextCoroutine(string text)
    {
        _isTyping = true;

        _tmp.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            // 리치텍스트 태그는 통째로 추가 (음성 콜백 미발생)
            if (text[i] == '<')
            {
                int closeIndex = text.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    _tmp.text += text.Substring(i, closeIndex - i + 1);
                    i = closeIndex;
                    continue;
                }
            }

            char ch = text[i];
            _tmp.text += ch;
            _onChar?.Invoke(ch);
            yield return _typingWait;
        }

        _isTyping = false;
        _onChar = null;
    }

    public string WrapText(string input)
    {
        return WrapText(input, _tmp);
    }

    public string WrapText(string input, TextMeshProUGUI target)
    {
        float maxWidth = target.rectTransform.rect.width - target.margin.x - target.margin.z;

        string[] words = input.Split(' ');
        StringBuilder result = new StringBuilder();
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine)
                ? word
                : currentLine + " " + word;

            Vector2 size = target.GetPreferredValues(testLine);

            if (size.x > maxWidth)
            {
                result.AppendLine(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        result.Append(currentLine);
        return result.ToString();
    }

    // 클릭이 들어오면 텍스트를 바로 완성합니다.
    public void CompleteTyping()
    {
        if (!_isTyping) return;

        StopCoroutine(_typingCoroutine);
        _tmp.text = _fullText;
        _isTyping = false;
        _onChar = null;
    }

    public bool IsTyping()
    {
        return _isTyping;
    }
}
