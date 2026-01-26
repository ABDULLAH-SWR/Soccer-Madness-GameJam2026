using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public bool isRedGoal; // Check this for the Left Goal (Red's side)

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            // Tell the Manager a goal happened
            GameManager.instance.GoalScored(isRedGoal);
        }
    }
}