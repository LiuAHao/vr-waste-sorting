using UnityEngine; 
using System.Collections; 
using System.Collections.Generic; 

public class Garbage : MonoBehaviour 
{
    public Player player; // 主角的脚本

    private void OnMouseDown() // 当鼠标点击到垃圾时调用此方法
    {
        player.clearCount += 1; // 增加主角清理的垃圾计数
        player.transform.GetComponent<AudioSource>().Play(); // 播放主角的音效
        Destroy(gameObject); // 销毁当前垃圾对象
    }
}
