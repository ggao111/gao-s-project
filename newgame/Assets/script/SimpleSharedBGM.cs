using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLimitedBGM : MonoBehaviour
{
    // 静态实例，确保只有一个背景音乐
    private static SceneLimitedBGM instance;
    
    // 引用AudioSource组件
    private AudioSource audioSource;
    
    // 允许播放BGM的场景索引（从0开始）
    [Header("场景配置")]
    [Tooltip("允许播放BGM的场景索引（0=第一个场景，1=第二个场景）")]
    public int[] allowedSceneIndices = new int[] { 0, 1 };
    
    // 当前场景索引
    private int currentSceneIndex;
    
    void Awake()
    {
        // 单例模式：如果实例已存在，销毁新的
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 设置实例为当前对象
        instance = this;
        
        // 确保在场景切换时不销毁
        DontDestroyOnLoad(gameObject);
        
        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
        
        // 监听场景切换事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 获取当前场景索引
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // 根据当前场景决定是否播放
        CheckAndPlayForCurrentScene();
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 更新当前场景索引
        int newSceneIndex = scene.buildIndex;
        
        // 如果场景索引变化，重新检查是否播放音乐
        if (newSceneIndex != currentSceneIndex)
        {
            currentSceneIndex = newSceneIndex;
            CheckAndPlayForCurrentScene();
        }
    }
    
    void CheckAndPlayForCurrentScene()
    {
        // 检查当前场景是否在允许列表中
        bool shouldPlay = false;
        foreach (int allowedIndex in allowedSceneIndices)
        {
            if (allowedIndex == currentSceneIndex)
            {
                shouldPlay = true;
                break;
            }
        }
        
        if (shouldPlay)
        {
            // 应该在当前场景播放音乐
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"场景 {currentSceneIndex} 允许播放BGM，开始播放");
            }
            else
            {
                Debug.Log($"场景 {currentSceneIndex} 允许播放BGM，继续播放");
            }
        }
        else
        {
            // 不应该在当前场景播放音乐
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                Debug.Log($"场景 {currentSceneIndex} 不允许播放BGM，已停止");
            }
        }
    }
    
    // 可选：提供一些控制方法
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
    
    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }
    
    // 手动添加允许播放的场景索引
    public void AddAllowedScene(int sceneIndex)
    {
        System.Array.Resize(ref allowedSceneIndices, allowedSceneIndices.Length + 1);
        allowedSceneIndices[allowedSceneIndices.Length - 1] = sceneIndex;
        Debug.Log($"添加场景索引 {sceneIndex} 到允许列表");
        
        // 如果当前就是这个场景，开始播放音乐
        if (currentSceneIndex == sceneIndex && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    // 手动移除允许播放的场景索引
    public void RemoveAllowedScene(int sceneIndex)
    {
        // 查找要移除的索引
        int indexToRemove = -1;
        for (int i = 0; i < allowedSceneIndices.Length; i++)
        {
            if (allowedSceneIndices[i] == sceneIndex)
            {
                indexToRemove = i;
                break;
            }
        }
        
        // 如果找到，移除它
        if (indexToRemove >= 0)
        {
            // 创建新数组，排除要移除的元素
            int[] newArray = new int[allowedSceneIndices.Length - 1];
            int newIndex = 0;
            for (int i = 0; i < allowedSceneIndices.Length; i++)
            {
                if (i != indexToRemove)
                {
                    newArray[newIndex] = allowedSceneIndices[i];
                    newIndex++;
                }
            }
            
            allowedSceneIndices = newArray;
            Debug.Log($"从允许列表移除场景索引 {sceneIndex}");
            
            // 如果当前就是这个场景，停止音乐
            if (currentSceneIndex == sceneIndex && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
    
    void OnDestroy()
    {
        // 取消监听场景切换事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}