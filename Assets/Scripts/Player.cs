using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
public class Player : MonoBehaviour
{
    #region InputActions
        public InputActionAsset inputActions;

        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction attackAction;
        private int currentAttack = 0;

        private void OnEnable() => inputActions.FindActionMap("Player").Enable();   
        void OnDisable() => inputActions.FindActionMap("Player").Disable();

        // Input Variables
        private Vector2 moveInput;
        private bool isSprinting;
        private bool jumpTriggered;
    #endregion
    

    #region StateVariables
        private Rigidbody2D rb;
        private Animator animator;
        private Transform groundCheckStart;
        private Transform groundCheckEnd;
        private AudioManager audioManager;
        private ParticleSystem dustPS;
    #endregion
    
    #region MovementVariables

        // Movement
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float acceleration = 0.025f;
        private bool facingRight = true;

        //Jumping
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private int maxJumpCount = 2;
        [SerializeField] private int jumpCount = 0;  

        //Ground Check
        [SerializeField] private bool isGrounded = false;
        private bool wasGrounded = false;        
        [SerializeField] private LayerMask groundLayer;
    #endregion

    void Start() {
        // Input Actions
        moveAction = inputActions.FindAction("Move");
        sprintAction = inputActions.FindAction("Sprint");
        jumpAction = inputActions.FindAction("Jump");
        attackAction = inputActions.FindAction("Attack");

        // State Variables
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        groundCheckStart = transform.GetChild(0);
        groundCheckEnd = transform.GetChild(1);
        dustPS = transform.GetChild(4).GetComponent<ParticleSystem>();

        audioManager = AudioManager.Instance;
    }

    void Update()
    {
        // Read Inputs
        moveInput = moveAction.ReadValue<Vector2>();
        isSprinting = sprintAction != null && sprintAction.ReadValue<float>() > 0.5f;

        // Handle Jump Input
        if (jumpAction.triggered)
            jumpTriggered = true;

        // Handle Facing Direction
        facingRight = moveInput.x > 0 || moveInput.x >= 0 && facingRight;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);

        // Update Animator Parameters
        animator.SetFloat("HorizontalVelocity", Math.Abs(rb.linearVelocity.x));
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);

        // Handle Attack Input
        if (attackAction.triggered) {
            currentAttack = (currentAttack + 1) % 3;            
            switch (currentAttack) {
                case 0: 
                    animator.SetTrigger("Attack1");
                    break;
                case 1:  
                    animator.SetTrigger("Attack2");
                    break;
                case 2:
                    animator.SetTrigger("Attack3");
                    break;
            }
        }
    }

    void FixedUpdate()
    {
        // Check if grounded
        isGrounded = IsGrounded();

        // Reset jump count when grounded
        if (isGrounded && rb.linearVelocity.y <= 0)
            jumpCount = maxJumpCount;

        // Play landing SFX and dust particles
        if (isGrounded && !wasGrounded) {
            audioManager.PlaySFX("land", true, (-0.25f, 0.25f), (-0.25f, 0.25f));
            dustPS.Play();
        }
        wasGrounded = isGrounded;

        // Handle horizontal movement
        Vector2 velocity = moveInput * (isSprinting ? sprintSpeed : moveSpeed);
        float horizontalVelocity = Mathf.Lerp(rb.linearVelocity.x, velocity.x, acceleration);
        rb.linearVelocity = new(horizontalVelocity, rb.linearVelocity.y);

        // Handle jumping
        if (jumpTriggered && jumpCount > 0) {
            if (jumpCount == maxJumpCount) dustPS.Play();
            rb.linearVelocity = new(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount--;
            audioManager.PlaySFX("jump_up", true, (-0.25f, 0.25f), (-0.25f, 0.25f));
        }
        jumpTriggered = false;
    }

    // Ground check using linecast
    bool IsGrounded() {
        RaycastHit2D hit = Physics2D.Linecast(groundCheckStart.position, groundCheckEnd.position, groundLayer);
        return hit.collider != null;
    }

    // Animation Event Methods
    public void PlaySwordSFX() {
        audioManager.PlaySFX("sword_swing", true, (-0.25f, 0.25f), (-0.25f, 0.25f));
    }
    public void PlayFootstepSFX() {
        audioManager.PlaySFX("footstep", true, (-0.25f, 0.25f), (-0.25f, 0.25f));
    }
}