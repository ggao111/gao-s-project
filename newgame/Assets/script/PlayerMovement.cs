using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private float moveSpeed;
    private LayerMask jumpableGround;
    
    public void Initialize(Rigidbody2D rigidbody, BoxCollider2D collider, float speed, LayerMask groundLayer)
    {
        rb = rigidbody;
        coll = collider;
        moveSpeed = speed;
        jumpableGround = groundLayer;
    }
    
    public void UpdateMovement(float dirX)
    {
        rb.velocity = new Vector2(dirX * moveSpeed, rb.velocity.y);
    }
    
    public bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, jumpableGround);
        
        if (hit.collider != null)
        {
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            return angle < 45f;
        }
        
        return false;
    }
}