using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class playerlife : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("音效设置")]
    [SerializeField] private AudioClip deadSoundEffect;
    [SerializeField] private AudioClip hurtSoundEffect;
    [SerializeField] private AudioClip healSoundEffect;
    [SerializeField] private AudioClip checkpointSound; // 新增：重生点音效
    
    private AudioSource audioSource; // 用于播放音效的AudioSource
    
    [Header("生命值系统")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invincibilityTime = 1.5f;
    [SerializeField] private GameObject[] healthHearts;
    [SerializeField] private float knockbackForce = 20f;
    
    [Header("击退设置")]
    [SerializeField] private float knockbackDuration = 0.3f;
    
    [Header("治疗设置")]
    [SerializeField] private int healAmount = 1;
    [SerializeField] private bool destroyHeartWhenFullHealth = false;
    
    [Header("检测设置")]
    [SerializeField] private float detectionRadius = 0.5f;
    [SerializeField] private LayerMask enemyAndTrapLayerMask = 1;
    
    private int currentHealth;
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    
    [Header("重生设置")]
    [SerializeField] private Transform respawnPoint;
    private Vector3 initialPosition;
    private bool isRespawnPointActive = false;
    
    [Header("重新开始设置")]
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private float showRestartDelay = 1.5f;
    private bool isDead = false;
    
    // 击退控制变量
    private bool isKnockbackActive = false;
    private float knockbackEndTime = 0f;
    private Vector2 knockbackVelocity;
    private PlayerController playerController;
    
    // 新增：存储死亡前的位置和状态
    private Vector3 deathPosition;
    private int deathHealth;
    
    // 新增：防止重复触发重生点音效
    private bool hasPlayedCheckpointSound = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 重生设置初始化
        initialPosition = transform.position;
        if (respawnPoint == null)
        {
            GameObject respawnObj = new GameObject("RespawnPoint");
            respawnPoint = respawnObj.transform;
            respawnPoint.position = initialPosition;
        }
        
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        if (restartPanel != null)
        {
            restartPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDead && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
        
        // 处理击退状态
        HandleKnockback();
        
        // 新增：持续检测敌人和陷阱
        if (!isInvincible && !isDead && !isKnockbackActive)
        {
            CheckForEnemyAndTrapContact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("deadzone") && !isDead)
        {
            FallToDeadZone();
        }
        else if (collision.gameObject.CompareTag("checkpoint") && !isDead)
        {
            SetRespawnPoint(collision.transform);
        }
        // 心形触发器检测
        else if (collision.gameObject.CompareTag("heart") && !isDead)
        {
            CollectHeart(collision.gameObject);
        }
    }

    // 收集心形的方法
    private void CollectHeart(GameObject heartObject)
    {
        // 检查是否满血
        if (currentHealth >= maxHealth)
        {
            // 满血时的处理
            HandleFullHealth(heartObject);
            return;
        }
        
        // 恢复生命值
        Heal(healAmount);
        
        // 播放治疗音效
        if (healSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(healSoundEffect);
        }
        
        // 销毁心形物体
        Destroy(heartObject);
        
        Debug.Log($"收集到心形，恢复 {healAmount} 点生命值，当前生命: {currentHealth}/{maxHealth}");
    }
    
    // 满血时的处理
    private void HandleFullHealth(GameObject heartObject)
    {
        // 根据设置决定是否销毁心形
        if (destroyHeartWhenFullHealth)
        {
            Destroy(heartObject);
            Debug.Log("生命值已满，心形被销毁");
        }
        else
        {
            Debug.Log("生命值已满，无法收集心形");
        }
    }
    
    // 处理击退状态
    private void HandleKnockback()
    {
        if (isKnockbackActive)
        {
            // 在击退状态下，直接控制速度，绕过其他系统
            rb.velocity = knockbackVelocity;
            
            // 检查击退是否结束
            if (Time.time >= knockbackEndTime)
            {
                isKnockbackActive = false;
                // 击退结束后恢复玩家控制
                EnablePlayerControl();
            }
        }
    }
    
    // 新增：持续检测敌人和陷阱接触
    private void CheckForEnemyAndTrapContact()
    {
        // 如果在冲刺，不受伤
        if (playerController != null && playerController.IsDashing)
        {
            return;
        }
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyAndTrapLayerMask);
        
        foreach (Collider2D hit in hits)
        {
            if ((hit.CompareTag("trap") || hit.CompareTag("Enemy")))
            {
                TakeDamage(hit.transform.position);
                break; // 一次只处理一个伤害
            }
        }
    }
    
    // 受伤方法
    public void TakeDamage(Vector3 damageSourcePosition)
    {
        if (isInvincible || isDead)
        {
            //玩家处于无敌或死亡状态，免疫伤害
            return;
        }
        currentHealth--;
        UpdateHealthUI();
        
        if (hurtSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSoundEffect);
        }
        
        anim.SetTrigger("hurt");
        
        // 禁用玩家控制
        DisablePlayerControl();
        
        // 应用击退效果
        StartCoroutine(ApplyKnockback(damageSourcePosition));
        
        StartCoroutine(InvincibilityFrames());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    // 使用协程应用击退效果
    private IEnumerator ApplyKnockback(Vector3 damageSourcePosition)
    {
        // 计算击退方向
        float horizontalDirection = (transform.position.x > damageSourcePosition.x) ? 1f : -1f;
        
        // 创建击退方向向量 - 增加水平分量
        Vector2 knockbackDirection = new Vector2(horizontalDirection, 0.3f).normalized;
        
        // 设置击退速度
        knockbackVelocity = knockbackDirection * knockbackForce;
        
        // 激活击退状态
        isKnockbackActive = true;
        knockbackEndTime = Time.time + knockbackDuration;
        
        // 重置当前速度
        rb.velocity = Vector2.zero;
        
        Debug.Log($"击退开始: 方向={knockbackDirection}, 速度={knockbackVelocity}");
        
        // 等待击退持续时间
        yield return new WaitForSeconds(knockbackDuration);
        
        // 击退结束
        isKnockbackActive = false;
        knockbackVelocity = Vector2.zero;
        
        Debug.Log("击退结束");
    }
    
    // 掉入deadzone的处理方法
    private float lastDeadZoneTime = 0f;
    private float deadZoneCooldown = 0.5f; // 半秒内不会重复触发
    private void FallToDeadZone()
    {
        // 添加冷却时间检查
        if (Time.time - lastDeadZoneTime < deadZoneCooldown)
        {
            return;
        }
        lastDeadZoneTime = Time.time;
        
        if (isDead) return;

        currentHealth--;
        UpdateHealthUI();
        
        if (hurtSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSoundEffect);
        }
        
        anim.SetTrigger("hurt");
        
        // 禁用玩家控制
        DisablePlayerControl();
        
        Respawn();
        
        StartCoroutine(InvincibilityFrames());
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        Debug.Log("掉入deadzone，生命值减少，回到重生点");
    }
    
    private void Respawn()
    {
        rb.velocity = Vector2.zero;
        if(currentHealth > 0)
        {
            transform.position = respawnPoint.position;
        }
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // 恢复玩家控制
        EnablePlayerControl();
    }

    //无敌时间
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        float elapsedTime = 0f;
        while (elapsedTime < invincibilityTime)
        {
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            
            elapsedTime += 0.2f;
        }
        
        spriteRenderer.color = Color.white;
        isInvincible = false;
    }
    
    // 冲刺结束后检查是否在陷阱中
    public void CheckTrapAfterDash()
    {
        if (isDead || isInvincible) return;
        
        // 使用OverlapCircleAll检查玩家周围是否有陷阱
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyAndTrapLayerMask);
        
        foreach (Collider2D hit in hits)
        {
            if ((hit.CompareTag("trap") || hit.CompareTag("Enemy")))
            {
                // 如果在冲刺结束后立即接触到陷阱，则受伤
                TakeDamage(hit.transform.position);
                Debug.Log("冲刺结束后，玩家在陷阱/敌人中，受伤");
                break;
            }
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthHearts != null && healthHearts.Length >= maxHealth)
        {
            for (int i = 0; i < maxHealth; i++)
            {
                if (healthHearts[i] != null)
                {
                    healthHearts[i].SetActive(i < currentHealth);
                }
            }
        }
    }
    
    // 恢复生命值方法
    public void Heal(int healAmount)
    {
        // 确保不会超过最大生命值
        int newHealth = currentHealth + healAmount;
        currentHealth = Mathf.Min(newHealth, maxHealth);
        UpdateHealthUI();
        
        Debug.Log($"恢复 {healAmount} 点生命值，当前生命: {currentHealth}/{maxHealth}");
    }
    
    // 设置重生点方法
    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        // 如果已经设置过这个重生点，不要重复触发
        if (respawnPoint == newRespawnPoint && isRespawnPointActive)
            return;
        
        respawnPoint = newRespawnPoint;
        
        isRespawnPointActive = true;
        hasPlayedCheckpointSound = true;
        
        // 播放重生点音效
        if (checkpointSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(checkpointSound);
        }
        
        // 尝试获取重生点的Animator并触发动画
        Animator checkpointAnimator = newRespawnPoint.GetComponent<Animator>();
        if (checkpointAnimator != null)
        {
            checkpointAnimator.SetBool("IsActive", true);
        }
        
        Debug.Log($"重生点已更新: {respawnPoint.position}");
    }

    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        rb.bodyType = RigidbodyType2D.Static;
        anim.SetTrigger("dead");
        
        // 存储死亡时的状态
        deathPosition = transform.position;
        deathHealth = currentHealth;
        
        // 禁用玩家控制
        DisablePlayerControl();
        
        if (deadSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(deadSoundEffect);
        }
        
        StartCoroutine(ShowRestartOption());
    }
    
    // 禁用玩家控制的方法
    private void DisablePlayerControl()
    {
        // 禁用PlayerController组件
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        Debug.Log("玩家控制已禁用");
    }
    
    // 新增：启用玩家控制的方法
    private void EnablePlayerControl()
    {
        // 如果玩家已经死亡，不启用控制
        if (isDead) return;
        
        // 启用PlayerController组件
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        Debug.Log("玩家控制已启用");
    }
    
    private IEnumerator ShowRestartOption()
    {
        // 等待死亡动画播放一段时间
        yield return new WaitForSeconds(showRestartDelay);
        
        // 如果有重新开始面板，显示它
        if (restartPanel != null)
        {
            restartPanel.SetActive(true);
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    // 新增：在重生点复活的方法
    public void RespawnAtCheckpoint()
    {
        // 重置生命值为满血
        currentHealth = maxHealth;
        UpdateHealthUI();

        // 重置位置到重生点
        transform.position = respawnPoint.position;
        // 强制重置动画状态为idle
        anim.Rebind();

        
        // 恢复刚体为动态
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.velocity = Vector2.zero;
        
        // 重置死亡状态
        isDead = false;
        
        // 重新启用玩家控制
        EnablePlayerControl();
        
        // 隐藏重新开始面板
        if (restartPanel != null)
        {
            restartPanel.SetActive(false);
        }
        
        // 触发复活动画（如果有）
        anim.SetTrigger("respawn");
        
        // 重置玩家能力状态
        ResetPlayerAbilities();
        
        // 给予短暂无敌时间
        StartCoroutine(InvincibilityFrames());
        
        Debug.Log($"在重生点复活: {respawnPoint.position}");
    }
    
    // 新增：重置玩家能力状态
    private void ResetPlayerAbilities()
    {
        // 重置跳跃能力
        PlayerJump jump = GetComponent<PlayerJump>();
        if (jump != null)
        {
            jump.ResetJumpAbility();
        }
        
        // 重置蹬墙跳状态
        PlayerWallJump wallJump = GetComponent<PlayerWallJump>();
        if (wallJump != null)
        {
            wallJump.ResetWallJumpState();
        }
        
        // 重置冲刺能力
        PlayerDash dash = GetComponent<PlayerDash>();
        if (dash != null)
        {
            dash.ResetDashAbility();
        }
        
        // 重置重生点音效播放状态
        hasPlayedCheckpointSound = false;
    }
    
    // 公共属性，供其他脚本检查击退状态
    public bool IsKnockbackActive
    {
        get { return isKnockbackActive; }
    }
    
    // 获取当前生命值和最大生命值
    public int CurrentHealth
    {
        get { return currentHealth; }
    }
    
    public int MaxHealth
    {
        get { return maxHealth; }
    }
    
    // 检查是否满血
    public bool IsFullHealth
    {
        get { return currentHealth >= maxHealth; }
    }
    
    // 重生点动画控制属性
    public bool IsRespawnPointActive
    {
        get { return isRespawnPointActive; }
        set { isRespawnPointActive = value; }
    }
}