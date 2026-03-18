using System.Collections;
using TMPro;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class TypewriterWithWrap : MonoBehaviour
{
    private TextMeshProUGUI _tmp;

    [SerializeField] private float _typingSpeed = 0.05f;

    private bool _isTyping;
    private string _fullText;

    private Coroutine _typingCoroutine;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    public void StartTyping(string input)
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }

        string wrapped = WrapText(input);
        _fullText = wrapped;
        _typingCoroutine = StartCoroutine(TypingTextCoroutine(wrapped));
    }

    private IEnumerator TypingTextCoroutine(string text)
    {
        _isTyping = true;

        _tmp.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            _tmp.text += text[i];
            yield return new WaitForSeconds(_typingSpeed);
        }

        _isTyping = false;
    }

    private string WrapText(string input)
    {
        float maxWidth = _tmp.rectTransform.rect.width - _tmp.margin.x - _tmp.margin.z;

        string[] words = input.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine)
                ? word
                : currentLine + " " + word;

            _tmp.text = testLine;
            _tmp.ForceMeshUpdate();

            if (_tmp.preferredWidth > maxWidth)
            {
                result += currentLine + "\n";
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        result += currentLine;
        return result;
    }

    // 클릭이 들어오면 텍스트를 바로 완성합니다.
    public void CompleteTyping()
    {
        if (!_isTyping) return;

        StopCoroutine(_typingCoroutine);
        _tmp.text = _fullText;
        _isTyping = false;
    }

    public bool IsTyping()
    {
        return _isTyping;
    }
}
