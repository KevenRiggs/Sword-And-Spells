# Global_ProjectileManager 设计方案

## 概述

`Global_ProjectileManager` 是弹射物的全局管理器，以单例模式运行，负责所有弹射物的生命周期管理、碰撞检测与对象池化。

弹射物模板采用**继承体系**设计，每种弹射物类型有独立的配置模板，精确匹配其行为特性。

---

## 模板继承体系

```
ProjectileTemplate（抽象基类）
    ├── FlyingProjectileTemplate    ← SpellQ 用
    ├── GroundAOEProjectileTemplate ← SpellR 用
    └── SlashProjectileTemplate     ← SwordR 用
```

### 为什么要用继承体系？

| 弹射物类型 | 移动方式 | 碰撞检测方式 | 持续性 | 关键参数 |
|-----------|---------|-------------|--------|---------|
| **Flying** | 直线飞行 | SphereCast，撞到第一个敌人停止 | 瞬间 | speed, radius, lifetime |
| **GroundAOE** | 不移动（生成后驻留） | 区域范围内持续检测 | 持续一段时间 | effectRadius, duration, tickInterval |
| **Slash** | 直线飞行 | BoxCast，路径上所有敌人受击 | 瞬间 | speed, slashLength, slashWidth |

三种类型参数差异大，继承体系可保证类型安全，避免单一模板类堆积冗余字段。

---

## ProjectileTemplate（抽象基类）

```csharp
/// <summary>
/// 弹射物类型枚举
/// </summary>
public enum ProjectileType
{
    Flying = 0,      // 飞行弹射物
    GroundAOE = 1,   // 地面持续范围
    Slash = 2        // 剑气
}

/// <summary>
/// 弹射物模板基类，定义共有字段和行为接口
/// </summary>
public abstract class ProjectileTemplate : ScriptableObject
{
    [Header("通用配置")]
    public GameObject prefab;       // 视觉 Prefab
    public GameObject hitVFX;       // 命中特效 Prefab（可选）
    public AudioClip hitSFX;        // 命中音效（可选）
    public LayerMask hitLayer;      // 碰撞检测层级（敌人发射填 Player 层，玩家发射填 Enemy 层）

    /// <summary>
    /// 获取该模板的类型枚举
    /// </summary>
    public abstract ProjectileType GetProjectileType();

    /// <summary>
    /// 注册到管理器的对象池
    /// </summary>
    public abstract void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0);
}
```

---

## FlyingProjectileTemplate（飞行弹射物 — SpellQ 用）

### 适用场景
SpellQ 法球等直线飞行的弹射物，撞到第一个敌人后消失。

### 配置字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `prefab` | GameObject | — | 视觉 Prefab |
| `hitVFX` | GameObject | — | 命中特效 Prefab（可选） |
| `hitSFX` | AudioClip | — | 命中音效（可选） |
| `hitLayer` | LayerMask | — | 碰撞检测层级（敌人发射填 Player 层，玩家发射填 Enemy 层） |
| `speed` | float | 20f | 飞行速度（米/秒） |
| `radius` | float | 0.5f | 碰撞体积半径（用于 SphereCast） |
| `maxLifetime` | float | 5f | 最大存活时间（秒） |
| `spawnOffset` | Vector3 | (0, 1, 0.5) | 发射位置偏移（世界坐标，相对于玩家位置） |

### 碰撞检测
每帧使用 `Physics.SphereCast` 检测从上一帧位置到当前位置的路径：
- 命中第一个敌人 → 触发命中事件 → 弹射物回收入池
- 存活时间超限 → 自动回收入池

### 代码定义

```csharp
[CreateAssetMenu(fileName = "FlyingProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/Flying")]
public class FlyingProjectileTemplate : ProjectileTemplate
{
    public float speed = 20f;
    public float radius = 0.5f;
    public float maxLifetime = 5f;
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0.5f);

    public override ProjectileType GetProjectileType() => ProjectileType.Flying;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterFlyingTemplate(this, warmupCount);
    }
}
```

---

## GroundAOEProjectileTemplate（地面持续范围 — SpellR 用）

### 适用场景
SpellR 地板持续范围技能，生成后不移动，在范围内持续检测敌人并造成伤害。

### 配置字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `effectRadius` | float | 5f | 影响范围半径 |
| `duration` | float | 3f | 持续时间（秒） |
| `tickInterval` | float | 0.5f | 伤害 tick 间隔（秒） |
| `damagePerTick` | float | 10f | 每 tick 伤害 |
| `groundIndicator` | GameObject | — | 地面指示器 Prefab（如圆形范围圈） |
| `spawnOffset` | Vector3 | (0, 0, 0) | 发射位置偏移（世界坐标，相对于玩家位置） |

### 碰撞检测
- 持续检测范围内所有敌人
- 每隔 `tickInterval` 对范围内敌人造成一次伤害
- 持续 `duration` 秒后自动销毁

### 代码定义

```csharp
[CreateAssetMenu(fileName = "GroundAOEProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/GroundAOE")]
public class GroundAOEProjectileTemplate : ProjectileTemplate
{
    public float effectRadius = 5f;
    public float duration = 3f;
    public float tickInterval = 0.5f;
    public float damagePerTick = 10f;
    public GameObject groundIndicator;
    public Vector3 spawnOffset = Vector3.zero;

    public override ProjectileType GetProjectileType() => ProjectileType.GroundAOE;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterGroundAoeTemplate(this, warmupCount);
    }
}
```

---

## SlashProjectileTemplate（剑气 — SwordR 用）

### 适用场景
SwordR 剑气技能，直线飞行，路径上所有敌人受击（不停止）。

### 配置字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `speed` | float | 30f | 剑气飞行速度（米/秒） |
| `slashLength` | float | 8f | 剑气长度（米） |
| `slashWidth` | float | 1.5f | 剑气宽度（米） |
| `maxLifetime` | float | 0f | 剑气最大存活时间（秒），0表示按 slashLength/speed 自动计算 |
| `spawnOffset` | Vector3 | (0, 1, 0) | 发射位置偏移（世界坐标，相对于玩家位置） |

### 碰撞检测
每帧使用 `Physics.BoxCast` 检测：
- 命中路径上所有敌人（非仅第一个）
- 对每个命中的敌人都触发命中事件
- **防重复伤敌**：通过 `hitEnemies` HashSet 记录本波已伤敌名单，同一敌人被跳过
- 弹射物继续飞行直到超出最大距离

### 代码定义

```csharp
[CreateAssetMenu(fileName = "SlashProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/Slash")]
public class SlashProjectileTemplate : ProjectileTemplate
{
    public float speed = 30f;
    public float slashLength = 8f;
    public float slashWidth = 1.5f;
    public float maxLifetime = 0f;  // 0表示按 slashLength/speed 自动计算
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    public override ProjectileType GetProjectileType() => ProjectileType.Slash;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterSlashTemplate(this, warmupCount);
    }
}
```

---

## Global_ProjectileManager

### 核心职责

| 职责 | 说明 |
|------|------|
| **单例管理** | 场景中唯一存在，提供 Instance 访问点 |
| **对象池** | 每种模板类型独立对象池，运行时回收复用 |
| **生命周期更新** | 每帧 Update 更新所有活跃弹射物 |
| **碰撞检测** | 根据弹射物类型选择合适的检测方式 |
| **命中通知** | 命中敌人后触发 OnProjectileHit，通知下游处理伤害 |
| **内部转发** | 命中时自动通过 OnSkillHit 转发给 Damage_OnEnemy |

### 对象池结构

```csharp
// 按模板类型分离对象池
Dictionary<FlyingProjectileTemplate, Queue<FlyingProjectile>> m_FlyingPool;
Dictionary<GroundAOEProjectileTemplate, Queue<GroundAOEProjectile>> m_GroundAoePool;
Dictionary<SlashProjectileTemplate, Queue<SlashProjectile>> m_SlashPool;

// 活跃中的弹射物列表（分开管理，每种类型独立更新）
List<FlyingProjectile> m_ActiveFlying;
List<GroundAOEProjectile> m_ActiveGroundAoe;
List<SlashProjectile> m_ActiveSlash;
```

### 弹射物实例结构

```csharp
// 飞行弹射物实例
public class FlyingProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public FlyingProjectileTemplate template;
    public Vector3 direction;
    public float speed;
    public float radius;
    public float lifetime;
    public float maxLifetime;
    public Vector3 prevPosition;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
}

// 地面AOE实例
public class GroundAOEProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public GroundAOEProjectileTemplate template;
    public float elapsed;
    public float duration;
    public float tickInterval;
    public float damagePerTick;
    public GameObject indicatorObject;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
}

// 剑气实例
public class SlashProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public SlashProjectileTemplate template;
    public Vector3 direction;
    public float speed;
    public float lifetime;
    public float maxLifetime;
    public Vector3 prevPosition;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
    public HashSet<GameObject> hitEnemies;  // 记录本波已伤害的敌人，防止重复伤敌
}
```

### 事件设计

```csharp
/// <summary>
/// 弹射物命中事件（供特效/音效系统订阅）
/// </summary>
public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate, Vector3> OnProjectileHit;
//   → GameObject                : 命中的敌人
//   → DamageTemplate.SkillType   : 技能类型
//   → PlayerPropertyTemplate     : 玩家属性（用于伤害计算）
//   → Vector3                    : 命中位置（用于特效/音效播放）

/// <summary>
/// 内部技能命中事件（自动转发给 Damage_OnEnemy，不包含命中位置）
/// </summary>
internal System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;
//   → GameObject                : 命中的敌人
//   → DamageTemplate.SkillType   : 技能类型
//   → PlayerPropertyTemplate     : 玩家属性（用于伤害计算）

/// <summary>
/// 弹射物销毁/回收事件（供特效系统订阅）
/// </summary>
public System.Action<GameObject> OnProjectileExpired;
```

### 伤害结算链路

```
弹射物命中
    → OnProjectileHit?.Invoke(enemy, skillType, playerProp, hitPoint)  // 触发特效/音效
    → OnSkillHit?.Invoke(enemy, skillType, playerProp)                // 转发给 Damage_OnEnemy
        → Damage_OnEnemy.HandleSkillHit()
            → Damage_OnEnemy.SettleDamage()
```

### Spawn 接口（按类型重载）

```csharp
// 飞行弹射物生成
void SpawnFlyingProjectile(
    FlyingProjectileTemplate template,
    Vector3 spawnPosition,
    Vector3 direction,
    DamageTemplate.SkillType skillType,
    PlayerPropertyTemplate playerProp
);

// 地面AOE生成
void SpawnGroundAOEProjectile(
    GroundAOEProjectileTemplate template,
    Vector3 spawnPosition,
    DamageTemplate.SkillType skillType,
    PlayerPropertyTemplate playerProp
);

// 剑气生成
void SpawnSlashProjectile(
    SlashProjectileTemplate template,
    Vector3 spawnPosition,
    Vector3 direction,
    DamageTemplate.SkillType skillType,
    PlayerPropertyTemplate playerProp
);
```

---

## 局限与扩展方向

| 方向 | 当前是否实现 | 如何扩展 |
|------|-------------|---------|
| 穿透 | ✅ 已实现 | SwordR 剑气通过 BoxCast + hitEnemies HashSet 实现穿透 |
| 弹跳 | 否 | 修改 FlyingProjectile.Update，转向计算 |
| 追踪 | 否 | 在 FlyingProjectile.Update 中每帧朝向目标计算 direction |
| 弹射物限流 | ✅ 已有限制 | MAX_ACTIVE_PROJECTILES 常量控制 |
| 防重复伤敌 | ✅ 已实现 | SlashProjectile.hitEnemies HashSet，同一波剑气不重复伤敌 |

---

## 全局限制（安全措施）

```csharp
// 最大同时存在弹射物数量（防止作弊/卡顿）
const int MAX_ACTIVE_PROJECTILES = 50;

// 超时强制回收（秒）
const float MAX_PROJECTILE_AGE = 10f;
```

超过上限时拒绝生成弹射物，并输出 Debug 警告。

---

## 实现状态

### Phase 1：代码文件 ✅ 已完成

| 文件 | 状态 |
|------|------|
| `SO_ProjectileTemplate.cs`（抽象基类） | ✅ |
| `SO_FlyingProjectileTemplate.cs` | ✅ |
| `SO_GroundAOEProjectileTemplate.cs` | ✅ |
| `SO_SlashProjectileTemplate.cs` | ✅ |
| `Global_ProjectileManager.cs`（完整实现） | ✅ |

### Phase 2：美术资产 ✅ 已完成

| 资产 | 状态 |
|------|------|
| SpellQ 法球 Prefab | ✅ |
| SpellR 地面指示器 Prefab | ✅ |
| SwordR 剑气 Prefab | ✅ |

### Phase 3.5：SwordR 剑气系统 ✅ 已实现

| 内容 | 状态 |
|------|------|
| SlashProjectile 新增 `hitEnemies` HashSet 防重复伤敌 | ✅ 已实现 |
| `SpawnSlashProjectile` 清空 `hitEnemies` | ✅ 已实现 |
| `UpdateSlashProjectiles` 查 HashSet 跳过已伤敌 | ✅ 已实现 |
| `PlayerSkillFunctions` 新增 `OnSwordRHit` / `SwordR_LaunchLoop` | ✅ 已实现 |
| `SlashProjectileTemplate` 新增 `maxLifetime` 可配置存活时间 | ✅ 已实现 |

> 注：`PlayerPropertyTemplate.SwordAOE_*` 字段（SwordAOE_Damage_Base、SwordAOE_Damage_Multiplier、SwordAOE_Count 等）已存在于 `SO_PlayerPropertyTemplate.cs`，无需新增。

### Phase 3：配置文件（.asset） ✅ 已完成

| 配置项 | 状态 |
|--------|------|
| `Flying_SpellQ.asset` | ✅ |
| `GroundAOE_SpellR.asset` | ✅ |
| `Slash_SwordR.asset` | ✅ |

### Phase 4：Scene 设置 ✅ 已完成

| 步骤 | 状态 |
|------|------|
| 创建空物体 `ProjectileManager_System` | ✅ |
| 挂载 `Global_ProjectileManager` 组件 | ✅ |
| 配置对象池预热列表 | ✅ |

---

## 代码结构（完整版）

```csharp
public class Global_ProjectileManager : MonoBehaviour
{
    // ===== 单例 =====
    public static Global_ProjectileManager Instance { get; private set; }

    // ===== 全局限制 =====
    private const int MAX_ACTIVE_PROJECTILES = 50;
    private const float MAX_PROJECTILE_AGE = 10f;

    // ===== Inspector 字段 =====
    [Header("对象池预热")]
    public List<FlyingProjectileTemplate> flyingTemplates;
    public List<GroundAOEProjectileTemplate> groundAoeTemplates;
    public List<SlashProjectileTemplate> slashTemplates;

    [Header("调试")]
    public bool showDebugGizmos = false;

    // ===== 事件 =====
    public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate, Vector3> OnProjectileHit;
    public System.Action<GameObject> OnProjectileExpired;
    internal System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;

    // ===== 对象池 =====
    private Dictionary<FlyingProjectileTemplate, Queue<FlyingProjectile>> m_FlyingPool;
    private Dictionary<GroundAOEProjectileTemplate, Queue<GroundAOEProjectile>> m_GroundAoePool;
    private Dictionary<SlashProjectileTemplate, Queue<SlashProjectile>> m_SlashPool;

    // ===== 活跃列表 =====
    private List<FlyingProjectile> m_ActiveFlying;
    private List<GroundAOEProjectile> m_ActiveGroundAoe;
    private List<SlashProjectile> m_ActiveSlash;

    // ===== 生命周期 =====
    void Awake();
    void Update();
    void OnDestroy();

    // ===== 模板注册 =====
    void RegisterFlyingTemplate(FlyingProjectileTemplate template, int warmupCount);
    void RegisterGroundAoeTemplate(GroundAOEProjectileTemplate template, int warmupCount);
    void RegisterSlashTemplate(SlashProjectileTemplate template, int warmupCount);

    // ===== Spawn 接口 =====
    void SpawnFlyingProjectile(FlyingProjectileTemplate template, Vector3 spawnPosition,
        Vector3 direction, DamageTemplate.SkillType skillType, PlayerPropertyTemplate playerProp);

    void SpawnGroundAOEProjectile(GroundAOEProjectileTemplate template, Vector3 spawnPosition,
        DamageTemplate.SkillType skillType, PlayerPropertyTemplate playerProp);

    void SpawnSlashProjectile(SlashProjectileTemplate template, Vector3 spawnPosition,
        Vector3 direction, DamageTemplate.SkillType skillType, PlayerPropertyTemplate playerProp);

    // ===== 对象池操作 =====
    FlyingProjectile GetFromFlyingPool(FlyingProjectileTemplate template);
    void DeactivateFlying(FlyingProjectile p, int listIndex);
    // ... 同理 GroundAOE / Slash

    // ===== 更新逻辑 =====
    void UpdateFlyingProjectiles(float deltaTime);
    void UpdateGroundAOEProjectiles(float deltaTime);
    void UpdateSlashProjectiles(float deltaTime);

    // ===== Gizmos =====
    void OnDrawGizmos();
}
```
