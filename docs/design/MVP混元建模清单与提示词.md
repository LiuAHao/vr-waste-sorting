# ParkClean VR MVP 混元建模清单与提示词

## 1. 使用目标

本文档用于快速生成 V0.3 MVP 需要的模型资源。当前阶段优先满足“键盘鼠标可玩的 Unity 垃圾分类 Demo”，因此模型不追求精修，只要求：

- 一眼能识别是什么物品。
- 能在 Unity 中单独导入、摆放、缩放、加 Collider。
- 风格统一，适合低成本实时运行。
- 文件名、Prefab 名和数据 ID 稳定，方便开发接入。

V0.3 采用“资产先行，开发后接入”的节奏：先生成 12 个垃圾模型和 4 个垃圾桶模型，再交给开发做分类、抓取、投放和 UI。

## 2. 提示词格式

为了提高生成效率，所有提示词统一使用简单结构：

```text
主体：xxx。特征：xxx。风格：低多边形/半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，无真实品牌，低面数。
```

不要写太长，不要一次塞入太多需求。主体说清楚“是什么”，特征说清楚“为什么能被识别”，风格保持统一即可。

## 3. 通用风格要求

所有模型统一遵守：

- 风格：低多边形或半写实卡通。
- 色彩：明亮、干净、亲和。
- 用途：Unity 垃圾分类小游戏。
- 结构：单独模型，不要和场景或其他垃圾合并。
- 性能：低面数，适合实时运行。
- 品牌：不要真实 Logo、商标、药名、店名。
- 文字：不要把复杂中文直接做死在模型里，文字和标签后续在 Unity 中补。
- 比例：接近真实比例，小物件可适当放大，方便玩家抓取。

通用负面要求：

```text
不要真实品牌，不要超高面数，不要过度写实，不要复杂透明玻璃，不要过度脏污，不要把多个可交互物品合并成一个模型。
```

## 4. Unity 资源命名规范

### 4.1 原始模型文件命名

混元导出的模型文件统一使用英文小写加下划线：

```text
garbage_plastic_bottle.glb
bin_recyclable_blue.glb
scene_canteen_mvp.glb
```

命名规则：

```text
类型_主体_特征.glb
```

类型建议：

| 类型 | 用途 |
| --- | --- |
| `garbage` | 可分类垃圾 |
| `bin` | 垃圾桶 |
| `scene` | 场景 |
| `ui` | UI 面板类模型 |
| `fx` | 反馈效果模型 |

### 4.2 Unity Prefab 命名

导入 Unity 后，Prefab 使用 PascalCase，并加 `PF_` 前缀：

```text
PF_Garbage_PlasticBottle
PF_Bin_RecyclableBlue
PF_Scene_CanteenMvp
```

### 4.3 开发数据 ID 命名

开发配置中的 `itemId` 使用英文小写加下划线，和原始模型文件名保持一致：

```text
garbage_plastic_bottle
garbage_cardboard_box
garbage_milk_tea_cup
```

注意：

- 文件名、Prefab 名、数据 ID 一旦交给开发，尽量不要再改。
- 如果模型重生成，保持同名替换。
- 不要使用中文文件名作为开发资源名。

### 4.4 推荐导入目录

原始生成文件可先放：

```text
3D-Model/V0.3/
```

导入 Unity 后建议整理为：

```text
Assets/Art/GarbageItems/
Assets/Art/TrashBins/
Assets/Art/Environment/
Assets/Art/UI/
Assets/Art/FX/
Assets/Prefabs/GarbageItems/
Assets/Prefabs/TrashBins/
Assets/Prefabs/Environment/
```

如果当前项目暂时没有这些目录，可以先创建；不要把所有新模型继续散放在根目录。

## 5. MVP 必需资产清单

| 类型 | 数量 | 说明 |
| --- | --- | --- |
| 场景 | 1 | 校园食堂局部场景 |
| 垃圾桶 | 4 | 可回收物、有害垃圾、厨余垃圾、其他垃圾 |
| 可回收物 | 3 | 塑料瓶、纸箱、易拉罐 |
| 厨余垃圾 | 3 | 剩饭、果皮、菜叶 |
| 有害垃圾 | 3 | 旧电池、过期药品、灯管 |
| 其他垃圾 | 3 | 污染纸巾、奶茶杯、油污外卖盒 |

基础接入优先级：

- 先接入 8 个：塑料瓶、纸箱、剩饭、果皮、旧电池、过期药品、污染纸巾、奶茶杯。
- 时间充足再补齐 4 个：易拉罐、菜叶、灯管、油污外卖盒。

## 6. 场景模型

| 资源名 | Unity Prefab | 主体 | 特征 | 风格提示词 |
| --- | --- | --- | --- | --- |
| `scene_canteen_mvp.glb` | `PF_Scene_CanteenMvp` | 校园食堂局部场景 | 1-2 张餐桌、椅子、浅色地砖、食堂窗口、菜单牌留白、预留垃圾和垃圾桶摆放区 | 主体：校园食堂局部场景。特征：餐桌、椅子、地砖、食堂窗口、菜单牌留白，前方预留玩家操作区。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，低面数。 |

注意：

- 不要把 12 个任务垃圾做死在场景里。
- 场景范围不要过大，玩家小范围移动即可完成任务。
- 菜单牌和提示牌留白，后续在 Unity 中加文字。

## 7. 垃圾桶模型

垃圾桶必须分开生成，不要生成四个合并模型。每个桶需要明显桶口，方便开发添加 DropZone。

| 资源名 | Unity Prefab | 类型 | 主体 | 特征 | 风格提示词 |
| --- | --- | --- | --- | --- | --- |
| `bin_recyclable_blue.glb` | `PF_Bin_RecyclableBlue` | 可回收物 | 蓝色分类垃圾桶 | 蓝色桶身、明显桶口、正面标签区域、循环箭头区域 | 主体：蓝色可回收物分类垃圾桶。特征：明显桶口，正面大标签区域，循环箭头图标区域。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `bin_hazardous_red.glb` | `PF_Bin_HazardousRed` | 有害垃圾 | 红色分类垃圾桶 | 红色桶身、明显桶口、正面标签区域、警示图标区域 | 主体：红色有害垃圾分类垃圾桶。特征：明显桶口，正面大标签区域，警示图标区域。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `bin_kitchen_green.glb` | `PF_Bin_KitchenGreen` | 厨余垃圾 | 绿色分类垃圾桶 | 绿色桶身、明显桶口、正面标签区域、叶片或食物图标区域 | 主体：绿色厨余垃圾分类垃圾桶。特征：明显桶口，正面大标签区域，叶片或食物图标区域。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `bin_other_gray.glb` | `PF_Bin_OtherGray` | 其他垃圾 | 灰色分类垃圾桶 | 灰色桶身、明显桶口、正面标签区域、普通垃圾图标区域 | 主体：灰色其他垃圾分类垃圾桶。特征：明显桶口，正面大标签区域，普通垃圾桶图标区域。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |

## 8. 垃圾模型

### 8.1 可回收物

| 资源名 | Unity Prefab | 数据 ID | 主体 | 特征 | 简短提示词 |
| --- | --- | --- | --- | --- | --- |
| `garbage_plastic_bottle.glb` | `PF_Garbage_PlasticBottle` | `garbage_plastic_bottle` | 塑料瓶 | 透明或浅蓝瓶身、瓶盖、无品牌标签、干净 | 主体：塑料矿泉水瓶。特征：透明瓶身、浅蓝瓶盖、无品牌标签、干净可回收。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_cardboard_box.glb` | `PF_Garbage_CardboardBox` | `garbage_cardboard_box` | 小纸箱 | 棕色纸板、胶带、折痕、干燥干净 | 主体：小型快递纸箱。特征：棕色纸板、封箱胶带、折痕、干燥干净。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_aluminum_can.glb` | `PF_Garbage_AluminumCan` | `garbage_aluminum_can` | 易拉罐 | 金属罐身、顶部拉环、轻微压扁、无品牌 | 主体：饮料易拉罐。特征：金属罐身、顶部拉环、轻微压扁、无品牌。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |

### 8.2 厨余垃圾

| 资源名 | Unity Prefab | 数据 ID | 主体 | 特征 | 简短提示词 |
| --- | --- | --- | --- | --- | --- |
| `garbage_leftover_rice.glb` | `PF_Garbage_LeftoverRice` | `garbage_leftover_rice` | 剩饭 | 小碗或餐盘、米饭残留、少量菜叶碎片 | 主体：餐盘中的剩饭。特征：米饭残留、少量菜叶碎片、小碗或餐盘承托。风格：低多边形或半写实卡通，明亮干净不过度脏污，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_fruit_peel.glb` | `PF_Garbage_FruitPeel` | `garbage_fruit_peel` | 果皮 | 香蕉皮为主、少量苹果皮、颜色鲜明、小堆整体 | 主体：一小堆果皮。特征：香蕉皮为主，少量苹果皮，颜色鲜明，做成一个可抓取整体。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_vegetable_leaf.glb` | `PF_Garbage_VegetableLeaf` | `garbage_vegetable_leaf` | 菜叶 | 几片绿色叶片、简单叶脉、小团整体 | 主体：一小团绿色菜叶。特征：几片绿色叶片，简单叶脉，略微卷曲，做成一个可抓取整体。风格：低多边形或半写实卡通，明亮干净，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |

### 8.3 有害垃圾

| 资源名 | Unity Prefab | 数据 ID | 主体 | 特征 | 简短提示词 |
| --- | --- | --- | --- | --- | --- |
| `garbage_battery.glb` | `PF_Garbage_Battery` | `garbage_battery` | 旧电池 | 一到两节圆柱电池、正负极明显、警示色块、无品牌 | 主体：旧电池。特征：一到两节圆柱电池，正负极明显，带警示色块，无品牌。风格：低多边形或半写实卡通，明亮清晰，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_expired_medicine.glb` | `PF_Garbage_ExpiredMedicine` | `garbage_expired_medicine` | 过期药品 | 小药盒、小药瓶、医疗十字或药品图标、无真实药名 | 主体：过期药品组合。特征：小药盒和小药瓶，医疗十字或药品图标，无真实药名和品牌。风格：低多边形或半写实卡通，明亮清晰，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_lamp_tube.glb` | `PF_Garbage_LampTube` | `garbage_lamp_tube` | 废旧灯管 | 短款灯管、两端金属接口、不破碎、玻璃不太透明 | 主体：废旧短款灯管。特征：浅色灯管，两端金属接口，不破碎，玻璃不太透明。风格：低多边形或半写实卡通，明亮清晰，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |

### 8.4 其他垃圾

| 资源名 | Unity Prefab | 数据 ID | 主体 | 特征 | 简短提示词 |
| --- | --- | --- | --- | --- | --- |
| `garbage_dirty_tissue.glb` | `PF_Garbage_DirtyTissue` | `garbage_dirty_tissue` | 污染纸巾 | 皱起纸团、油渍或水渍、浅色、有体积 | 主体：污染纸巾纸团。特征：皱起的浅色纸团，带油渍或水渍，有体积。风格：低多边形或半写实卡通，不过度脏污，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_milk_tea_cup.glb` | `PF_Garbage_MilkTeaCup` | `garbage_milk_tea_cup` | 奶茶杯 | 杯身、杯盖、吸管、底部残留液体、无品牌 | 主体：喝完的奶茶杯。特征：杯身、杯盖、吸管、底部残留液体，无品牌。风格：低多边形或半写实卡通，明亮清晰，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |
| `garbage_oily_takeout_box.glb` | `PF_Garbage_OilyTakeoutBox` | `garbage_oily_takeout_box` | 油污外卖盒 | 半开餐盒、油渍、酱汁痕迹、少量食物残留、无品牌 | 主体：油污外卖餐盒。特征：半开餐盒，内部有油渍、酱汁痕迹和少量食物残留，无品牌。风格：低多边形或半写实卡通，不过度脏污，适合 Unity 垃圾分类小游戏，单独模型，低面数。 |

## 9. 可选 UI 与反馈模型

如果时间紧，这部分可以不生成，直接用 Unity UI 和简单材质替代。

| 资源名 | Unity Prefab | 主体 | 特征 | 简短提示词 |
| --- | --- | --- | --- | --- |
| `ui_task_panel.glb` | `PF_UI_TaskPanel` | 任务提示面板 | 浅色圆角面板、留白、简洁边框 | 主体：任务提示面板。特征：浅色圆角面板，大面积留白，简洁边框。风格：低多边形或半写实卡通，适合 Unity 游戏 UI，不包含具体文字，低面数。 |
| `ui_result_panel.glb` | `PF_UI_ResultPanel` | 结算面板 | 标题区、数据区、列表区、按钮底板留白 | 主体：结算面板。特征：标题区、数据区、错误列表区、按钮底板留白。风格：低多边形或半写实卡通，适合 Unity 游戏 UI，不包含具体文字，低面数。 |
| `fx_select_ring.glb` | `PF_FX_SelectRing` | 选中光圈 | 浅蓝或浅绿圆环、半透明感、轻量 | 主体：选中光圈。特征：浅蓝或浅绿圆环，半透明感，放在物品底部。风格：低多边形，适合 Unity 交互反馈，单独模型，低面数。 |
| `ui_guide_arrow.glb` | `PF_UI_GuideArrow` | 引导箭头 | 浅蓝或绿色、方向明确、可悬浮 | 主体：引导箭头。特征：浅蓝或绿色，方向明确，可悬浮指向物品。风格：低多边形，适合 Unity 新手引导，单独模型，低面数。 |
| `fx_correct_ring.glb` | `PF_FX_CorrectRing` | 正确反馈环 | 绿色圆环、轻量、桶口附近闪烁 | 主体：正确反馈光环。特征：绿色圆环，轻量，适合放在垃圾桶桶口附近。风格：低多边形，适合 Unity 反馈效果，单独模型，低面数。 |
| `fx_wrong_ring.glb` | `PF_FX_WrongRing` | 错误反馈环 | 黄色或橙色圆环、轻量、桶口附近闪烁 | 主体：错误反馈光环。特征：黄色或橙色圆环，轻量，适合放在垃圾桶桶口附近。风格：低多边形，适合 Unity 反馈效果，单独模型，低面数。 |

## 10. 生成顺序

推荐按以下顺序生成：

1. 四个垃圾桶。
2. 12 个垃圾物品。
3. 校园食堂局部场景。
4. 可选 UI 和反馈模型。

原因：

- V0.3 开发最依赖垃圾和垃圾桶。
- 场景可以先用现有城市/社区资源或简单桌子占位。
- UI/反馈可以先用 Unity 自带 UI 替代。

## 11. 导入 Unity 前检查标准

每个模型交给开发前检查：

- 文件名是否符合资源命名规范。
- 模型主体是否一眼可识别。
- 是否没有真实品牌、商标、真实药名。
- 是否是单独模型，没有和其他可交互物体合并。
- 模型比例是否可接受，导入 Unity 后能快速缩放。
- 是否方便添加 Collider。
- 是否适合低面数实时运行。
- 易混淆物是否有关键特征：奶茶杯有吸管和残留液体，外卖盒有油污，污染纸巾有污渍。

## 12. 导入 Unity 后处理规范

每个垃圾模型导入后建议处理：

1. 放入 `Assets/Art/GarbageItems/`。
2. 创建对应 Prefab，放入 `Assets/Prefabs/GarbageItems/`。
3. Prefab 添加 Collider。
4. Prefab 添加 Rigidbody。
5. Prefab 添加开发 A 的 `GarbageItem` 组件。
6. 按数据表填写 `itemId`、`itemName`、`category`、`wrongReason`。

每个垃圾桶模型导入后建议处理：

1. 放入 `Assets/Art/TrashBins/`。
2. 创建对应 Prefab，放入 `Assets/Prefabs/TrashBins/`。
3. Prefab 添加 `TrashBin` 组件。
4. 在桶口或桶前方创建子物体 `DropZone`。
5. `DropZone` 添加 Trigger Collider。
6. `DropZone` 绑定对应 `TrashBin`。

## 13. V0.3 数据表对应关系

| 数据 ID | 中文名 | 分类 | 原始文件名 | Prefab 名 |
| --- | --- | --- | --- | --- |
| `garbage_plastic_bottle` | 塑料瓶 | Recyclable | `garbage_plastic_bottle.glb` | `PF_Garbage_PlasticBottle` |
| `garbage_cardboard_box` | 纸箱 | Recyclable | `garbage_cardboard_box.glb` | `PF_Garbage_CardboardBox` |
| `garbage_aluminum_can` | 易拉罐 | Recyclable | `garbage_aluminum_can.glb` | `PF_Garbage_AluminumCan` |
| `garbage_leftover_rice` | 剩饭 | Kitchen | `garbage_leftover_rice.glb` | `PF_Garbage_LeftoverRice` |
| `garbage_fruit_peel` | 果皮 | Kitchen | `garbage_fruit_peel.glb` | `PF_Garbage_FruitPeel` |
| `garbage_vegetable_leaf` | 菜叶 | Kitchen | `garbage_vegetable_leaf.glb` | `PF_Garbage_VegetableLeaf` |
| `garbage_battery` | 旧电池 | Hazardous | `garbage_battery.glb` | `PF_Garbage_Battery` |
| `garbage_expired_medicine` | 过期药品 | Hazardous | `garbage_expired_medicine.glb` | `PF_Garbage_ExpiredMedicine` |
| `garbage_lamp_tube` | 灯管 | Hazardous | `garbage_lamp_tube.glb` | `PF_Garbage_LampTube` |
| `garbage_dirty_tissue` | 污染纸巾 | Other | `garbage_dirty_tissue.glb` | `PF_Garbage_DirtyTissue` |
| `garbage_milk_tea_cup` | 奶茶杯 | Other | `garbage_milk_tea_cup.glb` | `PF_Garbage_MilkTeaCup` |
| `garbage_oily_takeout_box` | 油污外卖盒 | Other | `garbage_oily_takeout_box.glb` | `PF_Garbage_OilyTakeoutBox` |

## 14. 当前资产落位状态

已将当前 `3D-Model/` 中已有模型按规范命名并移入 Unity 资源目录：

### 14.1 已落位垃圾模型

目录：`Assets/Art/GarbageItems/`

| 中文名 | 当前文件 |
| --- | --- |
| 塑料瓶 | `garbage_plastic_bottle.glb` |
| 纸箱 | `garbage_cardboard_box.glb` |
| 易拉罐 | `garbage_aluminum_can.glb` |
| 剩饭 | `garbage_leftover_rice.glb` |
| 果皮 | `garbage_fruit_peel.glb` |
| 菜叶 | `garbage_vegetable_leaf.glb` |
| 旧电池 | `garbage_battery.glb` |
| 过期药品 | `garbage_expired_medicine.glb` |
| 灯管 | `garbage_lamp_tube.glb` |
| 污染纸巾 | `garbage_dirty_tissue.glb` |
| 奶茶杯 | `garbage_milk_tea_cup.glb` |
| 油污外卖盒 | `garbage_oily_takeout_box.glb` |

### 14.2 已落位垃圾桶模型

目录：`Assets/Art/TrashBins/`

| 中文名 | 当前文件 |
| --- | --- |
| 可回收垃圾桶 | `bin_recyclable_blue.glb` |
| 有害垃圾桶 | `bin_hazardous_red.glb` |
| 厨余垃圾桶 | `bin_kitchen_green.glb` |
| 其他垃圾桶 | `bin_other_gray.glb` |

参考图：

```text
Assets/Art/TrashBins/References/trash_bin_reference.webp
```
