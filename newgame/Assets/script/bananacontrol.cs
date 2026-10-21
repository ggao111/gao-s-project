using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bananacontrol : MonoBehaviour
{
    int banana = 0;
    
    [Header("UI设置")]
    [SerializeField] private Text bananatext;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip collectBananaSound; // 收集香蕉音效
    
    private AudioSource audioSource;

    private void Start()
    {
        // 初始化AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("banana"))
        {
            // 播放收集音效
            if (collectBananaSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectBananaSound);
            }
            
            Destroy(collision.gameObject);
            banana++;
            bananatext.text = "" + banana;
        }
    }
    
    // 公共方法，用于获取香蕉数量
    public int GetBananaCount()
    {
        return banana;
    }
}