using UnityEngine;

/// <summary>
/// 挂在 VR World Space Canvas 上，让 Canvas 在玩家转头/移动后
/// 平滑地跟随到摄像机正前方，始终保持可读距离。
///
/// 行为：
///   - 静止时：Canvas 停在当前位置不动（不抖动）
///   - 玩家转头超过一定角度时：Canvas 平滑滑向新朝向
///   - 切换到"强制更新"模式时（比如游戏开始、切换界面）：
///     直接把 Canvas 重置到摄像机正前方
/// </summary>
public class VRCanvasFollower : MonoBehaviour
{
    [Tooltip("Canvas 到摄像机的距离（米）")]
    public float distance = 2.5f;

    [Tooltip("Canvas 中心相对摄像机高度的偏移（米，负值=往下）")]
    public float verticalOffset = -0.1f;

    [Tooltip("摄像机朝向与 Canvas 朝向夹角超过多少度时开始跟随（防止小幅度抖动）")]
    public float followAngleThreshold = 30f;

    [Tooltip("Canvas 追上摄像机朝向的速度（Lerp 系数，越大越快）")]
    public float followSpeed = 2f;

    private Camera _cam;
    private bool _needsImmediateSnap = true;

    private void OnEnable()
    {
        // 每次 Canvas 被激活时，立刻将其重置到摄像机正前方
        _needsImmediateSnap = true;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        Vector3 camPos = _cam.transform.position;
        Vector3 camForward = _cam.transform.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude < 0.001f) camForward = Vector3.forward;
        camForward.Normalize();

        Vector3 targetPos = camPos
            + camForward * distance
            + Vector3.up * (verticalOffset);

        Quaternion targetRot = Quaternion.LookRotation(camForward);

        if (_needsImmediateSnap)
        {
            // 强制立刻对齐
            transform.position = targetPos;
            transform.rotation = targetRot;
            _needsImmediateSnap = false;
            return;
        }

        // 检查当前 Canvas 朝向和目标朝向的夹角
        Vector3 toCanvas = (transform.position - camPos);
        toCanvas.y = 0f;
        if (toCanvas.sqrMagnitude > 0.001f) toCanvas.Normalize();

        float angle = Vector3.Angle(toCanvas, camForward);
        if (angle > followAngleThreshold)
        {
            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
        }
    }

    /// <summary>外部调用，强制下一帧立刻对齐到摄像机正前方。</summary>
    public void SnapToCamera()
    {
        _needsImmediateSnap = true;
    }
}
