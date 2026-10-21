using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FallPlatform : MonoBehaviour
{
    [Header("平台设置")]
    [SerializeField] private float shakeDuration = 2f;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private float respawnTime = 5f; // 新增：复原时间
    
    [Header("组件引用")]
    [SerializeField] private Collider2D platformCollider;
    [SerializeField] private Collider2D triggerCollider;
    
    // 状态变量
    private bool isActivated = false;
    private bool isShaking = false;
    private bool isFalling = false;
    private bool isRespawning = false;
    private Vector3 originalPosition;
    private Vector3 visualOriginalPosition;
    
    // 组件缓存
    private SpriteRenderer spriteRenderer;
    private Transform visualTransform;
    
    // 记录站在平台上的玩家
    private List<Transform> playersOnPlatform = new List<Transform>();
    
    void Start()
    {
        // 自动查找视觉组件
        FindVisualComponents();
        
        // 保存原始位置
        originalPosition = transform.position;
        
        // 如果没有指定碰撞器，自动获取
        if (platformCollider == null)
            platformCollider = GetComponent<Collider2D>();
            
        // 确保没有 Rigidbody2D，或者设置为静态
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // 如果没有指定触发器，尝试自动创建
        if (triggerCollider == null)
        {
            // 尝试查找现有的触发器
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (Collider2D col in colliders)
            {
                if (col.isTrigger)
                {
                    triggerCollider = col;
                    break;
                }
            }
            
            // 如果没有找到触发器，创建一个
            if (triggerCollider == null)
            {
                // 复制平台碰撞体作为触发器
                if (platformCollider is BoxCollider2D)
                {
                    BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.size = ((BoxCollider2D)platformCollider).size;
                    boxCollider.offset = ((BoxCollider2D)platformCollider).offset;
                    boxCollider.isTrigger = true;
                    triggerCollider = boxCollider;
                }
                else if (platformCollider is CircleCollider2D)
                {
                    CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
                    circleCollider.radius = ((CircleCollider2D)platformCollider).radius;
                    circleCollider.offset = ((CircleCollider2D)platformCollider).offset;
                    circleCollider.isTrigger = true;
                    triggerCollider = circleCollider;
                }
            }
        }
    }
    
    // 自动查找视觉组件
    private void FindVisualComponents()
    {
        // 查找 SpriteRenderer
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // 如果有 SpriteRenderer，使用它的 Transform 作为视觉部分
        if (spriteRenderer != null)
        {
            visualTransform = spriteRenderer.transform;
            visualOriginalPosition = visualTransform.localPosition;
        }
        else
        {
            // 如果没有找到 SpriteRenderer，使用自身作为视觉部分
            visualTransform = transform;
            visualOriginalPosition = Vector3.zero;
            Debug.LogWarning("FallPlatform: 没有找到 SpriteRenderer，将使用自身作为视觉部分");
        }
    }
    
    void Update()
    {
        // 处理抖动效果
        if (isShaking)
        {
            HandleShake();
        }
        
        // 处理下落
        if (isFalling)
        {
            HandleFalling();
        }
    }
    
    // 玩家进入触发器
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 将玩家设置为平台的子对象
            collision.transform.SetParent(transform);
            playersOnPlatform.Add(collision.transform);
            
            Debug.Log("玩家站在平台上");
        }
    }
    
    // 玩家离开触发器
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 将玩家从平台子对象中移除
            collision.transform.SetParent(null);
            playersOnPlatform.Remove(collision.transform);
            
            Debug.Log("玩家离开平台");
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查是否被Player踩到
        if (!isActivated && !isRespawning && collision.gameObject.CompareTag("Player"))
        {
            // 检查碰撞点是否在平台上方
            if (IsCollisionFromAbove(collision))
            {
                ActivatePlatform();
            }
        }
    }
    
    private bool IsCollisionFromAbove(Collision2D collision)
    {
        // 检查碰撞接触点，判断是否从上方碰撞
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f) // 法线向下表示从上方碰撞
            {
                return true;
            }
        }
        return false;
    }
    
    private void ActivatePlatform()
    {
        if (isActivated || isRespawning) return;
        
        isActivated = true;
        StartCoroutine(PlatformSequence());
        
        Debug.Log("平台被激活，开始抖动");
    }
    
    private IEnumerator PlatformSequence()
    {
        // 阶段1：抖动
        isShaking = true;
        
        // 可选：改变颜色提示
        if (spriteRenderer != null)
            spriteRenderer.color = Color.yellow;
        
        yield return new WaitForSeconds(shakeDuration);
        
        // 阶段2：停止抖动，开始下落
        isShaking = false;
        isFalling = true;
        
        // 重置视觉部分位置
        if (visualTransform != null)
            visualTransform.localPosition = visualOriginalPosition;
        
        // 可选：改变颜色提示
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;
        
        Debug.Log("平台开始下落");
        
        // 阶段3：一段时间后隐藏平台
        yield return new WaitForSeconds(destroyDelay);
        
        // 隐藏平台而不是销毁
        HidePlatform();
        
        // 阶段4：等待复原时间
        yield return new WaitForSeconds(respawnTime);
        
        // 阶段5：复原平台
        RespawnPlatform();
    }
    
    private void HandleShake()
    {
        // 只抖动视觉部分，不影响碰撞体
        if (visualTransform == null) return;
        
        float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
        float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
        
        Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0);
        visualTransform.localPosition = visualOriginalPosition + shakeOffset;
    }
    
    private void HandleFalling()
    {
        // 使用 Transform 直接控制下落
        Vector3 newPosition = transform.position;
        newPosition.y -= fallSpeed * Time.deltaTime;
        transform.position = newPosition;
    }
    
    // 隐藏平台而不是销毁
    private void HidePlatform()
    {
        // 在隐藏前，确保所有玩家都不再是平台的子对象
        foreach (Transform player in playersOnPlatform)
        {
            if (player != null)
            {
                player.SetParent(null);
            }
        }
        playersOnPlatform.Clear();
        
        // 可以在这里添加隐藏效果（粒子、声音等）
        Debug.Log("平台隐藏");
        
        // 禁用碰撞器
        if (platformCollider != null)
            platformCollider.enabled = false;
            
        // 禁用触发器
        if (triggerCollider != null)
            triggerCollider.enabled = false;
        
        // 隐藏平台
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        
        // 设置状态为正在复原
        isRespawning = true;
        isFalling = false;
    }
    
    // 新增：复原平台
    private void RespawnPlatform()
    {
        // 重置位置
        transform.position = originalPosition;
        
        // 重置视觉部分位置
        if (visualTransform != null)
            visualTransform.localPosition = visualOriginalPosition;
        
        // 重置颜色
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        
        // 启用碰撞器
        if (platformCollider != null)
            platformCollider.enabled = true;
            
        // 启用触发器
        if (triggerCollider != null)
            triggerCollider.enabled = true;
        
        // 显示平台
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        
        // 重置状态
        isActivated = false;
        isRespawning = false;
        
        Debug.Log("平台复原");
    }
    
    // 重置平台状态（用于关卡重置等）
    public void ResetPlatform()
    {
        StopAllCoroutines();
        
        isActivated = false;
        isShaking = false;
        isFalling = false;
        isRespawning = false;
        
        // 重置位置
        transform.position = originalPosition;
        
        // 重置视觉部分位置
        if (visualTransform != null)
            visualTransform.localPosition = visualOriginalPosition;
        
        // 重置颜色
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        
        // 启用碰撞器
        if (platformCollider != null)
            platformCollider.enabled = true;
            
        // 启用触发器
        if (triggerCollider != null)
            triggerCollider.enabled = true;
        
        // 显示平台
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
        
        // 确保所有玩家都不再是平台的子对象
        foreach (Transform player in playersOnPlatform)
        {
            if (player != null)
            {
                player.SetParent(null);
            }
        }
        playersOnPlatform.Clear();
    }
    
    // 在编辑器中可视化触发区域
    void OnDrawGizmos()
    {
        if (platformCollider != null)
        {
            Gizmos.color = isActivated ? Color.red : (isRespawning ? Color.gray : Color.green);
            
            // 绘制碰撞器边界
            if (platformCollider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = platformCollider as BoxCollider2D;
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            }
            else if (platformCollider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = platformCollider as CircleCollider2D;
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
        
        // 绘制触发器区域
        if (triggerCollider != null)
        {
            Gizmos.color = Color.cyan;
            if (triggerCollider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = triggerCollider as BoxCollider2D;
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            }
            else if (triggerCollider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = triggerCollider as CircleCollider2D;
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
    }
    
    // 在编辑器中显示原始位置
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.5f);
        }
    }
}