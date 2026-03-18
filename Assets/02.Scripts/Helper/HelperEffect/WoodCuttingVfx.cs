using UnityEngine;

public class WoodCuttingVfx : MonoBehaviour
{
    [SerializeField] private float _maxDistance = 5f;
    [SerializeField] private float _speed = 5f;
    private int _damage;
    EGatherType _type;

    private Vector3 _startPosition;

    private void Awake()
    {
        _startPosition = transform.position;
    }

    public void Initiate(int damage, EGatherType type)
    {
        _damage = damage;
        _type = type;
    }

    private void Update()
    {
        transform.position += transform.forward *_speed * Time.deltaTime;

        float distance = Vector3.Distance(_startPosition, transform.position);
        if( distance > _maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other);
        IGatherable gatherable = other.GetComponent<IGatherable>();

        if (gatherable == null)
        {
            Debug.Log("NULL");
            return;
        }

        switch (_type)
        {
            case EGatherType.Wood:
                Debug.Log("wood");
                if (gatherable is Wood)
                {
                    Debug.Log("wood333");
                    gatherable.TryGather(_damage);
                }
                break;

            case EGatherType.Rock:
                Debug.Log("rock");
                if (gatherable is Stone)
                {
                    gatherable.TryGather(_damage);
                }
                break;
        }
    }
}
