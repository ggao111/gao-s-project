using UnityEngine;
using System.Collections;

public class SnailMove : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("移动设置")]
    [SerializeField] private float moveDistance = 2f; // 移动的固定距离
    [SerializeField] private float moveDuration = 1f; // 移动的持续时间
    [SerializeField] private float cooldownTime = 1f; // 冷却时间
    
    [Header("动画控制")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveBoolParameter = "IsMove";
    
    [Header("调试")]
    [SerializeField] private bool showGizmos = true;
    
    // 状态变量
    private bool isPlayerDetected = false;
    private bool isMoving = false;
    private bool isCooldown = false;
    private Transform player;
    private Vector2 moveDirection;
    private Vector2 startPosition;
    private float moveTimer = 0f;
    
    // 组件引用
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // 查找玩家
        FindPlayer();
    }
    
    void Update()
    {
        // 如果玩家为空，尝试重新查找
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        // 如果处于冷却状态或正在移动，不检测玩家
        if (isCooldown || isMoving)
            return;
            
        // 检测玩家
        CheckForPlayer();
        
        // 如果检测到玩家且不处于移动状态，开始移动动画
        if (isPlayerDetected && !isMoving)
        {
            StartMoveAnimation();
        }
    }
    
    // 查找玩家
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    // 检测玩家 - 使用 Linecast 方式
    private void CheckForPlayer()
    {
        if (player == null)
            return;
            
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // 检查是否在检测范围内
        if (distanceToPlayer <= detectionRange)
        {
            // 使用 Linecast 检查蜗牛和玩家之间是否有障碍物
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            
            // 没有障碍物阻挡
            if (hit.collider == null)
            {
                isPlayerDetected = true;
                
                Vector2 directionToPlayer = (player.position - transform.position).normalized;
                
                // 只在X轴上移动，所以只考虑水平方向
                moveDirection = new Vector2(directionToPlayer.x, 0f).normalized;
                
                // 根据玩家位置更新朝向
                FlipTowardsPlayer(directionToPlayer);
            }
            else
            {
                isPlayerDetected = false;
            }
        }
        else
        {
            isPlayerDetected = false;
        }
    }
    
    // 更新朝向
    private void FlipTowardsPlayer(Vector2 direction)
    {
        // 使用SpriteRenderer翻转，而不是改变localScale
        if (spriteRenderer != null)
        {
            if (direction.x > 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (direction.x < 0)
            {
                spriteRenderer.flipX = false;
            }
        }
        else
        {
            // 如果没有SpriteRenderer，回退到使用localScale
            if (direction.x > 0 && transform.localScale.x > 0)
            {
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            }
            else if (direction.x < 0 && transform.localScale.x < 0)
            {
                transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            }
        }
    }
    
    // 开始移动动画
    private void StartMoveAnimation()
    {
        isPlayerDetected = false;
        
        // 设置动画参数
        if (animator != null)
        {
            animator.SetBool(moveBoolParameter, true);
        }
        
        // 注意：实际移动将由Animation Event触发
    }
    
    // 动画事件调用的方法 - 开始实际移动
    public void OnMoveAnimationEvent()
    {
        if (!isMoving)
        {
            StartMoving();
        }
    }
    
    // 开始移动
    private void StartMoving()
    {
        isMoving = true;
        startPosition = transform.position;
        moveTimer = 0f;
        
        // 开始移动协程
        StartCoroutine(MoveFixedDistance());
    }
    
    // 移动固定距离
    private IEnumerator MoveFixedDistance()
    {
        Vector2 targetPosition = startPosition + moveDirection * moveDistance;
        
        while (moveTimer < moveDuration && isMoving)
        {
            moveTimer += Time.deltaTime;
            float progress = moveTimer / moveDuration;
            
            // 使用插值平滑移动
            if (rb != null)
            {
                // 使用物理移动
                Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, progress);
                rb.MovePosition(newPosition);
            }
            else
            {
                // 使用Transform移动
                transform.position = Vector2.Lerp(startPosition, targetPosition, progress);
            }
            
            yield return null;
        }
        
        // 确保最终位置准确
        if (rb != null)
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }
        
        // 移动结束
        EndMoving();
    }
    
    // 结束移动
    private void EndMoving()
    {
        isMoving = false;
        
        // 设置动画参数
        if (animator != null)
        {
            animator.SetBool(moveBoolParameter, false);
        }
        
        // 开始冷却
        StartCooldown();
    }
    
    // 开始冷却
    private void StartCooldown()
    {
        isCooldown = true;
        StartCoroutine(CooldownTimer());
    }
    
    // 冷却计时器
    private IEnumerator CooldownTimer()
    {
        yield return new WaitForSeconds(cooldownTime);
        isCooldown = false;
    }
    
    // 碰撞检测
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果撞到墙壁或其他障碍物，停止移动
        if (isMoving && (collision.gameObject.CompareTag("slip") || 
            (obstacleLayer.value & (1 << collision.gameObject.layer)) != 0))
        {
            StopAllCoroutines();
            EndMoving();
        }
    }
    
    // 绘制检测范围
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;
            
        // 绘制检测范围
        Gizmos.color = isPlayerDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (player != null)
        {
            if (isPlayerDetected)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, player.position);
            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, player.position);
            }
        }
        
        // 绘制移动距离预览
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPosition, startPosition + moveDirection * moveDistance);
        }
    }
    
    // 公共方法，用于强制停止移动
    public void StopMoving()
    {
        if (isMoving)
        {
            StopAllCoroutines();
            EndMoving();
        }
    }
    
    // 公共属性
    public bool IsMoving => isMoving;
    public bool IsPlayerDetected => isPlayerDetected;
    public bool IsCooldown => isCooldown;
}