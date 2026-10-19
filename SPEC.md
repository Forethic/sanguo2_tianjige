# Spec: 《群英策·天机阁》项目规范书 (Tianjige Specification)

## Phase 0: 架构能力地图 (Capability Map)

| 模块 ID (Module Id) | 核心职责 (Responsibility) | 依赖模块 (Depends on) |
| :--- | :--- | :--- |
| `core-models` | 静态典籍领域实体 (General, Skill, Item, Formation) 与内存数据结构定义 | — |
| `static-lore` | 400 武将长卷、全神兵、兵阵字典装载器、极速拼音首字母检索及反查引擎 | `core-models` |
| `cnc-launcher` | 内置 `cnc-ddraw` 转接层管理、多屏探测与可视化选屏（主/副屏）、`cnc-ddraw.ini` 动态坐标生成、游戏一键启动与窗口生命周期绑定 | — |
| `memory-radar` | 基于 Win32 `OpenProcess(PROCESS_VM_READ)` 的纯只读安全内存轮询与实时战局聚合器 | `core-models` |
| `advisor-ui` | WPF 古风视觉系统 (汉末五色、朱砂印章、隶书排印) 与五大参谋模块视图集成 | `core-models`, `static-lore`, `cnc-launcher`, `memory-radar` |

**构建依赖流向：**
`core-models` ➔ `static-lore` ➔ `cnc-launcher` ➔ `memory-radar` ➔ `advisor-ui`

---

## 1. 目标与定位 (Objective)

### 1.1 业务愿景与双窗体架构 (Dual-Window Architecture)
《群英策·天机阁》采用**“启动配置窗体 + 战局参谋 HUD 窗体”**的分离设计：
1. **【聚贤议政台】（LauncherWindow 启动与配置台）：** 标准桌面程序窗体。负责游戏路径配置、`cnc-ddraw` 转接层管理、多屏与任意分辨率可视化拓扑预览、HUD 尺寸与停靠位置微调，以及单机离线典籍查阅。
2. **【军师参谋屏风】（TacticalHudWindow 实时战局 HUD）：** 无边框轻量级置顶悬浮窗。战时全量呈现实时内存雷达、技能长卷与兵机罗盘，与游戏生命周期绑定。

### 1.2 任意分辨率自适应推导模型 (Resolution-Independent Math)
系统不硬绑定 1920×1080，适配 2K (2560×1440)、4K (3840×2160)、16:10 (1920×1200) 等任意屏幕：
* 目标屏幕物理尺寸：宽 $W_{screen}$，高 $H_{screen}$
* 4:3 游戏视口尺寸：高度 $H_{game} = H_{screen}$，宽度 $W_{game} = \lfloor H_{screen} \times \frac{4}{3} \rfloor$
* 净黑边空间：$W_{black} = W_{screen} - W_{game}$
  * 1080p: $W_{black} = 480\text{px}$（HUD 默认宽 380~420px）
  * 2K (1440p): $W_{black} = 640\text{px}$（HUD 默认宽 450~520px）
  * 4K (2160p): $W_{black} = 960\text{px}$（HUD 默认宽 600~720px）
* 启动配置窗内提供滑动条与预览图，支持用户微调 HUD 宽度与坐标偏移并保存。

### 1.3 核心用户场景 (User Stories)
* **场景 A（良将选拔与培养）：** 玩家敲击拼音首字母（如 `tsc` / `gy`）秒级定焦武将，查阅 1~40 级技能长卷与进阶兵种，彻底消除“盲目练将、不知何时学大招”的困惑。
* **场景 B（战局虚实穿透）：** 玩家在大地图进军前无需存盘，实时穿透全图城池驻军人数与预备兵精确数字，防范空城冒进或遭遇重兵伏击。
* **场景 C（逃将与在野追踪）：** 战后释放敌将或斩获败逃敌军时，雷达实时捕捉其逃窜去向与坠崖式忠诚度暴跌数值，为玩家招降离间提供客观事实依据。
* **场景 D（多屏自适应一键启程）：** 玩家在配置窗直观选择目标显示器并微调 HUD 尺寸，点击“启程入局”，天机阁自动生成对应分辨率坐标参数写入 `cnc-ddraw.ini`，拉起游戏并在所选屏幕右侧净黑边无缝升起 HUD 屏风。

---

## 2. 技术栈标准 (Tech Stack)

* **运行环境：** Windows 10 (19041+) / Windows 11 (x64)
* **开发框架：** .NET 9.0 / .NET 10.0 (C# 13, WindowsDesktop WPF)
* **表现层技术：** WPF (XAML, Hardware Acceleration, Per-Monitor DPI V2 Aware)
* **底层系统交互：** Win32 Native API (`kernel32.dll`, `user32.dll` via P/Invoke)
* **测试框架：** xUnit + FluentAssertions
* **图形转接依赖：** 内置开源 `cnc-ddraw` (DirectDraw to Direct3D9/OpenGL/GDI)

---

## 3. 标准构建与执行指令 (Commands)

* **代码编译：** `dotnet build`
* **单元测试：** `dotnet test --verbosity normal`
* **特定测试过滤：** `dotnet test --filter FullyQualifiedName~PinyinSearch`
* **启动开发应用：** `dotnet run --project src/Tianjige.UI`
* **独立单文件发布：** `dotnet publish src/Tianjige.UI -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true`

---

## 4. 解决方案与目录组织 (Project Structure)

```text
sanguo2_tianjige/
├── .agents/                    # 智能体技能与协作定义
├── docs/                       # 架构设计与理念文档
│   └── ideas/
│       └── tianjige.md         # 方案沉淀 One-Pager
├── tasks/                      # 敏捷规划与任务追踪
│   ├── plan.md                 # 实施规划文档
│   └── todo.md                 # 颗粒度任务清单
├── SPEC.md                     # 本项目规范书 (Source of Truth)
├── Tianjige.sln                # 顶层解决方案
├── src/
│   ├── Tianjige.Core/          # 领域核心模型与抽象接口
│   │   ├── Models/             # GeneralStaticInfo, SkillInfo, MemoryState
│   │   ├── Services/           # PinyinSearchEngine, SkillInvertedIndex
│   │   └── Assets/             # 内置 cnc-ddraw.dll 与模板
│   ├── Tianjige.Data/          # 静态数据与内存读取
│   │   ├── Repositories/       # StaticRepository (JSON 装载)
│   │   ├── Resources/          # Generals.json, Skills.json, Items.json
│   │   └── Memory/             # SafeMemoryReader (Win32 RPM 封装)
│   └── Tianjige.UI/            # WPF 表现层 (双窗体架构)
│       ├── Controls/           # SealBadge, CompassWidget, SkillTimeline, MonitorSelector
│       ├── Themes/             # Colors.xaml, Typography.xaml, Controls.xaml
│       ├── ViewModels/         # LauncherViewModel, TacticalHudViewModel, GeneralStarViewModel
│       └── Views/              # LauncherWindow (启动配置), TacticalHudWindow (战局参谋 HUD)
└── tests/
    └── Tianjige.Tests/         # 单元测试工程
        ├── PinyinSearchTests.cs
        ├── StaticDataTests.cs
        └── MemoryStructTests.cs
```

---

## 5. 代码风格与规范 (Code Style)

### 5.1 编码守则
* 必须启用 `<Nullable>enable</Nullable>`，严禁对空引用做无防御解引用。
* 领域模型优先采用 `record` 或带明确初始化的属性。
* P/Invoke Win32 API 统一归集至 `Tianjige.Core.Native.NativeMethods`，遵循 `LibraryImport` 或 `DllImport` 标注。
* XAML 样式严格解耦，色彩必须引用 `DynamicResource` 或 `StaticResource`，严禁在控件行内硬编码十六进制颜色。

### 5.2 规范代码范例

```csharp
namespace Tianjige.Core.Models;

/// <summary>
/// 武将静态档案（不可变客观规律）
/// </summary>
public sealed record GeneralStaticInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string PinyinIndex { get; init; }
    public required string SealTier { get; init; } // 【无双】/【良将】/【谋主】/【裨将】/【白身】
    public required byte BaseForce { get; init; }
    public required byte BaseIntellect { get; init; }
    public string Comment { get; init; } = string.Empty;
    public string InitialTroop { get; init; } = string.Empty;
    public string UnlockTroop { get; init; } = string.Empty;
    public IReadOnlyList<SkillTreeNode> SkillTree { get; init; } = Array.Empty<SkillTreeNode>();
}

/// <summary>
/// 纯只读内存扫描安全句柄封装
/// </summary>
public sealed class SafeProcessReader : IDisposable
{
    private readonly nint _processHandle;

    public SafeProcessReader(int processId)
    {
        // 严格以 PROCESS_VM_READ 纯只读权限打开，绝不索取写权限
        _processHandle = NativeMethods.OpenProcess(
            NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_QUERY_INFORMATION, 
            false, 
            processId);
        
        if (_processHandle == 0)
        {
            throw new InvalidOperationException($"无法以只读权限附加游戏进程: {processId}");
        }
    }

    public bool TryReadStruct<T>(nint baseAddress, out T value) where T : unmanaged
    {
        return NativeMethods.ReadProcessMemory(_processHandle, baseAddress, out value);
    }

    public void Dispose()
    {
        if (_processHandle != 0) NativeMethods.CloseHandle(_processHandle);
    }
}
```

---

## 6. 测试策略 (Testing Strategy)

1. **单元测试（Unit Tests - 快速覆盖）：**
   * **拼音引擎测试：** 覆盖汉字首字母转换、多音字兼容、大小写忽略、连续输入过滤。
   * **静态数据完整性测试：** 校验 `Generals.json` 覆盖 400 人无重复 ID；`Skills.json` 消耗非负；阵型互克环无自相矛盾。
   * **结构体内存映射测试：** 验证 C# x86 内存镜像 Struct 对齐与 byte offset 符合 Sango2 原版二进制布局。
2. **集成与环境测试：**
   * `CncDdrawManagerTests`：测试在临时沙箱目录下生成与修改 `cnc-ddraw.ini` 的文本对齐正确性。
3. **手工验收测试：**
   * 古风 UI 渲染质感、朱砂印章抗锯齿与缩放无失真。

---

## 7. 行为边界 (Boundaries)

* **Always（必须做到）：**
  * 对游戏进程内存仅使用 `PROCESS_VM_READ` 权限，遵循严格的只读观察者模型。
  * 所有文件 IO 操作必须包含容错捕获与路径有效性防御。
  * 保持客观呈现哲学，所有界面文案严禁出现主观战术指导。
* **Ask First（需先确认）：**
  * 引入新的第三方 NuGet 依赖包。
  * 调整 400 武将印章资质评级标准。
  * 更改默认 1440x1080 左置视口布局参数。
* **Never（严禁触碰）：**
  * 严禁调用 `WriteProcessMemory`、内存 Hook 或注入 DLL。
  * 严禁覆盖或删除用户的游戏原版主执行程序 `Sango2.exe`。
  * 严禁侵占游戏 1440×1080 视口区域（坚持 100% 物理零遮挡）。

---

## 8. 成功标准 (Success Criteria)

- [ ] `dotnet build` 编译 0 警告、0 错误。
- [ ] 离线模式下输入 `gy`、`tsc` 5ms 内精准定位关羽、太史慈并呈现 1~40 级技能长卷与反查列表。
- [ ] 启动器成功拉起 Sango2 并自动应用左置 1440x1080，屏幕右侧留下 480px 纯黑区，天机阁驻留无缝。
- [ ] 游戏运行状态下，内存采集器在 500ms 周期内稳定输出实时将领 HP/MP、忠诚度与城防，游戏进程持续稳定 0 闪退。
- [ ] 整体 UI 视觉高度符合汉末军师案台美学，朱砂印信与素米简帛浑然一体。

---

## 9. 开放问题 (Open Questions)
1. 针对极少数使用非 1080p 屏幕（如 2K 2560x1440 或 4K 3840x2160）的用户，系统是否直接按等比缩放计算左置参数？（架构已预留 DPI 与屏幕分辨率自适应公式）。
