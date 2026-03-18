using UnityEngine;

public class WoodCuttingVfx : MonoBehaviour
{
    [SerializeField] private float _maxDistance = 2f;
    [SerializeField] private float _speed = 5f;

    private Vector3 _startPosition;

    private void Awake()
    {
        _startPosition = transform.position;
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
}
