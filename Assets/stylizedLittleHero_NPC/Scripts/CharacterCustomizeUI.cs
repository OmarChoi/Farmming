using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Customization UI Manager
/// </summary>
public class CharacterCustomizeUI : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("CharacterPartSwapper that handles part swapping")]
    public CharacterPartSwapper partSwapper;

    [Header("Body Buttons")]
    public Button bodyPrevButton;
    public Button bodyNextButton;
    public Text bodyIndexText;  // Optional: display current index

    [Header("Hair Buttons")]
    public Button hairPrevButton;
    public Button hairNextButton;
    public Text hairIndexText;

    [Header("Hat Buttons")]
    public Button hatPrevButton;
    public Button hatNextButton;
    public Text hatIndexText;

    [Header("Eye Buttons")]
    public Button eyePrevButton;
    public Button eyeNextButton;
    public Text eyeIndexText;

    [Header("Mouth Buttons")]
    public Button mouthPrevButton;
    public Button mouthNextButton;
    public Text mouthIndexText;

    [Header("Eyebrow Buttons")]
    public Button eyebrowPrevButton;
    public Button eyebrowNextButton;
    public Text eyebrowIndexText;

    [Header("Cheek Buttons")]
    public Button cheekPrevButton;
    public Button cheekNextButton;
    public Text cheekIndexText;

    [Header("Other Buttons")]
    public Button randomizeButton;

    private void Start()
    {
        // Auto-find partSwapper
        if (partSwapper == null)
        {
            partSwapper = FindObjectOfType<CharacterPartSwapper>();
        }

        if (partSwapper == null)
        {
            Debug.LogError("CharacterPartSwapper not found!");
            return;
        }

        // Setup button events
        SetupButtons();

        // Display initial indices
        UpdateAllIndexTexts();
    }

    private void SetupButtons()
    {
        // Body buttons
        if (bodyPrevButton != null)
            bodyPrevButton.onClick.AddListener(() => { partSwapper.PreviousBody(); UpdateIndexText(bodyIndexText, partSwapper.currentBodyIndex); });
        if (bodyNextButton != null)
            bodyNextButton.onClick.AddListener(() => { partSwapper.NextBody(); UpdateIndexText(bodyIndexText, partSwapper.currentBodyIndex); });

        // Hair buttons
        if (hairPrevButton != null)
            hairPrevButton.onClick.AddListener(() => { partSwapper.PreviousHair(); UpdateIndexText(hairIndexText, partSwapper.currentHairIndex); });
        if (hairNextButton != null)
            hairNextButton.onClick.AddListener(() => { partSwapper.NextHair(); UpdateIndexText(hairIndexText, partSwapper.currentHairIndex); });

        // Hat buttons
        if (hatPrevButton != null)
            hatPrevButton.onClick.AddListener(() => { partSwapper.PreviousHat(); UpdateIndexText(hatIndexText, partSwapper.currentHatIndex); });
        if (hatNextButton != null)
            hatNextButton.onClick.AddListener(() => { partSwapper.NextHat(); UpdateIndexText(hatIndexText, partSwapper.currentHatIndex); });

        // Eye buttons
        if (eyePrevButton != null)
            eyePrevButton.onClick.AddListener(() => { partSwapper.PreviousEye(); UpdateIndexText(eyeIndexText, partSwapper.currentEyeIndex); });
        if (eyeNextButton != null)
            eyeNextButton.onClick.AddListener(() => { partSwapper.NextEye(); UpdateIndexText(eyeIndexText, partSwapper.currentEyeIndex); });

        // Mouth buttons
        if (mouthPrevButton != null)
            mouthPrevButton.onClick.AddListener(() => { partSwapper.PreviousMouth(); UpdateIndexText(mouthIndexText, partSwapper.currentMouthIndex); });
        if (mouthNextButton != null)
            mouthNextButton.onClick.AddListener(() => { partSwapper.NextMouth(); UpdateIndexText(mouthIndexText, partSwapper.currentMouthIndex); });

        // Eyebrow buttons
        if (eyebrowPrevButton != null)
            eyebrowPrevButton.onClick.AddListener(() => { partSwapper.PreviousEyebrow(); UpdateIndexText(eyebrowIndexText, partSwapper.currentEyebrowIndex); });
        if (eyebrowNextButton != null)
            eyebrowNextButton.onClick.AddListener(() => { partSwapper.NextEyebrow(); UpdateIndexText(eyebrowIndexText, partSwapper.currentEyebrowIndex); });

        // Cheek buttons
        if (cheekPrevButton != null)
            cheekPrevButton.onClick.AddListener(() => { partSwapper.PreviousCheek(); UpdateIndexText(cheekIndexText, partSwapper.currentCheekIndex); });
        if (cheekNextButton != null)
            cheekNextButton.onClick.AddListener(() => { partSwapper.NextCheek(); UpdateIndexText(cheekIndexText, partSwapper.currentCheekIndex); });

        // Randomize button
        if (randomizeButton != null)
            randomizeButton.onClick.AddListener(() => { partSwapper.RandomizeAll(); UpdateAllIndexTexts(); });
    }

    private void UpdateIndexText(Text textComponent, int index)
    {
        if (textComponent != null)
        {
            textComponent.text = (index + 1).ToString();
        }
    }

    private void UpdateAllIndexTexts()
    {
        UpdateIndexText(bodyIndexText, partSwapper.currentBodyIndex);
        UpdateIndexText(hairIndexText, partSwapper.currentHairIndex);
        UpdateIndexText(hatIndexText, partSwapper.currentHatIndex);
        UpdateIndexText(eyeIndexText, partSwapper.currentEyeIndex);
        UpdateIndexText(mouthIndexText, partSwapper.currentMouthIndex);
        UpdateIndexText(eyebrowIndexText, partSwapper.currentEyebrowIndex);
        UpdateIndexText(cheekIndexText, partSwapper.currentCheekIndex);
    }
}
