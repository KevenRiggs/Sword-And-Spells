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
    → PlayerSkillFunctions.HandleSkillTriggered (预处理，当前仅打印调试信息)
        → [动画播放 Sword_Q]
            → [动画第 N 帧 Animation Event]
                → OnSwordQHit()
                    → 扇形检测
                        → OnSkillHit(enemy, DamageTemplate.SkillType.SwordSingle, playerProp)
                            → Damage_OnEnemy.HandleSkillHit()
                                → Damage_OnEnemy.SettleDamage()
```

### 6. 事件签名

```csharp
// 技能命中事件（供 Damage_OnEnemy 订阅）
public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;
//   → GameObject           : 命中的敌人
//   → DamageTemplate.SkillType : 命中的技能类型（用于伤害计算）
//   → PlayerPropertyTemplate   : 玩家属性（从 PlayerController_Skills.PlayerPropertyAsset 获取，用于伤害计算）

// Animation Event 入口
public void OnSwordQHit();
```

---

## 对接说明

### PlayerSkillFunctions → Damage_OnEnemy

- `Damage_OnEnemy` 在 `OnEnable` 中通过 `player.GetComponent<PlayerSkillFunctions>()` 获取引用并订阅 `OnSkillHit`
- 收到事件后，`Damage_OnEnemy` 直接用 `EnemyPropertyAsset`（自身 Awake 中已设置）进行伤害结算
- 玩家属性（`PlayerPropertyAsset`）仍通过 `FindObjectOfType<PlayerController_Skills>()` 获取（待后续优化为事件参数传递）

### EnemyLayer 配置

- 必填字段：需要在 Inspector 中将 Enemy 层拖入 `EnemyLayer` 遮罩
- 只有设置了 Enemy 层的物体才会被扇形检测命中

---

## 后续技能设计预留

| 技能 | 当前状态 | 备注 |
|------|---------|------|
| SwordQ 单体 | ✅ 已实现 | 扇形单体 |
| SwordE 格挡 | 未设计 | 格挡无伤害，逻辑待定 |
| SwordR AOE | 未设计 | 扇形 → 多目标 |
| SpellQ 单体 | 未设计 | 可能为射线或投射物 |
| SpellE 格挡 | 未设计 |  |
| SpellR AOE | 未设计 |  |

---

## 当前代码结构

```csharp
public class PlayerSkillFunctions : MonoBehaviour
{
    // Inspector
    public float SwordQ_Radius = 5f;
    public float SwordQ_Angle = 60f;
    public LayerMask EnemyLayer;

    // 私有
    PlayerController_Skills m_PlayerCtrl;
    Transform m_PlayerTransform;

    // 事件
    public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;

    // 生命周期
    void Start() { m_PlayerCtrl = GetComponent<PlayerController_Skills>(); m_PlayerTransform = transform; }
    void OnEnable() { m_PlayerCtrl.OnSkillTriggered += HandleSkillTriggered; }
    void OnDisable() { m_PlayerCtrl.OnSkillTriggered -= HandleSkillTriggered; }

    // 事件处理（预处理，当前仅记录）
    void HandleSkillTriggered(PlayerController_Skills.SelectionState state, int skillIndex) { }

    // Animation Event 入口
    public void OnSwordQHit() { SwordQ_Detect(); }

    // 扇形检测
    GameObject DetectNearestEnemyInFan(Vector3 origin, Vector3 forward, float radius, float angle);
}
```
