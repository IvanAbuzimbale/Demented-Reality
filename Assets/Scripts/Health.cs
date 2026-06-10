using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Health : MonoBehaviour
{
    #region HealthVariables
        [SerializeField] int maxHealth = 4;
        [SerializeField] private int _currentHealth;
        public bool canTakeDamage = true;
    #endregion

    #region Components
        private SpriteRenderer _spriteRenderer;
        private Animator animator;
        [SerializeField] private TextMeshProUGUI healthText;
    #endregion

    #region Audio
        private AudioManager audioManager;
        [SerializeField] private string damageSFX;
        [SerializeField] private string deathSFX;
    #endregion

    // Flag to determine if this Health component belongs to the player, 
    // used to trigger different death behavior
    [SerializeField] bool isPlayer = false;


    void Start()
    {
        _currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        audioManager = AudioManager.Instance;

        if (healthText != null)
            healthText.text = "Health points: " + _currentHealth.ToString();        
    }
    
    public void TakeDamage(int damage)
    {
        // Prevent taking damage if currently invulnerable or already at 0 health
        canTakeDamage = false;
        audioManager.PlaySFX(damageSFX);

        // Reduce current health by damage amount and start flash red coroutine
        _currentHealth -= damage;
        if (healthText != null)
            healthText.text = "Health points: " + _currentHealth.ToString();
        StartCoroutine(FlashRed());

        // Check for death condition and trigger death behavior if health is 0 or below
        if (_currentHealth <= 0) {
            audioManager.PlaySFX(deathSFX);
            animator.SetBool("Die", true);
        }
    }

    // Coroutine to flash the sprite red when taking damage and 
    // then return to normal color after a short delay
    IEnumerator FlashRed()
    {
        _spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.25f);
        _spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.25f);
        canTakeDamage = true;
    }

    // Method to handle death behavior, called from animation 
    // event at the end of the death animation
    public void Die()
    {        
        // If this is the player, reload the current scene to simulate respawning
        if (isPlayer) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        else Destroy(gameObject);
        return;        
    }

    // Public method to get the current health value, can be 
    // used by other scripts to check health status
    public int GetCurrentHealth() => _currentHealth;
}
