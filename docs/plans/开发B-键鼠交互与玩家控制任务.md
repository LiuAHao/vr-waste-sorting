# 开发 B 任务安排：键鼠交互与玩家控制

## 1. 任务定位

开发 B 负责桌面端可玩层，让玩家能够：

- 在场景中移动和观察
- 用鼠标选中垃圾
- 抓取、移动并释放垃圾
- 把垃圾送到垃圾桶投放区域

这一层不负责分类规则，也不负责结算统计。

## 2. 当前主干对应实现

当前 `main` 中已经对应到以下脚本：

- `Assets/Scripts/Player.cs`
- `Assets/Scripts/Interaction/DesktopGarbageInteractor.cs`
- `Assets/Scripts/Interaction/DesktopInteractionBootstrap.cs`
- `Assets/Scripts/Interaction/SelectableHighlighter.cs`
- `Assets/Scripts/Interaction/CrosshairController.cs`

## 3. 已实现目标

当前桌面端已经具备：

1. WASD 移动。
2. 鼠标控制视角。
3. 屏幕中心射线选中垃圾。
4. 悬停高亮。
5. 鼠标左键抓取垃圾。
6. 垃圾跟随到摄像机前方持有点。
7. 松开左键释放垃圾。
8. 已完成垃圾不会再次被抓取。

## 4. 正确的行为边界

这一层应该遵守：

- 只依赖 `GarbageItem.CanInteract()`、`SetHeld()` 等公开接口
- 不重写分类判断
- 不在交互脚本里计算得分和结算
- 不依赖具体中文物体名称来识别垃圾

## 5. 当前实现需要继续优化的点

### 优先级较高

1. 正确投放后的视觉终态还可以更明确。
2. `DesktopInteractionBootstrap` 仍然依赖运行时查找对象，适合后续再配置化。
3. 抓取距离、持有点、跟随速度虽然可调，但还需要结合场景再校准一次。

### 暂不阻塞 MVP

1. 不需要现在就改成完整输入系统。
2. 不需要现在就为 VR 手柄重写一套抓取层。

## 6. 验收清单

- [ ] 玩家可以正常移动和转向
- [ ] 鼠标中心能选中垃圾
- [ ] 悬停时有高亮反馈
- [ ] 左键能抓起和释放垃圾
- [ ] 抓取过程不会明显乱飞
- [ ] 错误投放后垃圾回到起点并可重试
- [ ] 正确投放后垃圾不再可交互
