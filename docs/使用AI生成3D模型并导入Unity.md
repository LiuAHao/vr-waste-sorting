# 使用 AI 生成 3D 模型并导入 Unity

## 目标

这份文档面向当前项目组，说明如何把 AI 生成或外部下载的 3D 资源接入到 Unity 项目，并最终落到唯一主场景 `Assets/Scenes/2.unity`。

项目当前约束：
- 只保留一个主场景：`Assets/Scenes/2.unity`
- 新资源验收、摆放、交互调试都在 `2.unity` 中完成
- `Assets/Scenes/1.unity` 不再继续接入新内容

## 推荐资源格式

优先顺序：
1. `GLB`
2. `FBX`

原因：
- 当前项目推荐优先保证 `GLB` 导入链路可用，`GLB` 资源可以作为主流程继续使用。
- 如果个别模型导入异常，`FBX` 仍然是兼容性更高的兜底方案。

## 当前项目的导入方式

### 方案一：直接导入 `.glb`

适用条件：
- 当前 Unity 工程已经安装可用的 glTF 导入器
- 模型拖入 `Assets` 后能被 Unity 正常识别

操作步骤：
1. 把 `.glb` 文件放入项目资源目录，例如 `Assets/Art/GarbageItems` 或 `Assets/Art/TrashBins`
2. 等待 Unity 导入完成
3. 在 Project 视图中确认该资源可以展开或预览
4. 将资源直接拖入 `Assets/Scenes/2.unity` 测试

### 方案二：导入 `FBX`

适用条件：
- 某个 `GLB` 资源导入失败
- 材质、朝向或缩放不稳定

操作步骤：
1. 将 `FBX` 和相关贴图放进 `Assets` 目录
2. 等待 Unity 自动导入
3. 直接拖入 `2.unity` 验证显示效果

## 推荐接入流程

### 手动验证单个模型

1. 先把一个模型拖进 `2.unity`
2. 检查缩放是否正常
3. 检查朝向是否正确
4. 检查材质和贴图是否丢失
5. 检查模型底部是否贴地

### 批量接入现有垃圾分类资源

项目已经补了编辑器工具，推荐直接用工具完成批量接入：

1. 执行 `ParkClean/Content/Create Default Waste Content Catalog`
2. 打开 `ParkClean/Content/Waste Scene Builder`
3. 在窗口中点击“打开主场景 2.unity”
4. 点击“在主场景中批量构建内容”
5. 在 `2.unity` 中检查 12 个垃圾和 4 个垃圾桶的布局

## 导入后必须检查的内容

每个新资源至少检查以下几点：
- 缩放是否合理
- 朝向是否正确
- 材质与贴图是否正常
- 是否需要补 Collider
- 是否适合当前 VR/MVP 的性能预算

如果资源需要被抓取和投放，还要额外检查：
- 是否挂接了 `GarbageItem`
- 是否有 `Rigidbody`
- 是否有可用的 `Collider`

如果资源是垃圾桶，还要额外检查：
- 是否挂接了 `TrashBin`
- 是否存在 `DropZone`
- `DropZone` 是否配置为 Trigger

## 当前项目建议

对本项目来说，最稳的做法不是继续维护多个测试场景，而是：
- 所有新资源先在 `2.unity` 验收
- 用 `WasteContentCatalog` 维护资源路径和默认摆放信息
- 用 `WasteSceneBuilderWindow` 完成一键批量接入

这样可以避免场景分叉，也方便后续继续补玩法、得分、结算和验收流程。
