# WindowsReStartMenu 详细设计

> 本文件是入口文档 [AGENTS.md](../AGENTS.md) 的详细展开，保留原章节编号 §2–§11，避免内部引用失效。
> 工作流程、已确认决策、范围摘要与下一步以 `AGENTS.md` 为准。
> 状态：Phase 1 视觉骨架已完成（静态、占位图标），完整模拟交互待 Phase 2。最后更新：2026-10-25。

**已确认决策（详见 `AGENTS.md` §2）：**

| 编号 | 决策 |
| --- | --- |
| D1 | 26H2 新版外观：4 行固定项 + “全部”区 + 类别视图 |
| D2 | 外观 + 完整模拟交互，全部使用本地假数据 |
| D3 | WinUI 3 + .NET 10 + Windows App SDK 2.5.1，非打包 |
| D4 | `StartMenu.App` / `StartMenu.Core` 可构建；`StartMenu.HookHost` 仅占位 |
| D5 | 底部居中任务栏为验收基准，定位预留任意边 |
| D6 | 不做 Win 键拦截、系统开始菜单替换、真实应用枚举 |

---

## 2. 参考与视觉基准

### 2.1 参考来源

- 2026-10-25 在本机 26H2 上抓取的实机截图（1920×1080 物理像素，125% 缩放，深色，任务栏在右侧）。
- 微软 Windows Insider 博客对 2025 新版开始菜单的说明：固定区/推荐区/“全部”区、Category/Grid/List 三种视图、响应式列数（大屏 8 列固定 + 4 列类别，小屏 6 列固定 + 3 列类别）。
- 本机实测：可见面板约 **887×833 逻辑像素**；固定区 **8 列 × 4 行**；类别区 **4 列**（当前 4 个类别为 1 行）；搜索框高度约 32 逻辑像素；页脚高度约 48 逻辑像素。
- **注意**：参考截图里任务栏在右侧，开始菜单右边缘被屏幕/任务栏裁切，因此“887 宽”可能偏小，不能当作绝对真值。实现以**结构、比例、间距关系**为准，再用截图叠加校准。
- **个人截图不入库。** 参考图含用户桌面、应用与账户信息，只放本地 `docs/references/local/`（加入 `.gitignore`）。

### 2.2 整体结构

```text
┌──────────────────────────────────────────────────────────────┐
│  🔍  搜索应用、设置和文档                                      │  ← 固定在顶部
│                                                              │
│  已固定                                       显示精简信息 ⌃   │
│  ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐ ┌────┐    │
│  │icon│ │icon│ │icon│ │icon│ │icon│ │icon│ │icon│ │icon│    │  ← 8 列
│  │name│ │name│ │name│ │name│ │name│ │name│ │name│ │name│    │
│  └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘ └────┘    │
│  ... 共 4 行可见，超出滚动 ...                                │
│                                                              │
│  全部                                         视图: 类别 ⌄    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐         │
│  │ 2×2 icon │ │ 2×2 icon │ │ 2×2 icon │ │ 2×2 icon │         │  ← 4 列
│  │ 高效工作  │ │开发人员工具│ │  其他    │ │实用程序… │         │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘         │
│  ... 当前 4 类 1 行；更多类别换行/滚动 ...                     │
│                                                              │
│  (avatar) Xianene Q                                     ⏻    │  ← 固定在底部
└──────────────────────────────────────────────────────────────┘
```

- 搜索框与页脚**固定**；中间内容整体滚动。
- “已固定”和“全部”的标题随内容滚动，不做吸顶（首期）。
- 参考机没有“推荐的项目”区；首期不实现，见 §5.9。
- 滚动条为细条，默认半透明，悬停/滚动时更明显，宽度 3 逻辑像素，覆盖在内容右侧。

### 2.3 尺寸与布局 token（初版，待截图校准）

以下为 **逻辑像素（100% 基准）**。实现时统一乘 DPI 缩放，不写死物理像素。

| Token | 初版值 | 说明 |
| --- | --- | --- |
| `Panel.Width` | 888 | 上限；小屏按工作区收窄 |
| `Panel.Height` | 832 | 上限；小屏按工作区收窄，内容滚动 |
| `Panel.MinWidth` | 720 | 低于此宽度切 6 列 |
| `Panel.MinHeight` | 560 | 低于此高度只保证搜索/页脚/滚动 |
| `Panel.CornerRadius` | 8 | 与 DWM 圆角一致 |
| `Panel.Padding` | 24, 8, 24, 8 | 左、上、右、下 |
| `Search.Height` | 32 | 圆角 8，左右 24 |
| `Search.GapBelow` | 16 | 到“已固定”标题 |
| `Header.Height` | 24 | “已固定”/“全部”标题行 |
| `Header.FontSize` | 16 | SemiBold |
| `Pinned.Columns` | 8 | 宽 ≥ 860；720–859 为 6；< 720 为 4 |
| `Pinned.RowHeight` | 76 | 图标 36 + 标签 24 + 上下 8 |
| `Pinned.RowGap` | 4 | |
| `Pinned.LabelFontSize` | 12 | 最多 2 行，居中，省略号 |
| `Pinned.IconSize` | 36 | |
| `All.Columns` | 4 | 宽 ≥ 860；720–859 为 3；< 720 为 2 |
| `Category.CardWidth` | 201 | 由 4 列 + 12 间距算出 |
| `Category.CardHeight` | 148 | 2×2 图标 32 + 标签 |
| `Category.CardGap` | 12 | |
| `Category.IconSize` | 32 | 2×2 排列 |
| `Category.LabelFontSize` | 12 | |
| `Footer.Height` | 48 | 头像 28、名字 14、电源 20 |
| `Panel.MarginToTaskbar` | 8 | 底部任务栏时距工作区底边 |
| `Panel.MarginToScreen` | 8 | 其他方向的最小边距 |
| `Scrollbar.Width` | 3 | 覆盖式，不占布局宽度 |

**窗口高度策略**：`Panel.Height = min(832, 工作区高度 - 16)`；不足时保持最小高度，内部滚动。宽度策略类似：`Panel.Width = min(888, 工作区宽度 - 24)`，并受断点影响。

**视觉校准基准**：以 1920×1080、125%、底部居中任务栏、深色主题为第一基准；其次 100% 与 150%。允许结构位置 ±2–3 逻辑像素误差；字体大小允许 ±1。

### 2.4 材质、颜色、字体

- **材质**：Desktop Acrylic（不是 Mica，也不是仅 in-app Acrylic）。参考开始菜单是 transient 表面，用 Acrylic。
  - **实际采用**（Phase 0 已验证）：`SystemBackdropElement`（WASDK 2.0+）+ `DesktopAcrylicBackdrop`，在窗口内绘制 `CornerRadius=8` 的圆角亚克力；窗口本身透明。这样可避开“无边框窗口 DWM 圆角不裁剪内容”的问题。
  - 需要精确 tint 时，可在 `SystemBackdropElement` 上叠加半透明 tint，或改用 `DesktopAcrylicController` + `SystemBackdropConfiguration` 做整窗材质校准。
  - 关闭“透明效果”、节电模式、远程桌面、高对比度时自动回退纯色，必须保证可读。
- **颜色**：尽量使用 WinUI 主题资源，不硬编码大色块：
  - 文本：`TextFillColorPrimaryBrush` / `SecondaryBrush` / `TertiaryBrush`
  - 控件填充：`ControlFillColorDefaultBrush` / `SecondaryBrush`
  - 卡片：`CardBackgroundFillColorDefaultBrush`、`CardStrokeColorDefaultBrush`
  - 悬停/按下：`SubtleFillColorSecondaryBrush` / `TertiaryBrush`
  - 强调/焦点：`AccentFillColorDefaultBrush`、`FocusStrokeColorOuterBrush`
  - 深色亚克力初值：tint `#0F0F0F`，opacity 约 0.78，luminosity 约 0.82，fallback `#202020`
  - 浅色亚克力初值：tint `#F5F5F5`，opacity 约 0.72，luminosity 约 0.80，fallback `#F3F3F3`
  - 面板内边 1px：深色 `#14FFFFFF`，浅色 `#12000000`（可用 Border 绘制，也可用 DWM 边框色）
- **字体**：`Segoe UI Variable Text`（正文/标签），标题可用 `Segoe UI Variable Display`；中文由系统字体回退（Microsoft YaHei UI）。
- **字号/字重**：标题 16 SemiBold；搜索 14；固定项标签 12；类别标签 12；页脚 14；视图选择 12；搜索结果 14。
- **图标**：
  - 系统图标用 `Segoe Fluent Icons`：搜索 `&#xE721;`、电源 `&#xE7E8;`、上折 `&#xE70E;`、下折 `&#xE70D;`、左折 `&#xE76B;`、清除 `&#xE711;`。
  - 应用图标用静态 PNG（36 逻辑像素显示，源图 64×64 或 96×96），不使用运行时真实应用枚举。
  - 图标缺失时显示占位色块 + 首字母，不得崩溃。

### 2.5 状态与动效

| 状态 | 表现 |
| --- | --- |
| 窗口打开 | 整体淡入 + 向上位移 12–16px，约 150ms，减速曲线 |
| 窗口关闭 | 淡出 + 向下位移 8–12px，约 100ms，加速曲线 |
| 页面/视图切换 | 内容交叉淡入淡出 100–150ms |
| 搜索进入/退出 | 结果区淡入并上移 8px；返回时反向 |
| 类别详情进入 | 从右侧轻微滑入 12px + 淡入，约 180ms |
| 悬停 | 背景色 100ms 过渡 |
| 按下 | 轻微缩小到 0.98 或加深背景，80ms |
| 焦点 | 2px 强调色焦点框，保持可见 |
| 滚动 | 原生惯性滚动；滚动条自动隐藏 |

- 使用 XAML `Storyboard`/`ThemeTransition` 或 Composition；优先简单实现。
- 尊重系统“动画效果”开关：关闭时直接显示/隐藏，不做位移与淡入。
- 不为动画引入额外依赖。

### 2.6 响应式规则

以窗口逻辑宽度为断点：

| 宽度 | 固定列 | 类别列 |
| --- | --- | --- |
| ≥ 860 | 8 | 4 |
| 720–859 | 6 | 3 |
| < 720 | 4 | 2 |

- 高度不足时**不减少固定行数**，而是整体滚动；后续可再实现“分区折叠”。
- 多显示器：首期固定主显示器；后续按“鼠标所在显示器/任务栏所在显示器”放置。
- 100% / 125% / 150% 必须无裁切、无重叠；文本不溢出卡片。

### 2.7 视觉验收方法

1. 在本地采集参考图（不入库）：深色首页、浅色首页、搜索结果、网格视图、列表视图、类别详情。
2. 实现一个 **DEBUG 叠加模式**（例如 `F12` 或启动参数 `--reference-overlay <png>`）：把参考图半透明铺在窗口上，对齐面板左上角，切换显示/隐藏，用于逐项校准。
3. 用同一壁纸、同一缩放、同一任务栏位置截图；Acrylic 只做定性对比，不要求像素级一致。
4. 验收顺序：结构 → 尺寸/间距 → 字体/颜色 → 材质/圆角/阴影 → 动效。
5. 参考图仅本地保存；如果用户同意，再裁剪出“只含开始菜单”的版本入库。

---

## 3. 技术选型

### 3.1 版本基线

| 项 | 选择 |
| --- | --- |
| UI 框架 | WinUI 3（Windows App SDK） |
| .NET | .NET 10，`net10.0-windows10.0.26100.0` |
| Windows App SDK | 2.5.1（NuGet：`Microsoft.WindowsAppSDK`） |
| 最低系统 | Windows 11 22000（代码中不主动使用更低版本 API；实际验收 26H2） |
| 应用类型 | 非打包桌面应用（`WindowsPackageType=None`） |
| 平台 | x64（本机 AMD64）；arm64 后续 |
| 语言 | C# 14 / XAML（以 .NET 10 SDK 默认为准） |
| 解决方案 | `WindowsReStartMenu.sln`（根目录） |

> 已在本机临时目录用最小 WinUI 3 探针验证：`dotnet build -p:Platform=x64` 成功，生成 exe 后能启动。不要因为“没有 Visual Studio”而放弃 CLI 路线。

### 3.2 为什么非打包

- 可直接用 `dotnet build` / `dotnet run` 迭代，不需要 MSIX 部署、证书、包标识。
- 更容易做无边框、置顶、任意位置、DWM 属性、任务栏定位等“像开始菜单”的窗口行为。
- 首期不需要包标识；真实应用枚举/系统集成等后续能力再评估是否切打包或混合。
- 代价：部分依赖包标识的 API 不可用（首期不需要）；分发时要带 Windows App Runtime 或自包含发布。

### 3.3 依赖策略

- 首期**只引入 `Microsoft.WindowsAppSDK`**。
- 不引入 MVVM 框架（自己写一个很小的 `ObservableObject` 即可）、不引入 DI 容器、不引入 UI 组件库、不引入图标包。
- 需要新增依赖时，先说明“为什么现有 BCL/WinUI 做不到”，经用户同意再加。
- 版本集中管理：`Directory.Packages.props` 固定 `Microsoft.WindowsAppSDK` 2.5.1；`global.json` 固定 SDK 10.0.402（`rollForward: latestFeature`）。
- 不引入 `H.NotifyIcon` 等托盘库；如后续需要托盘，优先用 Win32 `Shell_NotifyIcon` 自己封装。

### 3.4 已知环境风险

| 风险 | 现状 | 应对 |
| --- | --- | --- |
| 没有 Visual Studio / WinUI 模板 | 已确认 | 手写 csproj/XAML；用 CLI 构建；已用探针验证 |
| 没有 Windows SDK Include | 已确认 | `net10.0-windows10.0.26100.0` 可通过 NuGet 还原 Windows SDK 投影；探针已验证 |
| WASDK 2.5.1 + .NET 10 较新 | 已用探针 build/run 成功 | 锁定版本；升级前先验证 |
| XAML 设计器不可用 | 无 VS | 改 XAML → 构建 → 运行 → 截图，配合叠加模式 |
| 无边框窗口的 DWM 阴影/圆角行为 | 未验证 | Phase 0 先做窗口 spike；准备回退方案 |
| 非打包窗口的焦点/激活 | 未验证 | Phase 0 验证；失焦关闭加延迟与保护 |
| 参考图含个人内容 | 已确认 | 只存本地，加 `.gitignore`；不提交 |

---

## 4. 解决方案结构

### 4.1 项目与依赖方向

```text
StartMenu.App  ──依赖──▶  StartMenu.Core
      │
      └──（本期不引用）──▶  StartMenu.HookHost（占位，未来独立进程）

StartMenu.Core：纯 .NET，不引用 WinUI / Windows App SDK。
StartMenu.HookHost：未来通过命名管道/IPC 与 App 通信，不直接引用 App。
```

- **App**：WinUI 3、XAML、ViewModel、窗口/Interop、UI 服务。
- **Core**：领域模型、假数据、搜索排序、类别分组、设置读写、接口。可单测、可复用。
- **HookHost**：未来的 Win 键/系统集成宿主；本期只有 `README.md` 职责说明，**不加入解决方案、不构建**。
- 不为了“看起来分层”而制造空项目；首期解决方案只有 App + Core。

### 4.2 目录树（计划创建）

```text
D:\Program\WindowsReStartMenu.sln\
├─ WindowsReStartMenu.sln              # 待创建
├─ Directory.Build.props               # 公共编译属性
├─ Directory.Packages.props            # 集中包版本
├─ global.json                         # 固定 .NET SDK 10.0.402
├─ .gitignore                          # bin/obj/.vs/本地参考图
├─ AGENTS.md                           # 本文件
├─ README.md                           # 保持不动
│
├─ StartMenu.App\                      # 由 SrartMenu.App 更正
│  ├─ StartMenu.App.csproj
│  ├─ app.manifest                     # PerMonitorV2、longPathAware、Win10/11 supportedOS
│  ├─ App.xaml / App.xaml.cs
│  ├─ StartMenuWindow.xaml / .cs       # 无边框、亚克力、定位、生命周期
│  ├─ Views\
│  │  └─ StartMenuPage.xaml / .cs      # 根页面：搜索 + 滚动内容 + 页脚
│  ├─ Controls\
│  │  ├─ SearchBar.xaml / .cs
│  │  ├─ PinnedSection.xaml / .cs
│  │  ├─ AppTile.xaml / .cs
│  │  ├─ AllAppsSection.xaml / .cs
│  │  ├─ CategoryCard.xaml / .cs
│  │  ├─ CategoryDetailView.xaml / .cs
│  │  ├─ SearchResultsView.xaml / .cs
│  │  └─ FooterBar.xaml / .cs
│  ├─ ViewModels\
│  │  ├─ ObservableObject.cs           # 极简 INotifyPropertyChanged 基类
│  │  ├─ StartMenuViewModel.cs
│  │  ├─ AppTileViewModel.cs
│  │  ├─ CategoryCardViewModel.cs
│  │  └─ SearchResultViewModel.cs
│  ├─ Services\
│  │  ├─ WindowPlacementService.cs     # DPI、工作区、任务栏边、锚点
│  │  ├─ ThemeService.cs               # 跟随系统主题 + 亚克力配置
│  │  ├─ IconService.cs                # IconKey → 静态资源路径 / 占位
│  │  └─ LogService.cs                 # 本地日志（可选）
│  ├─ Interop\
│  │  └─ NativeMethods.cs              # DWM、窗口样式、任务栏、DPI
│  ├─ Themes\
│  │  ├─ Tokens.xaml                   # 颜色/尺寸/字体资源
│  │  ├─ Styles.xaml                   # 按钮、搜索框、滚动条、文本样式
│  │  └─ Templates.xaml                # 固定项、类别卡片、结果行
│  └─ Assets\
│     ├─ AppIcons\*.png                # 静态 mock 图标
│     └─ Avatar.png                    # 占位头像（可选）
│
├─ StartMenu.Core\
│  ├─ StartMenu.Core.csproj            # net10.0，无 Windows/UI 依赖
│  ├─ Models\
│  │  ├─ AppEntry.cs
│  │  ├─ CategoryGroup.cs
│  │  ├─ SearchResult.cs
│  │  ├─ StartMenuViewMode.cs
│  │  └─ StartMenuSettings.cs
│  ├─ Abstractions\
│  │  ├─ IAppCatalog.cs
│  │  ├─ IAppSearchService.cs
│  │  ├─ ISettingsService.cs
│  │  └─ IAppLauncher.cs
│  └─ Services\
│     ├─ FakeAppCatalog.cs
│     ├─ AppSearchService.cs
│     ├─ JsonSettingsService.cs
│     └─ MockAppLauncher.cs
│
├─ StartMenu.HookHost\
│  └─ README.md                        # 仅职责与未来计划，不入解决方案
│
├─ StartMenu.Core.Tests\               # 可选，第二阶段再建
├─ tools\
│  └─ Extract-AppIcons.ps1             # 可选，开发期一次性提取静态图标
└─ docs\
   └─ references\
      └─ local\                        # 个人参考截图，gitignore
```

### 4.3 分层职责

#### `StartMenu.Core`（纯逻辑）

- 定义 `AppEntry`、`CategoryGroup`、`SearchResult`、`StartMenuViewMode`、`StartMenuSettings`。
- `FakeAppCatalog`：32 个固定项 + 约 48 个应用 + 6 个类别；不依赖本机真实应用。
- `AppSearchService`：名称/关键字匹配与排序；纯函数，便于单测。
- `JsonSettingsService`：读写 `%LOCALAPPDATA%\WindowsReStartMenu\settings.json`；损坏时回退默认值。
- `MockAppLauncher`：只写日志/调试输出，不启动真实进程。
- 不引用 `Microsoft.UI.*`、`Windows.*`；不出现 `BitmapImage`、`DispatcherQueue` 等 UI 类型。

#### `StartMenu.App`（WinUI 层）

- 窗口宿主：无边框、亚克力、圆角、定位、显示/关闭、DWM。
- XAML 视图与控件；ViewModel 负责状态与命令；code-behind 只做视图相关事件转发。
- `IconService` 把 Core 的 `IconKey` 映射到 `ms-appx:///Assets/AppIcons/...`，缺失时返回占位。
- `WindowPlacementService` 处理 DPI、工作区、任务栏边、锚点。
- 主题/动画/无障碍/日志。

#### `StartMenu.HookHost`（未来）

- 独立 Win32/.NET 进程，负责 Win 键/任务栏入口的捕获与转发，避免钩子崩溃拖垮 UI。
- 与 App 通过命名管道或窗口消息通信；App 只负责显示/隐藏。
- 本期**不实现、不加入解决方案**；只在目录 README 中记录职责与风险。

### 4.4 关键文件与职责

| 文件 | 职责 |
| --- | --- |
| `StartMenu.App.csproj` | WinExe、WinUI、非打包、x64、TFM、manifest |
| `app.manifest` | PerMonitorV2 DPI、supportedOS、longPathAware |
| `App.xaml.cs` | 单窗口启动、组装 Core 服务与 VM、异常兜底 |
| `StartMenuWindow.xaml.cs` | 窗口生命周期、DWM、定位、失焦关闭、动画入口 |
| `StartMenuPage.xaml` | 根布局：固定搜索 + 滚动内容 + 固定页脚 |
| `StartMenuViewModel.cs` | 状态：查询、视图模式、选中类别、紧凑模式、集合 |
| `WindowPlacementService.cs` | 逻辑尺寸 → 物理像素；工作区/任务栏边/锚点 |
| `Tokens.xaml` | 所有尺寸、间距、字体、颜色 token；禁止散落魔法数字 |
| `NativeMethods.cs` | `DwmSetWindowAttribute`、`GetDpiForWindow`、`SHAppBarMessage`、窗口样式等 |

### 4.5 解决方案与仓库卫生

- 用 `dotnet new sln -n WindowsReStartMenu` 创建解决方案，添加 App + Core；HookHost 暂不添加。
- `.gitignore` 至少包含：`bin/`、`obj/`、`.vs/`、`*.user`、`docs/references/local/`、`*.log`。
- 当前 `.vs/` 未被跟踪；首次提交时不要提交 `.vs/slnx.sqlite`。
- 不改 `README.md`；项目说明只写在本文件。

---

## 5. 实现方案

### 5.1 窗口与宿主

目标：一个**无边框、圆角、亚克力、置顶、无任务栏按钮、可定位**的窗口。

实现要点：

1. **窗口类型**：`Microsoft.UI.Xaml.Window`，根内容 `StartMenuPage`。
2. **无边框**：`OverlappedPresenter`：
   - `SetBorderAndTitleBar(false, false)`；
   - `IsResizable = false`、`IsMaximizable = false`、`IsMinimizable = false`；
   - 显示期间 `IsAlwaysOnTop = true`。
   - `AppWindow.IsShownInSwitchers = false`，并通过 `WS_EX_TOOLWINDOW` 避免任务栏/Alt+Tab 出现。
3. **标题栏**：`ExtendsContentIntoTitleBar = true`，不设置可见标题栏；确认没有 1px 标题栏残影。
4. **圆角**：`DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE (33), DWMWCP_ROUND (2))`；实际内容圆角由 `SystemBackdropElement`/`Border` 的 `CornerRadius=8` 负责，窗口四角保持透明。Phase 0 实测 DWM `corner=2`，但无边框窗口的内容不会被 DWM 自动裁剪，因此采用元素级圆角。
5. **边框**：优先 `DWMWA_BORDER_COLOR` 设成接近透明；如效果不理想，在 XAML 内画 1px `Border`（`CornerRadius=8`）并让 DWM 只负责裁剪。
6. **亚克力**：
   - 采用 `SystemBackdropElement` + `DesktopAcrylicBackdrop`，在圆角面板范围内绘制；根 `Grid` 与窗口背景保持透明；
   - 该方案不依赖 DWM 裁剪窗口内容，圆角由元素自身完成；Phase 0 已验证；
   - 主题切换/高对比度/透明效果关闭/不支持时回退纯色；
   - 若后续需要更精确的 tint、失焦变暗或整窗材质，再切换到 `DesktopAcrylicController` + `SystemBackdropConfiguration`，并按窗口 region 处理圆角。
7. **DPI**：manifest `PerMonitorV2`；`GetDpiForWindow` + `XamlRoot.RasterizationScale`；DPI 变化时重新计算窗口物理尺寸与位置。
8. **定位**（首期底部居中）：
   ```text
   workArea = DisplayArea.GetFromWindowId(AppWindow.Id, Primary).WorkArea   // 物理像素，已排除任务栏
   scale    = GetDpiForWindow(hwnd) / 96
   w        = min(888 * scale, workArea.Width  - 24 * scale)
   h        = min(832 * scale, workArea.Height - 16 * scale)
   x        = workArea.X + (workArea.Width - w) / 2
   y        = workArea.Bottom - h - 8 * scale
   ```
   - 任务栏边通过 `SHAppBarMessage(ABM_GETTASKBARPOS)` 获取；本期只验证底部；左/右/上按同样思路预留。
   - 不写死主显示器；首期用 Primary，后续按任务栏/鼠标所在显示器。
9. **阴影**：先验证 DWM 是否自动给无边框窗口阴影；若没有，Phase 3 再尝试 `WS_THICKFRAME` + `WM_NCHITTEST` 返回 `HTCLIENT`、或 `DwmExtendFrameIntoClientArea`、或合成阴影。**阴影不得影响点击命中**。
10. **显示/隐藏**：启动时 `MoveAndResize` → `Activate` → 播放打开动画；关闭时播放关闭动画后退出进程（首期最简）。不做托盘、不做常驻。
11. **调试开关**（仅 DEBUG/命令行）：
    - `--no-auto-close`：禁止失焦自动关闭，方便截图；
    - `--theme dark|light`：强制主题，方便对照；
    - `--reference-overlay <png>`：半透明参考图叠加。
    这些开关不得进入 Release 默认行为。

### 5.2 生命周期与交互

1. 启动：创建窗口 → 组装 Core 服务 → 绑定 VM → 计算位置/尺寸 → `Activate` → 打开动画。
2. 关闭：
   - `Esc`：若搜索有内容先清空；若在类别详情先返回；否则关闭。
   - 点击窗口外 → `Activated` 变为 `Deactivated` → 延迟约 150ms 关闭；若正在打开 flyout（视图选择/账户/电源），不关闭。
   - 关闭前播放 100ms 动画，再 `Close()`/退出。
3. 焦点：
   - 打开后 300ms 内不启用“失焦关闭”，避免启动瞬间被终端/截图工具抢焦点。
   - 不抢系统级焦点；不注册全局热键（本期）。
4. 键盘：
   - `Tab`/`Shift+Tab` 在搜索、固定项、视图选择、类别卡片、页脚间移动；
   - 方向键在固定项/类别卡片间移动；
   - `Enter` 激活当前项；
   - `Esc` 按上面的层级返回/关闭；
   - `Ctrl+F` 聚焦搜索；
   - 任意可打印字符：若焦点不在文本框，则聚焦搜索并输入（中文 IME 场景要求先点击搜索框；首期接受）。
5. 鼠标：
   - 悬停/按下反馈；点击固定项/结果/类别卡片执行对应命令；
   - 滚轮滚动中间内容；
   - 点击空白不关闭（关闭由失焦处理）。
6. 单实例：首期不做；重复启动会出现多个窗口，后续再处理。

### 5.3 UI 组合与导航

- **不使用 `Frame` 页面导航**。开始菜单是单一表面，用 `StartMenuViewModel` 的状态切换内容：
  - `Home`：固定区 + 全部区；
  - `Search`：搜索结果；
  - `CategoryDetail`：某个类别的应用列表；
  - `ViewMode`：Category / Grid / List，只影响“全部”区。
- 根页面结构：
  ```text
  Grid (Rows: Auto, *, Auto)
  ├─ SearchBar                 (固定)
  ├─ ScrollViewer              (滚动)
  │   └─ ContentControl        (Home / Search / CategoryDetail)
  └─ FooterBar                 (固定)
  ```
- Home 内容：
  ```text
  StackPanel
  ├─ PinnedSection
  │   ├─ Header ("已固定" + "显示精简信息")
  │   └─ ItemsRepeater (UniformGridLayout, 8 列)
  └─ AllAppsSection
      ├─ Header ("全部" + "视图: 类别")
      └─ ContentControl (Category / Grid / List)
  ```
- 类别卡片点击 → `CategoryDetail`（返回按钮 + 标题 + 应用网格）；`Esc`/返回恢复之前的滚动位置。
- 视图切换 → 重设“全部”区滚动到顶部，内容淡入；不改变固定区滚动位置。
- 搜索激活 → 内容区替换为结果列表；搜索框保持顶部；清空/`Esc` 回到 Home。
- 所有状态集中在 `StartMenuViewModel`；不以 code-behind 维护业务状态。

### 5.4 数据与 MVVM

- 极简 `ObservableObject`：`INotifyPropertyChanged` + `SetProperty`；不引入 MVVM Toolkit。
- `StartMenuViewModel` 主要成员：
  - `IReadOnlyList<AppTileViewModel> PinnedApps`
  - `IReadOnlyList<CategoryCardViewModel> Categories`
  - `IReadOnlyList<AppTileViewModel> AllApps`
  - `IReadOnlyList<SearchResultViewModel> SearchResults`
  - `string SearchQuery`
  - `bool IsSearchActive`
  - `StartMenuViewMode ViewMode`
  - `bool CompactPins`
  - `CategoryCardViewModel? SelectedCategory`
  - 命令：`ActivateApp`、`OpenCategory`、`GoBack`、`SetViewMode`、`ToggleCompact`、`ClearSearch`、`OpenPowerMenu`、`OpenAccountMenu`
- 视图模型只做展示状态与命令；搜索排序/分组逻辑在 Core。
- 图标：Core 只给 `IconKey`；App 的 `IconService` 转成 `BitmapImage`/路径。VM 可暴露 `ImageSource`（App 层类型），或由转换器处理；首期选后者，保持 VM 简单。
- 手动组装依赖（App.xaml.cs）：
  ```text
  FakeAppCatalog → AppSearchService / JsonSettingsService / MockAppLauncher → StartMenuViewModel
  ```
  不引入 DI 容器。

### 5.5 搜索

- 控件：自定义 `TextBox`（`PlaceholderText="搜索应用、设置和文档"`），左侧放大镜，右侧清除按钮；不用 `AutoSuggestBox` 的下拉列表。
- 触发：点击搜索框、`Ctrl+F`、或页面空白处直接键入（英文/数字；中文先点击）。
- 防抖：约 100ms；输入为空则回到 Home。
- 匹配：显示名、别名/关键字；顺序：完全匹配 > 名称前缀 > 词首匹配 > 名称包含 > 关键字包含；同级按“固定优先、字母序”。
- 结果：最多 8 条；每组可显示“最佳匹配/应用”；匹配子串用强调色高亮（`TextHighlighter` 或 `Run`）。
- 空结果：图标 + “没有找到与“{query}”匹配的内容”。
- `Enter` 激活第一条；`↑/↓` 移动选择；`Esc` 清空并回到 Home。
- 不联网、不查真实文件/设置；设置项也来自 mock 数据。
- 中文输入法正常输入；不实现拼音首字母搜索（后续）。

### 5.6 全部应用：类别 / 网格 / 列表

**类别视图（默认）**

- 4 列卡片（窄屏 3/2 列），每卡 2×2 图标 + 类别名。
- 类别由 mock 数据预置；少于 3 个应用的类别归入“其他”。
- 卡片按“固定项数量/使用频率”排序；首期用固定顺序。
- 点击卡片 → 类别详情：顶部返回按钮 + 类别名 + 应用网格；`Esc` 返回。
- 类别详情不改变“视图”设置；返回后仍在类别视图。

**网格视图**

- 按字母分组，字母标题 + 应用图标网格（每行 6–8 个，随宽度）。
- 适合快速扫视；顺序为字母序，固定项不额外置顶。
- 使用外层 `ItemsRepeater`（字母组）+ 内层 `ItemsRepeater`（应用），或扁平化 `DataTemplateSelector`；首期选前者，代码更直观。

**列表视图**

- 按字母分组的单列列表；每行 48 逻辑像素：图标 24 + 名称 + 类别（次要文字）。
- 同样支持点击激活、键盘上下移动。

**视图选择器**

- “全部”标题右侧：`视图: 类别` + 下折图标；点击弹 `MenuFlyout`（类别/网格/列表），当前项打勾。
- 切换后持久化到设置（`LastViewMode`）；重启恢复。
- flyout 打开期间禁止“失焦关闭”。

### 5.7 固定区与“显示精简信息”

- 固定项来自 mock 的 32 个应用；8 列 × 4 行可见；超出滚动。
- 每项：图标 36 + 两行标签（12px），居中；长名称两行后省略；悬停/按下有轻微背景。
- 点击：模拟启动（写日志/调试输出），播放关闭动画并退出；不启动真实进程。
- **“显示精简信息”**：
  - 位置：固定区标题右侧；文字 + 上折图标；无背景，悬停时轻背景。
  - **默认假设**：点击切换“图标-only 紧凑模式”——隐藏标签、行高降到约 56，按钮文字变为“显示详细信息”，图标改为下折；状态持久化。
  - 真实 26H2 的语义尚未从文档确认；若实机行为不同，按用户确认修改。**这是待确认项，不阻塞视觉骨架。**
- 不做拖拽排序、固定/取消固定、右键菜单（后续）。

### 5.8 页脚、账户与电源（仅模拟）

- 左侧：28px 圆形头像 + 用户名（14px）；点击弹账户菜单：
  - “账户设置”“锁定”“注销”——全部只写日志，不执行真实操作。
- 右侧：电源图标（20px）；点击弹电源菜单：
  - “睡眠”“关机”“重启”——全部只写日志，不执行真实系统命令。
- 菜单使用 `MenuFlyout`；打开时阻止失焦关闭。
- 头像首期用占位图/首字母圆；后续可读取当前账户图片（仍非本期必需）。
- 用户名首期用 mock（如 “Xianene Q”），或读取当前 Windows 显示名；实现时二选一，默认 mock 以免涉及隐私。

### 5.9 推荐区（本期不做）

- 参考机没有“推荐的项目”区；首期**不实现**。
- 架构上给未来留位置：Home 内容是一个 `StackPanel`，未来可在固定区与全部区之间插入 `RecommendedSection`。
- 不引入真实最近文件/网站/提示数据。

### 5.10 主题、DPI、无障碍

- 默认跟随系统深/浅色；`ActualThemeChanged` 时更新画刷、亚克力 tint、DWM 深色模式。
- DEBUG 可用 `--theme` 强制主题；Release 不提供 UI 开关。
- 高对比度：禁用亚克力/阴影，使用系统颜色；所有文字可读、焦点可见。
- 透明效果关闭/节电模式：回退纯色，不崩溃。
- 文本缩放：至少验证 100%/125%/150%；卡片文字不溢出。
- 无障碍：
  - 每个固定项/结果/类别卡片设置 `AutomationProperties.Name`（如“打开 VS Code”“高效工作，类别，8 个应用”）；
  - 搜索框有可读名称与占位符；
  - Tab 顺序合理；焦点框不被圆角/滚动条裁切；
  - 不用纯图标传达唯一信息（图标按钮配 ToolTip/Name）。
- 键盘操作与鼠标操作等价；`Esc` 行为分层明确。

### 5.11 图标策略

- 目标：视觉上接近参考图的真实应用图标，但不做运行时真实枚举。
- 方案 A（优先，视觉最好）：开发期用 `tools/Extract-AppIcons.ps1` 从本机已安装应用一次性提取图标，输出 64×64/96×96 PNG 到 `StartMenu.App/Assets/AppIcons/`；运行时只读静态资源，mock 目录仍由 `FakeAppCatalog` 提供。
- 方案 B（回退）：内置占位图标（Fluent 字形 + 品牌色圆角块），保证结构可验收。
- 图标来源需用户确认；不提交可能引起版权/隐私问题的素材。缺失图标一律回退占位，不崩溃。
- 应用图标显示尺寸 36；类别卡片 32；搜索结果 32；列表 24。

### 5.12 设置持久化

- 路径：`%LOCALAPPDATA%\WindowsReStartMenu\settings.json`。
- 内容：
  ```json
  {
    "schemaVersion": 1,
    "viewMode": "Category",
    "compactPins": false
  }
  ```
- 读取失败/损坏 → 默认值；写入用“临时文件 + 替换”，避免半写。
- 不持久化搜索词、滚动位置、窗口位置。
- 不写注册表、不写系统目录。

### 5.13 日志与错误处理

- DEBUG 用 `Debug.WriteLine`；需要排查时写 `%LOCALAPPDATA%\WindowsReStartMenu\logs\app.log`（简单文本，滚动/截断）。
- 启动失败：写日志并退出；不弹系统级错误框（非打包窗口可用 `MessageBox` P/Invoke，首期可选）。
- 图标缺失、设置损坏、亚克力不支持、定位失败：全部降级，不崩溃。

---

## 6. 分阶段实施计划

> 每个阶段结束都要向用户汇报结果与截图，确认后再进入下一阶段。

### Phase 0：环境与窗口 spike（先验证“能不能”）

> **进度（2026-10-25）**：目录重命名、解决方案与构建配置、App/Core 两项目、官方模板应用并转为非打包均已完成，build/run 通过。**窗口改造已完成**：无边框、`SystemBackdropElement` 圆角亚克力、置顶、无任务栏按钮、底部居中；125% 下 `1110×1040 @ (375,30)`，DWM `corner=2`，`Esc` 与失焦关闭验证通过。阴影细节留到 Phase 3。

- 更正目录名 `SrartMenu.App` → `StartMenu.App`。
- 创建解决方案、`Directory.Build.props`、`Directory.Packages.props`、`global.json`、`.gitignore`。
- 创建 App/Core 两个项目；App 只放一个空窗口 + 亚克力 + 无边框 + 定位。
- 验证：
  - `dotnet build -p:Platform=x64` 成功；
  - exe 启动，窗口无边框、圆角、亚克力、置顶、无任务栏按钮；
  - 底部居中定位正确，125% 下尺寸正确；
  - 失焦/`Esc` 能关闭；无崩溃。
- **Gate**：把窗口截图与构建日志给用户确认；若 DWM 阴影/圆角不理想，先定回退方案。
- 不在本阶段写业务 UI。

### Phase 1：视觉骨架（优先目标：“模样”）

> **进度（2026-10-25）**：搜索框、8×4 固定区、4 列类别卡片、页脚已用 mock 数据渲染；深色主题、圆角亚克力、底部居中。图标暂用「品牌色 + Fluent 字形」占位。`dotnet build` 通过，截图见 `docs/references/local/phase1-window.png`。

- Tokens/Styles/Templates。
- 搜索框、固定区 8×4、全部区 4 列类别卡片（当前 4 类 1 行）、页脚。
- 32 个 mock 固定项；静态图标/占位。
- 深色主题；底部居中；125% 截图对照；滚动。
- **Gate**：与参考图并排/叠加，结构、间距、字号、材质达到“一眼是它”；用户确认后进入交互。

### Phase 2：完整模拟交互

- 搜索（输入、结果、键盘、空状态）。
- 视图切换（类别/网格/列表）+ 设置持久化。
- 类别详情 + 返回。
- 悬停/按下/焦点/滚动条样式。
- 账户/电源 flyout（仅模拟）。
- 打开/关闭/切换动画；动画开关。
- **Gate**：逐项演示交互，用户确认。

### Phase 3：打磨与验收

- 亚克力 tint、圆角、阴影、边框校准。
- 浅色主题、高对比度、透明效果关闭回退。
- 100%/125%/150% DPI；不同窗口宽度断点。
- 无障碍（Name、Tab、焦点、文本缩放）。
- 启动/滚动性能；内存；无泄漏。
- 参考图叠加复核；整理“已知差异”清单。
- **Gate**：验收清单全部通过，用户确认首期完成。

### Phase 4（未来，不在本期）

- `StartMenu.HookHost`：Win 键/任务栏入口拦截、单实例、IPC。
- 真实应用枚举、图标提取、真实搜索、真实启动。
- 托盘/开机启动/设置界面。
- 评估是否切 MSIX 或混合打包。
- 任何系统级修改都必须先取得用户明确同意，并做可回滚设计。

---

## 7. 验收清单

### 7.1 视觉

- [ ] 1920×1080、125%、深色、底部居中：面板尺寸/位置/圆角/亚克力/边框/阴影与参考图结构一致。
- [ ] 搜索框高度、圆角、图标、占位文字、左右留白正确。
- [ ] 固定区 8 列 × 4 行可见；图标 36、标签 12、两行省略；列间距均匀。
- [ ] “已固定”“显示精简信息”“全部”“视图: 类别”位置/字号/颜色正确。
- [ ] 类别区 4 列；当前 4 个类别为 1 行，更多时换行/滚动；卡片 2×2 图标、标签、背景/边框/圆角正确。
- [ ] 页脚头像/用户名/电源位置正确；与内容区有清晰分隔但不突兀。
- [ ] 滚动条细、覆盖式、自动隐藏。
- [ ] 浅色主题可读；高对比度可读；透明效果关闭时回退正常。
- [ ] 无调试边框、占位文字、控制台窗口、标题栏残影。

### 7.2 交互

- [ ] 启动即显示，打开动画流畅；`Esc` 关闭；点击外部关闭；关闭动画后可退出。
- [ ] 搜索：点击输入、`Ctrl+F`、清除、空结果、`Enter` 激活、`Esc` 返回。
- [ ] 视图切换：类别/网格/列表；切换有动画；重启后恢复上次视图。
- [ ] 类别卡片 → 详情 → 返回；滚动位置合理。
- [ ] 固定项/搜索结果/类别项悬停、按下、焦点状态正确；点击有模拟启动日志。
- [ ] “显示精简信息”切换生效并持久化（按默认假设）。
- [ ] 账户/电源 flyout 可打开/关闭；选择只写日志，不执行真实操作。
- [ ] 键盘 Tab/方向键/Enter/Esc 完整可用；焦点可见。
- [ ] 失焦关闭不会误伤 flyout/截图工具；`--no-auto-close` 可用。

### 7.3 工程

- [ ] `dotnet build WindowsReStartMenu.sln -c Debug -p:Platform=x64` 0 error。
- [ ] `dotnet run --project StartMenu.App` 或直接运行 exe 可启动。
- [ ] Core 不引用 WinUI；App 不把业务逻辑写进 code-behind。
- [ ] 无新增未批准依赖；版本集中管理。
- [ ] `README.md` 未被修改；`.vs/`、`bin/`、`obj/`、个人截图未入库。
- [ ] 目录名 `StartMenu.App` 拼写正确。

### 7.4 性能与无障碍

- [ ] 冷启动到窗口可见 < 1.5s（本机）；重复启动无明显卡顿。
- [ ] 滚动 60fps 左右，无明显布局抖动；32 个图标异步/解码尺寸合理。
- [ ] 内存稳定；关闭后进程退出（首期）。
- [ ] 所有可点击项有可读 `AutomationProperties.Name`；Tab 顺序合理；焦点框可见。
- [ ] 文本缩放 150% 不溢出；高对比度下不依赖颜色传达信息。

### 7.5 截图对照流程

1. 关闭动画/失焦关闭（`--no-auto-close`）。
2. 固定壁纸、缩放、任务栏位置。
3. 截取窗口区域；用图像工具与参考图并排。
4. 打开 DEBUG 叠加模式，微调 token。
5. 记录“必须一致/可接受差异/待修”三类；不把 Acrylic 模糊差异当缺陷。

---

## 8. 风险与待确认事项

### 8.1 风险表

| 风险 | 影响 | 应对 |
| --- | --- | --- |
| 26H2 新版开始菜单细节缺少官方文档 | 交互语义/布局偏差 | 以实机截图为准；逐项确认；保留调整点 |
| 参考图任务栏在右侧，面板被裁切 | 尺寸测量偏差 | 以结构和比例为准；底部居中重新验收 |
| 无边框窗口的 DWM 阴影不稳定 | 视觉不像 | Phase 0 验证；回退 `WS_THICKFRAME`/`DwmExtendFrameIntoClientArea`/接受无阴影 |
| 失焦关闭与 flyout/截图冲突 | 误关闭 | 延迟关闭 + flyout guard + `--no-auto-close` |
| 非打包窗口激活/前台限制 | 打开后不聚焦 | `Activate` + `SetForegroundWindow` 兜底；Phase 0 验证 |
| 图标素材版权/隐私 | 无法入库或视觉差异 | 用户确认来源；优先本机静态提取；回退占位 |
| 中文输入法/键盘直输 | 搜索体验不完整 | 点击搜索框输入；后续增强 |
| 亚克力/高对比度/节电模式回退 | 可读性 | 统一走主题资源与 fallback |
| WASDK 2.5.1 较新 | 兼容性 | 锁定版本；升级前验证；本机已 build/run 探针 |
| 多个项目/空项目膨胀 | 违背最简原则 | 首期只建 App + Core；HookHost 不入解决方案 |

### 8.2 待确认（不阻塞视觉骨架，但影响完整体验）

1. **“显示精简信息”的真实语义**：默认按“图标-only 紧凑模式”实现；如果实机是折叠固定区/其他行为，请告知。
2. **类别卡片点击后的真实行为**：默认按“进入类别详情页 + 返回”实现；如果实机是原地展开，请告知。
3. **图标来源**：Phase 1 当前用「品牌色 + Fluent 字形」占位；是否切换为本机已安装应用的真实图标（一次性静态提取，不做运行时枚举）？
4. **参考截图**：是否同意裁剪出“只含开始菜单”的参考图入库（`docs/references/`）？默认只存本地、不入库。
5. **关闭行为**：首期默认“关闭即退出进程”，不做托盘/常驻/热键；是否接受？若希望常驻，需要额外做托盘或调试热键。
6. **用户名/头像**：首期用 mock（“Xianene Q” + 占位头像），还是读取当前 Windows 账户显示名/头像？
7. **单元测试**：首期是否要建 `StartMenu.Core.Tests`（搜索排序/类别分组/设置读写）？默认可在 Phase 2 再加。

---

## 9. 构建与运行

### 9.1 前置条件

- Windows 11 22000+（验收机 26H2 / 26300）。
- .NET SDK **10.0.402**（`dotnet --version`）。
- Windows App Runtime **2.5.1** 已安装（本机已有）。
- 模板包：`Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`（本机已安装 `0.0.7-alpha`；重装命令 `dotnet new install Microsoft.WindowsAppSDK.WinUI.CSharp.Templates`）。
- 不需要 Visual Studio；XAML 设计器不可用，靠构建 + 运行 + 截图。

### 9.2 常用命令

```powershell
# 还原
dotnet restore WindowsReStartMenu.sln

# 构建（x64）
dotnet build WindowsReStartMenu.sln -c Debug -p:Platform=x64

# 运行（二选一）
dotnet run --project .\StartMenu.App\StartMenu.App.csproj -c Debug -p:Platform=x64
.\StartMenu.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\StartMenu.App.exe

# 发布（后续，自包含示例）
dotnet publish .\StartMenu.App\StartMenu.App.csproj -c Release -p:Platform=x64 -p:WindowsAppSDKSelfContained=true
```

### 9.3 常见问题

- **找不到 WinUI 模板**：不需要模板；手写 csproj + App.xaml + MainWindow。已用临时探针验证可行。
- **`WindowsAppSDK` 还原失败**：确认 NuGet 源；锁定 2.5.1；不要混用 1.x/2.x 包。
- **启动报 `0x80070005`/找不到运行时**：安装 Windows App Runtime 2.5.1，或改为自包含发布。
- **窗口没有亚克力**：检查“透明效果”、节电模式、远程桌面；确认根背景透明；确认 controller 的 `IsSupported()`。
- **窗口有标题栏/边框**：确认 `OverlappedPresenter.SetBorderAndTitleBar(false, false)` 在 `Activate` 前执行。
- **窗口不在最前**：`IsAlwaysOnTop=true`；启动时 `Activate` + `SetForegroundWindow` 兜底。
- **关闭时崩溃**：确保释放 `DesktopAcrylicController`、取消事件订阅、停止动画。

---

## 10. 编码约定

- C#：`Nullable=enable`、`ImplicitUsings=enable`、文件级 namespace、4 空格；异步方法以 `Async` 结尾；事件处理器外不用 `async void`。
- XAML：资源键统一前缀（`StartMenu.*`、`TextStyle.*`、`Brush.*`、`Size.*`）；禁止散落魔法数字；颜色/尺寸先进 `Tokens.xaml`。
- code-behind 只做视图相关：焦点、键盘事件转发、窗口 interop；业务状态在 ViewModel/Core。
- P/Invoke 集中在 `Interop/NativeMethods.cs`；使用 `DllImport` 或 `LibraryImport`，加注释说明用途与失败回退。
- 所有用户可见字符串集中在 `Strings.cs`（首期），便于后续换 `.resw`；不散落在 XAML 里难以搜索。
- 日志不输出用户隐私/路径；本地日志目录不入库。
- 提交信息用中文或英文均可，但需说明“做了什么/为什么”；不提交生成物。
- 不修改 `README.md`；流程/决策更新 `AGENTS.md`，设计与实现细节更新本文件。

---

## 11. 决策记录（ADR 摘要）

| 编号 | 决策 | 理由 | 状态 |
| --- | --- | --- | --- |
| ADR-001 | 目标为 26H2 新版开始菜单 | 用户当前系统与参考截图一致 | 已确认 |
| ADR-002 | 首期使用假数据，但交互完整 | 先还原模样，避免真实枚举/搜索的复杂度 | 已确认 |
| ADR-003 | WinUI 3 + .NET 10 + WASDK 2.5.1 非打包 | CLI 可构建；窗口控制直接；已探针验证 | 已确认 |
| ADR-004 | 三目录分层，HookHost 仅占位 | 隔离未来系统钩子风险；首期不引入空项目 | 已确认 |
| ADR-005 | 底部居中为验收基准，定位预留任意边 | 最常见布局；参考机右侧任务栏只用于视觉参考 | 已确认 |
| ADR-006 | 不做推荐区 | 参考机未显示；减少首期范围 | 默认假设 |
| ADR-007 | “显示精简信息”= 图标-only 紧凑模式 | 文字最接近的解释；待实机确认 | 待确认 |
| ADR-008 | 类别卡片点击进入详情页 | 结构清晰、易实现；待实机确认 | 待确认 |
| ADR-009 | 关闭即退出，不做托盘/常驻 | 最简实现；符合“先还原模样” | 默认假设 |
| ADR-010 | 图标用本机静态提取或占位 | 兼顾视觉与“不做真实枚举” | 待确认 |

---

