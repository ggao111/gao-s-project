using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("子弹设置")]
    [SerializeField] private float lifetime = 3f; // 存活时间

    [Header("视觉效果")]
    [SerializeField] private GameObject hitEffect; // 命中特效
    
    void Start()
    {
        // 设置自动销毁
        Destroy(gameObject, lifetime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        // 如果击中了玩家
        if (other.CompareTag("Player")&&!(playerController != null && playerController.IsDashing))
        {
            Debug.Log("子弹击中玩家");
            
            // 获取玩家生命组件并造成伤害
            playerlife playerLife = other.GetComponent<playerlife>();
            if (playerLife != null)
            {
                
                playerLife.TakeDamage(transform.position);
            }

            // 播放命中特效
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 销毁子弹
            Destroy(gameObject);
        }
        
        // 如果击中了地面或墙壁（使用层检测）
        if (other.gameObject.layer == LayerMask.NameToLayer("terrain"))
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}