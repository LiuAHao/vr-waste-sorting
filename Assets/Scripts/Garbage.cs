using UnityEngine; 
using System.Collections; 
using System.Collections.Generic; 

public class Garbage : MonoBehaviour 
{
    public Player player; // 主角的脚本
    public bool CanInteract()
    {
        // 如果以后有“垃圾太重”或者“已经进桶”的逻辑，可以在这里改
        return true;
    }

    // 2. 给 Interactor 调用的接口：当抓起或放下时，垃圾自己想做什么
    public void SetHeld(bool held)
    {
        // 暂时留空，保证不报错即可
    }
    private void OnMouseDown() // 当鼠标点击到垃圾时调用此方法
    {
        player.clearCount += 1; // 增加主角清理的垃圾计数
        player.transform.GetComponent<AudioSource>().Play(); // 播放主角的音效
        Destroy(gameObject); // 销毁当前垃圾对象
    }
}
