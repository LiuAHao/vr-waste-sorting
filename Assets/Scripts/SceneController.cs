using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void ChangeScene(int index)
    {
        SceneManager.LoadScene(index); // 加载游戏场景
    }

    public void QuitGame()
    {
        Application.Quit(); // 退出游戏
    }

}
