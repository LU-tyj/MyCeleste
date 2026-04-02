# Celeste

## 1 Motivation

看了[b站 蔚蓝复刻](https://www.bilibili.com/video/BV1TZcrzUE1R/?spm_id_from=333.337.search-card.all.click&vd_source=ad716d3306df63ce18d6c86f46fec345)后，也想自己搓一个。

可以学习到：

- 学习状态机的构建
- 角色Animation的制作
- 以及平台跳跃游戏的关卡制作
- 平台跳跃游戏手感优化方法



## 2 RoadMap

### 阶段0：项目约束与基础环境
**实现时间** 0.5 days

**实现目标**：
建立统一物理规则，避免后续架构返工

**TODO**：
- 关闭 Rigidbody2D.simulated（或不使用 Rigidbody）
- 确定坐标系统：
  - 1 unit = 1 tile
  - 使用 Vector2Int 作为逻辑坐标
- 创建基础目录结构：
  - /PhysicsCore
  - /Gameplay

**验收**：
- 场景中无 Unity 物理参与
- 能打印整数坐标（无 float 漂移）

---

### 阶段1：Tilemap → Solid（碰撞数据生成）

**实现时间** 1 day

**实现目标**：
将 Tilemap 转换为可用于碰撞检测的 AABB（Solid）

**TODO**：
新增文件：
- Hitbox.cs
  - struct Hitbox { int x, y, w, h }
  - bool Overlaps(Hitbox)

- Solid.cs
  - Vector2Int position
  - Hitbox hitbox

- World.cs
  - List\<Solid\> solids
  - bool Collide(Hitbox)

- TilemapConverter.cs
  - Tilemap → bool[,]
  - bool[,] → List\<Solid\>（每 tile 一个）

- GizmoDrawer.cs（可选）
  - 绘制 AABB

实现内容：
- 遍历 Tilemap
- 生成 Solid(1x1)
- 注册到 World

**验收**：
- Scene 中能看到所有 AABB（Gizmo）
- 手动测试 Hitbox overlap 正确
- World.Collide 返回正确结果
- 通过Tilemap实现简单的世界代码化生成

---

### 阶段2：Actor + MoveX（核心里程碑）

**实现时间** 1~2 days

**实现目标**：
实现稳定的逐像素水平移动

**TODO**：
新增文件：
- Actor.cs
  - Vector2Int position
  - Hitbox hitbox
  - float xRemainder

实现内容：
- MoveX(float amount, Func<Hitbox,bool> collide)
  - remainder 累积
  - RoundToInt
  - 逐像素 step 移动
  - 碰撞即停止

**验收**：
- 向墙移动刚好停住
- 高速移动不穿透
- 无 jitter / 卡墙问题

---

### 阶段3：MoveY + 重力系统

**实现时间** 1 day

**实现目标**：
实现垂直运动（重力 + 碰撞）

**TODO**：
修改 Actor.cs：
- float yRemainder
- MoveY(float amount, Func<Hitbox,bool> collide)

新增：
- speedY（在 Player 或 Actor 中）

实现内容：
- speedY += gravity * dt
- MoveY(speedY * dt)

**验收**：
- 角色自然下落
- 精确落地（无嵌入/悬空）
- 高速下落不穿地

---

### 阶段4：Player 控制层

**实现时间** 1 day

**实现目标**：
实现基础可玩角色控制

**TODO**：
新增文件：
- Player.cs（继承 Actor）

实现内容：
- playerVelocity
- 输入读取（左右移动）
- 跳跃（设置 speedY）

调用：
- MoveX(playerVelocity.x * dt)
- MoveY(playerVelocity.y * dt)

**验收**：
- 可以左右移动
- 可以跳跃
- 手感稳定，无穿透

---

### 阶段5：Unity 适配层（显示与逻辑解耦）

**实现时间** 0.5~1 day

**实现目标**：
Unity 仅作为表现层

**TODO**：
新增文件：
- ActorView.cs
  - 同步 transform.position = actor.position

- SolidView.cs
  - 同步位置

- WorldBootstrap.cs
  - 初始化 World
  - 注册 Actor / Solid

**验收**：
- 修改 Actor 数据 → 画面同步
- Unity 中无物理参与

---

### 阶段6：Solid 推动 Actor（关键机制）

**实现时间** 2 days

**实现目标**：
实现移动平台推动角色（Celeste核心）

**TODO**：
修改：
- Solid.cs

实现内容：
- Solid.Move(dx, dy, world)
  1. 找到接触 Actor
  2. 先移动 Solid
  3. 推动 Actor（调用 MoveX/MoveY 或强制位移）

关键点：
- 顺序必须正确
- 避免 Actor 被夹入

**验收**：
- 平台移动时角色被带走
- 不穿透、不抖动
- 不丢失接触

---

### 阶段7：Tile 合并优化（性能优化）

**实现时间** 1~2 days

**实现目标**：
减少 Solid 数量，提高效率

**TODO**：
修改：
- TilemapConverter.cs

实现内容：
- Greedy 合并矩形
- 多 tile → 单个大 AABB

**验收**：
- Solid 数量显著减少
- 碰撞结果完全一致

---

### 阶段8：Celeste 手感增强

**实现时间** 2~3 days

**实现目标**：
复刻 Celeste 操作手感

**TODO**：

1. Coyote Time
   - 记录离地时间窗口

2. Jump Buffer
   - 缓存输入

3. Corner Correction
   - X/Y 自动修正避免卡边

4. Dash（可选）

**验收**：
- 跳跃更宽容
- 输入响应更自然
- 边角不卡顿

---

### 阶段9：系统扩展与重构

**实现时间** 持续优化

**实现目标**：
提高系统扩展性

**TODO**：
- 状态机（Locomotion / Air / Dash）
- ScriptableObject 参数配置
- One-way platform
- 动画系统接入

**验收**：
- 系统结构清晰
- 可扩展性强



## Reference

1. [b站 蔚蓝复刻](https://www.bilibili.com/video/BV1TZcrzUE1R/?spm_id_from=333.337.search-card.all.click&vd_source=ad716d3306df63ce18d6c86f46fec345)
2. [MADDY MAKES GAMES Forgiveness](https://www.mattmakesgames.com/articles/celeste_and_forgiveness/index.html) 蔚蓝开发者文档，关于跳跃手感的优化
3. [MADDY MAKES GAMES Towerfall Physics](https://www.mattmakesgames.com/articles/celeste_and_towerfall_physics/index.html) 蔚蓝开发者文档，物理系统
4. [Celeste Wiki](https://celeste.ink/wiki/Main_Page)

