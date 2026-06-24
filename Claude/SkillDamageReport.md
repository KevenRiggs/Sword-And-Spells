# SkillDamageReport 通知方案

## 1. 概述

本方案仅设计 `PlayerController_Skills` 向 `DamageOnEnemy` 传递技能伤害通知的机制，不包含碰撞检测逻辑。

**核心设计思路：**
- `PlayerController_Skills` 作为技能触发源，通过事件系统广播技能通知
- `DamageOnEnemy` 订阅该事件，接收通知后执行伤害计算与结算

---

## 2. 现有事件结构（已存在）

`PlayerController_Skills` 已定义以下事件：

```csharp
/// <summary>
/// 在选定状态下触发技能时触发
/// state: 当前选定状态（LeftHold/RightHold）
/// skillIndex: 0=Q, 1=E, 2=R
/// </summary>
public System.Action<SelectionState, int> OnSkillTriggered;
```

**触发时机：**
- 在 `TryTriggerSkill()` 方法中，动画播放后触发
- 参数传递完整，可由此推断具体技能类型

---

## 3. 通知数据结构

### 3.1 新增事件参数类（可选方案）

```csharp
/// <summary>
/// 技能伤害通知数据包
/// </summary>
[System.Serializable]
public struct SkillDamageReport
{
    /// <summary>技能类型</summary>
    public SkillType SkillType;
    /// <summary>伤害来源：物理/法术</summary>
    public DamageSource DamageSource;
    /// <summary>玩家属性引用（用于获取技能伤害参数）</summary>
    public PlayerPropertyTemplate PlayerProperty;
    /// <summary>受击目标（可选，用于精确指定目标）</summary>
    public GameObject Target;
}
```

### 3.2 简化方案（直接使用现有事件）

不新增结构体，直接利用现有 `OnSkillTriggered(SelectionState, int)` 事件：

```csharp
// SkillType 映射逻辑（供 DamageOnEnemy 使用）
private SkillType MapToSkillType(SelectionState state, int skillIndex)
{
    if (state == SelectionState.LeftHold)
    {
        return skillIndex switch
        {
            0 => SkillType.SwordSingle,
            1 => SkillType.SwordBlock,
            2 => SkillType.SwordAOE,
            _ => SkillType.SwordSingle
        };
    }
    else // RightHold
    {
        return skillIndex switch
        {
            0 => SkillType.SpellSingle,
            1 => SkillType.SpellBlock,
            2 => SkillType.SpellAOE,
            _ => SkillType.SpellSingle
        };
    }
}
```

---

## 4. DamageOnEnemy 订阅方案

```csharp
public class DamageOnEnemy : DamageTemplate
{
    private PlayerController_Skills m_PlayerSkills;

    private void OnEnable()
    {
        m_PlayerSkills = FindObjectOfType<PlayerController_Skills>();
        if (m_PlayerSkills != null)
        {
            m_PlayerSkills.OnSkillTriggered += HandleSkillTriggered;
        }
    }

    private void OnDisable()
    {
        if (m_PlayerSkills != null)
        {
            m_PlayerSkills.OnSkillTriggered -= HandleSkillTriggered;
        }
    }

    private void HandleSkillTriggered(PlayerController_Skills.SelectionState state, int skillIndex)
    {
        // 过滤格挡技能（不造成伤害）
        SkillType skillType = MapToSkillType(state, skillIndex);
        if (skillType == SkillType.SwordBlock || skillType == SkillType.SpellBlock)
        {
            // 格挡技能不触发伤害结算，可在此处理格挡特效等逻辑
            return;
        }

        // 获取技能伤害参数
        SkillDamageParams damageParams = GetSkillDamageParams(skillType);

        // 计算并结算伤害
        CalculateAndSettleDamage(skillType, damageParams);
    }

    private SkillType MapToSkillType(PlayerController_Skills.SelectionState state, int skillIndex)
    {
        // 详见上方 3.3 节
    }

    private SkillDamageParams GetSkillDamageParams(SkillType skillType)
    {
        // 从 PlayerPropertyAsset 提取对应技能参数
    }

    private void CalculateAndSettleDamage(SkillType skillType, SkillDamageParams damageParams)
    {
        // 调用 DamageTemplate 中的虚方法计算伤害
        float rawDamage = CalculateRawDamage(skillType, damageParams);
        float finalDamage = ApplyDefenseReduction(rawDamage, damageParams.Source, EnemyProperty);
        SettleDamage(finalDamage);
    }
}
```

---

## 5. 新增数据类占位符

```csharp
/// <summary>
/// 技能伤害参数（从 PlayerProperty 提取）
/// </summary>
[System.Serializable]
public struct SkillDamageParams
{
    public float BaseDamage;         // 基础伤害
    public float Multiplier;         // 增伤倍率
    public float CritChance;         // 暴击概率
    public DamageSource Source;       // 伤害来源
}
```

---

## 6. 流程图

```
PlayerController_Skills.TryTriggerSkill()
    │
    ├── 播放动画
    ├── 触发 OnSkillTriggered(state, skillIndex)
    │
    ▼
DamageOnEnemy.HandleSkillTriggered()
    │
    ├── MapToSkillType(state, skillIndex) → SkillType
    │
    ├── 判断是否为格挡技能 → 是则跳过
    │
    ├── GetSkillDamageParams(skillType) → SkillDamageParams
    │
    ├── CalculateRawDamage() → 原始伤害
    │
    ├── ApplyDefenseReduction() → 防御减免后伤害
    │
    └── SettleDamage() → 结算到 EnemyProperty.Health_Current
```

---

## 7. 后续扩展方向

本方案仅实现通知传递，碰撞检测与敌人交互待后续设计。建议扩展点：

1. **碰撞检测整合**：在技能命中敌人后，调用 `DamageOnEnemy.ReceiveDamageFromPlayer()`
2. **AOE 技能**：需要获取范围内所有敌人，循环调用伤害结算
3. **受击反馈**：触发敌人受击动画、音效、特效
4. **伤害飘字**：在结算时生成伤害数字 UI
