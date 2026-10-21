using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 为每个关卡创建单独的函数
    public void LoadLevel1()
    {
        SceneManager.LoadScene("level1");
    }
    
    public void LoadLevel2()
    {
        SceneManager.LoadScene("level2");
    }
    
    public void LoadLevel3()
    {
        SceneManager.LoadScene("level3");
    }
    
    // 主菜单
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Mainmenu");
    }
    public void LoadChoose()
    {
        SceneManager.LoadScene("levelchoose");
    }
}