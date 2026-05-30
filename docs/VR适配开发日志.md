# VR 适配开发日志

> 本文档记录 `VRadjustment` 分支从桌面端 MVP 到 PICO4 VR 可运行版本的所有改动。

---

## 改动概览

| 类别 | 文件/组件 | 改动内容 |
|---|---|---|
| **包依赖** | `Packages/manifest.json` | 新增 XR Interaction Toolkit 2.6.5、OpenXR Plugin、PICO XR SDK |
| **新增脚本** | `VRGarbageInteractor.cs` | VR 抓取桥接 |
| **新增脚本** | `VRInteractionBootstrap.cs` | VR 自动初始化 |
| **新增脚本** | `VRCanvasFollower.cs` | World Space Canvas 跟随摄像机 |
| **新增脚本** | `VRCanvasHelper.cs` | Canvas 转换辅助（已被 Factory 替代，保留兼容）|
| **修改脚本** | `WasteUiFactory.cs` | VR 下创建 World Space Canvas，附加 Raycaster 和 Follower |
| **修改脚本** | `WasteHudView.cs` | 激活时 SnapToCamera |
| **修改脚本** | `WastePauseView.cs` | 激活时 SnapToCamera，放大按钮以便射线点击 |
| **修改脚本** | `WasteGameBootstrap.cs` | 添加 VR X 键暂停（上升沿检测） |
| **修改脚本** | `DropZone.cs` | 新增 OnTriggerEnter 支持 VR 物理投掷判定 |
| **修改脚本** | `DesktopInteractionBootstrap.cs` | VR 活跃时跳过初始化 |
| **修改脚本** | `Player.cs` | VR 活跃时禁用键鼠输入 |
| **Prefab** | `XR Origin (XR Rig).prefab` | 调整移速/转速/转向模式/禁用重力 |
| **场景** | `Assets/Scenes/2.unity` | 放置 XR Interaction Setup，降低 Camera Offset 高度 |
| **XR 配置** | `Assets/XR/` | OpenXR Loader、PICO Loader、Controller Profile 等 |

---

## 详细改动记录

### 包依赖（`Packages/manifest.json`）

新增：
- `com.unity.xr.interaction.toolkit@2.6.5`
- `com.unity.xr.openxr@1.x`
- PICO XR SDK（`com.pvr.sdk` 或 `com.unity.xr.picoxr`）

---

### 新增：`VRGarbageInteractor.cs`

**路径**：`Assets/Scripts/Interaction/VRGarbageInteractor.cs`

**职责**：桥接 XRI Select 事件到游戏抓取逻辑。

**关键逻辑**：
- `OnSelectEntered`：设置 Rigidbody Kinematic，调用 `item.SetHeld(true)`
- `OnSelectExited`：恢复物理，施加手柄速度，调用即时投放判定
- `Update`：每帧将物品 Lerp 到 `Camera.main.forward * holdDistanceFromCamera`（摄像机前方固定 0.6m）
- 手柄速度追踪：`(transform.position - _prevHandPosition) / deltaTime`

**设计决策**：
- 物品锁定在**摄像机前方**（而非手柄前方），避免转视角时物品跟着旋转
- 松手时若手柄速度 < 0.5m/s，给予默认向前向下速度，避免物品悬停

---

### 新增：`VRInteractionBootstrap.cs`

**路径**：`Assets/Scripts/Interaction/VRInteractionBootstrap.cs`

**触发方式**：`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`

**初始化内容**：
1. 找到场景中所有 `XRRayInteractor` 和 `XRDirectInteractor`，为其挂载 `VRGarbageInteractor`
2. 为 `XRDirectInteractor` 添加 `SphereCollider(isTrigger=true, radius=0.1)`（消除 "no trigger collider" 警告）
3. 为所有 `GarbageItem` 添加 `XRGrabInteractable`（Kinematic 模式，禁用 attachTransform，禁用 throwOnDetach）
4. 禁用 `TunnelingVignette`（防止移动时眩晕黑晕）

---

### 新增：`VRCanvasFollower.cs`

**路径**：`Assets/Scripts/UI/VRCanvasFollower.cs`

**行为**：
- `OnEnable`：设置 `_needsImmediateSnap = true`，下一帧立即对齐
- `LateUpdate`：计算摄像机前方方向（水平分量），若当前 Canvas 朝向与目标偏差 > 30°，则 Lerp 跟随（速度 2f）
- `SnapToCamera()`：公开方法，立即在下一帧强制对齐（用于 Show/SetVisible 调用）

**参数**：
- `distance = 2.5f`（Canvas 距离摄像机 2.5m）
- `verticalOffset = -0.1f`（视线中央轻微下偏）
- `followAngleThreshold = 30f`（触发跟随的最小角度）

---

### 修改：`WasteUiFactory.CreateCanvasRoot()`

**VR 分支**的 World Space Canvas 配置：
```csharp
canvas.renderMode = RenderMode.WorldSpace;
canvasRect.sizeDelta = new Vector2(1920f, 1080f);  // 关键：让相对锚点 UI 正确计算
root.transform.localScale = Vector3.one * 0.001f;   // 1920px * 0.001 = 1.92m
// TrackedDeviceGraphicRaycaster (maxRaycastDistance=10)
// 所有子物体 Layer = 0 (Default)
// VRCanvasFollower (distance=2.5, verticalOffset=-0.1)
```

**注意**：`sizeDelta = (1920, 1080)` 是 HUD TopBar 等锚点布局能正常显示的关键——没有这行，锚点计算基于 (0,0) 导致 TopBar 高度为 0（显示为细线）。

---

### 修改：`WasteGameBootstrap`：VR 暂停键

```csharp
private bool IsVRMenuButtonPressed()
{
    InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    bool currentPressed = false;
    leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out currentPressed);
    
    bool wasPressed = _lastXButtonState;
    _lastXButtonState = currentPressed;
    
    return currentPressed && !wasPressed;  // 上升沿检测，避免按住时连续触发
}
```

---

### 修改：`DropZone.OnTriggerEnter`

```csharp
private void OnTriggerEnter(Collider other)
{
    GarbageItem item = other.GetComponentInParent<GarbageItem>()
                    ?? other.GetComponent<GarbageItem>();
    if (item == null || item.IsHeld || item.IsCompleted) return;
    Classify(item);
}
```

---

### XR Origin Prefab 调整

| 参数 | 原值 | 新值 | 说明 |
|---|---|---|---|
| `ContinuousMoveProvider.m_MoveSpeed` | 1 | 5 | 移动速度（用户调整）|
| `ActionBasedControllerManager.m_SmoothTurnEnabled` | 0 | 1 | 启用平滑连续转向 |
| `ContinuousTurnProvider.m_TurnSpeed` | 60 | 90 | 转向速度 |
| `DynamicMoveProvider.m_UseGravity` | 1 | 0 | 禁用重力（PICO4 Floor 追踪）|
| `Camera Offset.m_LocalPosition.y` | 1.41 | 1.1 | 摄像机高度降低约 30cm |

---

## 已知限制

| 问题 | 状态 | 说明 |
|---|---|---|
| 抓取后物品跟随手柄旋转 | 未完全解决 | XRI 的 XRGrabInteractable parent 机制与自定义位置控制存在冲突；位置计算（摄像机前方）已正确，但旋转跟随无法完全消除 |
| DirectInteractor "no trigger collider" 警告 | 无害 | Bootstrap 在 `AfterSceneLoad` 时才添加 Collider，XRDirectInteractor.Awake 已经报过一次警告；Collider 实际已补上，功能正常 |
| 暂停菜单手柄射线点击 | 基本可用 | 按钮已放大至 360×90px；TrackedDeviceGraphicRaycaster maxDistance=10 |
