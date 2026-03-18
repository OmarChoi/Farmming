using UnityEngine;

public class TestHelperSummoner : MonoBehaviour
{
    [SerializeField] private HelperController _helperPrefab;
    [SerializeField] private PlayerHelperInteractionAbility _playerHelperInteraction;

    private HelperController _instance;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (_instance == null)
                _instance = Instantiate(_helperPrefab);

            _playerHelperInteraction.Summon(_instance);
        }
    }
}