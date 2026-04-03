# 蔚蓝 / TowerFall 物理系统 —— Unity 实现

来源：https://www.mattmakesgames.com/articles/celeste_and_towerfall_physics/index.html

---

## 文件清单

| 文件 | 职责 |
|---|---|
| `PhysicsWorld.cs` | 全局单例，管理所有 Actor / Solid 的注册与 AABB 查询 |
| `Actor.cs` | 物理对象基类（MoveX / MoveY，像素级逐格碰撞） |
| `Solid.cs` | 可移动碰撞体基类（Move，携带 + 推动 Actor） |
| `PlayerActor.cs` | 玩家示例（重力、跳跃、土狼时间、跳跃缓冲） |
| `MovingPlatform.cs` | 移动平台示例（两点往返） |
| `StaticSolid.cs` | 静态墙壁 / 地板（无需额外逻辑） |

---

## 快速上手

### 1. 创建 PhysicsWorld
在场景层级中新建一个空 GameObject，命名为 `PhysicsWorld`，
挂载 `PhysicsWorld.cs`。**必须在任何 Actor / Solid 的 Awake 之前执行**，
将其 Script Execution Order 设为 -100（Edit → Project Settings → Script Execution Order）。

### 2. 创建静态地形
- 新建 GameObject → 挂载 `StaticSolid`
- 在 Inspector 中调整 `Collider Offset` 和 `Collider Size`（像素单位）
- 无需 Unity 的 Physics 2D 碰撞体，所有碰撞都由本系统处理

### 3. 创建玩家
- 新建 GameObject → 挂载 `PlayerActor`
- 调整跳跃、重力等参数
- 默认使用 Unity Input 的 Horizontal 轴和 Jump 按钮

### 4. 创建移动平台
- 新建 GameObject → 挂载 `MovingPlatform`
- 设置 `Point A`（通常留 0,0）和 `Point B`（相对偏移）
- Scene 视图中会显示黄色路径

---

## 核心规则（原文摘要）

### Actor.MoveX / MoveY
```
每次只移动 1 像素
逐格检测前方是否有 Solid（可碰撞的 AABB 重叠）
碰到则停止并触发 onCollide 回调
浮点余量累积，避免整数截断误差
```

### Solid.Move
```
1. 移动前：收集所有 IsRiding(this) 为 true 的 Actor → 携带列表
2. 临时将自身 Collidable = false
3. 移动后：
   - 若 Actor 与 Solid 重叠 → 推动（只推刚好齐平的量），回调 = Squish
   - 若 Actor 在携带列表 → 携带（全速，无回调）
   - 推动优先级 > 携带
4. 恢复 Collidable = true
```

---

## 扩展建议

- **IsRiding** 可重写以支持抓墙（cling）、爬绳等
- **Squish** 可重写为掉血、弹开等，不必直接销毁
- 平台类型扩展：单向平台（OneWayPlatform）只需在 `Actor.MoveY` 中
  额外判断 Actor 的运动方向和来源方向即可
- 瓦片地图：将每个实体 Tile 注册为 StaticSolid，或用一个大 Solid
  表示整块地形区域均可
