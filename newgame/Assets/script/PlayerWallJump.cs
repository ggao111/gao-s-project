using UnityEngine;
using System.Collections;

public class PlayerWallJump : MonoBehaviour
{
    [Header("蹬墙跳设置")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpPushForce = 8f;
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpControlDelay = 0.2f;
    
    [Header("墙上行为设置")]
    [SerializeField] private float wallStickTime = 0.5f;
    [SerializeField] private float wallStickDelay = 0.1f;
    [SerializeField] private float wallLeaveForce = 5f;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip wallJumpSound;     // 蹬墙跳音效
    
    public enum WallPhase { None, Sticking, Sliding }
    
    private Rigidbody2D rb;
    private BoxCollider2D coll;
    private LayerMask jumpableGround;
    private PlayerMovement movement;
    
    // 音效相关
    private AudioSource audioSource;
    
    // 状态变量
    public bool IsTouchingWall { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsWallJumping { get; private set; }
    public bool JustTouchedWall { get; private set; }
    
    private int wallDirection = 1;
    private bool wasTouchingWallLastFrame = false;
    private float wallStickTimer = 0f;
    private float wallContactTime = 0f;
    private WallPhase currentWallPhase = WallPhase.None;
    
    // 新增：用于立即切换动画的变量
    private bool forceWallAnimation = false;
    
    // 新增：自动吸附状态管理
    private bool isAutoSticking = false;
    private int lastWallDirection = 1;
    
    public void Initialize(Rigidbody2D rigidbody, BoxCollider2D collider, LayerMask groundLayer)
    {
        rb = rigidbody;
        coll = collider;
        jumpableGround = groundLayer;
        movement = GetComponent<PlayerMovement>();
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    public void UpdateWallJump()
    {
        CheckWalls();
        HandleWallBehavior();
        
        // 更新墙壁接触状态
        JustTouchedWall = IsTouchingWall && !wasTouchingWallLastFrame;
        wasTouchingWallLastFrame = IsTouchingWall;
        
        // 如果刚接触墙壁，强制更新动画
        if (JustTouchedWall && !movement.IsGrounded())
        {
            forceWallAnimation = true;
            isAutoSticking = true;
            Debug.Log("刚接触墙壁，开始自动吸附");
        }
        
        // 更新接触墙壁的时间
        if (IsTouchingWall && !movement.IsGrounded())
        {
            wallContactTime += Time.deltaTime;
        }
        else
        {
            wallContactTime = 0f;
            forceWallAnimation = false;
            isAutoSticking = false;
        }
        
        // 检查是否应该离开墙壁
        CheckWallLeave();
    }
    
    private void CheckWalls()
    {
        Vector2 rayOrigin = coll.bounds.center;
        float rayLength = coll.bounds.extents.x + wallCheckDistance;
        
        // 使用多个检测点提高准确性
        Vector2[] detectionPoints = {
            new Vector2(rayOrigin.x, coll.bounds.max.y - 0.1f), // 上
            rayOrigin, // 中
            new Vector2(rayOrigin.x, coll.bounds.min.y + 0.1f)  // 下
        };
        
        bool hitLeft = false;
        bool hitRight = false;
        
        foreach (Vector2 point in detectionPoints)
        {
            RaycastHit2D hitLeftRay = Physics2D.Raycast(point, Vector2.left, rayLength, jumpableGround);
            RaycastHit2D hitRightRay = Physics2D.Raycast(point, Vector2.right, rayLength, jumpableGround);
            
            if (hitLeftRay.collider != null && IsValidWallHit(hitLeftRay) && !IsSlipperyWall(hitLeftRay))
                hitLeft = true;
            if (hitRightRay.collider != null && IsValidWallHit(hitRightRay) && !IsSlipperyWall(hitRightRay))
                hitRight = true;
            
            // 调试可视化
            Debug.DrawRay(point, Vector2.left * rayLength, hitLeftRay.collider != null ? Color.red : Color.white);
            Debug.DrawRay(point, Vector2.right * rayLength, hitRightRay.collider != null ? Color.red : Color.white);
        }
        
        IsTouchingWall = hitLeft || hitRight;
        
        if (IsTouchingWall)
        {
            wallDirection = hitLeft ? -1 : 1;
            lastWallDirection = wallDirection;
        }
    }
    
    private bool IsValidWallHit(RaycastHit2D hit)
    {
        float angle = Vector2.Angle(hit.normal, Vector2.up);
        return angle > 45f && angle < 135f;
    }
    
    private bool IsSlipperyWall(RaycastHit2D hit)
    {
        return hit.collider.CompareTag("slip");
    }
    
    // 修改：优化墙上行为处理，支持自动吸附
    private void HandleWallBehavior()
    {
        float horizontalInput = GetHorizontalInput();
        
        // 检查是否应该进入墙上状态
        bool shouldBeOnWall = IsTouchingWall && 
                             !movement.IsGrounded() && 
                             wallContactTime >= wallStickDelay &&
                             !IsTouchingSlipperyWall() &&
                             isAutoSticking;
        
        if (shouldBeOnWall)
        {
            IsWallSliding = true;
            
            // 处理停留阶段
            if (wallStickTimer > 0)
            {
                currentWallPhase = WallPhase.Sticking;
                wallStickTimer -= Time.deltaTime;
                
                // 停留阶段：停止垂直移动，并轻微向墙壁方向推
                rb.velocity = new Vector2(lastWallDirection * 0.5f, 0f);
            }
            // 处理滑墙阶段
            else
            {
                currentWallPhase = WallPhase.Sliding;
                
                // 限制下落速度，实现滑墙效果
                if (rb.velocity.y < -wallSlideSpeed)
                {
                    rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
                }
                
                // 滑墙阶段也保持轻微向墙壁方向的推力
                rb.velocity = new Vector2(lastWallDirection * 0.3f, rb.velocity.y);
            }
        }
        else
        {
            IsWallSliding = false;
            currentWallPhase = WallPhase.None;
            
            // 重置计时器
            if (IsTouchingWall && !movement.IsGrounded() && 
                wallContactTime >= wallStickDelay &&
                !IsTouchingSlipperyWall() &&
                isAutoSticking)
            {
                // 刚满足条件，开始停留计时
                wallStickTimer = wallStickTime;
            }
            else
            {
                wallStickTimer = 0f;
            }
        }
    }
    
    // 新增：检查是否应该离开墙壁
    private void CheckWallLeave()
    {
        if (!IsWallSliding) return;
        
        float horizontalInput = GetHorizontalInput();
        
        // 如果按下与墙壁相反的方向键，离开墙壁
        if (Mathf.Abs(horizontalInput) > 0.1f && Mathf.Sign(horizontalInput) != lastWallDirection)
        {
            LeaveWall();
        }
    }
    
    // 新增：离开墙壁的方法
    private void LeaveWall()
    {
        IsWallSliding = false;
        IsTouchingWall = false;
        currentWallPhase = WallPhase.None;
        isAutoSticking = false;
        
        // 给一个小的推力离开墙壁
        float horizontalInput = GetHorizontalInput();
        rb.velocity = new Vector2(horizontalInput * wallLeaveForce, rb.velocity.y);
        
        Debug.Log("离开墙壁");
    }
    
    private bool IsTouchingSlipperyWall()
    {
        Vector2 rayOrigin = coll.bounds.center;
        float rayLength = coll.bounds.extents.x + wallCheckDistance;
        
        // 检查左右两侧是否有slippery墙壁
        RaycastHit2D hitLeft = Physics2D.Raycast(rayOrigin, Vector2.left, rayLength, jumpableGround);
        RaycastHit2D hitRight = Physics2D.Raycast(rayOrigin, Vector2.right, rayLength, jumpableGround);
        
        bool leftIsSlippery = hitLeft.collider != null && hitLeft.collider.CompareTag("slip");
        bool rightIsSlippery = hitRight.collider != null && hitRight.collider.CompareTag("slip");
        
        return leftIsSlippery || rightIsSlippery;
    }
    
    public void PerformWallJump()
    {
        if (!IsWallSliding || IsTouchingSlipperyWall()) 
        {
            Debug.Log("无法执行蹬墙跳：不在墙上状态或接触到slippery墙壁");
            return;
        }
        
        // 计算蹬墙跳方向
        int jumpDirection = -lastWallDirection;
        
        // 计算蹬墙跳速度：向上跳并向墙壁反方向推开
        Vector2 jumpVelocity = new Vector2(jumpDirection * wallJumpPushForce, wallJumpForce);
        
        // 直接设置速度，确保反弹效果
        rb.velocity = jumpVelocity;
        
        // 设置蹬墙跳状态
        IsWallJumping = true;
        
        // 重置墙上状态
        IsWallSliding = false;
        IsTouchingWall = false;
        currentWallPhase = WallPhase.None;
        wallStickTimer = 0f;
        wallContactTime = 0f;
        forceWallAnimation = false;
        isAutoSticking = false;
        
        // 播放蹬墙跳音效
        if (wallJumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(wallJumpSound);
        }
        
        // 短暂禁用移动控制，让蹬墙跳更流畅
        StartCoroutine(DisableMovementTemporarily());
        
        Debug.Log("执行蹬墙跳，方向: " + jumpDirection);
    }
    
    private IEnumerator DisableMovementTemporarily()
    {
        // 等待控制延迟时间
        yield return new WaitForSeconds(wallJumpControlDelay);
        
        // 恢复移动控制
        IsWallJumping = false;
    }
    
    public void ResetWallJumpState()
    {
        IsWallJumping = false;
        IsWallSliding = false;
        currentWallPhase = WallPhase.None;
        wallStickTimer = 0f;
        wallContactTime = 0f;
        forceWallAnimation = false;
        isAutoSticking = false;
    }
    
    private float GetHorizontalInput()
    {
        return Input.GetAxisRaw("Horizontal");
    }
    
    // 获取当前墙上阶段
    public WallPhase GetCurrentWallPhase()
    {
        return currentWallPhase;
    }
    
    // 是否在停留阶段
    public bool IsWallSticking()
    {
        return currentWallPhase == WallPhase.Sticking;
    }
    
    // 强制动画更新
    public bool ShouldForceWallAnimation()
    {
        return forceWallAnimation;
    }
    
    // 重置强制动画标志
    public void ResetForceAnimation()
    {
        forceWallAnimation = false;
    }
    
    // 修改：获取墙壁方向（用于动画等）
    public int GetWallDirection()
    {
        return lastWallDirection;
    }
}