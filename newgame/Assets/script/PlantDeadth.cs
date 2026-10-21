using UnityEngine;
using System.Collections;

public class PlantDeadth : MonoBehaviour
{
    [Header("植物设置")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private GameObject stompDetector; // 用于检测踩踏的触发器
    
    [Header("音效设置")]
    [SerializeField] private AudioClip stompSound; // 踩踏植物音效
    
    [Header("闪烁效果设置")]
    [SerializeField] private Color flashColor = Color.red; // 闪烁颜色
    [SerializeField] private int flashCount = 3;           // 闪烁次数
    [SerializeField] private float flashDuration = 0.4f;   // 总闪烁时间
    [SerializeField] private bool enableFlash = true;      // 是否启用闪烁效果
    
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDying = false;
    
    void Start()
    {
        // 如果没有指定踩踏检测器，使用自身
        if (stompDetector == null)
            stompDetector = gameObject;
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 获取 SpriteRenderer 组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    // 当玩家进入踩踏检测区域时
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDying) return; // 防止重复触发
        
        if (other.CompareTag("Player"))
        {
            // 获取玩家的刚体
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            
            // 如果玩家正在下落（从上方踩下）
            if (playerRb != null && playerRb.velocity.y < 0)
            {
                isDying = true;
                StartCoroutine(DeathSequence(playerRb));
            }
        }
    }
    
    private IEnumerator DeathSequence(Rigidbody2D playerRb)
    {
        // 1. 立即给玩家一个弹跳
        playerRb.velocity = new Vector2(playerRb.velocity.x, 7f);
        
        // 2. 播放踩踏音效
        if (stompSound != null)
        {
            AudioSource.PlayClipAtPoint(stompSound, transform.position);
        }
        
        // 3. 播放闪烁效果
        if (enableFlash && spriteRenderer != null)
        {
            yield return StartCoroutine(FlashEffect());
        }
        
        // 4. 播放死亡特效
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // 5. 消灭植物
        Destroy(gameObject);
    }
    
    // 颜色闪烁效果协程
    private IEnumerator FlashEffect()
    {
        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(0.08f);
        }
    }
}