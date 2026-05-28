# WPF 权限管理控件实现计划

## 一、需求分析

### 1.1 核心需求

根据用户描述，需要实现一个 WPF 权限管理控件，具备以下特性：

| 需求点       | 描述                                    | 优先级   |
| :-------- | :------------------------------------ | :---- |
| 控件状态控制    | 控制其他控件的 `IsEnabled` 和 `Visibility` 状态 | 高     |
| 角色配置      | 支持多角色配置                               | 高     |
| 权限配置      | 每个角色包含不同的权限集合，支持权限合并               | 高     |
| 黑名单模式     | **配置不可用的控件**（而非可用的控件），减少配置量           | 高     |
| 控件识别      | 通过自动化机制捕捉应用中的所有控件                    | 高     |
| **可视化配置** | **提供图形化界面配置权限 JSON 文件**               | **高** |
| **控件层级**   | **支持控件树状展示和层级选择**                    | **高** |
| **动态控件**   | **支持动态生成控件的权限控制**                    | **高** |

### 1.2 设计理念

#### 1.2.1 黑名单模式
与传统的白名单（配置可用控件）不同，采用黑名单（配置不可用控件），适合控件数量多但受限控件少的场景。例如：一个窗口有100个控件，只有5个需要限制，黑名单只需配置5个，而白名单需要配置95个。

#### 1.2.2 声明式标记（核心概念）
**声明式标记**是指在 XAML 中直接为控件添加权限标识，而不需要在代码中编写逻辑。通过 WPF 的**附加属性**（Attached Property）实现：

**传统方式（命令式）：**
```csharp
// 在代码中手动检查每个控件
btnDelete.IsEnabled = PermissionManager.Instance.CheckControlEnabled("btnDelete");
btnSave.IsEnabled = PermissionManager.Instance.CheckControlEnabled("btnSave");
// ... 每个控件都需要写一行代码
```

**声明式方式（推荐）：**
```xml
<!-- 在 XAML 中直接声明权限标识 -->
<Button x:Name="btnDelete" 
        pm:PermissionBehavior.PermissionTag="btnDelete" 
        Content="删除" />
<Button x:Name="btnSave" 
        pm:PermissionBehavior.PermissionTag="btnSave" 
        Content="保存" />
```

**优势：**
* 权限配置与 UI 定义分离但紧密关联
* 无需在代码中逐个检查控件
* 支持设计时预览和工具支持

#### 1.2.3 集中管理
通过权限管理器统一管理角色和权限配置，所有权限检查逻辑集中在一处，便于维护和扩展。

#### 1.2.4 控件检索机制（核心实现）
针对"如何捕捉应用中的众多控件"的问题，实现**自动化控件检索**，支持三种扫描模式：

| 模式           | 是否需要手动标记 | 说明                                  | 适用场景           |
| :----------- | :------- | :---------------------------------- | :------------- |
| **精确模式**     | 是        | 只有添加了 `PermissionTag` 的控件才会被扫描      | 控件数量多，只需控制部分控件 |
| **自动模式**     | 否        | 自动扫描所有有 `Name` 属性的控件，使用 `Name` 作为标识 | 控件数量少，希望全部控制   |
| **混合模式（默认）** | 可选       | 优先使用 `PermissionTag`，没有则使用 `Name`   | 灵活控制，关键控件自定义标识 |

**推荐使用混合模式**，关键控件自定义 `PermissionTag`，普通控件自动使用其 `Name` 属性。

#### 1.2.5 控件层级支持
支持控件的树状结构展示，选择父节点时自动禁用其下所有子控件。配置文件存储控件的完整路径，权限检查时自动匹配子路径。

#### 1.2.6 动态控件支持
- **自动注册**：控件创建并加入 VisualTree 时自动注册并应用权限
- **延迟扫描**：提供防抖和增量扫描机制避免性能问题
- **异步支持**：确保异步加载的控件在加入 VisualTree 时权限生效

---

## 二、技术方案

### 2.1 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                      应用层 (Application)                   │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              控件树 (VisualTree)                    │    │
│  │  Window → Grid → Button (btnSave)                  │    │
│  │                → StackPanel → TextBox (txtName)     │    │
│  └────────────┬────────────────────────────────────────┘    │
└───────────────┼─────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│              PermissionBehavior (附加属性层)                 │
│  - PermissionTag: 标记控件的权限标识符                       │
│  - AutoCheck: 是否自动检查权限                               │
└─────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│              PermissionManager (核心逻辑层)                  │
│  - 扫描模式配置 (ScanMode)                                 │
│  - 延迟扫描机制 (Debounce)                                 │
│  - 增量扫描优化 (Incremental Scan)                         │
│  - 权限继承处理 (Inheritance)                             │
└─────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│              配置模型 (Configuration Models)                 │
│  - Role: 角色定义                                           │
│  - Permission: 权限定义（支持多权限合并）                      │
│  - PermissionConfig: 完整配置                                │
└─────────────────────────────────────────────────────────────┘
                │
                ▼
┌─────────────────────────────────────────────────────────────┐
│                    配置文件 (JSON)                           │
│  permissions.json（存储控件完整路径）                         │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 核心组件

#### 2.2.1 配置模型

| 类名                 | 职责   | 关键字段                                         |
| :----------------- | :--- | :------------------------------------------- |
| `Role`             | 角色定义 | `Name`, `Permissions`（支持多权限）                  |
| `Permission`       | 权限定义 | `Name`, `DisabledControls`, `HiddenControls`（完整路径） |
| `PermissionConfig` | 完整配置 | `Roles`, `CurrentRole`, `ScanMode`            |

#### 2.2.2 权限管理器

**PermissionManager** 核心方法：

| 方法名                   | 功能说明         | 参数                      | 返回值    |
| :-------------------- | :----------- | :---------------------- | :----- |
| `LoadConfiguration`   | 加载权限配置       | `string filePath`       | `void` |
| `SetCurrentRole`      | 设置当前角色       | `string roleName`       | `void` |
| `GetCurrentRole`      | 获取当前角色       | -                       | `Role` |
| `CheckControlEnabled` | 检查控件是否可用（含继承） | `string controlPath`    | `bool` |
| `CheckControlVisible` | 检查控件是否可见（含继承） | `string controlPath`    | `bool` |
| `ApplyPermissions`    | 遍历并应用权限      | `DependencyObject root` | `void` |
| `RequestScan`         | 请求延迟扫描       | -                       | `void` |
| `RegisterControl`     | 手动注册控件       | `string tag, FrameworkElement control` | `void` |

#### 2.2.3 附加属性

**PermissionBehavior** 附加属性：

| 属性名             | 类型          | 说明                                 |
| :-------------- | :---------- | :--------------------------------- |
| `PermissionTag` | `string`    | 控件的权限标识符（可选，优先使用）              |
| `AutoCheck`     | `bool`      | 是否自动检查权限（默认 true）                  |
| `CheckMode`     | `CheckMode` | 检查模式：Enabled/Visible/Both（默认 Both） |

#### 2.2.4 可视化配置界面

**PermissionConfigEditor** 配置编辑器窗口：

| 组件                 | 功能说明                     |
| :----------------- | :----------------------- |
| `RoleListView`     | 角色列表，支持增删改查            |
| `PermissionEditor` | 权限详情编辑，管理禁用/隐藏控件列表     |
| `ControlSelector`  | **控件树选择器，支持层级选择**       |
| `ConfigPreview`    | 配置预览，实时显示 JSON 内容      |

**配置编辑器功能：**

| 功能      | 描述                                  |
| :------ | :---------------------------------- |
| 角色管理    | 添加、删除、重命名角色                       |
| 权限编辑    | 为每个角色配置禁用/隐藏的控件                  |
| 控件选择    | **树状展示控件，支持选择父节点（自动包含子控件）**       |
| 配置导入/导出 | 支持 JSON 文件的导入导出                     |
| 配置验证    | 实时验证配置的有效性                        |
| **热更新**   | **默认启用，配置文件变化时自动重新加载**            |

---

## 三、核心实现机制

### 3.1 延迟扫描机制

#### 3.1.1 防抖机制（Debounce）

使用防抖避免频繁扫描：

```csharp
private DispatcherTimer _scanDebounceTimer;

public void RequestScan()
{
    if (_scanDebounceTimer != null)
    {
        _scanDebounceTimer.Stop();
    }
    
    _scanDebounceTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(500)
    };
    
    _scanDebounceTimer.Tick += (s, e) =>
    {
        _scanDebounceTimer.Stop();
        PerformIncrementalScan();
    };
    
    _scanDebounceTimer.Start();
}
```

**效果**：在 500ms 内多次调用 `RequestScan()` 只会执行一次扫描。

#### 3.1.2 增量扫描（避免全量扫描）

```csharp
private HashSet<string> _scannedPaths = new HashSet<string>();

public void PerformIncrementalScan(DependencyObject root)
{
    var newPaths = new HashSet<string>();
    CollectControlPaths(root, "", newPaths);
    
    var addedPaths = newPaths.Except(_scannedPaths);
    var removedPaths = _scannedPaths.Except(newPaths);
    
    foreach (var path in addedPaths)
    {
        RegisterControl(path, FindControlByPath(root, path));
    }
    
    foreach (var path in removedPaths)
    {
        UnregisterControl(path);
    }
    
    _scannedPaths = newPaths;
}
```

**效果**：只处理新增和删除的控件，避免重复处理已有控件。

#### 3.1.3 缓存机制

```csharp
private Dictionary<string, WeakReference<FrameworkElement>> _controlCache = 
    new Dictionary<string, WeakReference<FrameworkElement>>();

public void RegisterControl(string path, FrameworkElement control)
{
    _controlCache[path] = new WeakReference<FrameworkElement>(control);
}
```

**效果**：缓存控件引用但不阻止垃圾回收。

### 3.2 异步控件加载支持

当控件加入 VisualTree 时自动应用权限：

```csharp
public void ApplyControlPermission(FrameworkElement control, string path)
{
    if (!control.IsLoaded)
    {
        RoutedEventHandler loadedHandler = null;
        loadedHandler = (s, e) =>
        {
            control.Loaded -= loadedHandler;
            ApplyControlPermissionInternal(control, path);
        };
        control.Loaded += loadedHandler;
        return;
    }
    
    ApplyControlPermissionInternal(control, path);
}
```

**效果**：异步加载的控件在完全加载后自动应用权限，避免闪烁。

### 3.3 权限继承机制

检查控件权限时自动考虑父控件的权限：

```csharp
public bool CheckControlEnabled(string controlPath)
{
    var currentPath = controlPath;
    
    while (!string.IsNullOrEmpty(currentPath))
    {
        if (_disabledPaths.Contains(currentPath))
        {
            return false;
        }
        
        currentPath = GetParentPath(currentPath);
    }
    
    return true;
}

private string GetParentPath(string path)
{
    var lastDotIndex = path.LastIndexOf('.');
    return lastDotIndex > 0 ? path.Substring(0, lastDotIndex) : null;
}
```

**效果**：选择父节点时自动禁用其下所有子控件。

---

## 四、配置文件格式

采用 JSON 格式存储配置，支持控件完整路径：

```json
{
  "ScanMode": "Hybrid",
  "CurrentRole": "Operator",
  "Roles": [
    {
      "Name": "Admin",
      "Permissions": [
        {
          "Name": "FullAccess",
          "DisabledControls": [],
          "HiddenControls": []
        }
      ]
    },
    {
      "Name": "Operator",
      "Permissions": [
        {
          "Name": "BasicAccess",
          "DisabledControls": [
            "MainWindow.Grid.btnDelete",
            "MainWindow.Grid.btnExport"
          ],
          "HiddenControls": [
            "MainWindow.Menu.mnuSettings"
          ]
        }
      ]
    },
    {
      "Name": "ReadOnly",
      "Permissions": [
        {
          "Name": "ReadOnlyAccess",
          "DisabledControls": [
            "MainWindow.Grid"  // 禁用整个 Grid 及其所有子控件
          ],
          "HiddenControls": [
            "MainWindow.Menu"
          ]
        }
      ]
    }
  ]
}
```

---

## 五、项目结构

```
DeviceEngine.PermissionManagement/
├── src/
│   ├── DeviceEngine.PermissionManagement/
│   │   ├── Models/
│   │   │   ├── Role.cs
│   │   │   ├── Permission.cs
│   │   │   ├── PermissionConfig.cs
│   │   │   └── ScanMode.cs
│   │   ├── Managers/
│   │   │   ├── PermissionManager.cs
│   │   │   └── IPermissionManager.cs
│   │   ├── Behaviors/
│   │   │   └── PermissionBehavior.cs
│   │   ├── Converters/
│   │   │   └── PermissionToVisibilityConverter.cs
│   │   ├── Editors/
│   │   │   ├── PermissionConfigEditor.xaml
│   │   │   ├── PermissionConfigEditor.xaml.cs
│   │   │   ├── RoleListViewModel.cs
│   │   │   ├── PermissionEditorViewModel.cs
│   │   │   └── ControlTreeViewModel.cs
│   │   ├── Properties/
│   │   │   └── AssemblyInfo.cs
│   │   └── DeviceEngine.PermissionManagement.csproj
│   ├── DeviceEngine.PermissionManagement.Demo/
│   │   ├── App.xaml
│   │   ├── App.xaml.cs
│   │   ├── MainWindow.xaml
│   │   ├── MainWindow.xaml.cs
│   │   ├── Config/
│   │   │   └── permissions.json
│   │   └── DeviceEngine.PermissionManagement.Demo.csproj
│   └── DeviceEngine.PermissionManagement.Tests/
│       ├── PermissionManagerTests.cs
│       ├── PermissionBehaviorTests.cs
│       └── DeviceEngine.PermissionManagement.Tests.csproj
└── DeviceEngine.PermissionManagement.sln
```

---

## 六、实现步骤

| 序号 | 任务             | 描述                                          | 预计工时   |
| :- | :------------- | :------------------------------------------ | :----- |
| 1  | 创建权限配置模型类      | 定义 Role、Permission、PermissionConfig、ScanMode     | 2h     |
| 2  | 创建权限管理器接口      | 定义 IPermissionManager 接口                        | 1h     |
| 3  | 实现权限管理器核心逻辑    | 实现扫描、注册、权限检查、继承逻辑                      | 5h     |
| 4  | 创建附加属性         | 实现 PermissionBehavior，支持自动注册                   | 3h     |
| 5  | 创建配置加载器        | 支持 JSON 配置文件读写和热更新                         | 2h     |
| 6  | **创建可视化配置编辑器** | **实现 PermissionConfigEditor，支持树状控件选择**       | **6h** |
| 7  | 创建示例项目         | 演示权限管理控件的使用                                 | 3h     |
| 8  | 编写单元测试         | 验证权限管理逻辑、继承机制、动态控件支持                  | 3h     |

---

## 七、关键技术点

### 7.1 VisualTree 遍历与路径生成

```csharp
private void CollectControlPaths(DependencyObject parent, string parentPath, HashSet<string> paths)
{
    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
    {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is FrameworkElement fe)
        {
            string tag = PermissionBehavior.GetPermissionTag(fe);
            string name = string.IsNullOrEmpty(tag) ? fe.Name : tag;
            
            if (!string.IsNullOrEmpty(name))
            {
                string path = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}.{name}";
                paths.Add(path);
                CollectControlPaths(child, path, paths);
            }
            else
            {
                CollectControlPaths(child, parentPath, paths);
            }
        }
        else
        {
            CollectControlPaths(child, parentPath, paths);
        }
    }
}
```

### 7.2 附加属性值变更回调

```csharp
private static void OnPermissionTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is FrameworkElement element && e.NewValue is string tag && !string.IsNullOrEmpty(tag))
    {
        Application.Current.Dispatcher.BeginInvoke(() => 
        {
            PermissionManager.Instance.RegisterControl(tag, element);
            PermissionManager.Instance.ApplyControlPermission(element, tag);
        });
    }
}
```

### 7.3 动态角色切换

```csharp
public void SetCurrentRole(string roleName)
{
    _currentRoleName = roleName;
    RaiseRoleChanged();
    ApplyPermissionsToAllRegisteredControls();
}
```

---

## 八、风险与应对

| 风险         | 描述                      | 应对措施                                             |
| :--------- | :---------------------- | :----------------------------------------------- |
| 性能问题       | 控件数量过多时遍历耗时             | 1. 防抖机制 2. 增量扫描 3. 缓存控件引用 4. 使用 WeakReference |
| 配置错误       | 配置文件格式错误导致崩溃            | 1. 添加配置验证 2. 提供默认配置 3. 错误日志记录                    |
| 控件标识冲突     | 不同控件使用相同的 PermissionTag | 1. 建议使用完整路径 2. 添加重复检测 3. 警告提示                    |
| 权限遗漏       | 某些控件未被权限管理覆盖            | 1. 提供权限扫描工具 2. 日志记录未标记的控件                        |
| 异步加载问题     | 动态控件权限不生效               | 1. 监听 Loaded 事件 2. 延迟扫描机制 3. 自动注册机制                 |
| **设计时干扰**   | **设计器中权限生效影响开发**        | **设计时禁用权限检查，所有控件保持启用状态**                        |

---

## 九、主体应用集成指南

### 9.1 目标框架
- **Target Framework**: .NET Framework 4.8 (net48)
- **Dependencies**: Newtonsoft.Json

### 9.2 集成步骤

#### 9.2.1 第一步：添加引用

**方式一：项目引用**
1. 在解决方案资源管理器中右键点击项目
2. 选择"添加" → "引用"
3. 在"项目"选项卡中选择 `DeviceEngine.PermissionManagement`
4. 点击"确定"

**方式二：NuGet 包（可选）**
```
Install-Package DeviceEngine.PermissionManagement
```

#### 9.2.2 第二步：在 XAML 中引用命名空间

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:pm="clr-namespace:DeviceEngine.PermissionManagement.Behaviors;assembly=DeviceEngine.PermissionManagement"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
```

#### 9.2.3 第三步：标记需要权限控制的控件

**基本用法（自动使用控件 Name 属性）：**
```xml
<!-- 不需要手动添加 PermissionTag，会自动使用 x:Name -->
<Button x:Name="btnSave" Content="保存" />
<Button x:Name="btnDelete" Content="删除" />
<Button x:Name="btnExport" Content="导出" />
<TextBox x:Name="txtUserName" />
```

**自定义标识（使用 PermissionTag）：**
```xml
<!-- 使用自定义标识，不受控件名称变更影响 -->
<Button x:Name="btnDeleteUser" pm:PermissionBehavior.PermissionTag="UserManagement_Delete" Content="删除用户" />
<MenuItem x:Name="mnuAdvancedSettings" pm:PermissionBehavior.PermissionTag="System_Settings" Header="高级设置" />
```

**容器控件（支持层级继承）：**
```xml
<!-- 禁用整个 StackPanel 会自动禁用其下所有子控件 -->
<StackPanel x:Name="pnlAdminActions">
    <Button x:Name="btnCreateUser" Content="创建用户" />
    <Button x:Name="btnManageRoles" Content="管理角色" />
    <Button x:Name="btnSystemLogs" Content="系统日志" />
</StackPanel>
```

#### 9.2.4 第四步：初始化权限管理器

在 `App.xaml.cs` 的 `OnStartup` 方法中初始化：

```csharp
using DeviceEngine.PermissionManagement.Managers;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // 1. 加载权限配置
        PermissionManager.Instance.LoadConfiguration("Config/permissions.json");
        
        // 2. 设置当前用户角色（通常从登录信息获取）
        string currentUserRole = GetCurrentUserRole(); // 例如: "Operator"
        PermissionManager.Instance.SetCurrentRole(currentUserRole);
        
        // 3. 启用配置热更新（可选，默认启用）
        PermissionManager.Instance.EnableHotReload = true;
    }
    
    private string GetCurrentUserRole()
    {
        // 从登录服务或数据库获取当前用户角色
        return "Operator";
    }
}
```

在每个窗口的 `Loaded` 事件中扫描控件：

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 扫描当前窗口的所有控件并应用权限
        PermissionManager.Instance.ScanControls(this);
    }
}
```

#### 9.2.5 第五步：运行时切换角色

```csharp
private void cmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    string selectedRole = (cmbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
    
    // 切换角色，权限会自动应用到所有已注册的控件
    PermissionManager.Instance.SetCurrentRole(selectedRole);
}
```

#### 9.2.6 第六步：打开配置编辑器

```csharp
private void btnOpenConfigEditor_Click(object sender, RoutedEventArgs e)
{
    var editor = new PermissionConfigEditor();
    editor.Owner = this;
    
    // 配置编辑器关闭后自动重新加载配置
    editor.Closed += (s, args) =>
    {
        PermissionManager.Instance.ReloadConfiguration();
    };
    
    editor.ShowDialog();
}
```

### 9.3 动态控件处理

#### 9.3.1 手动注册动态创建的控件

```csharp
private void AddDynamicButton()
{
    var dynamicBtn = new Button
    {
        Name = "btnDynamicAction",
        Content = "动态按钮"
    };
    
    // 手动注册控件
    PermissionManager.Instance.RegisterControl("btnDynamicAction", dynamicBtn);
    
    // 添加到容器
    pnlDynamicControls.Children.Add(dynamicBtn);
}
```

#### 9.3.2 使用附加属性注册

```csharp
private void AddDynamicButtonWithTag()
{
    var dynamicBtn = new Button
    {
        Content = "动态按钮"
    };
    
    // 设置 PermissionTag（会自动触发注册）
    PermissionBehavior.SetPermissionTag(dynamicBtn, "DynamicAction_Special");
    
    pnlDynamicControls.Children.Add(dynamicBtn);
}
```

### 9.4 完整示例

**MainWindow.xaml：**
```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:pm="clr-namespace:DeviceEngine.PermissionManagement.Behaviors;assembly=DeviceEngine.PermissionManagement"
        Title="权限管理示例" Height="400" Width="600">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        
        <!-- 角色选择器 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10" >
            <Label Content="当前角色：" />
            <ComboBox x:Name="cmbRole" Width="150" SelectionChanged="cmbRole_SelectionChanged">
                <ComboBoxItem Content="Admin" />
                <ComboBoxItem Content="Operator" />
                <ComboBoxItem Content="ReadOnly" />
            </ComboBox>
        </StackPanel>
        
        <!-- 功能按钮区域 -->
        <StackPanel Grid.Row="1" Margin="10" Orientation="Vertical" >
            <Button x:Name="btnSave" Content="保存" Margin="5" Padding="10,5" />
            <Button x:Name="btnDelete" Content="删除" Margin="5" Padding="10,5" />
            <Button x:Name="btnExport" Content="导出" Margin="5" Padding="10,5" />
            <Button x:Name="btnAdvanced" Content="高级设置" Margin="5" Padding="10,5" />
            
            <Separator Margin="5" />
            
            <StackPanel x:Name="pnlAdminPanel" Orientation="Vertical">
                <Button x:Name="btnManageUsers" Content="管理用户" Margin="5" Padding="10,5" />
                <Button x:Name="btnManageRoles" Content="管理角色" Margin="5" Padding="10,5" />
            </StackPanel>
        </StackPanel>
        
        <!-- 配置按钮 -->
        <StackPanel Grid.Row="2" Margin="10" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button x:Name="btnConfig" Content="配置权限" Click="btnConfig_Click" Padding="10,5" />
        </StackPanel>
    </Grid>
</Window>
```

**MainWindow.xaml.cs：**
```csharp
using DeviceEngine.PermissionManagement.Managers;
using DeviceEngine.PermissionManagement.Editors;
using System.Windows;

namespace MyApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 扫描当前窗口控件并应用权限
            PermissionManager.Instance.ScanControls(this);
        }
        
        private void cmbRole_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem is ComboBoxItem selectedItem)
            {
                PermissionManager.Instance.SetCurrentRole(selectedItem.Content.ToString());
            }
        }
        
        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            var editor = new PermissionConfigEditor
            {
                Owner = this
            };
            
            editor.Closed += (s, args) =>
            {
                PermissionManager.Instance.ReloadConfiguration();
                PermissionManager.Instance.ScanControls(this);
            };
            
            editor.ShowDialog();
        }
    }
}
```

### 9.5 配置文件示例

**Config/permissions.json：**
```json
{
  "ScanMode": "Hybrid",
  "CurrentRole": "Operator",
  "Roles": [
    {
      "Name": "Admin",
      "Permissions": [
        {
          "Name": "FullAccess",
          "DisabledControls": [],
          "HiddenControls": []
        }
      ]
    },
    {
      "Name": "Operator",
      "Permissions": [
        {
          "Name": "BasicAccess",
          "DisabledControls": ["btnDelete", "btnAdvanced"],
          "HiddenControls": []
        }
      ]
    },
    {
      "Name": "ReadOnly",
      "Permissions": [
        {
          "Name": "ReadOnlyAccess",
          "DisabledControls": ["btnSave", "btnDelete", "btnExport"],
          "HiddenControls": ["pnlAdminPanel"]
        }
      ]
    }
  ]
}
```

### 9.6 预期行为

| 角色 | btnSave | btnDelete | btnExport | btnAdvanced | pnlAdminPanel |
| :--- | :--- | :--- | :--- | :--- | :--- |
| Admin | ✅ 启用 | ✅ 启用 | ✅ 启用 | ✅ 启用 | ✅ 可见 |
| Operator | ✅ 启用 | ❌ 禁用 | ✅ 启用 | ❌ 禁用 | ✅ 可见 |
| ReadOnly | ❌ 禁用 | ❌ 禁用 | ❌ 禁用 | ✅ 启用 | ❌ 隐藏 |

---

## 十、测试计划

### 10.1 单元测试覆盖

| 测试项      | 测试内容              | 预期结果                   |
| :------- | :---------------- | :--------------------- |
| 配置加载     | 加载有效/无效配置文件       | 有效配置正常加载，无效配置抛出异常      |
| 角色切换     | 切换不同角色            | 权限状态正确更新               |
| 控件禁用     | 验证禁用列表中的控件        | IsEnabled = false      |
| 控件隐藏     | 验证隐藏列表中的控件        | Visibility = Collapsed |
| 权限继承     | 验证父控件禁用时子控件状态    | 子控件自动禁用                |
| 权限合并     | 验证多权限合并            | 正确合并禁用/隐藏列表           |
| 增量扫描     | 动态添加/删除控件          | 正确识别新增和删除的控件          |
| 异步加载     | 异步创建控件             | 控件加载后权限正确生效           |

### 10.2 集成测试

| 测试场景   | 描述                    |
| :------- | :-------------------- |
| 完整流程   | 启动应用→加载配置→切换角色→验证控件状态 |
| 动态切换   | 运行时切换角色，验证控件状态实时更新    |
| 热更新     | 修改配置文件，验证自动重新加载      |
| 层级选择   | 选择父节点，验证子控件自动禁用      |
| 异常处理   | 配置文件损坏、角色不存在等异常情况     |

---

## 十一、后续优化

* [ ] 添加权限变更事件通知
* [ ] 支持权限组（批量管理多个控件）
* [ ] 添加权限调试工具（可视化显示控件权限状态）
* [ ] 支持数据库存储配置
* [ ] 添加权限验证规则（如表达式权限）
* [ ] 支持控件模板中的权限标记