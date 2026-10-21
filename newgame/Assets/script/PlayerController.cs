using UnityEngine;
using System.Text; // ================ 新增：用于StringBuilder ================

public class PlayerController : MonoBehaviour
{
    [Header("组件引用")]
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Animator anim;
    public BoxCollider2D coll;
    
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private LayerMask jumpableGround;
    
    [Header("平台穿透设置")]
    [SerializeField] private LayerMask platformLayerMask; // 在Inspector中设置为平台的图层
    [SerializeField] private float dropDownTime = 0.3f;   // 穿透平台的持续时间
    [SerializeField] private float dropDownForce = 3f;    // 穿透时向下的力
    
    [Header("暂停菜单")]
    [SerializeField] private GameObject pauseMenu;
    private bool isPaused = false;
    
    // 弹跳状态控制
    private bool isBouncing = false;
    private float bounceEndTime = 0f;
    
    // 平台穿透状态控制
    private bool isDroppingThroughPlatform = false;
    
    // 组件引用
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerWallJump wallJump;
    private PlayerDash dash;
    
    // 输入
    private float dirX = 0f;
    private bool jumpInput = false;
    private bool dashInput = false;
    private bool dropInput = false;

    private enum MoveState { idle, running, jumping, falling, doublejump, wallslide, dashing }

    void Start()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        coll = GetComponent<BoxCollider2D>();
        
        // 初始化各个功能模块
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        wallJump = GetComponent<PlayerWallJump>();
        dash = GetComponent<PlayerDash>();
        
        // 传递引用
        if (movement) movement.Initialize(rb, coll, moveSpeed, jumpableGround);
        if (jump) jump.Initialize(rb, coll, jumpableGround, platformLayerMask); // 传递platformLayerMask
        if (wallJump) wallJump.Initialize(rb, coll, jumpableGround);
        if (dash) dash.Initialize(rb, sprite);
        
        // 初始化暂停菜单
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
    }

    void Update()
    {
        // ESC键检测
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
        
        // 如果游戏暂停，不处理其他输入
        if (isPaused)
        {
            return;
        }
        
        // 获取输入
        GetInput();
        
        // 如果在弹跳状态，只更新动画，不处理其他输入
        if (isBouncing && Time.time < bounceEndTime)
        {
            UpdateAnimation();
            return;
        }
        else if (isBouncing)
        {
            isBouncing = false; // 弹跳状态结束
        }
        
        // 如果在冲刺中，不处理其他输入
        if (dash != null && dash.IsDashing)
        {
            UpdateAnimation();
            return;
        }
        
        // ================ 修改开始：调整Update执行顺序 ================
        // 先更新能力和状态检测
        UpdateAbilities();
        // ================ 修改结束 ================
        
        // 处理平台穿透输入
        if (dropInput && !isDroppingThroughPlatform)
        {
            // 检查是否在平台上
            if (jump != null && jump.IsOnPlatform())
            {
                StartCoroutine(DropThroughPlatform());
            }
        }
        
        // 更新各个模块
        if (wallJump != null) wallJump.UpdateWallJump();
        
        // 处理移动 - 检查蹬墙跳状态
        if (wallJump != null && !wallJump.IsWallJumping)
        {
            if (movement != null) movement.UpdateMovement(dirX);
        }
        
        // 处理跳跃输入
        if (jumpInput)
        {
            HandleJumpInput();
        }
        
        // 处理冲刺输入
        if (dashInput && dash != null)
        {
            dash.HandleDashInput(dirX);
        }
        
        // 最后更新动画
        UpdateAnimation();
        
        // ================ 新增：调试功能 ================
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugGroundCollision();
        }
        // ================ 新增结束 ================
    }
    
    // 暂停/继续游戏
    private void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            // 暂停游戏
            Time.timeScale = 0f;
            
            // 显示暂停菜单
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(true);
            }
            
            Debug.Log("游戏已暂停");
        }
        else
        {
            // 继续游戏
            Time.timeScale = 1f;
            
            // 隐藏暂停菜单
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
            }
            
            Debug.Log("游戏已继续");
        }
    }
    
    // 公共方法：从暂停菜单按钮调用
    public void ResumeGame()
    {
        Debug.Log("游戏已继续");
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        
        
    }
    
    private void GetInput()
    {
        dirX = Input.GetAxisRaw("Horizontal");
        jumpInput = Input.GetButtonDown("Jump");
        dashInput = Input.GetKeyDown(KeyCode.LeftShift);
        dropInput = Input.GetKeyDown(KeyCode.S) || 
                   Input.GetKeyDown(KeyCode.DownArrow) || 
                   Input.GetAxisRaw("Vertical") < -0.5f;
    }
    
    private void HandleJumpInput()
    {
        // 蹬墙跳优先级最高
        if (wallJump != null && wallJump.IsWallSliding)
        {
            wallJump.PerformWallJump();
        }
        // 然后是地面跳跃
        else if (jump != null && (jump.IsGrounded()||jump.IsOnPlatform()))
        {
            jump.PerformJump();
        }
        // 最后是二段跳
        else if (jump != null && jump.CanDoubleJump)
        {
            jump.PerformDoubleJump();
        }
    }
    
    /// <summary>
    /// 平台穿透协程：临时忽略与平台的碰撞，允许玩家从平台上方下落
    /// </summary>
    private System.Collections.IEnumerator DropThroughPlatform()
    {
        isDroppingThroughPlatform = true;
        
        // 给一个向下的力，让下落更自然
        if (rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, -dropDownForce);
        }
        
        // 临时忽略所有平台碰撞
        Collider2D[] allColliders = FindObjectsOfType<Collider2D>();
        foreach (Collider2D otherCollider in allColliders)
        {
            if (((1 << otherCollider.gameObject.layer) & platformLayerMask) != 0)
            {
                Physics2D.IgnoreCollision(coll, otherCollider, true);
            }
        }
        
        Debug.Log("开始穿过平台");
        
        // 等待一段时间
        yield return new WaitForSeconds(dropDownTime);
        
        // 恢复与平台的碰撞
        foreach (Collider2D otherCollider in allColliders)
        {
            if (((1 << otherCollider.gameObject.layer) & platformLayerMask) != 0)
            {
                Physics2D.IgnoreCollision(coll, otherCollider, false);
            }
        }
        
        isDroppingThroughPlatform = false;
        Debug.Log("恢复平台碰撞");
    }
    
    // ================ 主要修改开始：重构UpdateAbilities方法 ================
    private void UpdateAbilities()
    {
        // 统一管理所有能力重置
        if (jump != null)
        {
            // 检测是否刚刚与地面/平台碰撞
            if (jump.JustTouchedGround)
            {
                Debug.Log("检测到地面碰撞，重置所有能力");
                
                // 重置所有能力
                if (jump != null) jump.ResetJumpAbility();
                if (wallJump != null) wallJump.ResetWallJumpState();
                if (dash != null) dash.ResetDashAbility();
                
                // 清除标志，防止重复触发
                jump.ClearJustTouchedGround();
            }
        }
        
        // 墙壁接触检测重置能力
        if (wallJump != null && wallJump.JustTouchedWall)
        {
            Debug.Log("检测到墙壁接触，重置跳跃和冲刺能力");
            
            if (jump != null) jump.ResetJumpAbility();
            if (dash != null) dash.ResetDashAbility();
        }
    }
    // ================ 主要修改结束 ================
    
    private MoveState currentMoveState = MoveState.idle;
    private void UpdateAnimation()
    {
        MoveState state = MoveState.idle;
        
        // 冲刺状态优先级最高
        if (dash != null && dash.IsDashing)
        {
            state = MoveState.dashing;
        }
        // 墙壁滑行优先级次高
        else if (wallJump != null && wallJump.IsWallSliding)
        {
            state = MoveState.wallslide;
        }
        // 二段跳
        else if (jump != null && jump.IsDoubleJumping)
        {
            state = rb.velocity.y > 0.1f ? MoveState.doublejump : MoveState.falling;
        }
        // 空中状态
        else if (!jump.IsGrounded())
        {
            state = rb.velocity.y > 0.1f ? MoveState.jumping : MoveState.falling;
        }
        // 地面状态
        else
        {
            state = Mathf.Abs(dirX) > 0.01f ? MoveState.running : MoveState.idle;
        }
        
        // 最简翻转：只要不是冲刺状态，都根据输入翻转
        if (!(dash != null && dash.IsDashing))
        {
            // 注意：这里dirX等于0时不改变翻转
            if (dirX > 0f) sprite.flipX = false;
            else if (dirX < 0f) sprite.flipX = true;
        }
        
        anim.SetInteger("state", (int)state);
    }
    
    // 设置弹跳状态的方法
    public void SetBouncingState(float duration = 0.3f)
    {
        isBouncing = true;
        bounceEndTime = Time.time + duration;
        Debug.Log($"进入弹跳状态，持续 {duration} 秒");
    }
    
    // 重置所有能力（包括冲刺和二段跳）
    public void ResetAllAbilities()
    {
        if (jump != null) jump.ResetJumpAbility();
        if (dash != null) dash.ResetDashAbility();
        if (wallJump != null) wallJump.ResetWallJumpState();
        
        Debug.Log("所有能力已重置");
    }
    
    // ================ 新增：调试方法 ================
    private void DebugGroundCollision()
    {
        if (jump == null) return;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== 地面碰撞调试 ===");
        sb.AppendLine($"IsGrounded: {jump.IsGrounded()}");
        sb.AppendLine($"IsOnPlatform: {jump.IsOnPlatform()}");
        sb.AppendLine($"JustTouchedGround: {jump.JustTouchedGround}");
        sb.AppendLine($"速度Y: {rb.velocity.y:F2}");
        sb.AppendLine($"二段跳状态: CanDoubleJump={jump.CanDoubleJump}, IsDoubleJumping={jump.IsDoubleJumping}");
        
        if (dash != null)
        {
            sb.AppendLine($"冲刺状态: CanDash={dash.CanDash}, 冷却剩余={dash.GetCooldownRemaining():F2}");
        }
        
        Debug.Log(sb.ToString());
    }
    // ================ 新增结束 ================
    
    // 公共属性，供其他脚本访问
    public bool IsGrounded => jump != null && jump.IsGrounded();
    public bool IsWallSliding => wallJump != null && wallJump.IsWallSliding;
    public bool IsDashing => dash != null && dash.IsDashing;
    public bool IsWallJumping => wallJump != null && wallJump.IsWallJumping;
    public bool IsDroppingThroughPlatform => isDroppingThroughPlatform;
    
    // 暂停状态属性
    public bool IsPaused => isPaused;
}