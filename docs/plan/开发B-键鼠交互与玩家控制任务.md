# 开发 B 任务安排：键鼠交互与玩家控制

## 1. 任务定位

开发 B 负责 V0.3 MVP 的“键盘鼠标可玩层”。本模块让玩家能够进入 Unity 场景，用 WASD 和鼠标观察环境，用鼠标选中、抓取、移动、释放垃圾，并把垃圾送到垃圾桶触发区域。

本任务的重点是稳定可玩，不是追求真实投掷物理。V0.3 先实现桌面端交互，后续 VR 适配人员只需要替换输入层，分类逻辑和任务逻辑可以继续复用。

## 2. 前置条件

开发正式开始前，最好已经具备：

- 统筹侧交付的第一批垃圾模型和垃圾桶模型。
- 开发 A 提供的 `GarbageItem`、`DropZone`、`TrashBin` 基础脚本。
- 一个可用于测试的场景，例如 MVP 食堂或社区投放点场景。

如果开发 A 尚未完成，可以先用临时接口测试抓取，但最终必须接回 `GarbageItem.CanInteract()` 和 `GarbageItem.SetHeld(bool held)`。

## 3. 需要达成的目标

### 必须完成

1. 玩家可以用 WASD 在场景中移动。
2. 玩家可以用鼠标控制视角。
3. 鼠标准星或屏幕中心射线可以选中垃圾。
4. 被选中的垃圾有明显高亮或提示。
5. 鼠标左键可以抓取垃圾。
6. 抓取后垃圾稳定跟随摄像机前方或射线目标点。
7. 松开鼠标左键可以释放垃圾。
8. 释放后的垃圾可以进入垃圾桶触发区，交给开发 A 的分类逻辑判断。
9. 已正确分类的垃圾不能再被抓取。
10. 投错并复位后的垃圾可以再次抓取。

### 尽量完成

- 屏幕中心加一个简单准星。
- 抓取距离、持有距离、移动速度可以在 Inspector 中调整。
- 抓取时暂时关闭重力或锁定旋转，减少乱飞。
- 当准星指向可抓取垃圾时显示物品名称。

### 不做

- 不做垃圾分类判断。
- 不做正确率和结算统计。
- 不做复杂 UI。
- 不做 VR 手柄输入。
- 不做真实抛物线投掷。
- 不做多场景切换。

## 4. 建议代码修改区域

只新增或修改以下区域：

```text
Assets/Scripts/Player/
Assets/Scripts/Interaction/
```

建议新增文件：

```text
Assets/Scripts/Player/PlayerController.cs
Assets/Scripts/Interaction/DesktopGarbageInteractor.cs
Assets/Scripts/Interaction/SelectableHighlighter.cs
Assets/Scripts/Interaction/CrosshairController.cs
```

旧文件 `Assets/Scripts/Player.cs` 可以作为参考，但不要继续把所有新逻辑都堆在这个脚本里。V0.3 新逻辑建议拆分到新目录。

## 5. 建议实施步骤

### 第 1 步：整理玩家控制

创建 `PlayerController.cs`，负责键盘移动和鼠标视角。

建议字段：

```text
float moveSpeed
float lookSpeed
float minPitch
float maxPitch
Transform cameraRoot
```

实现要点：

- WASD 控制水平移动。
- 鼠标 X 控制玩家水平旋转。
- 鼠标 Y 控制摄像机俯仰，限制角度，例如 -45 到 45 度。
- 保持桌面端可调试，不要求 VR 设备。
- 暂时可以不做跳跃，除非场景确实需要。

注意：

- 如果沿用旧 `Player.cs`，要避免它继续承担倒计时、胜负面板和垃圾计数。
- 玩家控制只负责移动和视角，不直接管理任务进度。

### 第 2 步：实现鼠标射线选中

创建 `DesktopGarbageInteractor.cs`，挂在玩家或摄像机上。

建议字段：

```text
Camera playerCamera
float interactDistance
LayerMask interactMask
GarbageItem currentHover
GarbageItem heldItem
Transform holdPoint
```

逻辑：

1. 每帧从摄像机中心发射 Raycast。
2. 命中带 `GarbageItem` 的物体时，判断 `CanInteract()`。
3. 更新当前 hover 目标。
4. 通知 `SelectableHighlighter` 开启或关闭高亮。

开发规范：

- Raycast 层级要和开发 A 协商，避免射到环境物体后无法选中垃圾。
- 不要用物体名称判断是否垃圾，必须依赖 `GarbageItem` 组件。
- 不要在选中逻辑里做分类判断。

### 第 3 步：实现抓取和持有

推荐交互方式：

- 鼠标左键按下：抓取当前选中的垃圾。
- 鼠标左键按住：垃圾跟随 `holdPoint`。
- 鼠标左键松开：释放垃圾。

`holdPoint` 可以是摄像机前方一个空物体，距离 1.5-2.5 米。这样实现简单稳定，也更接近后续 VR 射线抓取。

抓取时建议：

- 调用 `GarbageItem.SetHeld(true)`。
- 记录垃圾原始 Rigidbody 状态。
- 临时关闭重力或设置 `isKinematic = true`。
- 让垃圾位置平滑跟随 `holdPoint`。

释放时建议：

- 调用 `GarbageItem.SetHeld(false)`。
- 恢复 Rigidbody 状态。
- 给垃圾一个很小的速度或直接静止。
- 由 DropZone 判断是否进入垃圾桶。

注意：

- V0.3 不做甩手抛掷。
- 抓取时不要让垃圾离玩家太远。
- 释放后如果垃圾掉出场景，可由开发 A 的复位逻辑处理，或加一个简单跌落复位区。

### 第 4 步：实现高亮提示

创建 `SelectableHighlighter.cs`，挂在垃圾物体或模型子物体上。

可选实现方式：

- 替换材质颜色。
- 开启一个描边组件。
- 开启一个高亮子物体。
- 简单改变 emission。

要求：

- 当前 hover 的垃圾能明显看出被选中。
- 离开 hover 或抓取其他垃圾时，高亮能关闭。
- 已完成垃圾不再高亮。

### 第 5 步：和分类逻辑联调

联调目标：

1. 玩家抓起垃圾。
2. 移动到垃圾桶前。
3. 松开垃圾。
4. 垃圾进入 `DropZone`。
5. 开发 A 的分类事件触发。
6. 开发 C 的 UI 能收到结果。

如果垃圾难以进入 Trigger，可以临时调整：

- 增大 DropZone 的 Collider。
- 增大垃圾 Collider。
- 释放时稍微降低垃圾位置。
- 释放后保持 Rigidbody 开启。

### 第 6 步：处理完成和错误状态

开发 A 的 `GarbageItem` 应提供状态。开发 B 需要遵守：

- `Completed` 垃圾不能抓取。
- `Held` 垃圾不应被 DropZone 立即判定，除非设计为“拿着碰到桶就算投放”。
- 错误复位后的垃圾重新进入 `Idle`，可以再次抓取。

建议 V0.3 采用：

> 松开垃圾后进入 DropZone 才算投放。

这样更稳定，也减少“拿着垃圾经过桶口就误判”的问题。

## 6. 与其他开发的接口

### 依赖开发 A

需要使用：

```text
GarbageItem.CanInteract()
GarbageItem.SetHeld(bool held)
GarbageItem.ResetToStartPosition()
GarbageItem.IsCompleted
```

不应修改：

```text
GarbageItem.Category
GarbageItem.WrongReason
TrashBin.Category
```

这些值应通过开发 A 提供的公开属性读取，不要直接改内部字段。

### 依赖开发 C

可选使用：

```text
HUDController.ShowHoverItemName(itemName)
HUDController.ClearHoverItemName()
```

如果开发 C 暂时没做物品名提示，开发 B 不应阻塞，可以先只做高亮。

### 提供给开发 C

开发 B 不直接提供统计数据。分类结果由开发 A 的事件给开发 C。开发 B 只保证玩家能把垃圾送入 DropZone。

## 7. 开发规范

### 代码规范

- 输入逻辑放在 `Interaction` 或 `Player` 模块，不写进 Gameplay。
- 不用中文作为类名、方法名、字段名。
- 可配置参数使用 `[SerializeField]` 暴露到 Inspector。
- 不在 `Update()` 中频繁 `FindObjectOfType`。
- 不通过物体名称判断垃圾类型。
- 不在交互脚本里写 UI 结算和分类规则。

### 操作体验规范

- 抓取距离不要太短，避免玩家频繁低头。
- 持有点位置要稳定，垃圾不要抖动。
- 鼠标灵敏度可调。
- 尽量避免玩家卡进桌子、垃圾桶或墙体。
- 如果场景较小，移动速度不要太快。

### 物理规范

- 抓取时可以临时 `isKinematic = true`。
- 释放时恢复 Rigidbody，但不要给过大速度。
- 需要 Collider 才能 Raycast 选中。
- 需要合理 Rigidbody/Collider 才能触发 DropZone。

## 8. 验收清单

开发 B 完成后，需要能通过以下检查：

- [ ] 玩家可用 WASD 移动。
- [ ] 玩家可用鼠标看向垃圾和垃圾桶。
- [ ] 屏幕中心射线能选中垃圾。
- [ ] 选中垃圾有高亮或明确提示。
- [ ] 鼠标左键能抓取垃圾。
- [ ] 抓取时垃圾稳定跟随玩家视野前方。
- [ ] 松开鼠标后垃圾能释放。
- [ ] 垃圾能进入垃圾桶触发区。
- [ ] 投错后的垃圾仍可再次抓取。
- [ ] 投对后的垃圾不能重复抓取。
- [ ] 交互脚本没有写死任何垃圾分类规则。
- [ ] 不依赖 VR 设备即可完整测试。

## 9. 常见风险

| 风险 | 处理方式 |
| --- | --- |
| Raycast 选不中垃圾 | 检查 Collider、LayerMask、摄像机引用、交互距离 |
| 抓取后垃圾抖动 | 抓取时设为 kinematic，用 holdPoint 控制位置 |
| 垃圾释放后乱飞 | 释放时清空速度或减小 Rigidbody 速度 |
| 拿着垃圾经过桶就误判 | DropZone 忽略 Held 状态，要求释放后判定 |
| 垃圾卡进场景 | 加复位键或让开发 A 的错误复位处理异常位置 |
