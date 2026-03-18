using UnityEngine;

public class TestInteracter : MonoBehaviour
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _interactionLayer;
    [SerializeField] private KeyCode _interactKey = KeyCode.P;

    private void Update()
    {
        if (Input.GetKeyDown(_interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _interactionLayer);

        INpcInteraction closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            INpcInteraction interactable = hit.GetComponentInParent<INpcInteraction>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        if (closest != null)
        {
            closest.RequestInteract(transform);
        }
    }
}
