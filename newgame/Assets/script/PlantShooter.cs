using UnityEngine;

public class PlantShooter : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("射击设置")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 8f;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip shootSound; // 新增：射击音效
    
    [Header("动画设置")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackBool = "isattack";
    
    private Transform player;
    private bool playerInRange = false;
    private AudioSource audioSource; // 新增：音频源
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (firePoint == null)
            firePoint = transform;
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    void Update()
    {
        if (player == null) 
        {
            // 重新查找玩家
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            return;
        }
        
        CheckForPlayer();
        UpdateAttackState();
    }
    
    void CheckForPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            Vector2 directionToPlayer = (player.position - transform.position).normalized;
            
            // 使用 Linecast 检查植物和玩家之间是否有障碍物
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.position, obstacleLayer);
            
            if (hit.collider == null) // 没有障碍物阻挡
            {
                playerInRange = true;
                FlipTowardsPlayer(directionToPlayer);
            }
            else
            {
                playerInRange = false;
            }
        }
        else
        {
            playerInRange = false;
        }
    }
    
    void UpdateAttackState()
    {
        if (animator != null && !string.IsNullOrEmpty(attackBool))
        {
            animator.SetBool(attackBool, playerInRange);
        }
    }
    
    void FlipTowardsPlayer(Vector2 direction)
    {
        if (direction.x > 0 && transform.localScale.x < 0)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
        else if (direction.x < 0 && transform.localScale.x > 0)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
    }
    
    // 这个方法将由动画事件调用
    public void FireBullet()
    {
        // 播放射击音效
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            float direction = transform.localScale.x > 0 ? 1f : -1f;
            bulletRb.velocity = new Vector2(direction * bulletSpeed, 0f);
            
            if (direction < 0)
            {
                bullet.transform.localScale = new Vector3(-bullet.transform.localScale.x, bullet.transform.localScale.y, bullet.transform.localScale.z);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = playerInRange ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (player != null)
        {
            if (playerInRange)
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
    }
}