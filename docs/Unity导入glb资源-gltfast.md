# Unity 导入 `.glb` 资源方案 1：通过 Package Manager 安装 glTFast

## 1. 适用场景

当前项目已经把垃圾和垃圾桶模型放进仓库：

- `Assets/Art/GarbageItems/`
- `Assets/Art/TrashBins/`

这些资源的格式是 `.glb`。  
Unity 2020.3 默认**不能直接把 `.glb` 当成可实例化模型资源使用**，因此项目需要先安装 glTF 导入器。

本方案使用 `glTFast`，适合：

- 希望继续保留 `.glb` 原始资源
- 不想手动把每个模型转成 `.fbx`
- 希望后续由 Unity 直接识别 `.glb`

## 2. 目标

完成本方案后，Unity 应具备以下能力：

1. 能识别 `Assets/Art/.../*.glb`
2. 能在 Inspector 中正确显示模型资源内容
3. 能把 `.glb` 拖入场景
4. 能配合项目中的内容接入工具，批量生成垃圾和垃圾桶对象

## 3. 操作步骤

### 第一步：打开 Package Manager

在 Unity 顶部菜单中打开：

```text
Window > Package Manager
```

### 第二步：通过 Git URL 添加包

在 Package Manager 左上角点击：

```text
+
```

然后选择：

```text
Add package from git URL...
```

### 第三步：输入 glTFast 仓库地址

输入以下地址：

```text
https://github.com/atteneder/glTFast.git
```

点击确认，等待 Unity 下载并导入。

## 4. 安装成功后的验证方式

安装完成后，请按下面方式验证：

### 验证 1：Unity 是否完成重新编译

观察 Editor：

- Console 没有新的编译错误
- 右下角导入进度结束

### 验证 2：检查 `.glb` 是否被正确识别

在 Project 面板中点击任意一个 `.glb` 文件，例如：

```text
Assets/Art/GarbageItems/garbage_plastic_bottle.glb
```

如果导入成功，通常会出现以下特征：

- 不再只是单纯 `DefaultImporter`
- Inspector 中会出现和 glTF 模型相关的导入信息
- 资源可展开或可被实例化

### 验证 3：尝试把 `.glb` 拖到场景里

将一个 `.glb` 直接拖到 Hierarchy 或 Scene 视图：

- 如果能生成可见模型对象，说明导入成功
- 如果仍然不能实例化，说明导入器尚未生效或项目需要重新导入资源

## 5. 导入成功后的项目内下一步

项目里已经预留了后续接入工具：

- `ParkClean/Content/Create Default Waste Content Catalog`
- `ParkClean/Content/Waste Scene Builder`

建议顺序：

1. 先完成 glTFast 安装
2. 验证 `.glb` 可被 Unity 识别
3. 执行：

```text
ParkClean/Content/Create Default Waste Content Catalog
```

4. 再执行：

```text
ParkClean/Content/Waste Scene Builder
```

5. 选择生成出来的 `WasteContentCatalog`
6. 点击“构建当前场景”

这样可以批量把 12 个垃圾和 4 个垃圾桶接入场景，并自动挂基础组件。

## 6. 常见问题

### 问题 1：Package Manager 无法联网

如果出现类似错误：

```text
Cannot connect to 'packages.unity.com'
ECONNRESET
```

说明当前网络环境可能有限制。注意：

- 这个报错是 Package Manager 联网层问题
- 但 `Add package from git URL` 和 Unity 官方源不是完全同一条链路
- 可以先实际试一次 Git URL 导入，不要只根据 `packages.unity.com` 报错就直接放弃

### 问题 2：Git URL 导入失败

可能原因：

- 当前网络无法访问 GitHub
- 公司/校园网拦截
- 本机代理未配置

如果失败，建议把完整报错保留并同步给项目同学，再切换到“手动下载包本地接入”的备用方案。

### 问题 3：安装后 `.glb` 仍然不能正常拖入场景

可依次尝试：

1. 重新打开 Unity
2. 右键资源目录，执行 Reimport
3. 检查 Console 是否有编译错误
4. 检查 glTFast 是否真的出现在 Package Manager 已安装列表中

## 7. 团队协作建议

为了避免每个组员各自摸索，建议统一约定：

1. 先由一位同学验证本方案在当前 Unity 版本下可行
2. 验证成功后，其他组员按同样步骤安装
3. 所有人统一使用同一种 glTF 导入方案
4. 不要有人直接改成 `.fbx`，有人继续用 `.glb`，避免资产流程分叉

## 8. 当前结论

当前项目的 `.glb` 资源并不缺，真正缺的是 Unity 对 `.glb` 的导入支持。  
方案 1 的目标不是改玩法逻辑，而是先打通“资源进入 Unity 并可实例化”的第一步。
