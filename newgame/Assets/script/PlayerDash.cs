using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    [Header("冲刺设置")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f; // 新增：冲刺冷却时间
    [SerializeField] private TrailRenderer dashTrail;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip dashSound; // 冲刺音效
    
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private AudioSource audioSource;
    
    // 状态变量
    public bool IsDashing { get; private set; }
    public bool CanDash { get; private set; } = true;
    
    private float dashDirection = 1f;
    private float originalGravity;
    
    // 新增：冷却时间相关变量
    private float cooldownTimer = 0f;
    public float CooldownProgress => cooldownTimer / dashCooldown; // 冷却进度，可用于UI显示
    
    // 用于地面检测
    private PlayerJump jump;
    private PlayerController controller;
    
    public void Initialize(Rigidbody2D rigidbody, SpriteRenderer spriteRenderer)
    {
        rb = rigidbody;
        sprite = spriteRenderer;
        originalGravity = rb.gravityScale;
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 获取其他组件引用
        jump = GetComponent<PlayerJump>();
        controller = GetComponent<PlayerController>();
        
        if (dashTrail == null)
            dashTrail = GetComponent<TrailRenderer>();
        
        // 初始化时确保轨迹是隐藏的
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.Clear(); // 清除可能存在的轨迹
        }
        
        // 初始化冷却计时器
        cooldownTimer = 0f;
    }
    
    public void HandleDashInput(float dirX)
    {
        // 检查是否可以冲刺（不在冷却中且不在墙滑状态）
        if (CanDash && cooldownTimer <= 0f && !IsWallSliding())
        {
            // 更新冲刺方向
            if (Mathf.Abs(dirX) > 0.1f)
            {
                dashDirection = Mathf.Sign(dirX);
            }
            else
            {
                // 如果没有水平输入，使用角色朝向
                dashDirection = sprite.flipX ? -1f : 1f;
            }
            
            StartCoroutine(PerformDash());
        }
    }
    
    // 新增：更新冷却时间的Update方法
    public void UpdateDashCooldown()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
    
    private IEnumerator PerformDash()
    {
        IsDashing = true;
        CanDash = false;
        
        // 保存当前重力并暂时禁用重力
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        
        // 设置冲刺速度
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);
        
        // 播放冲刺音效
        if (dashSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(dashSound);
        }
        
        // 激活冲刺轨迹
        EnableDashTrail();
        
        // 冲刺持续时间
        yield return new WaitForSeconds(dashTime);
        
        // 结束冲刺
        EndDash();
        
        CheckTrapAfterDash();
        // 新增：开始冷却计时
        cooldownTimer = dashCooldown;
        
        // 冲刺结束后检测是否在地面
        CheckGroundAfterDash();
    }
    
    private void CheckTrapAfterDash()
    {
        playerlife playerLife = GetComponent<playerlife>();
        if (playerLife != null)
        {
            playerLife.CheckTrapAfterDash();
        }
    }
    
    // 启用冲刺轨迹
    private void EnableDashTrail()
    {
        if (dashTrail != null)
        {
            dashTrail.emitting = true;
            // 可选：在启用时清除之前的轨迹，确保每次冲刺轨迹都是新的
            // dashTrail.Clear();
        }
    }
    
    // 禁用冲刺轨迹
    private void DisableDashTrail()
    {
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            // 可选：立即清除现有轨迹
            dashTrail.Clear();
        }
    }
    
    // 结束冲刺的公共方法，可以在外部调用（例如被击中时强制结束冲刺）
    public void EndDash()
    {
        if (IsDashing)
        {
            // 恢复重力
            rb.gravityScale = originalGravity;
            
            // 重置水平速度，保持垂直速度
            rb.velocity = new Vector2(0f, rb.velocity.y);
            
            // 禁用冲刺轨迹
            DisableDashTrail();
            
            IsDashing = false;
        }
    }
    
    public void ResetDashAbility()
    {
        CanDash = true;
        cooldownTimer = 0f; // 重置冷却时间
    }
    
    // 冲刺结束后检测地面并重置能力
    private void CheckGroundAfterDash()
    {
        // 等待一帧，让物理系统更新地面状态
        StartCoroutine(CheckGroundAfterDelay());
    }
    
    private IEnumerator CheckGroundAfterDelay()
    {
        // 等待一帧
        yield return null;
        
        // 检查是否在地面或平台上
        bool isOnGround = false;
        
        if (jump != null)
        {
            isOnGround = jump.IsGrounded() || jump.IsOnPlatform();
        }
        
        if (isOnGround)
        {
            Debug.Log("冲刺结束后检测到在地面，重置冲刺能力");
            ResetDashAbility();
        }
        else
        {
            // 如果不在空中，添加一个检测：在空中时如果接触到地面也重置
            StartCoroutine(MonitorGroundContactAfterDash());
        }
    }
    
    private IEnumerator MonitorGroundContactAfterDash()
    {
        // 监控一段时间（比如2秒），如果在此期间接触到地面，则重置冲刺能力
        float monitorTime = 2f;
        float timer = 0f;
        
        while (timer < monitorTime)
        {
            timer += Time.deltaTime;
            
            if (jump != null && (jump.IsGrounded() || jump.IsOnPlatform()))
            {
                Debug.Log("冲刺后在空中接触到地面，重置冲刺能力");
                ResetDashAbility();
                yield break; // 提前结束协程
            }
            
            yield return null;
        }
    }
    
    // 强制开始冷却（例如在受到伤害时）
    public void StartCooldown()
    {
        cooldownTimer = dashCooldown;
    }
    
    // 检查是否在冷却中
    public bool IsOnCooldown()
    {
        return cooldownTimer > 0f;
    }
    
    // 获取冷却剩余时间
    public float GetCooldownRemaining()
    {
        return cooldownTimer;
    }
    
    // 获取冷却进度（0到1的值，可用于UI显示）
    public float GetCooldownProgress()
    {
        return Mathf.Clamp01(1f - (cooldownTimer / dashCooldown));
    }
    
    // 改进墙壁滑行检测
    private bool IsWallSliding()
    {
        PlayerWallJump wallJump = GetComponent<PlayerWallJump>();
        return wallJump != null && wallJump.IsWallSliding;
    }
    
    // 在脚本禁用或对象销毁时确保轨迹被正确清理
    private void OnDisable()
    {
        DisableDashTrail();
    }
    
    private void OnDestroy()
    {
        DisableDashTrail();
    }
}