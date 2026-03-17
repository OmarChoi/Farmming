using UnityEngine;

public class TestHelperSummoner : MonoBehaviour
{
    [SerializeField] private HelperController _helperPrefab;
    [SerializeField] private PlayerHelperAbility _playerHelper;

    private HelperController _instance;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (_instance == null)
                _instance = Instantiate(_helperPrefab);

            _playerHelper.Summon(_instance);
        }
    }
}