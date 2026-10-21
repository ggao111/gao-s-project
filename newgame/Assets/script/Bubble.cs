using UnityEngine;
using System.Collections;

public class Bubble : MonoBehaviour
{
    [Header("泡泡弹跳设置")]
    [SerializeField] private float bounceForce = 12f;          // 弹跳力度
    [SerializeField] private float disableControlDuration = 0.2f; // 禁止控制的时间
    [SerializeField] private bool destroyOnContact = true;    // 接触后是否销毁泡泡
    [SerializeField] private float destroyDelay = 0.1f;       // 销毁延迟
    
    [Header("泡泡移动设置")]
    [SerializeField] private float floatSpeed = 2f;           // 泡泡上浮速度
    [SerializeField] private float floatAcceleration = 0.5f;  // 上浮加速度
    [SerializeField] private float maxFloatSpeed = 5f;        // 最大上浮速度
    [SerializeField] private bool applyRandomDrift = true;    // 是否应用随机漂移
    [SerializeField] private float driftAmount = 0.5f;        // 漂移量
    
    [Header("地形碰撞设置")]
    [SerializeField] private LayerMask terrainLayerMask;      // 地形层
    [SerializeField] private bool destroyOnTerrain = true;    // 碰到地形是否销毁
    [SerializeField] private float terrainDestroyDelay = 0.2f; // 地形销毁延迟
    
    [Header("自动销毁设置")]
    [SerializeField] private float lifetime = 10f;            // 泡泡生存时间
    [SerializeField] private bool autoDestroy = true;         // 是否自动销毁
    
    [Header("视觉效果")]
    [SerializeField] private ParticleSystem popEffect;        // 泡泡破裂效果
    [SerializeField] private Animator animator;              // 泡泡动画
    [SerializeField] private string popAnimation = "Pop";    // 破裂动画名称
    [SerializeField] private float shrinkTime = 0.1f;        // 收缩动画时间
    
    [Header("音效设置")]
    [SerializeField] private AudioClip popSound;             // 破裂音效（支持MP3）
    [SerializeField] private float popSoundVolume = 1.0f;    // 破裂音效音量
    
    private Rigidbody2D rb;
    private float currentFloatSpeed;
    private float spawnTime;
    private bool isDestroying = false;
    private BubbleSpawner spawner; // 生成器引用
    private bool hasPlayedSound = false; // 防止重复播放

    void Start()
    {
        // 获取组件引用
        if (animator == null)
            animator = GetComponent<Animator>();
            
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 无重力
            rb.drag = 0.5f;       // 一点阻力
        }
        
        // 初始化上浮速度
        currentFloatSpeed = floatSpeed;
        
        // 记录生成时间
        spawnTime = Time.time;
        
        // 查找生成器
        if (transform.parent != null)
        {
            spawner = transform.parent.GetComponent<BubbleSpawner>();
        }
        
        Debug.Log("泡泡初始化完成");
    }
    
    void Update()
    {
        // 自动上浮
        ApplyFloatMovement();
        
        // 自动销毁检查
        if (autoDestroy && Time.time - spawnTime > lifetime && !isDestroying)
        {
            StartCoroutine(DestroyBubble());
        }
    }
    
    // 应用上浮移动
    private void ApplyFloatMovement()
    {
        // 增加上浮速度（加速度）
        currentFloatSpeed += floatAcceleration * Time.deltaTime;
        currentFloatSpeed = Mathf.Min(currentFloatSpeed, maxFloatSpeed);
        
        // 计算上浮力
        Vector2 floatForce = Vector2.up * currentFloatSpeed;
        
        // 应用随机漂移
        if (applyRandomDrift && rb != null)
        {
            float driftX = Mathf.PerlinNoise(Time.time * 0.5f, transform.position.y * 0.5f) - 0.5f;
            floatForce.x += driftX * driftAmount;
        }
        
        // 应用力
        if (rb != null)
        {
            rb.AddForce(floatForce);
            
            // 限制最大速度
            Vector2 velocity = rb.velocity;
            if (velocity.magnitude > maxFloatSpeed)
            {
                rb.velocity = velocity.normalized * maxFloatSpeed;
            }
        }
        else
        {
            // 如果没有刚体，直接移动位置
            transform.Translate(floatForce * Time.deltaTime);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            
            // 获取玩家刚体
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            
            if (playerRb == null)
            {
                return;
            }
            
            // 获取玩家控制器并重置所有能力
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.ResetAllAbilities();
                Debug.Log("已重置玩家所有能力");
            }
            
            // 应用弹跳力
            ApplyBounceForce(playerRb);
            
            // 播放泡泡破裂效果（包含音效）
            PlayPopEffectsForPlayer();
            
            // 如果设置为接触后销毁，则销毁泡泡
            if (destroyOnContact)
            {
                StartCoroutine(DestroyBubble());
            }
        }
        // 检测地形碰撞
        else if (destroyOnTerrain && terrainLayerMask == (terrainLayerMask | (1 << other.gameObject.layer)))
        {
            Debug.Log($"泡泡触碰到地形: {other.gameObject.name}");
            
            // 播放泡泡破裂效果（不含音效）
            PlayPopEffectsNoSound();
            
            // 销毁泡泡
            StartCoroutine(DestroyBubble(terrainDestroyDelay));
        }
    }
    
    // 应用弹跳力
    private void ApplyBounceForce(Rigidbody2D playerRb)
    {
        Debug.Log("泡泡弹跳力应用");
        
        // 首先停止玩家当前垂直速度，确保弹跳效果一致
        Vector2 currentVelocity = playerRb.velocity;
        playerRb.velocity = new Vector2(currentVelocity.x, 0f);
        
        // 计算向上的弹跳力
        Vector2 bounceForceVector = Vector2.up * bounceForce;
        
        // 使用冲击力模式，确保力度足够
        playerRb.AddForce(bounceForceVector, ForceMode2D.Impulse);
        
        // 通知玩家控制器进入弹跳状态
        PlayerController playerController = playerRb.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetBouncingState(disableControlDuration);
        }
    }
    
    // 播放泡泡破裂效果（玩家接触时调用，包含音效）
    private void PlayPopEffectsForPlayer()
    {
        // 防止重复播放
        if (hasPlayedSound)
        {
            return;
        }
        
        // 播放破裂动画
        if (animator != null && !string.IsNullOrEmpty(popAnimation))
        {
            animator.SetTrigger(popAnimation);
        }
        
        // 播放粒子效果
        if (popEffect != null)
        {
            popEffect.Play();
        }
        
        // 播放音效（玩家接触时播放）
        PlayPopSound();
        
        // 添加收缩效果
        StartCoroutine(ShrinkBubble());
    }
    
    // 播放泡泡破裂效果（地形碰撞时调用，不含音效）
    private void PlayPopEffectsNoSound()
    {
        // 播放破裂动画
        if (animator != null && !string.IsNullOrEmpty(popAnimation))
        {
            animator.SetTrigger(popAnimation);
        }
        
        // 播放粒子效果
        if (popEffect != null)
        {
            popEffect.Play();
        }
        
        // 添加收缩效果
        StartCoroutine(ShrinkBubble());
    }
    
    // 播放破裂音效 - 使用AudioSource.PlayClipAtPoint（不受对象销毁影响）
    private void PlayPopSound()
    {
        try
        {
            // 使用AudioSource.PlayClipAtPoint播放音效
            // 这个方法会在指定位置创建一个临时的AudioSource来播放音效
            // 即使当前游戏对象被销毁，音效也会继续播放完成
            AudioSource.PlayClipAtPoint(popSound, transform.position, popSoundVolume);
            hasPlayedSound = true;
            

        }
        catch (System.Exception e)
        {

        }
    }
    
    // 泡泡收缩效果协程
    private IEnumerator ShrinkBubble()
    {
        float elapsedTime = 0f;
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = Vector3.zero;
        
        while (elapsedTime < shrinkTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / shrinkTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    // 销毁泡泡协程
    private IEnumerator DestroyBubble(float delay = -1f)
    {
        if (isDestroying) yield break;
        
        isDestroying = true;
        
        float actualDelay = (delay < 0) ? destroyDelay : delay;
        
        // 确保音效有足够时间播放
        if (hasPlayedSound && popSound != null)
        {
            // 等待音效播放完成（至少等待音效长度的80%）
            float soundDuration = popSound.length * 0.8f;
            if (actualDelay < soundDuration)
            {
                actualDelay = soundDuration;
                Debug.Log($"延长销毁延迟以等待音效播放: {actualDelay}秒");
            }
        }
        
        yield return new WaitForSeconds(actualDelay);
        
        // 销毁游戏对象
        Destroy(gameObject);
    }
    
    // 设置泡泡生存时间
    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
        spawnTime = Time.time;
    }
    
    // 设置上浮速度
    public void SetFloatSpeed(float speed, float maxSpeed = -1f)
    {
        currentFloatSpeed = speed;
        if (maxSpeed >= 0) maxFloatSpeed = maxSpeed;
    }
       
    void OnDrawGizmosSelected()
    {
        // 绘制弹跳方向指示器
        Gizmos.color = Color.cyan;
        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + Vector3.up * 2f;
        
        Gizmos.DrawLine(startPoint, endPoint);
        
        // 绘制箭头
        Vector3 rightArrow = Quaternion.Euler(0, 0, 30) * -Vector3.up * 0.5f;
        Vector3 leftArrow = Quaternion.Euler(0, 0, -30) * -Vector3.up * 0.5f;
        
        Gizmos.DrawLine(endPoint, endPoint + rightArrow);
        Gizmos.DrawLine(endPoint, endPoint + leftArrow);
        
        // 绘制泡泡范围
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(transform.position, circleCollider.radius);
        }
        
        // 绘制上浮方向
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint, startPoint + Vector3.up);
    }
}