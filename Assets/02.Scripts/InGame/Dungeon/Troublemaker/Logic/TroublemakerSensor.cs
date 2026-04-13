using UnityEngine;

public class TroublemakerSensor : MonoBehaviour
{
    public Transform FindNearestTarget(float detectRange)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        Transform bestTarget = null;
        float bestSqrDistance = detectRange * detectRange;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;

            float sqrDistance = (players[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            bestTarget = players[i].transform;
        }

        return bestTarget;
    }
}
