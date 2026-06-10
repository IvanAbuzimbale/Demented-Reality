using System;
using UnityEngine;

public class Damage : MonoBehaviour
{
    #region DamageVariables
        [SerializeField] int damageAmount = 1;
        [SerializeField] float knockbackForce = 7f;
        [SerializeField] LayerMask ignoreLayers;
        [SerializeField] bool damageDisabled = false;
    #endregion

    // Helper method to check if a GameObject should be ignored based 
    // on the ignoreLayers mask
    private bool ShouldIgnore(GameObject go) =>
        go == gameObject || (ignoreLayers.value & (1 << go.layer)) != 0;


    // Handle trigger damage application
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (ShouldIgnore(collision.gameObject)) return;

        if (collision.TryGetComponent<Health>(out var health)) {
            
            // Check if the target can take damage before applying it
            if (!CanApplyDamage(health)) return;
            health.TakeDamage(damageAmount);

            // Apply knockback if the target has a Rigidbody2D
            Rigidbody2D rb = collision.attachedRigidbody;
            if (rb != null) ApplyKnockback(rb, collision.transform);
        }
    }


    // Handle collision damage application for non-trigger colliders
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (ShouldIgnore(collision.gameObject)) return;

        if (collision.collider.TryGetComponent<Health>(out var health)) {

            // Check if the target can take damage before applying it
            if (!CanApplyDamage(health)) return;
            health.TakeDamage(damageAmount);

            // Apply knockback if the target has a Rigidbody2D
            Rigidbody2D rb = collision.rigidbody;
            if (rb != null) ApplyKnockback(rb, collision.transform);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (ShouldIgnore(collision.gameObject)) return;

        if (collision.collider.TryGetComponent<Health>(out var health)) {

            // Check if the target can take damage before applying it
            if (!CanApplyDamage(health)) return;
            health.TakeDamage(damageAmount);

            // Apply knockback if the target has a Rigidbody2D
            Rigidbody2D rb = collision.rigidbody;
            if (rb != null) ApplyKnockback(rb, collision.transform);
        }
    }

    private bool CanApplyDamage(Health health) =>
        !damageDisabled && health != null && health.canTakeDamage && health.GetCurrentHealth() > 0;

    // Apply knockback force to the target Rigidbody2D based on the direction from the damage source
    private void ApplyKnockback(Rigidbody2D rb, Transform target)
    {
        // Calculate horizontal direction from the damage source to the target
        float horizontal = Mathf.Sign(target.position.x - transform.position.x);
        Vector2 forceDirection = new Vector2(horizontal, 0.75f).normalized;

        // Reset current velocity before applying knockback to ensure consistent force application
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(forceDirection * knockbackForce, ForceMode2D.Impulse);
    }
}