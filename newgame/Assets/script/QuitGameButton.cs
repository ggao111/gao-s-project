using UnityEngine;
using UnityEngine.UI;

public class QuitGameButton : MonoBehaviour
{
    void Start()
    {
        // 获取按钮组件并添加点击事件监听
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(QuitGame);
        }
        else
        {
            Debug.LogError("QuitGameButton脚本没有附加到Button对象上！");
        }
    }

    // 退出游戏的方法
    public void QuitGame()
    {
        Debug.Log("退出游戏");

        // 如果在Unity编辑器中运行，则停止播放
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 在打包的游戏版本中，退出游戏
            Application.Quit();
        #endif
    }
}