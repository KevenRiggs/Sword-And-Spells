# PlayerSkillFunctions 设计方案

## 概述

`PlayerSkillFunctions` 是玩家技能逻辑的执行者，订阅 `PlayerController_Skills.OnSkillTriggered` 事件，在技能触发时执行对应逻辑（特效播放、碰撞检测、命中通知等），通过 Animation Event 驱动分阶段检测。

---

## Sword 单体 Q 技能（SwordQ）✅ 已实现

### 1. 检测原理

在动画的第 N 帧（由 Animation Event 触发 `OnSwordQHit`）时，以玩家 `Transform.forward` 为基准，检测前方**扇形区域**内距离最近的敌人，对其触发命中。

### 2. 可配置参数（Inspector 暴露）

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SwordQ_Radius` | float | 5f | 扇形检测半径（米） |
| `SwordQ_Angle` | float | 60f | 扇形角度（度） |
| `EnemyLayer` | LayerMask | — | Enemy 层遮罩（必填） |

### 3. 扇形检测算法

```
以 Player.position 为圆心，Transform.forward 为对称轴
扇形范围：forward ± Angle/2，深度 0 ~ Radius

命中条件：
  1. 敌人在 Radius 范围内（在 EnemyLayer 内）
  2. 敌人与 Player 的夹角 ≤ Angle/2
  3. 距离最近（取 min）
```

实现步骤：
1. `Physics.OverlapSphere(player.position, SwordQ_Radius, EnemyLayer)` 收集范围内所有敌人
2. 遍历结果，通过 `Vector3.Dot(forward, dirToTarget)` 点积求夹角，过滤 > halfAngle 的敌人
3. 从剩余敌人中选距离最近（SqrMagnitude 最小的）1 个
4. 若命中，触发 `OnSkillHit` 事件通知下游

### 4. 可扩展性设计

| 扩展方向 | 当前是否实现 | 如何扩展 |
|---------|-------------|---------|
| 命中特效 | 否 | 在 `OnSwordQHit` 中调用 `VFXManager.Spawn(...)` |
| 音效 | 否 | 在 `OnSwordQHit` 中调用 `AudioSource.PlayOneShot(...)` |
| 击退/击飞 | 否 | 在命中后调用 `enemy.GetComponent<Rigidbody>().AddForce(...)` |
| 多目标 | 否 | 将"取最近1个"改为"取所有在范围内的" |
| 命中闪烁 | 否 | 在 `Damage_OnEnemy` 或独立 HitFlash 脚本中处理 |

### 5. 事件链

```
PlayerController_Skills.OnSkillTriggered (LeftHold, 0)
    → PlayerSkillFunctions.HandleSkillTriggered (预处理，记录调试信息)
        → [动画播放 Sword_Q]
            → [动画第 N 帧 Animation Event]
                → OnSwordQHit()
                    → 扇形检测 (DetectNearestEnemyInFan)
                        → OnSkillHit(enemy, DamageTemplate.SkillType.SwordSingle, playerProp)
                            → Damage_OnEnemy.HandleSkillHit()
                                → Damage_OnEnemy.SettleDamage()
```

### 6. 事件签名

```csharp
// 技能命中事件（供 Damage_OnEnemy 订阅）
public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;
//   → GameObject                : 命中的敌人
//   → DamageTemplate.SkillType  : 命中的技能类型（用于伤害计算）
//   → PlayerPropertyTemplate    : 玩家属性（用于伤害计算）

// 伤害结算调试事件（供 PlayerSkillFunctions 屏幕显示订阅）
public System.Action<string, string, float, bool, float> OnDamageDebug;
//   → 敌人名称 / 技能名称 / 伤害值 / 是否暴击 / 剩余血量

// Animation Event 入口
public void OnSwordQHit();
```

---

## Spell 单体 Q 技能（SpellQ）✅ 已实现

### 1. 发射原理

在动画的第 N 帧（由 Animation Event 触发 `OnSpellQHit`）时，根据 `PlayerPropertyTemplate.SpellSingle_Spell_Count` 配置的数量，向角色朝向方向依次发射多个法球。

### 2. 配置方式

SpellQ 的弹射物参数不在 PlayerSkillFunctions Inspector 中配置，而是通过 **FlyingProjectileTemplate.asset** 配置：

| 配置位置 | 字段 | 说明 |
|---------|------|------|
| `FlyingProjectileTemplate.asset` | `prefab` | 法球视觉 Prefab |
| `FlyingProjectileTemplate.asset` | `speed` | 飞行速度（米/秒） |
| `FlyingProjectileTemplate.asset` | `radius` | 碰撞体积半径 |
| `FlyingProjectileTemplate.asset` | `maxLifetime` | 最大存活时间 |
| `FlyingProjectileTemplate.asset` | `hitVFX` | 命中特效 Prefab（可选） |
| `FlyingProjectileTemplate.asset` | `hitSFX` | 命中音效（可选） |
| `PlayerSkillFunctions` Inspector | `SpellQ_Template` | 引用的 `.asset` |
| `PlayerSkillFunctions` Inspector | `SpellQ_SpawnInterval` | 多球发射间隔（秒） |

### 3. 多球发射逻辑

```
OnSpellQHit()
    → 读取 PlayerPropertyAsset.SpellSingle_Spell_Count（配置的法球数量）
    → 启动协程 SpellQ_LaunchLoop()
        → for i in [0, count):
            → 计算发射方向（玩家朝向，不偏移）
            → 调用 Global_ProjectileManager.SpawnFlyingProjectile()
            → 等待 SpellQ_SpawnInterval 秒
```

**间隔保证**：每发法球间隔 `SpellQ_SpawnInterval`（默认0.1秒），避免视觉重叠。

### 4. 事件链

```
PlayerController_Skills.OnSkillTriggered (RightHold, 0)
    → PlayerSkillFunctions.HandleSkillTriggered (预处理，记录调试信息)
        → [动画播放 Spell_Q]
            → [动画第 N 帧 Animation Event]
                → OnSpellQHit()
                    → [协程] 依次 Spawn 法球
                        → [每发法球命中时]
                            → Global_ProjectileManager.OnProjectileHit (特效/音效)
                            → Global_ProjectileManager.OnSkillHit (内部转发)
                                → Damage_OnEnemy.HandleSkillHit()
                                    → Damage_OnEnemy.SettleDamage()
```

### 5. 与 Global_ProjectileManager 的协作

| 职责方 | 职责 |
|--------|------|
| `PlayerSkillFunctions` | 管理发射节奏（多球间隔），通知管理器生成法球 |
| `Global_ProjectileManager` | 接收生成请求，管理法球生命周期、移动、碰撞检测 |

### 6. 扩展方向

| 扩展方向 | 当前是否实现 | 如何扩展 |
|---------|-------------|---------|
| 穿透 | 否 | Global_ProjectileManager.UpdateFlyingProjectiles 不回收弹射物，继续飞 |
| 追踪 | 否 | 在 FlyingProjectile.Update 中朝向目标计算方向 |
| 弹跳 | 否 | 修改 direction 转向计算 |
| 爆炸范围 | 否 | 命中时通知范围内所有敌人 |

---

## SwordR 多波剑气技能（Slash）✅ 已实现

### 1. 发射原理

在动画的第 N 帧（由 Animation Event 触发 `OnSwordRHit`）时，根据 `PlayerPropertyTemplate.SwordAOE_Slash_Count` 配置的波数，向角色朝向方向依次释放多波剑气。每波剑气独立，对路径上的所有敌人造成伤害。

### 2. 与 SpellQ 的核心区别

| 特性 | SpellQ (法球) | SwordR (剑气) |
|------|--------------|--------------|
| 碰撞方式 | `SphereCast`，撞到第1个敌人**停止** | `BoxCast`，路径上**所有**敌人受击 |
| 命中后行为 | 弹射物回收入池 | 剑气**穿透**敌人，继续飞行 |
| 单波伤害数 | 每波最多1人 | 每波对**所有**路径敌人各1次 |
| 多波行为 | 各波独立，可能同一人被多波命中 | 各波独立，同一波**内**不重复伤敌 |

### 3. 配置方式

SwordR 的弹射物参数通过 **SlashProjectileTemplate.asset** 配置：

| 配置位置 | 字段 | 说明 |
|---------|------|------|
| `SlashProjectileTemplate.asset` | `prefab` | 剑气视觉 Prefab |
| `SlashProjectileTemplate.asset` | `speed` | 剑气飞行速度（米/秒） |
| `SlashProjectileTemplate.asset` | `slashLength` | 剑气长度（米） |
| `SlashProjectileTemplate.asset` | `slashWidth` | 剑气宽度（米） |
| `SlashProjectileTemplate.asset` | `maxLifetime` | 剑气最大存活时间（秒），0表示按 slashLength/speed 自动计算 |
| `SlashProjectileTemplate.asset` | `spawnOffset` | 发射位置偏移（世界坐标） |
| `PlayerSkillFunctions` Inspector | `SwordR_Template` | 引用的 `.asset` |
| `PlayerSkillFunctions` Inspector | `SwordR_SpawnInterval` | 多波发射间隔（秒） |

### 4. 防重复伤敌机制

剑气使用 `BoxCast` 每帧检测，每波剑气持续飞行期间（通常几十帧），同一敌人可能被连续多帧命中多次。

**解决方案**：在 `SlashProjectile` 实例中增加 `HashSet<GameObject> hitEnemies`，每次命中时查表：

```
BoxCast 命中 EnemyA
    → 检查 hitEnemies 是否包含 EnemyA
        → 不包含：
            → 加入 hitEnemies
            → 触发 OnProjectileHit（特效/音效）
            → 触发 OnSkillHit（伤害结算）
        → 已包含：跳过（剑气继续飞行）
```

每波新剑气 Spawn 时清空 `hitEnemies`。

### 5. 事件链

```
PlayerController_Skills.OnSkillTriggered (LeftHold, 2)
    → PlayerSkillFunctions.HandleSkillTriggered (预处理，记录调试信息)
        → [动画播放 Sword_R]
            → [动画第 N 帧 Animation Event]
                → OnSwordRHit()
                    → [协程] 依次 Spawn 剑气波
                        → [每波剑气每帧 BoxCast]
                            → [首次命中 EnemyA]
                                → OnProjectileHit (特效/音效)
                                → OnSkillHit
                                    → Damage_OnEnemy.HandleSkillHit()
                                        → Damage_OnEnemy.SettleDamage()
                            → [后续帧再碰到 EnemyA] → 查 HashSet → 跳过
                        → [下一波发射时重置 HashSet]
```

### 6. 扩展方向

| 扩展方向 | 当前是否实现 | 如何扩展 |
|---------|-------------|---------|
| 剑气碰撞后分裂 | 否 | 命中后从该点 Spawn 多个小剑气 |
| 剑气弯曲 | 否 | 修改 UpdateSlashProjectiles 中 direction 转向计算 |

---

## 对接说明

### PlayerSkillFunctions → Global_ProjectileManager

- `PlayerSkillFunctions` 在 `Start` 中通过 `FindAnyObjectByType<Global_ProjectileManager>()` 获取管理器引用并缓存
- `OnSpellQHit` 调用 `m_ProjectileManager.SpawnFlyingProjectile()` 生成法球

### Global_ProjectileManager → Damage_OnEnemy

- `Global_ProjectileManager` 在 `Awake` 中单例化
- `OnEnable` / `OnDisable` 中订阅/取消订阅 `Global_ProjectileManager.Instance.OnSkillHit`
- 命中时，`Global_ProjectileManager` 内部同时触发：
  - `OnProjectileHit` — 供特效/音效系统订阅
  - `OnSkillHit`（internal）— 自动转发给 `Damage_OnEnemy.HandleSkillHit`

### EnemyLayer 配置

- 必填字段：需要在 Inspector 中将 Enemy 层拖入 `EnemyLayer` 遮罩
- 只有设置了 Enemy 层的物体才会被扇形检测命中

---

## 调试功能

### 编辑器 Gizmos
- `OnDrawGizmos`：始终绘制扇形检测范围（青色半透明）
- `OnDrawGizmosSelected`：选中玩家时绘制高亮扇形（绿色）

### 游戏视图调试
- `OnPostRender`：命中检测时绘制球形调试图形（青色半透明，持续0.5秒）
- `OnGUI`：屏幕显示调试面板，包含技能触发信息和伤害结算信息

### 伤害结算调试
- `Damage_OnEnemy` 触发 `OnDamageDebug` 事件
- `PlayerSkillFunctions` 订阅该事件并在屏幕显示结算结果（伤害值、暴击标识、剩余血量）

---

## 后续技能设计预留

| 技能 | 当前状态 | 备注 |
|------|---------|------|
| SwordQ 单体 | ✅ 已实现 | 扇形单体 |
| SwordE 格挡 | 未设计 | 格挡无伤害，逻辑待定 |
| SwordR AOE | ✅ 已实现 | 多波剑气，BoxCast 穿透路径所有敌人 |
| SpellQ 单体 | ✅ 已实现 | 多球弹射物 |
| SpellE 格挡 | 未设计 |  |
| SpellR AOE | 未设计 | 地板范围技能 |

---

## 当前代码结构

```csharp
public class PlayerSkillFunctions : MonoBehaviour
{
    // ===== Inspector 字段 =====
    // SwordQ
    public float SwordQ_Radius = 5f;
    public float SwordQ_Angle = 60f;

    // SpellQ
    public FlyingProjectileTemplate SpellQ_Template;  // 引用的 .asset
    public float SpellQ_SpawnInterval = 0.1f;       // 发射间隔

    // SwordR
    public SlashProjectileTemplate SwordR_Template;  // 引用的 .asset
    public float SwordR_SpawnInterval = 0.15f;      // 多波发射间隔

    public LayerMask EnemyLayer;

    // ===== 私有字段 =====
    PlayerController_Skills m_PlayerCtrl;
    Transform m_PlayerTransform;
    Global_ProjectileManager m_ProjectileManager;

    // ===== 事件 =====
    public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;
    public System.Action<string, string, float, bool, float> OnDamageDebug;

    // ===== 生命周期 =====
    void Start();
    void OnEnable();
    void OnDisable();
    void Update();

    // ===== 事件处理（预处理） =====
    void HandleSkillTriggered(PlayerController_Skills.SelectionState state, int skillIndex);

    // ===== Animation Event 入口 =====
    public void OnSwordQHit();
    public void OnSpellQHit();

    // ===== 扇形检测 =====
    GameObject DetectNearestEnemyInFan(Vector3 origin, Vector3 forward, float radius, float angle);

    // ===== SpellQ 发射逻辑 =====
    System.Collections.IEnumerator SpellQ_LaunchLoop(int count);

    // ===== SwordR 发射逻辑 =====
    public void OnSwordRHit();
    System.Collections.IEnumerator SwordR_LaunchLoop(int count);

    // ===== 调试辅助 =====
    void SetDebugMessage(string msg);
    void HandleDamageDebug(string enemyName, string skillName, float damage, bool isCrit, float remainingHp);
    void OnGUI();

    // ===== Gizmos =====
    void OnDrawGizmos();
    void OnDrawGizmosSelected();
    void DrawFanGizmos(Vector3 origin, Vector3 forward, float radius, float angle, bool bright);
    void DrawFanArcGizmos(Vector3 origin, Vector3 forward, float radius, float halfAngle, int segments);

    // ===== 游戏视图绘制 =====
    void DrawFanInGameView(Vector3 origin, Vector3 forward, float radius, float angle);
    void OnPostRender();
    void DrawWireSphere(Vector3 center, float radius, int latLines, int lonLines);
}
```
