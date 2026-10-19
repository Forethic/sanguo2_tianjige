# 任务清单：天机阁实施路线与任务追踪 (终版修订案)

## 阶段一：古风骨架与静态典籍

### Task 1.1: 基础工程架构搭建
- **Description:** 创建 .NET 解决方案体系，包含核心领域库 `Tianjige.Core`、数据解析库 `Tianjige.Data`、表现层 `Tianjige.UI` (WPF) 以及单元测试工程 `Tianjige.Tests`。
- **Acceptance criteria:**
  - [x] 解决方案 `Tianjige.slnx` 包含四个工程且项目依赖关系清晰
  - [x] 启用 Nullable 检查与 C# 13 最新语言特性
  - [x] `dotnet build` 编译 0 警告 0 错误
- **Verification:**
  - [x] Build succeeds: `dotnet build` (0 warnings, 0 errors)
  - [x] Test succeeds: `dotnet test` (Passed: 1, Failed: 0)
- **Dependencies:** None
- **Files likely touched:**
  - `Tianjige.sln`
  - `src/Tianjige.Core/Tianjige.Core.csproj`
  - `src/Tianjige.Data/Tianjige.Data.csproj`
  - `src/Tianjige.UI/Tianjige.UI.csproj`
  - `tests/Tianjige.Tests/Tianjige.Tests.csproj`
- **Estimated scope:** Medium (3-5 files)

---

### Task 1.2: 古风设计系统与 XAML 样式资源库
- **Description:** 构建汉末五色体系与古典交互视觉规范，包括绢帛沉木底色（`#1F1A16` 至 `#2B241E` 渐变）、简帛素米文本（`#E6DAC0`）、朱砂赤红印章（`#A63428`）、砂金古铜边框（`#C8A355`）、隶书/华文中宋排版规范以及仿古朱文印章自定义控件。
- **Acceptance criteria:**
  - [x] 颜色资源字典 `Colors.xaml` 完整定义规范色板（汉末五色、沉木绢帛渐变）
  - [x] 字体与排印样式字典 `Typography.xaml` 涵盖标题隶书与属性数值规范
  - [x] 自定义朱文印章控件 `SealBadge` 支持【无双】【良将】【谋主】【裨将】【白身】五级印泥质感
  - [x] 古风卷轴滚动条、搜索框与分割线控件模板 `Controls.xaml`
- **Verification:**
  - [x] 样式资源成功加载并在 App.xaml 中完成合并，XAML 编译 0 警告 0 错误
  - [x] `dotnet build` 编译成功，`dotnet test` 通过
- **Dependencies:** Task 1.1
- **Files likely touched:**
  - `src/Tianjige.UI/Themes/Colors.xaml`
  - `src/Tianjige.UI/Themes/Typography.xaml`
  - `src/Tianjige.UI/Themes/Controls.xaml`
  - `src/Tianjige.UI/Controls/SealBadge.xaml`
  - `src/Tianjige.UI/Controls/SealBadge.xaml.cs`
- **Estimated scope:** Medium (4-5 files)

---

### Task 1.3: 静态典籍数据构建与序列化模型
- **Description:** 编写并内置《三国群英传2》原生静态数据字典，包括 `Skills.json`（全武将技与军师技消耗及客观效果）、`Items.json`（神兵宝物加成属性）、`Formations.json`（八门阵型与兵种相克矩阵）及典型名将全周期技能树种子数据。
- **Acceptance criteria:**
  - [x] 静态实体模型支持 GeneralStaticInfo, SkillInfo, ItemInfo, FormationInfo, TroopCounterInfo
  - [x] `Skills.json` 覆盖核心武将技（如鬼哭神号、烈火旋灯、神剑闪华等）与军师技（如八门金锁、恢复技力高）消耗与客观释义
  - [x] `Items.json` 覆盖方天画戟、青龙偃月刀、赤兔马、兵法二十四篇等核心宝物
  - [x] `Formations.json` 准确记录 8 种阵型站位与兵种互克数据
  - [x] 嵌入式资源装载器 `StaticRepository`，冷启动毫秒级解析加载
- **Verification:**
  - [x] 单元测试 `StaticDataTests` 验证 JSON 数据反序列化完整且字段无缺失 (5 tests passed)
- **Dependencies:** Task 1.1
- **Files likely touched:**
  - `src/Tianjige.Core/Models/GeneralStaticInfo.cs`
  - `src/Tianjige.Core/Models/SkillInfo.cs`
  - `src/Tianjige.Core/Models/ItemInfo.cs`
  - `src/Tianjige.Core/Models/FormationInfo.cs`
  - `src/Tianjige.Data/Resources/Skills.json`
  - `src/Tianjige.Data/Resources/Items.json`
  - `src/Tianjige.Data/Resources/Formations.json`
  - `src/Tianjige.Data/Repositories/StaticRepository.cs`
  - `tests/Tianjige.Tests/StaticDataTests.cs`
- **Estimated scope:** Medium (5-7 files)

---

### Task 1.4: 拼音首字母检索与技能反查核心引擎
- **Description:** 实现极速拼音首字母检索引擎与技能-武将倒排索引，支持用户输入 `gy` 即刻精准匹配关羽，输入 `tsc` 匹配太史慈；点击任意技能可瞬间逆向获取掌握该技能的武将清单。
- **Acceptance criteria:**
  - [x] `PinyinSearchEngine` 支持汉字匹配、拼音首字母缩写、前缀匹配与相关度权重排序
  - [x] `SkillInvertedIndex` 实现技能反查武将与等级匹配，排序依解锁等级与武力升降序，单次查询延迟 < 1ms
  - [x] 编写充分的单元测试覆盖拼音检索各种边界情况与多名将反查
- **Verification:**
  - [x] Tests pass: `dotnet test` (15 tests passed, 0 failed)
  - [x] 拼音检索关羽、夏侯惇、诸葛亮、太史慈等验证精准命中
- **Dependencies:** Task 1.3
- **Files likely touched:**
  - `src/Tianjige.Core/Services/PinyinSearchEngine.cs`
  - `src/Tianjige.Core/Services/SkillInvertedIndex.cs`
  - `tests/Tianjige.Tests/PinyinSearchTests.cs`
- **Estimated scope:** Small (3 files)

---

### Task 1.5: 【将星录】视图与 1~40 级技能长卷
- **Description:** 实现主模块【将星录】，包含武将名号印信、客观特色概括、初始与进阶兵种、红蓝分色 1~40 级技能时间轴长卷，以及点击技能展开的“技能逆查名录”。
- **Acceptance criteria:**
  - [x] 武将名号右侧展示古风朱砂资质印信（如【无双】）
  - [x] 技能树分列武将技（赤红）与军师技（幽蓝），客观显示耗蓝与作用说明 (`SkillTimeline.xaml`)
  - [x] 点击任意技能名称触发 `LookupSkillCommand`，抽屉浮层展示全图习得该技能的武将列表 (`GeneralStarView.xaml`)
- **Verification:**
  - [x] `dotnet build` 编译通过，UI 数据绑定与 ViewModel 逻辑就绪
  - [x] 单元测试通过
- **Dependencies:** Task 1.2, Task 1.4
- **Files likely touched:**
  - `src/Tianjige.UI/ViewModels/GeneralStarViewModel.cs`
  - `src/Tianjige.UI/Views/GeneralStarView.xaml`
  - `src/Tianjige.UI/Views/GeneralStarView.xaml.cs`
  - `src/Tianjige.UI/Controls/SkillTimeline.xaml`
- **Estimated scope:** Medium (4-5 files)

---

### Task 1.6: 【兵机盘】交互罗盘与八门阵型视图
- **Description:** 实现【兵机盘】展开视图与右上角常驻入口，以环形相克图呈现短兵、远兵、长兵、骑兵、特殊兵种的克制关系，并提供八门阵型优劣速查表。
- **Acceptance criteria:**
  - [x] 交互式罗盘点击任意兵种高亮显示“我克制”、“被克制”与“平手”对应兵种
  - [x] 八门阵型卡片清晰列出站位重心与对抗优劣
  - [x] 保持纯客观法则，不提供战术保姆说教
- **Verification:**
  - [x] 单元测试通过：`TroopCompassTests` 验证长枪兵克制铁骑兵、受制于弓箭兵等生克闭环，八门阵型完整度校验 (21 tests total)
  - [x] `dotnet build` 编译成功，XAML 数据模板与视图模型绑定就绪
- **Dependencies:** Task 1.2, Task 1.3
- **Files likely touched:**
  - `src/Tianjige.Core/Services/TroopRelationService.cs`
  - `src/Tianjige.UI/ViewModels/TroopCompassViewModel.cs`
  - `src/Tianjige.UI/Views/TroopCompassView.xaml`
  - `src/Tianjige.UI/Views/TroopCompassView.xaml.cs`
  - `src/Tianjige.UI/Controls/CompassWidget.xaml`
  - `src/Tianjige.UI/Controls/CompassWidget.xaml.cs`
  - `tests/Tianjige.Tests/TroopCompassTests.cs`
- **Estimated scope:** Small (3-4 files)

---

### Task 1.7: 桌面参谋工作台（离线大典与 HUD 基础窗体）整合与单机速查验证
- **Description:** 整合阶段一各模块成果，构建桌面参谋工作台基础架构。支持离线大典全功能查阅（顶部拼音秒搜、武将卡片、1~40 级技能长卷与反查、兵机相克罗盘与运筹底栏），为阶段二双窗体演进奠定坚实的表现层基底。
- **Acceptance criteria:**
  - [x] 参谋工作台支持 400px 优雅宽幅（适配 380~420px 物理黑边），支持拖动与置顶切换
  - [x] 拼音快速搜索栏即搜即选，键盘上下键可快速切将
  - [x] Tab 页面丝滑切换（将星录 / 兵机盘），离线亦可作为单机三国大典
  - [x] 运筹底栏占位呈现古风时节与气运状态（岁次时令与气运印信）
- **Verification:**
  - [x] 启动程序进行端到端手工体验验证：进程健康拉起无崩溃，单机速查、技能逆查、兵机相克罗盘交互运行完美
- **Dependencies:** Task 1.2 - Task 1.6
- **Files likely touched:**
  - `src/Tianjige.UI/MainWindow.xaml`
  - `src/Tianjige.UI/MainWindow.xaml.cs`
  - `src/Tianjige.UI/ViewModels/MainViewModel.cs`
- **Estimated scope:** Medium (4 files)

---

### 阶段一里程碑检查点（Checkpoint 1）
- [x] 所有单元测试通过 (`dotnet test` 21/21 tests passed)
- [x] 应用程序编译并正常启动，无报错 (`Tianjige.UI.exe` 进程正常启动)
- [x] 拼音搜索 `gy` 秒切关羽，技能长卷与反查列表正常工作
- [x] 兵机罗盘兵种相克环展示正确
- [x] 汉末古风视觉设计（汉式五色、简帛素米、朱砂印信、沉木渐变）浑然一体

---

## 阶段二：一体化启动台与环境对齐（内置 cnc-ddraw & 双窗体联动）

### Task 2.1: 内置 `cnc-ddraw` 核心组件与任意分辨率配置引擎
- **Description:** 将开源 `cnc-ddraw` 预编译二进制文件（`ddraw.dll` 及默认模板）内嵌于程序资源中，开发 `CncDdrawManager`。内置任意分辨率 4:3 视口推导算法（计算所选屏幕的 $H_{game} = H_{screen}, W_{game} = \lfloor H_{screen} \times 4/3 \rfloor, W_{black} = W_{screen} - W_{game}$），依据选定屏幕与偏好生成精确的 `cnc-ddraw.ini`。
- **Acceptance criteria:**
  - [x] 自动检测游戏目录下是否存在 `ddraw.dll`，不存在则安全写入
  - [x] 纯算力兼容 1080p (480px 黑边)、2K (640px 黑边)、4K (960px 黑边) 与 16:10 等任意屏幕
  - [x] 动态生成 `ddraw.ini` 精确注入 `width`, `height`, `posX`, `posY`, `windowed=true`, `border=false`, `savesettings=0`, `center_window=0`
  - [x] `DisplayMonitorService` 实现多显示器 P/Invoke 枚举与安全兜底
- **Verification:**
  - [x] 单元测试覆盖：`DisplayGeometryTests` (4 tests) + `CncDdrawManagerTests` (3 tests) 验证 1080p、2K 副屏偏移、4K、16:10 几何推导与临时目录真实写入 (29 tests total)
  - [x] `dotnet build` 编译 0 警告 0 错误
- **Dependencies:** Checkpoint 1
- **Files likely touched:**
  - `src/Tianjige.Core/Models/DisplayGeometry.cs`
  - `src/Tianjige.Core/Services/DisplayGeometryCalculator.cs`
  - `src/Tianjige.Core/Services/CncDdrawManager.cs`
  - `src/Tianjige.Core/Native/DisplayMonitorService.cs`
  - `src/Tianjige.Core/Assets/CncDdraw/ddraw.dll`
  - `src/Tianjige.Core/Assets/CncDdraw/cnc-ddraw.ini.template`
  - `tests/Tianjige.Tests/DisplayGeometryTests.cs`
  - `tests/Tianjige.Tests/CncDdrawManagerTests.cs`
- **Estimated scope:** Medium (4-8 files)

### Task 2.2: 双窗体架构（启动配置窗 LauncherWindow + 实时战局 HUD 窗 TacticalHudWindow）
- **Description:** 打造双窗体体验：
  1. `LauncherWindow`（聚贤议政台）：作为日常管理与启动主窗，提供游戏路径选择、多屏可视化磁贴选屏（屏幕1/屏幕2）、HUD 尺寸与位置滑动条微调预览、离线典籍入口与“启程入局”一键启动。
  2. `TacticalHudWindow`（军师参谋屏风）：点击启程后无缝拉起，精准吸附至所选屏幕右侧黑边（`X = monitor.Bounds.Left + W_game`），随游戏生命周期自动绑定。
- **Acceptance criteria:**
  - [x] 启动配置窗可视化呈现多屏拓扑与 HUD 尺寸/位置微调滑块，配置可保存 (`UserSettingsService`)
  - [x] 点击“启程入局”，自动生成配置、启动 `Sango2.exe` 并于右侧升起 `TacticalHudWindow`
  - [x] 游戏关闭时 HUD 自动平滑解绑并返回启动配置窗
- **Verification:**
  - [x] 单元测试覆盖：`UserSettingsTests` (2 tests) + `GameLauncherServiceTests` (2 tests) (33 tests total)
  - [x] 进程端到端拉起与稳定性测试验证通过
- **Dependencies:** Task 2.1
- **Files likely touched:**
  - `src/Tianjige.Core/Models/UserSettings.cs`
  - `src/Tianjige.Core/Services/UserSettingsService.cs`
  - `src/Tianjige.Core/Services/GameLauncherService.cs`
  - `src/Tianjige.UI/ViewModels/LauncherViewModel.cs`
  - `src/Tianjige.UI/Views/LauncherWindow.xaml`
  - `src/Tianjige.UI/Views/LauncherWindow.xaml.cs`
  - `src/Tianjige.UI/Views/TacticalHudWindow.xaml`
  - `src/Tianjige.UI/Views/TacticalHudWindow.xaml.cs`
  - `src/Tianjige.UI/App.xaml`
- **Estimated scope:** Medium (4-8 files)

### Task 2.3: 极简防崩溃窗口生命周期与纯顶层置顶
- **Description:** 放弃侵入式焦点拦截与暂停逻辑，使用标准 `TopMost` 与基础置顶，天机阁仅作为无害的侧边情报面板，游戏与参谋窗各自独立稳定运行。
- **Acceptance criteria:**
  - [x] 天机阁置顶时不阻断游戏点击，两者独立交互，零卡死、零崩溃
  - [x] 窗体顶栏提供“📌 置顶/浮动”实时切换与“⚙️ 启动台”平滑回切
- **Verification:**
  - [x] 采用纯 WPF TopMost 与无边框桌面吸附，零内存注入/键盘钩子，物理无死锁
- **Dependencies:** Task 2.2
- **Estimated scope:** Small (2 files)

---

## 阶段三：纯只读内存引擎与大局呈现

### Task 3.1: x86 `SafeMemoryReader` 核心引擎
- **Description:** 实现基于 Win32 `OpenProcess(PROCESS_VM_READ)` 的纯只读安全内存采集器，定位 Sango2 武将数组（400 将）与城池数组（60 城）基址，提供 500ms 级别的毫秒级低负载快照。
- **Acceptance criteria:**
  - [x] 纯只读访问，零内存写入，即使读取失败也优雅回退，绝不导致 Sango2 崩溃
  - [x] 能够稳定解析当前武将实时 HP/MP、忠诚度、所在城池与出仕状态
  - [x] `AobPatternMatcher` 支持 AOB 通配特征码搜索定位
- **Verification:**
  - [x] 单元测试覆盖：`MemoryStructTests` 验证结构体大小与偏移对齐、AOB 匹配、快照生成 (43 tests total)
- **Dependencies:** Checkpoint 2
- **Files likely touched:**
  - `src/Tianjige.Core/Native/NativeMethods.cs`
  - `src/Tianjige.Data/Memory/Models/MemoryModels.cs`
  - `src/Tianjige.Data/Memory/AobPatternMatcher.cs`
  - `src/Tianjige.Data/Memory/IMemoryReader.cs`
  - `src/Tianjige.Data/Memory/SafeMemoryReader.cs`
  - `src/Tianjige.Data/Memory/MockMemoryReader.cs`
- **Estimated scope:** Medium (4-5 files)

### Task 3.2: 【天下势】割据版图、实时城池虚实穿透与诸侯忠诚度降序雷达
- **Description:** 根据实时内存数据聚合全图诸侯领地、城内驻防武将与预备兵精确数字，支持按阵营查看武将实时忠诚度倒序排列。
- **Acceptance criteria:**
  - [x] 实时穿透城内预备兵数，哪座是重兵城哪座是空虚城一览无余
  - [x] 实时忠诚度降序，及时发现可招募/离间目标
- **Verification:**
  - [x] 单元测试覆盖：`WorldSituationTests` 验证预备兵城池聚合与忠诚度升序排序
- **Dependencies:** Task 3.1
- **Files likely touched:**
  - `src/Tianjige.UI/ViewModels/WorldSituationViewModel.cs`
  - `src/Tianjige.UI/Views/WorldSituationView.xaml`
  - `src/Tianjige.UI/Views/WorldSituationView.xaml.cs`
- **Estimated scope:** Medium (3-4 files)

### Task 3.3: 【天下势】实时在野寻贤、登场倒计时、逃将流动监控与英雄冢
- **Description:** 实时过滤 `Status == 2`（在野）与 `Status == 3`（阵亡），并追踪残血逃窜将领逃往的邻近城池与当前忠诚。
- **Acceptance criteria:**
  - [x] 逃将实时追踪：战后释放或溃败的武将实时出现在雷达中
  - [x] 在野名士城池精准定位与能力排序
- **Verification:**
  - [x] `WorldSituationTests` 验证流浪/在野将领准确筛选
- **Dependencies:** Task 3.1
- **Files likely touched:**
  - `src/Tianjige.Data/Memory/Models/MemoryModels.cs`
  - `src/Tianjige.UI/Views/WorldSituationView.xaml`
- **Estimated scope:** Medium (3-4 files)

### Task 3.4: 【天工坊】实时地脉勘宝与名器持有者透视
- **Description:** 穿透全图未被搜寻的神兵宝书在地底的埋藏城池分布，盘点全图已出世宝物的具体持有武将。
- **Acceptance criteria:**
  - [x] 清晰展示赤兔马、方天画戟、青囊书等当前在哪个城的地底未被搜出
- **Verification:**
  - [x] 单元测试 `Snapshot_UndergroundItems_FoundAccurately` 验证地脉宝物关联城池正确
- **Dependencies:** Task 3.1
- **Files likely touched:**
  - `src/Tianjige.Data/Memory/Models/MemoryModels.cs`
  - `src/Tianjige.UI/Views/WorldSituationView.xaml`
- **Estimated scope:** Small (3 files)

---

## 阶段四：底栏运筹与平滑封装

### Task 4.1: 战前老虎机乱闪防抖逻辑
- **Description:** 监测游戏当前场景状态，在战前电脑武将头像飞速变换时保持 HUD 静止，仅在确认对战大门拉开时淡入对阵卡。
- **Acceptance criteria:**
  - [x] 战前选将界面零抽搐，优雅平滑 (`BattleDebouncer` 状态机与 `BattleState == 2` 防抖)
- **Verification:**
  - [x] 单元测试 `BattleDebouncer_WhenSlotMachineSpinning_SuppressesFlicker` 验证防抖抑制与交锋锁定
- **Dependencies:** Task 3.1
- **Files likely touched:**
  - `src/Tianjige.Core/Services/BattleDebouncer.cs`
  - `tests/Tianjige.Tests/CalendarAndDebounceTests.cs`
- **Estimated scope:** Small (2 files)

### Task 4.2: 【运筹底栏】建安年号纪年映射与气运玉印状态联动
- **Description:** 从内存提取当前游戏年月，映射为古风年纪（如“建安五年 · 岁在庚辰 · 孟秋七月”），联动玉印状态。
- **Acceptance criteria:**
  - [x] 游戏运行时显示玉印 `☯ 气运连通 · 战局洞若观火`，退出或未连通时显示 `○ 闭关静思 · 典籍速查模式`
  - [x] 年号干支与孟仲季四时时令精准计算
- **Verification:**
  - [x] 单元测试 `AncientCalendar_ShouldFormatAccurately` (10 tests) 验证建安、黄初、太和等古风年号与干支计算
  - [x] UI 运筹底栏双窗体（MainWindow 与 TacticalHudWindow）完美绑定
- **Dependencies:** Task 3.1
- **Files likely touched:**
  - `src/Tianjige.Core/Services/AncientCalendarService.cs`
  - `src/Tianjige.UI/ViewModels/MainViewModel.cs`
  - `src/Tianjige.UI/Views/TacticalHudWindow.xaml`
  - `tests/Tianjige.Tests/CalendarAndDebounceTests.cs`
- **Estimated scope:** Small (2 files)

### Task 4.3: 单文件便携式打包与交付发布
- **Description:** 将整个工程打包为绿色单文件，包含内置 cnc-ddraw 与静态典籍，无需额外运行时安装。
- **Acceptance criteria:**
  - [ ] 双击即可运行，单文件便携即用
- **Dependencies:** Checkpoint 4
- **Estimated scope:** Small (1-2 files)
