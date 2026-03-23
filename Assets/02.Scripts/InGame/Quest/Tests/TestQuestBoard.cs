using UnityEngine;

public class TestQuestBoard : MonoBehaviour
{
    [SerializeField] private QuestBoard _questBoard;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            _questBoard.Interact();
        }
    }
}
