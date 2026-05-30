# ParkClean — VR 垃圾分类小游戏

**ParkClean** 是一个基于 Unity 的垃圾分类教育 VR 小游戏。玩家置身于校园场景中，通过 VR 手柄拾取散落的垃圾，按照可回收物、有害垃圾、厨余垃圾、其他垃圾四大类别投入对应的垃圾桶，并在计时结束后查看正确率和复盘。

---

## 分支说明

| 分支 | 说明 |
|---|---|
| `main` | 桌面端（键鼠）完整可玩版本，作为原始 MVP 保留 |
| `VRadjustment` | **VR 适配版本**（本分支），面向 PICO4 等 OpenXR 头显 |

---

## 运行环境

| 条件 | 要求 |
|---|---|
| Unity | 2022.3 LTS（推荐 2022.3.62f1）|
| XR 插件 | XR Interaction Toolkit 2.6.5、OpenXR Plugin |
| 头显 | PICO4（Oculus Touch Controller Profile）|
| 连接方式 | PICO Link 串流（PC 运行） 或 打包 APK 独立运行 |
| 平台 | PC（Windows / macOS）+ SteamVR，或 Android Standalone |

---

## 快速开始（PC 串流，推荐开发调试）

1. 在 Unity 编辑器中打开 `Assets/Scenes/2.unity`
2. 确认 **Project Settings → XR Plug-in Management** 已勾选 **OpenXR**
3. 确认 **OpenXR → Interaction Profiles** 已添加 **Oculus Touch Controller Profile**
4. 连接 PICO4（开启 PICO Link 串流，SteamVR 检测到头显）
5. 点击 Unity **Play**，戴上 PICO4 即可游玩

---

## VR 操作说明

| 操作 | 绑定 |
|---|---|
| **移动** | 左手柄摇杆（前/后/左/右平移）|
| **转视角** | 右手柄摇杆（左右连续旋转）|
| **选中垃圾** | 右手柄射线瞄准垃圾（射线变蓝+手柄震动）|
| **抓取垃圾** | 右手柄 Grip 键（抓握键）|
| **松手/投放** | 松开 Grip 键（垃圾会以手柄速度飞出，飞入桶口自动判定）|
| **呼出/关闭暂停菜单** | 左手柄 **X 键** |
| **点击 UI 按钮** | 手柄射线瞄准后按 **Trigger 键** |

---

## 项目结构（VR 分支关键文件）

```
Assets/
├── Scenes/
│   └── 2.unity                      # 唯一游戏场景
├── Scripts/
│   ├── Core/
│   │   └── WasteGameBootstrap.cs    # 游戏主引导，所有 UI / 流程在此初始化
│   ├── Gameplay/
│   │   ├── GarbageItem.cs           # 垃圾物品逻辑（可持有状态、分类属性）
│   │   └── DropZone.cs              # 垃圾桶投放区触发判定
│   ├── Interaction/
│   │   ├── VRGarbageInteractor.cs   # VR 抓取桥接：XRI Select 事件 → 游戏逻辑
│   │   ├── VRInteractionBootstrap.cs# VR 自动初始化：挂载 Interactor、配置物品
│   │   └── DesktopInteractionBootstrap.cs # 桌面端（已在 VR 下禁用）
│   └── UI/
│       ├── WasteUiFactory.cs        # UI 工厂（VR 模式自动创建 World Space Canvas）
│       ├── VRCanvasFollower.cs      # Canvas 跟随摄像机，始终保持在视野内
│       ├── WasteHudView.cs          # 游戏局内 HUD（时间/得分/进度/投放反馈）
│       └── WastePauseView.cs        # 暂停菜单
├── Samples/XR Interaction Toolkit/2.6.5/Starter Assets/
│   ├── Prefabs/
│   │   ├── XR Interaction Setup.prefab  # 场景中的 XR Rig（XR Origin + 手柄 + 交互管理器）
│   │   └── XR Origin (XR Rig).prefab   # XR Origin 主 Prefab（移动速度、转向、重力等配置）
│   └── Scripts/
│       └── ActionBasedControllerManager.cs  # 控制平滑转向模式
└── XR/
    └── Settings/
        └── OpenXR Package Settings.asset    # OpenXR 配置（Oculus Touch Controller Profile）
```

---

## VR 适配技术说明

### 架构设计原则

本次 VR 适配遵循**最小侵入原则**：桌面端游戏逻辑（`GarbageItem`、`DropZone`、所有游戏模式控制器）**完全未修改**，所有 VR 特化逻辑通过新增文件和装饰模式叠加实现。

### 关键改动

#### 1. XR Rig（`XR Origin (XR Rig).prefab`）
- 移动速度：`m_MoveSpeed = 5`
- 转向模式：`ActionBasedControllerManager.m_SmoothTurnEnabled = 1`（平滑转向，替代原来的固定角度 Snap Turn）
- 重力：`m_UseGravity = 0`（PICO4 使用 Floor 追踪，无需重力模拟）
- 转速：`ContinuousTurnProvider.m_TurnSpeed = 90`

#### 2. VR 抓取交互（`VRGarbageInteractor.cs`）
- 监听 `XRRayInteractor` 和 `XRDirectInteractor` 的 Select 事件
- 抓取时物品锁定在**摄像机前方固定距离（0.6m）**，不跟随手柄旋转
- 松手时根据手柄速度计算抛出物理，配合 `DropZone.OnTriggerEnter` 触发投放判定
- 追踪手柄速度用于投掷（每帧 `Update` 计算）

#### 3. VR UI 系统（`WasteUiFactory.cs` + `VRCanvasFollower.cs`）
- 检测 `XRSettings.isDeviceActive`，VR 模式下自动将所有 Canvas 切换为 **World Space**
- Canvas 参考尺寸设为 `1920×1080`，缩放 `0.001`，映射为约 1.92m × 1.08m 的空间面板
- 每个 Canvas 挂载 `VRCanvasFollower`：玩家转头超过 30° 后 Canvas 平滑跟随
- 界面激活（`Show()`/`SetVisible(true)`）时调用 `SnapToCamera()` 立即对齐到视野中央

#### 4. 投放判定（`DropZone.cs`）
新增 `OnTriggerEnter` 回调，支持垃圾飞入垃圾桶后物理触发判定（兼容 VR 投掷场景）

#### 5. 暂停系统（`WasteGameBootstrap.cs`）
- 左手柄 **X 键**（`CommonUsages.primaryButton`）呼出/关闭暂停菜单
- 使用上升沿检测（`currentPressed && !wasPressed`）避免连续触发

#### 6. TunnelingVignette 禁用
移动时的周边黑晕效果容易造成眩晕，在 `VRInteractionBootstrap.TryInstall()` 中运行时禁用

---

## 文档目录

| 文档 | 内容 |
|---|---|
| [产品需求文档 PRD](docs/PRD.md) | 功能范围、验收标准 |
| [项目结构大纲](docs/PROJECT_STRUCTURE.md) | 工程结构、模块划分 |
| [总体设计文档](docs/design/VR垃圾分类小游戏总体设计文档.md) | 完整体验设计背景 |
| [MVP 设计方案](docs/design/MVP设计方案.md) | 首个版本具体玩法取舍 |
| [AI 生成 3D 模型并导入 Unity](docs/使用AI生成3D模型并导入Unity.md) | 资产制作流程 |
| [v1 技术实现文档](docs/v1/) | 各游戏模式技术细节 |
