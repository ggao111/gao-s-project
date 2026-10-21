using UnityEngine;
using System.Collections;

public class BubbleSpawner : MonoBehaviour
{
    [Header("泡泡生成设置")]
    [SerializeField] private GameObject bubblePrefab;        // 泡泡预制体
    [SerializeField] private Transform spawnPoint;           // 生成点
    [SerializeField] private float spawnInterval = 2f;       // 生成间隔
    [SerializeField] private float spawnForce = 5f;          // 生成时的初始力
    [SerializeField] private Vector2 spawnDirection = Vector2.up; // 生成方向
    [SerializeField] private float bubbleLifetime = 10f;     // 泡泡存在时间
    
    [Header("不销毁时间设置")]
    [SerializeField] private float invincibleTime = 0.5f;    // 泡泡无敌时间（秒）
    
    [Header("随机化设置")]
    [SerializeField] private bool randomizeInterval = true;  // 随机化间隔
    [SerializeField] private float minInterval = 1.5f;       // 最小间隔
    [SerializeField] private float maxInterval = 3f;         // 最大间隔
    
    private float spawnTimer = 0f;

    void Start()
    {
        // 初始化生成点
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
        
        // 设置初始计时器
        spawnTimer = GetSpawnInterval();
    }

    void Update()
    {
        // 更新生成计时器
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnBubble();
            spawnTimer = GetSpawnInterval();
        }
    }
    
    // 生成泡泡
    private void SpawnBubble()
    {
        if (bubblePrefab == null) return;
        
        // 实例化泡泡
        GameObject bubble = Instantiate(bubblePrefab, spawnPoint.position, Quaternion.identity);
        
        // 给泡泡添加初始速度
        Rigidbody2D bubbleRb = bubble.GetComponent<Rigidbody2D>();
        if (bubbleRb != null)
        {
            Vector2 force = spawnDirection.normalized * spawnForce;
            bubbleRb.velocity = force;
        }
        
        // 设置泡泡无敌时间
        StartCoroutine(SetBubbleInvincible(bubble));
        
        // 设置泡泡自动销毁时间
        Bubble bubbleScript = bubble.GetComponent<Bubble>();
        if (bubbleScript != null)
        {
            // 如果泡泡有Bubble脚本，设置生存时间
            bubbleScript.SetLifetime(bubbleLifetime);
        }
        else
        {
            // 如果没有脚本，使用Destroy
            Destroy(bubble, bubbleLifetime);
        }
    }
    
    // 设置泡泡无敌时间
    private IEnumerator SetBubbleInvincible(GameObject bubble)
    {
        // 获取泡泡的所有碰撞体
        Collider2D[] colliders = bubble.GetComponentsInChildren<Collider2D>();
        
        // 临时禁用所有碰撞体
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
        
        // 等待无敌时间
        yield return new WaitForSeconds(invincibleTime);
        
        // 重新启用所有碰撞体
        foreach (Collider2D collider in colliders)
        {
            if (collider != null) // 检查碰撞体是否仍然存在（防止泡泡已被销毁）
            {
                collider.enabled = true;
            }
        }
    }
    
    // 获取生成间隔
    private float GetSpawnInterval()
    {
        if (randomizeInterval)
        {
            return Random.Range(minInterval, maxInterval);
        }
        return spawnInterval;
    }
    
    // 在编辑器中显示生成点
    void OnDrawGizmosSelected()
    {
        // 显示生成点
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.2f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + (Vector3)spawnDirection.normalized);
        }
    }
}