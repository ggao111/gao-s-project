using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEditor.Search.Providers;
using UnityEngine;

public class playercontroler1 : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer Sprite;
    private Animator anim;
    private BoxCollider2D coll;
    private float dirx = 0f;
    [SerializeField] private float movespeed = 7f;
    [SerializeField] private float jumpspeed = 7f;
    [SerializeField] private LayerMask jumpableground;


    private enum movestate { idle, running, jumping, falling, doublejump}




    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        coll = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        dirx = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(dirx * movespeed, rb.velocity.y);
        if (Input.GetButtonDown("Jump") && isground())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpspeed);
        }
        updateanimation();

    }
    private void updateanimation()
    {
        movestate state;
        if (dirx > 0f)
        {
            state = movestate.running;
            Sprite.flipX = false;
        }
        else if (dirx < 0f)
        {
            state = movestate.running;
            Sprite.flipX = true;
        }
        else
        {
            state = movestate.idle;
        }
        if (rb.velocity.y > .1f)
        {
            state = movestate.jumping;
        }
        else if (rb.velocity.y < -.1f)
        {
            state = movestate.falling;
        }
        anim.SetInteger("state", (int)state);
    }

    
    private bool isground()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableground);
    }
    
}