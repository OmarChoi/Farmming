using UnityEngine;

/// <summary>
/// Character Customization System
/// - Body: SkinnedMeshRenderer mesh swapping + bone rebinding
/// - Face Parts: Instantiated under head_dummy
/// </summary>
public class CharacterPartSwapper : MonoBehaviour
{
    [Header("Basic Setup")]
    [Tooltip("Character's base Body SkinnedMeshRenderer (for bone structure reference)")]
    public SkinnedMeshRenderer baseBodyRenderer;

    [Tooltip("Head_Dummy Transform (parent of face parts)")]
    public Transform headDummy;

    [Header("Part Prefab Lists")]
    [Tooltip("Available Body prefabs")]
    public GameObject[] bodyPrefabs;

    [Tooltip("Available Hair prefabs")]
    public GameObject[] hairPrefabs;

    [Tooltip("Available Hat prefabs")]
    public GameObject[] hatPrefabs;

    [Tooltip("Available Eye prefabs")]
    public GameObject[] eyePrefabs;

    [Tooltip("Available Mouth prefabs")]
    public GameObject[] mouthPrefabs;

    [Tooltip("Available Eyebrow prefabs")]
    public GameObject[] eyebrowPrefabs;

    [Tooltip("Available Cheek prefabs")]
    public GameObject[] cheekPrefabs;

    [Header("Current Selected Indices")]
    public int currentBodyIndex = 0;
    public int currentHairIndex = 0;
    public int currentHatIndex = 0;
    public int currentEyeIndex = 0;
    public int currentMouthIndex = 0;
    public int currentEyebrowIndex = 0;
    public int currentCheekIndex = 0;

    [Header("Current Parts (Auto-updated)")]
    public GameObject currentHair;
    public GameObject currentHat;
    public GameObject currentEye;
    public GameObject currentMouth;
    public GameObject currentEyebrow;
    public GameObject currentCheek;

    private void Start()
    {
        // Auto-find baseBodyRenderer if not set
        if (baseBodyRenderer == null)
        {
            baseBodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        // Auto-find headDummy if not set
        if (headDummy == null)
        {
            headDummy = FindDeepChild(transform, "head_dummy");
            if (headDummy == null)
            {
                Debug.LogWarning("Could not find head_dummy!");
            }
        }
    }

    // ==================== Body Part Swapping ====================
    [ContextMenu("Body: Next")]
    public void NextBody()
    {
        if (bodyPrefabs == null || bodyPrefabs.Length == 0) return;
        currentBodyIndex = (currentBodyIndex + 1) % bodyPrefabs.Length;
        SetBody(currentBodyIndex);
    }

    [ContextMenu("Body: Previous")]
    public void PreviousBody()
    {
        if (bodyPrefabs == null || bodyPrefabs.Length == 0) return;
        currentBodyIndex = (currentBodyIndex - 1 + bodyPrefabs.Length) % bodyPrefabs.Length;
        SetBody(currentBodyIndex);
    }

    public void SetBody(int index)
    {
        if (bodyPrefabs == null || index < 0 || index >= bodyPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Body index: {index}");
            return;
        }
        currentBodyIndex = index;
        SwapBodyMesh(baseBodyRenderer, bodyPrefabs[index]);
        Debug.Log($"Body part swapped: {bodyPrefabs[index].name}");
    }

    // ==================== Hair Part Swapping ====================
    [ContextMenu("Hair: Next")]
    public void NextHair()
    {
        if (hairPrefabs == null || hairPrefabs.Length == 0) return;
        currentHairIndex = (currentHairIndex + 1) % hairPrefabs.Length;
        SetHair(currentHairIndex);
    }

    [ContextMenu("Hair: Previous")]
    public void PreviousHair()
    {
        if (hairPrefabs == null || hairPrefabs.Length == 0) return;
        currentHairIndex = (currentHairIndex - 1 + hairPrefabs.Length) % hairPrefabs.Length;
        SetHair(currentHairIndex);
    }

    public void SetHair(int index)
    {
        if (hairPrefabs == null || index < 0 || index >= hairPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Hair index: {index}");
            return;
        }
        currentHairIndex = index;
        currentHair = SwapFacePart(currentHair, hairPrefabs[index]);
        Debug.Log($"Hair part swapped: {hairPrefabs[index].name}");
    }

    // ==================== Hat Part Swapping ====================
    [ContextMenu("Hat: Next")]
    public void NextHat()
    {
        if (hatPrefabs == null || hatPrefabs.Length == 0) return;
        currentHatIndex = (currentHatIndex + 1) % hatPrefabs.Length;
        SetHat(currentHatIndex);
    }

    [ContextMenu("Hat: Previous")]
    public void PreviousHat()
    {
        if (hatPrefabs == null || hatPrefabs.Length == 0) return;
        currentHatIndex = (currentHatIndex - 1 + hatPrefabs.Length) % hatPrefabs.Length;
        SetHat(currentHatIndex);
    }

    public void SetHat(int index)
    {
        if (hatPrefabs == null || index < 0 || index >= hatPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Hat index: {index}");
            return;
        }
        currentHatIndex = index;
        currentHat = SwapFacePart(currentHat, hatPrefabs[index]);
        Debug.Log($"Hat part swapped: {hatPrefabs[index].name}");
    }

    // ==================== Eye Part Swapping ====================
    [ContextMenu("Eye: Next")]
    public void NextEye()
    {
        if (eyePrefabs == null || eyePrefabs.Length == 0) return;
        currentEyeIndex = (currentEyeIndex + 1) % eyePrefabs.Length;
        SetEye(currentEyeIndex);
    }

    [ContextMenu("Eye: Previous")]
    public void PreviousEye()
    {
        if (eyePrefabs == null || eyePrefabs.Length == 0) return;
        currentEyeIndex = (currentEyeIndex - 1 + eyePrefabs.Length) % eyePrefabs.Length;
        SetEye(currentEyeIndex);
    }

    public void SetEye(int index)
    {
        if (eyePrefabs == null || index < 0 || index >= eyePrefabs.Length)
        {
            Debug.LogWarning($"Invalid Eye index: {index}");
            return;
        }
        currentEyeIndex = index;
        currentEye = SwapFacePart(currentEye, eyePrefabs[index]);
        Debug.Log($"Eye part swapped: {eyePrefabs[index].name}");
    }

    // ==================== Mouth Part Swapping ====================
    [ContextMenu("Mouth: Next")]
    public void NextMouth()
    {
        if (mouthPrefabs == null || mouthPrefabs.Length == 0) return;
        currentMouthIndex = (currentMouthIndex + 1) % mouthPrefabs.Length;
        SetMouth(currentMouthIndex);
    }

    [ContextMenu("Mouth: Previous")]
    public void PreviousMouth()
    {
        if (mouthPrefabs == null || mouthPrefabs.Length == 0) return;
        currentMouthIndex = (currentMouthIndex - 1 + mouthPrefabs.Length) % mouthPrefabs.Length;
        SetMouth(currentMouthIndex);
    }

    public void SetMouth(int index)
    {
        if (mouthPrefabs == null || index < 0 || index >= mouthPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Mouth index: {index}");
            return;
        }
        currentMouthIndex = index;
        currentMouth = SwapFacePart(currentMouth, mouthPrefabs[index]);
        Debug.Log($"Mouth part swapped: {mouthPrefabs[index].name}");
    }

    // ==================== Eyebrow Part Swapping ====================
    [ContextMenu("Eyebrow: Next")]
    public void NextEyebrow()
    {
        if (eyebrowPrefabs == null || eyebrowPrefabs.Length == 0) return;
        currentEyebrowIndex = (currentEyebrowIndex + 1) % eyebrowPrefabs.Length;
        SetEyebrow(currentEyebrowIndex);
    }

    [ContextMenu("Eyebrow: Previous")]
    public void PreviousEyebrow()
    {
        if (eyebrowPrefabs == null || eyebrowPrefabs.Length == 0) return;
        currentEyebrowIndex = (currentEyebrowIndex - 1 + eyebrowPrefabs.Length) % eyebrowPrefabs.Length;
        SetEyebrow(currentEyebrowIndex);
    }

    public void SetEyebrow(int index)
    {
        if (eyebrowPrefabs == null || index < 0 || index >= eyebrowPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Eyebrow index: {index}");
            return;
        }
        currentEyebrowIndex = index;
        currentEyebrow = SwapFacePart(currentEyebrow, eyebrowPrefabs[index]);
        Debug.Log($"Eyebrow part swapped: {eyebrowPrefabs[index].name}");
    }

    // ==================== Cheek Part Swapping ====================
    [ContextMenu("Cheek: Next")]
    public void NextCheek()
    {
        if (cheekPrefabs == null || cheekPrefabs.Length == 0) return;
        currentCheekIndex = (currentCheekIndex + 1) % cheekPrefabs.Length;
        SetCheek(currentCheekIndex);
    }

    [ContextMenu("Cheek: Previous")]
    public void PreviousCheek()
    {
        if (cheekPrefabs == null || cheekPrefabs.Length == 0) return;
        currentCheekIndex = (currentCheekIndex - 1 + cheekPrefabs.Length) % cheekPrefabs.Length;
        SetCheek(currentCheekIndex);
    }

    public void SetCheek(int index)
    {
        if (cheekPrefabs == null || index < 0 || index >= cheekPrefabs.Length)
        {
            Debug.LogWarning($"Invalid Cheek index: {index}");
            return;
        }
        currentCheekIndex = index;
        currentCheek = SwapFacePart(currentCheek, cheekPrefabs[index]);
        Debug.Log($"Cheek part swapped: {cheekPrefabs[index].name}");
    }

    // ==================== Core Logic ====================

    /// <summary>
    /// Face part swapping core logic (instantiated under head_dummy)
    /// </summary>
    /// <param name="currentPart">Current part GameObject</param>
    /// <param name="newPartPrefab">New part prefab</param>
    /// <returns>Created new part GameObject</returns>
    private GameObject SwapFacePart(GameObject currentPart, GameObject newPartPrefab)
    {
        if (headDummy == null)
        {
            Debug.LogError("head_dummy is not set!");
            return currentPart;
        }

        // Destroy existing part
        if (currentPart != null)
        {
            if (Application.isPlaying)
            {
                Destroy(currentPart);
            }
            else
            {
                DestroyImmediate(currentPart);
            }
        }

        // Instantiate new part
        GameObject newPart = Instantiate(newPartPrefab, headDummy);
        newPart.transform.localPosition = Vector3.zero;
        newPart.transform.localRotation = Quaternion.identity;
        newPart.transform.localScale = Vector3.one;

        return newPart;
    }

    /// <summary>
    /// Body mesh swapping + bone rebinding
    /// </summary>
    /// <param name="targetRenderer">SkinnedMeshRenderer to swap</param>
    /// <param name="newBodyPrefab">New Body prefab</param>
    private void SwapBodyMesh(SkinnedMeshRenderer targetRenderer, GameObject newBodyPrefab)
    {
        // Get SkinnedMeshRenderer from new part
        SkinnedMeshRenderer newPartRenderer = newBodyPrefab.GetComponentInChildren<SkinnedMeshRenderer>();

        if (newPartRenderer == null)
        {
            Debug.LogError($"SkinnedMeshRenderer not found in {newBodyPrefab.name}!");
            return;
        }

        // Backup existing bone array
        Transform[] existingBones = targetRenderer.bones;
        Transform existingRootBone = targetRenderer.rootBone;

        // ===== Unity Bug Workaround: Clear before mesh swap =====
        // Disable renderer
        bool wasEnabled = targetRenderer.enabled;
        targetRenderer.enabled = false;

        // Clear mesh (important!)
        targetRenderer.sharedMesh = null;

        // Assign new mesh and materials
        targetRenderer.sharedMesh = newPartRenderer.sharedMesh;
        targetRenderer.materials = newPartRenderer.sharedMaterials;

        // Rebind bones
        RebindBones(targetRenderer, newPartRenderer, existingBones, existingRootBone);

        // Recalculate bounds (fixes rendering issues)
        targetRenderer.localBounds = newPartRenderer.localBounds;

        // Re-enable renderer
        targetRenderer.enabled = wasEnabled;

        // Force update
        if (Application.isPlaying)
        {
            targetRenderer.forceMatrixRecalculationPerRender = true;
        }
    }

    /// <summary>
    /// Bone rebinding - matching by name
    /// </summary>
    /// <param name="targetRenderer">Renderer to swap</param>
    /// <param name="sourceRenderer">New part's renderer</param>
    /// <param name="existingBones">Existing bone array</param>
    /// <param name="existingRootBone">Existing root bone</param>
    private void RebindBones(SkinnedMeshRenderer targetRenderer, SkinnedMeshRenderer sourceRenderer,
                             Transform[] existingBones, Transform existingRootBone)
    {
        // Find root bone
        Transform rootBone = existingRootBone;

        if (rootBone == null)
        {
            // Find Bip001 (root bone of this asset)
            rootBone = FindDeepChild(transform, "Bip001");

            if (rootBone == null && baseBodyRenderer != null)
            {
                rootBone = baseBodyRenderer.rootBone;
            }
        }

        if (rootBone == null)
        {
            Debug.LogError("Root bone not found!");
            return;
        }

        targetRenderer.rootBone = rootBone;

        // Rebuild bones array
        Transform[] newBones = new Transform[sourceRenderer.bones.Length];

        for (int i = 0; i < sourceRenderer.bones.Length; i++)
        {
            if (sourceRenderer.bones[i] != null)
            {
                string boneName = sourceRenderer.bones[i].name;

                // Priority 1: Find in existing bone array by name
                Transform foundBone = null;
                if (existingBones != null)
                {
                    foreach (var bone in existingBones)
                    {
                        if (bone != null && bone.name == boneName)
                        {
                            foundBone = bone;
                            break;
                        }
                    }
                }

                // Priority 2: Recursive search from rootBone
                if (foundBone == null)
                {
                    foundBone = FindBoneByName(rootBone, boneName);
                }

                // Priority 3: Search entire transform
                if (foundBone == null)
                {
                    foundBone = FindDeepChild(transform, boneName);
                }

                if (foundBone != null)
                {
                    newBones[i] = foundBone;
                }
                else
                {
                    Debug.LogWarning($"Bone not found: {boneName}");
                }
            }
        }

        targetRenderer.bones = newBones;

        Debug.Log($"Bone rebinding completed: {newBones.Length} bones");
    }

    /// <summary>
    /// Find bone by name (recursive)
    /// </summary>
    /// <param name="parent">Starting Transform</param>
    /// <param name="boneName">Bone name to find</param>
    /// <returns>Found Transform or null</returns>
    private Transform FindBoneByName(Transform parent, string boneName)
    {
        if (parent == null) return null;

        // Check if current Transform matches
        if (parent.name == boneName)
        {
            return parent;
        }

        // Recursively search children
        foreach (Transform child in parent)
        {
            Transform result = FindBoneByName(child, boneName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Set part at runtime (called by index)
    /// </summary>
    /// <param name="partType">Part type (Hair, Hat, Eye, Mouth, Eyebrow, Cheek, Body)</param>
    /// <param name="index">Part index</param>
    public void SetPartByType(string partType, int index)
    {
        switch (partType.ToLower())
        {
            case "body":
                SetBody(index);
                break;
            case "hair":
                SetHair(index);
                break;
            case "hat":
                SetHat(index);
                break;
            case "eye":
                SetEye(index);
                break;
            case "mouth":
                SetMouth(index);
                break;
            case "eyebrow":
                SetEyebrow(index);
                break;
            case "cheek":
                SetCheek(index);
                break;
            default:
                Debug.LogWarning($"Unknown part type: {partType}");
                break;
        }
    }

    /// <summary>
    /// Random customization
    /// </summary>
    [ContextMenu("Randomize All")]
    public void RandomizeAll()
    {
        if (bodyPrefabs != null && bodyPrefabs.Length > 0)
            SetBody(Random.Range(0, bodyPrefabs.Length));

        if (hairPrefabs != null && hairPrefabs.Length > 0)
            SetHair(Random.Range(0, hairPrefabs.Length));

        if (hatPrefabs != null && hatPrefabs.Length > 0)
            SetHat(Random.Range(0, hatPrefabs.Length));

        if (eyePrefabs != null && eyePrefabs.Length > 0)
            SetEye(Random.Range(0, eyePrefabs.Length));

        if (mouthPrefabs != null && mouthPrefabs.Length > 0)
            SetMouth(Random.Range(0, mouthPrefabs.Length));

        if (eyebrowPrefabs != null && eyebrowPrefabs.Length > 0)
            SetEyebrow(Random.Range(0, eyebrowPrefabs.Length));

        if (cheekPrefabs != null && cheekPrefabs.Length > 0)
            SetCheek(Random.Range(0, cheekPrefabs.Length));

        Debug.Log("Random customization completed!");
    }

    /// <summary>
    /// Find child in transform hierarchy by name (recursive)
    /// </summary>
    /// <param name="parent">Starting Transform</param>
    /// <param name="name">Child name to find</param>
    /// <returns>Found Transform or null</returns>
    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;

        // Check direct child first
        Transform result = parent.Find(name);
        if (result != null) return result;

        // Recursively search all children
        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name);
            if (result != null) return result;
        }

        return null;
    }
}
