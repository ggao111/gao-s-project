using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Level1End : MonoBehaviour
{
    [Header("星级评分设置")]
    [SerializeField] private int bananasForThreeStars = 10;
    [SerializeField] private GameObject starRatingUI;
    [SerializeField] private Image[] starImages;
    [SerializeField] private Text scoreText;
    
    [Header("按钮设置")]
    [SerializeField] private Button returnToMenuButton;
    [SerializeField] private Button retryButton;
    
    [Header("星星动画设置")]
    [SerializeField] private float starAnimationDuration = 0.5f;
    [SerializeField] private float starAnimationDelay = 0.2f;
    [SerializeField] private float starStartScale = 2.0f;
    [SerializeField] private AnimationCurve starScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("音效设置")]
    [SerializeField] private AudioClip endSound;
    [SerializeField] private AudioClip starSound;
    [SerializeField] private float endSoundVolume = 1.0f;
    [SerializeField] private float starSoundVolume = 1.0f;
    
    [Header("视觉效果")]
    [SerializeField] private GameObject endEffect;
    
    // === 新增的BGM控制字段 ===
    [Header("BGM控制")]
    [Tooltip("将BGM的AudioSource组件拖到这里")]
    [SerializeField] private AudioSource bgmAudioSource;
    
    private bool isLevelCompleted = false;
    private bananacontrol bananaController;
    private AudioSource audioSource;
    
    void Start()
    {
        bananaController = FindObjectOfType<bananacontrol>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // 设置为2D音效（不受距离影响）
        audioSource.spatialBlend = 0f; // 0 = 2D音效，1 = 3D音效
        
        // 检查BGM引用是否设置
        if (bgmAudioSource == null)
        {
            Debug.LogWarning("BGM AudioSource未指定！关卡完成时可能无法停止背景音乐");
        }
        
        if (starRatingUI != null)
        {
            starRatingUI.SetActive(false);
        }
        
        if (returnToMenuButton != null)
        {
            returnToMenuButton.gameObject.SetActive(false);
        }
        
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(false);
        }
        
        HideAllStars();
    }
    
    private void HideAllStars()
    {
        if (starImages != null)
        {
            foreach (Image star in starImages)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isLevelCompleted)
        {
            CompleteLevel();
        }
    }
    
    private void CompleteLevel()
    {
        isLevelCompleted = true;
        
        Debug.Log("关卡完成！");
        
        // 播放结束特效
        if (endEffect != null)
        {
            Instantiate(endEffect, transform.position, Quaternion.identity);
        }
        
        // === 新增：停止BGM ===
        StopBGM();
        
        // 禁用玩家控制
        DisablePlayerControl();
        
        // 显示星级评分（这会触发星星动画，在星星动画结束后播放结束音效）
        ShowStarRating();
    }
    
    // === 新增：停止BGM的方法 ===
    private void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
            Debug.Log("背景音乐已停止");
        }
        else if (bgmAudioSource == null)
        {
            Debug.LogWarning("BGM AudioSource未指定，无法停止背景音乐");
        }
    }
    
    private void ShowStarRating()
    {
        int bananaCount = GetBananaCount();
        int stars = CalculateStars(bananaCount);
        
        if (starRatingUI != null)
        {
            starRatingUI.SetActive(true);
        }
        StartCoroutine(AnimateStars(stars));
    }
    
    private int GetBananaCount()
    {
        if (bananaController != null)
        {
            return bananaController.GetBananaCount();
        }
        
        return 0;
    }
    
    private int CalculateStars(int bananaCount)
    {
        if (bananaCount >= bananasForThreeStars)
        {
            return 3;
        }
        else if (bananaCount >= bananasForThreeStars * 2 / 3)
        {
            return 2;
        }
        else if (bananaCount >= bananasForThreeStars * 1 / 3)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
    
    private IEnumerator AnimateStars(int stars)
    {
        yield return null;
        
        // 如果没有星星，也等待一小段时间让UI显示
        if (stars <= 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        for (int i = 0; i < stars && i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].gameObject.SetActive(true);
                
                // 播放星星音效（使用2D音效）
                if (starSound != null)
                {
                    // 使用2D音效播放，确保音量不受位置影响
                    Play2DSound(starSound, starSoundVolume);
                }
                
                yield return StartCoroutine(AnimateSingleStar(starImages[i].transform));
                
                if (i < stars - 1)
                {
                    yield return new WaitForSeconds(starAnimationDelay);
                }
            }
        }
        
        // 所有星星动画播放完成后，播放结束音效
        if (endSound != null)
        {
            Play2DSound(endSound, endSoundVolume);
            Debug.Log("星星动画结束，播放结束音效");
        }
        
        // 然后显示返回按钮
        ShowReturnButton();
    }
    
    // 播放2D音效的方法
    private void Play2DSound(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null && audioSource != null)
        {
            // 使用PlayOneShot可以同时播放多个音效而不打断当前播放
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    private IEnumerator AnimateSingleStar(Transform starTransform)
    {
        Vector3 startScale = Vector3.one * starStartScale;
        Vector3 endScale = Vector3.one;
        
        starTransform.localScale = startScale;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < starAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / starAnimationDuration;
            float curveValue = starScaleCurve.Evaluate(progress);
            
            starTransform.localScale = Vector3.Lerp(startScale, endScale, curveValue);
            yield return null;
        }
        
        starTransform.localScale = endScale;
    }
    
    private void ShowReturnButton()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.gameObject.SetActive(true);
            Debug.Log("返回按钮已显示");
        }
        else
        {
            Debug.LogWarning("返回按钮未设置！");
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
            Debug.Log("重试按钮已显示");
        }
        else
        {
            Debug.LogWarning("重试按钮未设置！");
        }
    }
    
    private void DisablePlayerControl()
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            
            Rigidbody2D rb = playerController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            Debug.Log("玩家控制已禁用");
        }
    }
    
    public void CompleteLevelExternal()
    {
        if (!isLevelCompleted)
        {
            CompleteLevel();
        }
    }
    
    public bool IsLevelCompleted
    {
        get { return isLevelCompleted; }
    }
}