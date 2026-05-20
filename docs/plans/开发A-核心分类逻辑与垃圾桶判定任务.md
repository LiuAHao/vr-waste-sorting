# 开发 A 任务安排：核心分类逻辑与垃圾桶判定

## 1. 任务定位

开发 A 负责垃圾分类规则本身，包括：

- 垃圾物体的分类数据
- 垃圾桶的分类定义
- 垃圾进入投放区域后的正确/错误判定
- 对外广播分类结果

这一层不负责 UI、倒计时、结算，也不负责桌面端抓取方式。

## 2. 当前主干对应实现

当前 `main` 中已经对应到以下脚本：

- `Assets/Scripts/Gameplay/WasteCategory.cs`
- `Assets/Scripts/Gameplay/GarbageItem.cs`
- `Assets/Scripts/Gameplay/TrashBin.cs`
- `Assets/Scripts/Gameplay/DropZone.cs`
- `Assets/Scripts/Gameplay/ClassificationResult.cs`
- `Assets/Scripts/Gameplay/ClassificationEvents.cs`

## 3. 核心职责

### `GarbageItem`

至少负责：

- 记录 `itemId`
- 记录 `itemName`
- 记录正确分类 `category`
- 记录错误提示 `wrongReason`
- 记录初始位置，用于错误后复位
- 记录是否已完成、是否正在被抓取

### `TrashBin`

至少负责：

- 记录垃圾桶分类
- 提供显示名称
- 提供 `Accepts(GarbageItem item)` 这样的判断能力

### `DropZone`

至少负责：

- 监听垃圾进入投放区域
- 从碰撞体解析出 `GarbageItem`
- 调用垃圾桶判定
- 构造 `ClassificationResult`
- 通过 `ClassificationEvents` 广播结果

## 4. 当前正确的行为边界

必须满足：

1. 正确投放后，垃圾会被标记为已完成。
2. 已完成垃圾不能再被重复计分。
3. 错误投放时，不应把垃圾标记为完成。
4. 错误投放后，垃圾仍然允许重试。
5. 判定层不关心玩家是鼠标抓取还是 VR 手柄抓取。

不应该做：

- 不在 `DropZone` 里写 HUD 更新
- 不在 `GarbageItem` 里写结算逻辑
- 不在这一层直接重载场景

## 5. 当前需要继续关注的点

后续如果继续完善，这一层优先看：

1. 正确投放后的视觉终态是否要更明确。
2. `DropZone` 是否需要进一步避免“持有中误判”。
3. 是否需要统一异常掉落、出界复位逻辑。

## 6. 验收清单

- [ ] 每个垃圾都有分类和错误原因
- [ ] 每个垃圾桶都有明确分类
- [ ] 垃圾进入投放区后能正确判定
- [ ] 正确投放只计一次
- [ ] 错误投放不会直接完成目标
- [ ] 错误投放后垃圾可继续重试
