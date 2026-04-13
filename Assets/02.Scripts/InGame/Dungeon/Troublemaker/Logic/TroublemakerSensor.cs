using UnityEngine;

public class TroublemakerSensor : MonoBehaviour
{
    public PlayerController FindNearestTarget(float detectRange)
    {
        if (SaveManager.Instance == null) return null;

        PlayerController bestTarget = null;
        float bestSqrDistance = detectRange * detectRange;
        Vector3 origin = transform.position;

        foreach (PlayerController player in SaveManager.Instance.RegisteredPlayers)
        {
            if (player == null) continue;
            if (!player.gameObject.activeInHierarchy) continue;

            float sqrDistance = (player.transform.position - origin).sqrMagnitude;
            if (sqrDistance > bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            bestTarget = player;
        }

        return bestTarget;
    }
}
