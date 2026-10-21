using UnityEngine;
using System.Collections;

public class SnailDeath : MonoBehaviour
{
    [Header("死亡特效")]
    [SerializeField] private GameObject deathEffectPrefab; // 死亡特效预制体
    
    [Header("音效设置")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1.0f;
    
    [Header("闪烁效果设置")]
    [SerializeField] private Color flashColor = Color.red; // 闪烁颜色
    [SerializeField] private int flashCount = 3;           // 闪烁次数
    [SerializeField] private float flashDuration = 0.4f;   // 总闪烁时间
    [SerializeField] private bool enableFlash = true;      // 是否启用闪烁效果
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDying = false;
    
    void Start()
    {
        // 确保有碰撞体
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("SnailDeath需要Collider2D组件！");
        }
        
        // 获取 SpriteRenderer 组件
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 防止重复触发死亡
        if (isDying) return;
        
        if (other.CompareTag("trap"))
        {
            isDying = true;
            StartCoroutine(DeathSequence());
        }
    }
    
    private IEnumerator DeathSequence()
    {
        // 1. 禁用碰撞体（防止重复触发）
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        
        // 2. 播放死亡音效
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }
        
        // 3. 播放闪烁效果
        if (enableFlash && spriteRenderer != null)
        {
            yield return StartCoroutine(FlashEffect());
        }
        
        // 4. 生成死亡特效
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 5. 销毁蜗牛
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