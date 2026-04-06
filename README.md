# L2Bot 2.0

Yet another ingame bot for Lineage 2 Interlude.

[English](#overview) | [中文](#中文说明)

![image_2023-08-09_00-43-10](https://github.com/k0t9i/L2Bot2.0/assets/7733997/8b4356b0-f362-4ba8-8ca2-8c5cb949d873)

---

## Overview

L2Bot 2.0 is a modular bot framework for Lineage 2 Interlude. It consists of an injectable DLL that hooks into the game client, a core C++ framework, and a WPF desktop client for control and visualization.

## Architecture

```
┌─────────────────┐     Named Pipe (JSON)     ┌─────────────────┐
│   L2BotDll.dll  │ ◄──────────────────────► │  Client (WPF)   │
│  (injected into │                           │  - AI Engine     │
│   l2.exe)       │                           │  - Map / UI      │
└────────┬────────┘                           │  - Pathfinding   │
         │                                    └─────────────────┘
┌────────┴────────┐
│   L2BotCore     │
│  (C++ Framework)│
└────────┬────────┘
┌────────┴────────┐
│ InjectionLibrary│
│  (DLL Injector) │
└─────────────────┘
```

## Project Components

| Component | Language | Description |
|-----------|----------|-------------|
| **L2BotCore** | C++ | Bot framework — defines entities, events, repositories, and transports |
| **L2BotDll** | C++ | Injectable DLL for Lineage 2 Interlude — reads game memory and relays data |
| **InjectionLibrary** | C++ | Helper library for DLL injection via Windows hooks |
| **Client** | C# (.NET 6.0, WPF) | Desktop client with AI, map visualization, and configuration UI |

## Prerequisites

- **Visual Studio 2022** with:
  - C++ Desktop Development workload (for L2BotCore, L2BotDll, InjectionLibrary)
  - .NET 6.0 SDK (for Client)
- **Lineage 2 Interlude** client (private server recommended)
- **L2jGeodataPathFinder.dll** — external pathfinding library ([L2jGeodataPathFinder](https://github.com/k0t9i/L2jGeodataPathFinder))
- **Geodata files** — L2 Interlude geodata placed in `Client/Assets/geodata/`

## Build

1. Open `L2Bot.sln` in Visual Studio 2022
2. Build the C++ projects first (x86 or x64 matching your L2 client):
   - `InjectionLibrary`
   - `L2BotCore`
   - `L2BotDll`
3. Build the C# Client:
   ```
   dotnet build Client/Client.csproj
   ```
   Or build via Visual Studio (Client project).

## Configuration

Edit `Client/config.json`:

```json
{
  "DLLName": "L2BotDll.dll",
  "ConnectionPipeName": "PipeL2Bot",
  "MaxPassableHeight": 30,
  "GeoDataDirectory": "geodata",
  "NodeWaitingTime": 3.0,
  "NodeDistanceTolerance": 8,
  "NextNodeDistanceTolerance": 16
}
```

| Key | Description |
|-----|-------------|
| `DLLName` | Name of the injectable DLL file |
| `ConnectionPipeName` | Named pipe identifier (must match DLL) |
| `MaxPassableHeight` | Maximum terrain height the pathfinder considers passable |
| `GeoDataDirectory` | Folder name under `Assets/` containing geodata files |
| `NodeWaitingTime` | Seconds to wait at each path node before declaring stuck |
| `NodeDistanceTolerance` | Distance (units) to consider a node reached |
| `NextNodeDistanceTolerance` | Distance to advance early to next node |

## Usage

### 1. Start the Game

Launch your Lineage 2 Interlude client and log in with your character.

### 2. Launch the Client

Run `Client.exe`. The client will:
- Load `L2BotDll.dll` into the game process
- Connect via named pipe
- Display the game world on the map

### 3. AI Control

From the menu bar: **AI options**

- **Start / Stop** — Toggle AI execution
- **AI type** — Select between:
  - **Combat** — Automated hunting (attack, spoil, pickup, rest)
  - **Deleveling** — Attack town guards to lose experience
- **Config** — Open AI configuration dialog

### 4. Combat AI Settings

| Tab | Settings |
|-----|----------|
| **Combat** | Attack distances (melee/bow), skill conditions, auto soulshot/spiritshot |
| **Mobs** | Max delta Z, included/excluded mobs, level range |
| **Drop** | Pickup toggle, radius, delta Z, included/excluded items |
| **Spoil** | Spoil/sweep skills, priority, mob filters |
| **Rest** | HP/MP thresholds to start/stop resting |

### 5. Client UI

| Area | Description |
|------|-------------|
| **Map** (left) | Real-time map with creatures, drops, path visualization, combat zone |
| **Environment tab** | Sorted creature/drop lists with click-to-target |
| **Skills tab** | Active and passive skill panels |
| **Inventory tab** | Items and quest items |
| **Log tab** | Activity log with timestamped notifications |
| **Chat** (bottom-left) | In-game chat messages with auto-scroll |
| **Hero panel** (bottom-right) | Character stats, position, exp, target info |
| **Status bar** | Connection state, movement state, AI state, last notification |

### 6. Map Interaction

- **Left click** on creature → Acquire target
- **Right click** on creature → Move to creature
- **Left click** on drop → Pick up
- Mouse position and zoom level displayed in map controls

## Recent Changes (Fork)

### Bug Fixes
- **Pathfinding**: Fixed `Random` bug where a new instance was created per path node, causing identical coordinates when processed within the same millisecond. Now uses `Random.Shared`.
- **Pipe Transport**: Fixed silent failure when named pipe closes (`readBytes == 0`). Added `Connected` / `Disconnected` events.
- **Reconnection**: Fixed infinite spin loop on disconnect. Added exponential backoff (1s → 30s max, 10 attempts).
- **Error Handling**: Parser and handler exceptions are now propagated to the UI instead of being silently swallowed.
- **Startup Protection**: Added try-catch around application startup with MessageBox error display and a global `DispatcherUnhandledException` handler.

### Stability Infrastructure
- **NotificationEvent** system: WorldHandler validation warnings are now published through the event bus and displayed in the Activity Log tab.
- **MovementState** tracking: `AsyncPathMover` now exposes `Idle`, `PathFinding`, `Moving`, `Stuck` states with a `StateChanged` event.

### UI Completion
- **Status Bar**: Connection indicator (green/red), movement state, AI state, and last notification.
- **Activity Log tab**: Timestamped, auto-scrolling log of all notifications and errors.
- **Chat panel**: Added header label and auto-scroll behavior.

## AI Overview

### Combat AI State Machine

```
       ┌──────┐
       │ Dead │◄──── (any state, on death)
       └──┬───┘
          │ revived
          ▼
       ┌──────┐     attackers     ┌────────────┐
       │ Idle │──────────────────►│ FindTarget  │
       └──┬───┘                   └──────┬─────┘
          │ low HP/MP                    │ target found
          ▼                              ▼
       ┌──────┐                   ┌──────────────┐
       │ Rest │                   │ MoveToTarget  │
       └──────┘                   └──────┬───────┘
                                         │ in range
                                         ▼
                                  ┌──────────┐
                                  │  Attack  │
                                  └──────┬───┘
                                         │ target dead
                                         ▼
                                  ┌──────────┐
                                  │  Pickup  │
                                  └──────────┘
```

### Deleveling AI State Machine

```
  Idle → FindGuard → MoveToTarget → AttackGuard → Dead → Idle
```

## Pathfinding

Uses [L2jGeodataPathFinder](https://github.com/k0t9i/L2jGeodataPathFinder) for geodata-based path calculation with line-of-sight checks.

![image_2023-10-29_20-53-56](https://github.com/k0t9i/L2Bot2.0/assets/7733997/104e5ff2-7435-4def-be5c-3223f02e37c5)

## License

MIT License. See [LICENSE](LICENSE).

## Disclaimer

This project is for **educational and research purposes only**. Using bots violates the Lineage 2 Terms of Service. The authors are not responsible for any consequences of using this software.

---

# 中文说明

## 概述

L2Bot 2.0 是一个用于天堂2（Lineage 2 Interlude）的模块化游戏内机器人框架。由可注入DLL、C++核心框架和WPF桌面客户端组成。

## 架构

```
┌─────────────────┐     命名管道 (JSON)       ┌─────────────────┐
│   L2BotDll.dll  │ ◄──────────────────────► │  客户端 (WPF)    │
│  (注入到        │                           │  - AI引擎        │
│   l2.exe)       │                           │  - 地图/界面     │
└────────┬────────┘                           │  - 寻路系统      │
         │                                    └─────────────────┘
┌────────┴────────┐
│   L2BotCore     │
│  (C++ 框架)     │
└────────┬────────┘
┌────────┴────────┐
│ InjectionLibrary│
│  (DLL注入器)    │
└─────────────────┘
```

## 项目组件

| 组件 | 语言 | 说明 |
|------|------|------|
| **L2BotCore** | C++ | 机器人框架 — 定义实体、事件、仓库和传输 |
| **L2BotDll** | C++ | 天堂2 Interlude可注入DLL — 读取游戏内存并转发数据 |
| **InjectionLibrary** | C++ | 通过Windows钩子进行DLL注入的辅助库 |
| **Client** | C# (.NET 6.0, WPF) | 桌面客户端，包含AI、地图可视化和配置界面 |

## 环境要求

- **Visual Studio 2022**：
  - C++桌面开发工作负载（用于L2BotCore、L2BotDll、InjectionLibrary）
  - .NET 6.0 SDK（用于Client）
- **天堂2 Interlude** 客户端（建议使用私服）
- **L2jGeodataPathFinder.dll** — 外部寻路库（[L2jGeodataPathFinder](https://github.com/k0t9i/L2jGeodataPathFinder)）
- **地理数据文件** — 放置在 `Client/Assets/geodata/` 目录下

## 编译

1. 用 Visual Studio 2022 打开 `L2Bot.sln`
2. 先编译C++项目（x86或x64，需与L2客户端匹配）：
   - `InjectionLibrary`
   - `L2BotCore`
   - `L2BotDll`
3. 编译C#客户端：
   ```
   dotnet build Client/Client.csproj
   ```

## 配置

编辑 `Client/config.json`：

```json
{
  "DLLName": "L2BotDll.dll",
  "ConnectionPipeName": "PipeL2Bot",
  "MaxPassableHeight": 30,
  "GeoDataDirectory": "geodata",
  "NodeWaitingTime": 3.0,
  "NodeDistanceTolerance": 8,
  "NextNodeDistanceTolerance": 16
}
```

| 键名 | 说明 |
|------|------|
| `DLLName` | 可注入DLL的文件名 |
| `ConnectionPipeName` | 命名管道标识符（须与DLL匹配） |
| `MaxPassableHeight` | 寻路系统认为可通过的最大地形高度 |
| `GeoDataDirectory` | `Assets/` 下存放地理数据文件的目录名 |
| `NodeWaitingTime` | 到达每个路径节点的等待超时时间（秒） |
| `NodeDistanceTolerance` | 视为"已到达节点"的距离阈值 |
| `NextNodeDistanceTolerance` | 提前推进到下一节点的距离阈值 |

## 使用方法

### 1. 启动游戏

启动天堂2 Interlude客户端并登录角色。

### 2. 启动客户端

运行 `Client.exe`，客户端将：
- 将 `L2BotDll.dll` 注入游戏进程
- 通过命名管道连接
- 在地图上显示游戏世界

### 3. AI控制

菜单栏：**AI options**

- **Start / Stop** — 开启/关闭AI
- **AI type** — 选择AI类型：
  - **Combat** — 自动狩猎（攻击、掏取、拾取、休息）
  - **Deleveling** — 攻击城镇守卫以减少经验
- **Config** — 打开AI配置对话框

### 4. 战斗AI设置

| 选项卡 | 设置内容 |
|--------|----------|
| **Combat** | 攻击距离（近战/远程）、技能条件、自动灵魂弹/精灵弹 |
| **Mobs** | 最大Z轴差、包含/排除怪物、等级范围 |
| **Drop** | 拾取开关、范围、Z轴差、包含/排除物品 |
| **Spoil** | 掏取/清扫技能、优先级、怪物筛选 |
| **Rest** | 开始/停止休息的HP/MP阈值 |

### 5. 客户端界面

| 区域 | 说明 |
|------|------|
| **地图**（左侧） | 实时地图，显示生物、掉落物、路径可视化、战斗区域 |
| **Environment标签** | 按距离排序的生物/掉落物列表，点击可选目标 |
| **Skills标签** | 主动和被动技能面板 |
| **Inventory标签** | 物品和任务物品 |
| **Log标签** | 带时间戳的活动日志 |
| **Chat**（左下） | 游戏内聊天消息，自动滚动 |
| **Hero面板**（右下） | 角色属性、位置、经验值、目标信息 |
| **状态栏** | 连接状态、移动状态、AI状态、最近通知 |

## 最近更新（Fork版本）

### 缺陷修复
- **寻路系统**：修复了 `Random` 缺陷 — 原代码在每个路径节点创建新Random实例，在同一毫秒内处理时会产生相同坐标。现在使用 `Random.Shared`。
- **管道传输**：修复了命名管道关闭时的静默失败（`readBytes == 0`）。增加了 `Connected` / `Disconnected` 事件。
- **重连机制**：修复了断开连接时的无限循环。增加了指数退避（1秒→最长30秒，最多10次）。
- **错误处理**：解析器和处理器异常现在会传播到UI，而非被静默吞掉。
- **启动保护**：在应用启动流程中增加了try-catch和MessageBox错误提示，以及全局 `DispatcherUnhandledException` 处理器。

### 稳定性基础设施
- **NotificationEvent系统**：WorldHandler的验证警告现在通过事件总线发布，并显示在Activity Log标签中。
- **MovementState追踪**：`AsyncPathMover` 现在暴露 `Idle`、`PathFinding`、`Moving`、`Stuck` 状态和 `StateChanged` 事件。

### 界面完善
- **状态栏**：连接指示器（绿色/红色）、移动状态、AI状态和最近通知。
- **Activity Log标签**：带时间戳、自动滚动的通知和错误日志。
- **Chat面板**：增加了标题标签和自动滚动功能。

## AI概述

### 战斗AI状态机

```
       ┌──────┐
       │ 死亡 │◄──── (任何状态，角色死亡时)
       └──┬───┘
          │ 复活
          ▼
       ┌──────┐     被攻击      ┌────────────┐
       │ 空闲 │────────────────►│  寻找目标   │
       └──┬───┘                 └──────┬─────┘
          │ HP/MP不足                  │ 找到目标
          ▼                            ▼
       ┌──────┐                 ┌──────────────┐
       │ 休息 │                 │  移动到目标   │
       └──────┘                 └──────┬───────┘
                                       │ 进入攻击范围
                                       ▼
                                ┌──────────┐
                                │   攻击   │
                                └──────┬───┘
                                       │ 目标死亡
                                       ▼
                                ┌──────────┐
                                │   拾取   │
                                └──────────┘
```

### 降级AI状态机

```
  空闲 → 寻找守卫 → 移动到目标 → 攻击守卫 → 死亡 → 空闲
```

## 寻路系统

使用 [L2jGeodataPathFinder](https://github.com/k0t9i/L2jGeodataPathFinder) 进行基于地理数据的路径计算和视线检测。

![image_2023-10-29_20-53-56](https://github.com/k0t9i/L2Bot2.0/assets/7733997/104e5ff2-7435-4def-be5c-3223f02e37c5)

## 许可证

MIT许可证。详见 [LICENSE](LICENSE)。

## 免责声明

本项目仅供**教育和研究目的**。使用机器人违反天堂2服务条款。作者不对使用本软件产生的任何后果负责。
