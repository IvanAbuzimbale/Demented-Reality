using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{

    #region Fields
        private Rigidbody2D rb;
        private Transform groundCheckStart;
        private Transform groundCheckEnd;
        private Transform wallCheckStart;
        private Transform wallCheckEnd;
        [SerializeField] private LayerMask layerMask;
    #endregion

    #region MovementVariables
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float acceleration = 0.025f;
    #endregion

    #region Audio
        private AudioManager audioManager;
        [SerializeField] private string moveSFX;
    #endregion

    void Start() {
        groundCheckStart = transform.GetChild(0);
        groundCheckEnd = transform.GetChild(1);
        wallCheckStart = transform.GetChild(2);
        wallCheckEnd = transform.GetChild(3);

        rb = GetComponent<Rigidbody2D>();
        audioManager = AudioManager.Instance;
    }

    void FixedUpdate()
    {
        // Move enemy horizontally based on moveSpeed and acceleration
        float horizontalVelocity = Mathf.Lerp(rb.linearVelocity.x, -moveSpeed, acceleration);
        rb.linearVelocity = new(horizontalVelocity, rb.linearVelocity.y);

        // Check for walls and edges to determine if the enemy should turn around
        if (ShouldTurnAround()) {
            moveSpeed = -moveSpeed;
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
    }

    // Method to determine if the enemy should turn around based on wall and ground checks
    bool ShouldTurnAround() {
        RaycastHit2D wallHit = Physics2D.Linecast(wallCheckStart.position, wallCheckEnd.position, layerMask);
        RaycastHit2D groundHit = Physics2D.Linecast(groundCheckStart.position, groundCheckEnd.position, layerMask);
        return wallHit.collider != null || groundHit.collider == null;
    }

    // Method to play movement sound effect, can be called from animation events or other triggers
    public void PlayMoveSFX() {
        audioManager.PlaySFX(moveSFX, true, (-0.25f, 0.25f), (-0.25f, 0.25f), false, transform.position, 4f);
    }
}
