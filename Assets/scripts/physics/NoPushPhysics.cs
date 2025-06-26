using UnityEngine;

public class NoPushPhysics : MonoBehaviour
{
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb != null)
        {
            if (gameObject.CompareTag("Player") && collision.gameObject.CompareTag("Enemy") ||
                gameObject.CompareTag("Enemy") && collision.gameObject.CompareTag("Player") ||
                gameObject.CompareTag("Enemy") && collision.gameObject.CompareTag("Enemy"))
            {
                rb.velocity = Vector2.zero;
                otherRb.velocity = Vector2.zero;
            }
        }
    }
}
