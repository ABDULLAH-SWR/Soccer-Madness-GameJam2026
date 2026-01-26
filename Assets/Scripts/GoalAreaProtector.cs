using UnityEngine;

public class GoalAreaProtector : MonoBehaviour
{
    // How far to push them out?
    public float pushDistance = 2.0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Check if the object is a Player (ignore the Ball!)
        if (other.CompareTag("Player"))
        {
            EjectPlayer(other.transform);
        }
    }

    void EjectPlayer(Transform playerTransform)
    {
        // 2. Calculate the direction towards the center of the field (0,0)
        // This works for both Left and Right goals automatically.
        Vector2 centerField = Vector2.zero;
        Vector2 goalPosition = transform.position;

        // We only care about Left/Right direction, not Up/Down
        Vector2 pushDirection = (centerField - goalPosition).normalized;
        pushDirection.y = 0; // Keep them on the same vertical line, just push them horizontally

        // 3. Move the player out
        playerTransform.position = new Vector2(
            playerTransform.position.x + (pushDirection.x * pushDistance),
            playerTransform.position.y
        );

        // Optional: Stop their momentum so they don't slide right back in
        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Player entered restricted area and was ejected!");
    }
}