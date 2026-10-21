using UnityEngine;

public class Springboard : MonoBehaviour
{
    public enum BounceDirection
    {
        Up,        // 垂直向上
        Right,     // 水平向右
        Left       // 水平向左
    }
    
    [Header("弹簧板设置")]
    [SerializeField] private BounceDirection bounceDirection = BounceDirection.Up;
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private float upwardForceRatio = 0.3f;
    [SerializeField] private float disableControlDuration = 0.3f; // 禁止控制的时间
    
    [Header("动画设置")]
    [SerializeField] private Animator animator;
    [SerializeField] private string bounceAnimation = "springboard";
    
    [Header("音效设置")]
    [SerializeField] private AudioClip bounceSound; // 改为使用AudioClip
    
    private AudioSource audioSource; // 音频源
    
    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("弹簧板没有找到 Animator 组件！");
        }
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家触发了弹簧板");
            
            // 获取玩家刚体
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            
            if (playerRb == null)
            {
                Debug.LogError("玩家没有 Rigidbody2D 组件！");
                return;
            }
            
            // 获取玩家控制器并重置所有能力
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ResetAllAbilities();
                Debug.Log("已重置玩家所有能力");
            }
            
            // 立即应用弹跳力
            ApplyBounceForce(playerRb);
            
            // 播放弹簧板动画
            if (animator != null && !string.IsNullOrEmpty(bounceAnimation))
            {
                Debug.Log($"播放动画: {bounceAnimation}");
                animator.SetTrigger(bounceAnimation);
            }
        }
    }
    
    // 立即应用弹跳力 - 修改为固定弹跳距离
    private void ApplyBounceForce(Rigidbody2D playerRb)
    {
        Debug.Log("应用弹跳力");
        
        // 首先完全停止玩家当前速度，确保弹跳效果一致
        playerRb.velocity = Vector2.zero;
        
        // 计算弹跳方向
        Vector2 bounceVector = CalculateBounceDirection();
        Debug.Log($"计算出的弹跳方向: {bounceVector}");
        
        // 使用 AddForce 而不是直接设置 velocity，这样更符合物理规律
        // 但同时确保力度足够大以达到固定距离
        Vector2 bounceForceVector = bounceVector * bounceForce;
        playerRb.AddForce(bounceForceVector, ForceMode2D.Impulse);
        
        Debug.Log($"应用的弹跳力: {bounceForceVector}, 玩家质量: {playerRb.mass}");
        
        // 通知玩家控制器进入弹跳状态
        PlayerController playerController = playerRb.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetBouncingState(disableControlDuration);
            Debug.Log($"已通知玩家控制器禁止移动，持续 {disableControlDuration} 秒");
        }
        else
        {
            Debug.LogError("未找到玩家控制器");
        }
        
        // 播放音效
        if (bounceSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bounceSound);
            Debug.Log("播放弹跳音效");
        }
    }
    
    // 计算弹跳方向
    private Vector2 CalculateBounceDirection()
    {
        Vector2 direction = Vector2.zero;
        
        switch (bounceDirection)
        {
            case BounceDirection.Up:
                direction = Vector2.up;
                break;
                
            case BounceDirection.Right:
                direction = new Vector2(1f, upwardForceRatio).normalized;
                break;
                
            case BounceDirection.Left:
                direction = new Vector2(-1f, upwardForceRatio).normalized;
                break;
        }
        
        return direction;
    }
    
    void OnDrawGizmosSelected()
    {
        Vector2 direction = CalculateBounceDirection();
        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3)(direction * 2f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint, endPoint);
        
        Vector3 rightArrow = Quaternion.Euler(0, 0, 30) * -direction * 0.5f;
        Vector3 leftArrow = Quaternion.Euler(0, 0, -30) * -direction * 0.5f;
        
        Gizmos.DrawLine(endPoint, endPoint + rightArrow);
        Gizmos.DrawLine(endPoint, endPoint + leftArrow);
    }
}