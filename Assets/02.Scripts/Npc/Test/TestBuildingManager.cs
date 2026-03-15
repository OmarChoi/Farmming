using UnityEngine;

public class TestBuildingManager : MonoBehaviour
{
    public static TestBuildingManager Instance { get; private set; }

    [SerializeField] private Transform _homePoint;
    [SerializeField] private Transform _shopPoint;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetLocationPosition(string npcId, ENpcLocationType type)
    {
        switch (type)
        {
            case ENpcLocationType.Home:
                return _homePoint.position;

            case ENpcLocationType.Shop:
                return _shopPoint.position;
        }

        return Vector3.zero;
    }
}
