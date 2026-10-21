using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [Header("跳跃设置")]
    [SerializeField] private float jumpSpeed = 7f;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip jumpSound;       // 普通跳跃音效
    [SerializeField] private AudioClip doubleJumpSound; // 二段跳音效
    
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private LayerMask jumpableGround;
    private LayerMask platformLayerMask;
    private AudioSource audioSource;
    
    // 二段跳相关
    public bool CanDoubleJump { get; private set; }
    public bool IsDoubleJumping { get; private set; }
    
    // 地面状态跟踪
    private bool wasGroundedLastFrame = true;
    
    // 新增：碰撞检测相关属性
    public bool JustTouchedGround { get; private set; } = false;
    private bool isJumpingThisFrame = false;
    private float minTimeBetweenResets = 0.1f;
    private float lastResetTime = 0f;

    // 射线检测相关变量
    [Header("射线检测设置")]
    [SerializeField] private float raycastLength = 0.15f;
    [SerializeField] private int raycastCount = 3;
    [SerializeField] private float raycastSpread = 0.1f;
    [SerializeField] private bool showDebugRays = true;
    
    // 用于防止二段跳状态错误的变量
    private bool isDoubleJumpActive = false;
    
    public void Initialize(Rigidbody2D rigidbody, BoxCollider2D collider, LayerMask groundLayer, LayerMask platformLayer)
    {
        rb = rigidbody;
        coll = collider;
        jumpableGround = groundLayer;
        platformLayerMask = platformLayer;
        
        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        ResetJumpAbility();
        wasGroundedLastFrame = true;
        JustTouchedGround = false;
        isDoubleJumpActive = false;
    }
    
    public void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        isJumpingThisFrame = true;
        JustTouchedGround = false;
        isDoubleJumpActive = false;
        
        // 播放跳跃音效
        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }
    
    public void PerformDoubleJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
        CanDoubleJump = false;
        IsDoubleJumping = true;
        isJumpingThisFrame = true;
        isDoubleJumpActive = true;
        JustTouchedGround = false;
        
        // 播放二段跳音效（如果没有二段跳音效，则使用普通跳跃音效）
        if (audioSource != null)
        {
            AudioClip clip = doubleJumpSound != null ? doubleJumpSound : jumpSound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
    
    public void ResetJumpAbility()
    {
        if (Time.time - lastResetTime < minTimeBetweenResets)
            return;
            
        CanDoubleJump = true;
        IsDoubleJumping = false;
        isDoubleJumpActive = false;
        lastResetTime = Time.time;
    }
    
    public bool IsGrounded()
    {
        bool isBoxCastGrounded = CheckGroundedWithBoxCast();
        bool isRaycastGrounded = CheckGroundedWithRaycast();
        bool isGrounded = isBoxCastGrounded || isRaycastGrounded;
        
        bool wasInAirLastFrame = !wasGroundedLastFrame;
        bool isFalling = rb.velocity.y <= 0.1f;
        
        JustTouchedGround = wasInAirLastFrame && isGrounded && !isJumpingThisFrame && isFalling;
        
        if (isGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                ResetJumpAbility();
                JustTouchedGround = true;
            }
            else if (isDoubleJumpActive || IsDoubleJumping)
            {
                IsDoubleJumping = false;
                isDoubleJumpActive = false;
                CanDoubleJump = true;
            }
            
            if (rb.velocity.y <= 0.5f)
            {
                isJumpingThisFrame = false;
            }
        }
        
        wasGroundedLastFrame = isGrounded;
        return isGrounded;
    }
    
    private bool CheckGroundedWithBoxCast()
    {
        LayerMask combinedLayerMask = jumpableGround | platformLayerMask;
        RaycastHit2D hit = Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, combinedLayerMask);
        
        if (hit.collider != null)
        {
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            
            if (((1 << hit.collider.gameObject.layer) & platformLayerMask) != 0)
            {
                PlayerController playerController = GetComponent<PlayerController>();
                if (playerController != null && playerController.IsDroppingThroughPlatform)
                {
                    return false;
                }
                else
                {
                    return angle < 45f;
                }
            }
            else
            {
                return angle < 45f;
            }
        }
        
        return false;
    }
    
    private bool CheckGroundedWithRaycast()
    {
        Vector2 raycastOrigin = coll.bounds.center;
        raycastOrigin.y = coll.bounds.min.y;
        
        bool hitGround = false;
        float startX = raycastOrigin.x - (raycastCount - 1) * raycastSpread / 2;
        
        for (int i = 0; i < raycastCount; i++)
        {
            float currentX = startX + i * raycastSpread;
            Vector2 currentOrigin = new Vector2(currentX, raycastOrigin.y);
            
            RaycastHit2D hit = Physics2D.Raycast(currentOrigin, Vector2.down, raycastLength, jumpableGround);
            
            if (showDebugRays)
            {
                Debug.DrawRay(currentOrigin, Vector2.down * raycastLength, hit.collider != null ? Color.green : Color.red);
            }
            
            if (hit.collider != null)
            {
                float angle = Vector2.Angle(hit.normal, Vector2.up);
                if (angle < 45f)
                {
                    hitGround = true;
                }
            }
        }
        
        return hitGround;
    }
    
    void Update()
    {
        IsGrounded();
    }
    
    public bool IsOnPlatform()
    {
        RaycastHit2D hit = Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, 0.1f, platformLayerMask);
        
        if (hit.collider != null)
        {
            float angle = Vector2.Angle(hit.normal, Vector2.up);
            
            PlayerController playerController = GetComponent<PlayerController>();
            if (playerController != null && playerController.IsDroppingThroughPlatform)
            {
                return false;
            }
            
            return angle < 45f;
        }
        
        return false;
    }
    
    public void ClearJustTouchedGround()
    {
        JustTouchedGround = false;
    }
}