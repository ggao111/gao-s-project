using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderWater : MonoBehaviour
{
    [Header("重力设置")]
    [SerializeField] private float waterGravityScale = 2.5f;    // 在水中的重力缩放
    [SerializeField] private float normalGravityScale =4f; // 正常重力缩放
    
    [Header("水域效果")]
    [SerializeField] private ParticleSystem waterRipples;    // 水波纹效果
    [SerializeField] private AudioSource waterEnterSound;    // 进入水域音效
    [SerializeField] private AudioSource waterExitSound;     // 离开水域音效
    
    [Header("玩家效果")]
    [SerializeField] private Color waterTintColor = new Color(0.5f, 0.8f, 1f, 0.8f); // 玩家在水中时的颜色
    
    // 当前在水中的玩家引用
    private Rigidbody2D playerRb;
    private SpriteRenderer playerSprite;
    
    void Start()
    {
        // 确保碰撞器设置为触发器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
        {
            collider.isTrigger = true;
            Debug.Log("已将水域碰撞器设置为触发器");
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家进入水域");
            
            // 获取玩家组件
            playerRb = other.GetComponent<Rigidbody2D>();
            playerSprite = other.GetComponent<SpriteRenderer>();
            
            if (playerRb == null)
            {
                Debug.LogError("玩家没有Rigidbody2D组件！");
                return;
            }
            
            // 立即改变重力缩放为水域重力
            playerRb.gravityScale = waterGravityScale;
            
            // 应用视觉效果
            if (playerSprite != null)
            {
                playerSprite.color = waterTintColor;
            }
            
            // 播放进入音效
            if (waterEnterSound != null)
            {
                waterEnterSound.Play();
            }
            
            // 播放水波纹效果
            if (waterRipples != null)
            {
                waterRipples.Play();
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerRb != null)
        {
            Debug.Log("玩家离开水域");
            
            // 立即恢复为正常重力缩放
            playerRb.gravityScale = normalGravityScale;
            
            // 移除视觉效果
            if (playerSprite != null)
            {
                playerSprite.color = Color.white;
            }
            
            // 播放离开音效
            if (waterExitSound != null)
            {
                waterExitSound.Play();
            }
            
            // 重置引用
            playerRb = null;
            playerSprite = null;
        }
    }
    
    void OnDrawGizmos()
    {
        // 绘制水域区域可视化
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        
        // 尝试获取各种碰撞器并绘制
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            if (collider is BoxCollider2D boxCollider)
            {
                Gizmos.DrawCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
            else if (collider is PolygonCollider2D)
            {
                // 多边形碰撞器比较复杂，这里简单绘制一个边界框
                Bounds bounds = collider.bounds;
                Gizmos.DrawCube(bounds.center, bounds.size);
            }
        }
    }
}
