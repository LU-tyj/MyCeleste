# Celeste

## 1 Motivation

看了[b站 蔚蓝复刻](https://www.bilibili.com/video/BV1TZcrzUE1R/?spm_id_from=333.337.search-card.all.click&vd_source=ad716d3306df63ce18d6c86f46fec345)后，也想自己搓一个。

可以学习到：

- 学习状态机的构建
- 角色Animation的制作
- 以及平台跳跃游戏的关卡制作
- 平台跳跃游戏手感优化方法



## 2 RoadMap

| **Phase**                        | **时长** | **任务**                                                     | **交付物**                                        |
| -------------------------------- | -------- | ------------------------------------------------------------ | ------------------------------------------------- |
| **Phase 1** **搭建物理框架**     | 1d       | 实现  `PhysicsWorld.cs：Actor/Solid` 注册表 <br />定义  `PPU=16` 坐标系，整数 AABB 碰撞结构<br />实现  `Actor.MoveX/MoveY`（余数累计 + 逐像素步进）<br />实现  `BoxCast` 碰撞检测替代方案 | **Actor** **能在静态地形中移动且不穿透**          |
| **Phase 2 Solid** **动态交互**   | 1d       | 实现  `Solid.Move(x,y)` 完整逻辑<br />实现  IsRiding 默认逻辑（站立于顶部）<br />实现  Carry（携带）与 Push（推送）分离 <br />实现  Squish 默认行为<br />处理  Collidable 临时关闭的 Layer 逻辑 | **角色能站在移动平台上被携带，能被推送至边缘**    |
| **Phase 3 PlayerActor** **移植** | 1d       | 将原有  Rigidbody 跳跃代码迁移为 Actor 模式 <br />速度/加速度由 PlayerActor 维护，调用 MoveX/MoveY <br />移植预输入（Jump  Buffer）<br />移植土狼时间（Coyote Time）<br />移植下落加速（Fall Gravity Multiplier）<br />移植顶端静止（Apex Modifier）  <br />添加跳跃顶端修正 | **PlayerActor** **手感与原版 Unity 物理版本一致** |
| **Phase 4** **进阶特性**         | 1d       | PlayerActor 重写  IsRiding（蹬墙/抓墙骑乘判断）<br />PlayerActor 重写  Squish（死亡/重生逻辑）<br />实现冲刺（Dash）：短时间免碰撞 + 方向速度注入<br />实现爬墙/墙跳（Wall Grab/Jump） | **完整蔚蓝核心动作集可用**                        |
| **Phase 5** **集成测试**         | 2d       | 用静态 + 移动平台搭建测试关卡<br />压力测试：多个 Solid 同时移动时无穿透<br />边缘情况：Squish、卡缝、角碰撞验证 <br />与原  Unity 物理版本手感对比调参<br />性能分析：PhysicsWorld  遍历优化（空间分区备用） | **完整可玩 Demo，无物理 Bug**                     |

 ```
 Assets/
 ├── Scripts/
 │
 │   ├── Core/                                 # 🔧 纯底层（无任何游戏逻辑）
 │   │
 │   │   ├── Physics/
 │   │   │   ├── PhysicsWorld.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 管理所有 Actor / Solid 列表
 │   │   │   │       - 提供 Overlap(RectInt) 查询接口（核心）
 │   │   │   │       - 提供 BoxCast / CheckCollision
 │   │   │   │       - 提供 Register / Remove（生命周期管理）
 │   │   │   │       - 可扩展：空间分区（优化性能）
 │   │   │   │
 │   │   │   ├── Collision.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 封装 RectInt.Overlaps 调用
 │   │   │   │       - 提供统一接口（避免未来替换实现困难）
 │   │   │   │       - 可扩展：返回碰撞信息（法线/方向）
 │   │   │   │
 │   │   │   ├── Raycast.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 实现 BoxCast（用多次 Overlap 模拟）
 │   │   │   │       - 用于地面检测 / 墙检测
 │   │   │   │       - 替代 Physics2D.BoxCast
 │
 │   │   ├── Utils/
 │   │       ├── Timer.cs
 │   │       │   └── 【职责】
 │   │       │       - 通用计时器（用于 Coyote Time / Dash）
 │   │       │
 │   │       ├── InputReader.cs
 │   │       │   └── 【职责】
 │   │       │       - 输入缓存（Jump Buffer 实现关键）
 │
 │   ├── PhysicsObjects/                        # 🧱 Celeste核心抽象层
 │   │
 │   │   ├── Actor.cs
 │   │   │   └── 【职责】
 │   │   │       - 可移动对象（玩家/敌人）
 │   │   │       - 持有 RectInt collider
 │   │   │       - 持有 Position（int）
 │   │   │       - 维护 xRemainder / yRemainder（子像素）
 │   │   │
 │   │   │       - 核心函数：
 │   │   │         → MoveX(float amount, Action onCollide)
 │   │   │           - 余数累计
 │   │   │           - Round → 整数移动
 │   │   │           - 逐像素移动（while）
 │   │   │           - 每步用 PhysicsWorld.Overlap 检测
 │   │   │
 │   │   │         → MoveY(...)
 │   │   │
 │   │   │         → IsRiding(Solid)
 │   │   │           - 默认：是否站在顶部
 │   │   │
 │   │   │
 │   │   ├── Solid.cs
 │   │   │   └── 【职责】
 │   │   │       - 不可穿透对象（地面 / 平台）
 │   │   │       - 持有 RectInt collider
 │   │   │
 │   │   │       - 核心函数：
 │   │   │         → Move(int dx, int dy)
 │   │   │           - 暂时关闭自身 Collidable
 │   │   │           - 移动自身 RectInt
 │   │   │           - 遍历 Actor：
 │   │   │               1. Riding → Carry（一起移动）
 │   │   │               2. 碰撞 → Push（推开）
 │   │   │               3. 推不开 → Squish（挤压）
 │   │   │           - 恢复 Collidable
 │   │   │
 │   │   │
 │   │   ├── ICollidable.cs
 │   │   │   └── 【职责】
 │   │   │       - 统一接口
 │   │   │
 │   │   │       - 定义：
 │   │   │         → RectInt Bounds
 │   │   │         → bool Collidable
 │   │   │
 │   │   │
 │   │   ├── PhysicsComponent.cs
 │   │   │   └── 【职责】（Unity桥接层‼）
 │   │   │       - 挂在 GameObject 上
 │   │   │       - 持有 Actor / Solid
 │   │   │       - 同步：
 │   │   │           Transform.position ↔ RectInt.position
 │   │   │       - Awake 时自动注册到 PhysicsWorld
 │
 │   ├── Gameplay/                              # 🎮 游戏逻辑层
 │   │
 │   │   ├── Player/
 │   │   │
 │   │   │   ├── PlayerActor.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - Celeste 手感核心
 │   │   │   │       - 管理 velocity（float）
 │   │   │   │       - 计算：
 │   │   │   │           → gravity
 │   │   │   │           → jump
 │   │   │   │           → fall multiplier
 │   │   │   │           → apex modifier
 │   │   │   │
 │   │   │   │       - 调用：
 │   │   │   │           MoveX / MoveY（Actor）
 │   │   │   │
 │   │   │   │       - 实现：
 │   │   │   │           → Jump Buffer
 │   │   │   │           → Coyote Time
 │   │   │
 │   │   │
 │   │   │   ├── PlayerController.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 读取输入
 │   │   │   │       - 转换为 movement / jump / dash
 │   │   │   │       - 不处理物理
 │   │   │
 │   │   │
 │   │   │   ├── PlayerStateMachine.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 管理状态切换
 │   │   │
 │   │   │
 │   │   │   ├── States/
 │   │   │   │   ├── IdleState.cs
 │   │   │   │   ├── RunState.cs
 │   │   │   │   ├── JumpState.cs
 │   │   │   │   ├── FallState.cs
 │   │   │   │   ├── DashState.cs
 │   │   │   │   ├── WallState.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 每个状态只处理行为逻辑
 │   │   │
 │   │   ├── Environment/
 │   │   │   ├── MovingPlatform.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 调用 Solid.Move 实现移动平台
 │   │   │
 │   │   │   ├── OneWayPlatform.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 可从下穿透的平台
 │   │   │
 │   │   │   ├── Hazard.cs
 │   │   │   │   └── 【职责】
 │   │   │   │       - 碰到即死亡（刺）
 │
 │   ├── Data/                                  # 📊 数据驱动
 │   │
 │   │   ├── PlayerStats.cs
 │   │   │   └── 【职责】
 │   │   │       - ScriptableObject
 │   │   │       - 参数：
 │   │   │           runSpeed / jumpForce / gravity
 │   │   │           coyoteTime / jumpBuffer
 │   │   │
 │   │   ├── PhysicsSettings.cs
 │   │   │   └── 【职责】
 │   │   │       - PPU（建议=16）
 │   │   │       - LayerMask
 │
 │   ├── Debug/
 │       ├── Gizmos/
 │       │   ├── PlayerJumpGizmo.cs
 │       │   │   └── 【职责】
 │       │   │       - 在编辑器中绘制跳跃轨迹
 │       │   │
 │       │   ├── CollisionGizmo.cs
 │       │   │   └── 【职责】
 │       │   │       - 可视化 RectInt 碰撞盒
 ```



## Reference

1. [b站 蔚蓝复刻](https://www.bilibili.com/video/BV1TZcrzUE1R/?spm_id_from=333.337.search-card.all.click&vd_source=ad716d3306df63ce18d6c86f46fec345)
2. [MADDY MAKES GAMES Forgiveness](https://www.mattmakesgames.com/articles/celeste_and_forgiveness/index.html) 蔚蓝开发者文档，关于跳跃手感的优化
3. [MADDY MAKES GAMES Towerfall Physics](https://www.mattmakesgames.com/articles/celeste_and_towerfall_physics/index.html) 蔚蓝开发者文档，物理系统
4. [Celeste Wiki](https://celeste.ink/wiki/Main_Page)

