using UnityEngine;

public class FollowPlayer : MonoBehaviour
{

    Transform player;
    [SerializeField] [Range(0.01f, 0.1f)] float speed = 0.01f;
    [SerializeField] [Range(0f, 5f)] float lookAheadDistance = 2f;
    [SerializeField] [Range(0f, 5f)] float verticalOffset = 2f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        // Calculate the target position based on the player's position and 
        // apply a look-ahead offset in the direction the player is facing
        Vector3 newPos = new(player.position.x, player.position.y + verticalOffset, transform.position.z);
        Vector3 lookDirectionOffset = player.transform.localScale.x * Vector3.right * lookAheadDistance;
        transform.position = Vector3.Lerp(transform.position, newPos + lookDirectionOffset, speed);
    }
}
