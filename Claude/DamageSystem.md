# DamageSystem 设计方案

## 1. 架构概述

伤害系统分为三个层次：

```
DamageTemplate（基类）
├── DamageOnEnemy.cs（敌人受击结算）
└── DamageOnPlayer.cs（玩家受击结算）
```

**职责划分：**
- `DamageTemplate`：伤害计算公式基类，定义虚方法供子类重写
- `DamageOnEnemy`：订阅来自 `PlayerController_Skills` 的技能触发事件，根据技能类型计算并结算伤害
- `DamageOnPlayer`：接收敌人攻击通知，结算玩家受到的伤害

---

## 2. 技能类型枚举

```csharp
public enum SkillType
{
    // 左键 + Q：剑单体攻击
    SwordSingle,
    // 左键 + E：剑格挡（不造成伤害，用于格挡相关逻辑）
    SwordBlock,
    // 左键 + R：剑AOE
    SwordAOE,
    // 右键 + Q：法单体攻击
    SpellSingle,
    // 右键 + E：法格挡
    SpellBlock,
    // 右键 + R：法AOE
    SpellAOE,
}
```

**技能槽位映射（与 PlayerController_Skills 保持一致）：**

| SelectionState | Q (index=0) | E (index=1) | R (index=2) |
|----------------|-------------|------------|-------------|
| LeftHold       | SwordSingle | SwordBlock | SwordAOE    |
| RightHold      | SpellSingle | SpellBlock | SpellAOE    |

---

## 3. DamageTemplate 基类设计

### 3.1 核心概念

- **伤害来源（DamageSource）**：物理 / 法术
- **伤害类型（DamageType）**：对应 6 种技能
- **最终伤害（FinalDamage）**：经过计算后的实际伤害值

### 3.2 关键属性

```csharp
// 引用
protected PlayerPropertyTemplate PlayerProperty;  // 玩家属性（用于获取技能伤害参数）
protected EnemyPropertyTemplate  EnemyProperty;   // 敌人属性（用于获取敌人防御等）

// 伤害来源
protected DamageSource CurrentDamageSource;
protected SkillType    CurrentSkillType;

// 临时计算结果
protected float FinalDamage;       // 最终伤害值
protected bool   IsCriticalHit;     // 是否暴击
protected float CriticalMultiplier = 1.5f; // 暴击倍率（默认）
```

### 3.3 核心虚方法

```csharp
// 计算物理/法术伤害（防御减免前的原始伤害）
protected virtual float CalculateRawDamage(SkillType skillType, PlayerPropertyTemplate playerProp) { }

// 应用防御减伤
protected virtual float ApplyDefenseReduction(float rawDamage, DamageSource source, EnemyPropertyTemplate enemyProp) { }

// 计算暴击（各技能可自定义暴击逻辑）
protected virtual bool RollCriticalHit(SkillType skillType, float critChance) { }

// 结算伤害（更新生命值等）
protected virtual void SettleDamage(float finalDamage) { }
```

### 3.4 伤害计算流程

```
接收技能通知
    ↓
确定技能类型 (SkillType)
    ↓
获取玩家属性 (PlayerProperty) → 计算原始伤害
    ↓
判定暴击 → 计算暴击伤害
    ↓
获取敌人属性 (EnemyProperty) → 应用防御减伤
    ↓
结算伤害 → 更新敌人生命值
    ↓
触发伤害结算事件（如需通知其他系统）
```

---

## 4. DamageOnEnemy 实现方案

### 4.1 组件配置

```
Inspector 字段：
├── EnemyPropertyAsset : EnemyPropertyTemplate   // 拖入敌人属性资产
├── PlayerPropertyAsset : PlayerPropertyTemplate // 拖入玩家属性资产（用于获取技能参数）
└── CriticalMultiplier : float                  // 暴击倍率（默认1.5）
```

### 4.2 订阅技能通知

在 `OnEnable()` 中订阅 `PlayerController_Skills.OnSkillTriggered`：

```csharp
private void OnEnable()
{
    PlayerController_Skills player = FindObjectOfType<PlayerController_Skills>();
    if (player != null)
    {
        player.OnSkillTriggered += OnPlayerSkillTriggered;
    }
}

private void OnDisable()
{
    if (player != null)
    {
        player.OnSkillTriggered -= OnPlayerSkillTriggered;
    }
}
```

### 4.3 技能伤害计算（虚方法待填充）

以下六个方法在 `DamageTemplate` 中声明为 `virtual`，由 `DamageOnEnemy` 重写具体计算公式：

#### SwordSingle（剑单体攻击）
```csharp
// 待填充：剑单体伤害计算
// 参考属性：SwordSingle_Damage_Base, SwordSingle_Damage_Multiplier, SwordSingle_Crit_Chance
```

#### SwordBlock（剑格挡）
```csharp
// 待填充：剑格挡（此技能不造成伤害，可用于特殊逻辑或直接忽略）
```

#### SwordAOE（剑AOE）
```csharp
// 待填充：剑AOE伤害计算
// 参考属性：SwordAOE_Damage_Base, SwordAOE_Damage_Multiplier
```

#### SpellSingle（法单体攻击）
```csharp
// 待填充：法单体伤害计算
// 参考属性：SpellSingle_Damage_Base, SpellSingle_Damage_Multiplier
```

#### SpellBlock（法格挡）
```csharp
// 待填充：法格挡（此技能不造成伤害，可用于特殊逻辑或直接忽略）
```

#### SpellAOE（法AOE）
```csharp
// 待填充：法AOE伤害计算
// 参考属性：SpellAOE_Damage_Base, SpellAOE_Damage_Multiplier
```

### 4.4 防御减伤计算

```csharp
protected override float ApplyDefenseReduction(float rawDamage, DamageSource source, EnemyPropertyTemplate enemyProp)
{
    if (source == DamageSource.Physical)
    {
        float reduction = rawDamage * (enemyProp.Physical_Defense / 100f);
        return Mathf.Max(rawDamage - reduction, 0f);
    }
    else // Magic
    {
        float reduction = rawDamage * (enemyProp.Magic_Defense / 100f);
        return Mathf.Max(rawDamage - reduction, 0f);
    }
}
```

### 4.5 伤害结算

```csharp
protected override void SettleDamage(float finalDamage)
{
    if (EnemyProperty == null) return;

    EnemyProperty.Health_Current = Mathf.Max(
        EnemyProperty.Health_Current - finalDamage,
        0f
    );

    // 触发死亡检查
    if (EnemyProperty.Health_Current <= 0f)
    {
        OnEnemyDefeated();
    }
}
```

---

## 5. 伤害来源枚举

```csharp
public enum DamageSource
{
    Physical,
    Magic
}
```

| 技能类型   | 伤害来源 |
|-----------|---------|
| SwordSingle | Physical |
| SwordBlock  | Physical（但不造成伤害）|
| SwordAOE    | Physical |
| SpellSingle | Magic    |
| SpellBlock  | Magic（但不造成伤害）|
| SpellAOE    | Magic    |

---

## 6. 文件结构

```
Assets/Scripts/S_DamageSystem/
├── DamageTemplate.cs      // 基类（抽象/虚方法）
├── DamageOnEnemy.cs        // 敌人受击结算
└── DamageOnPlayer.cs       // 玩家受击结算（本次不实现）
```

---

## 7. 待填充的计算公式占位符

以下方法签名已声明，计算公式留空供后续填充：

| 方法 | 所属类 | 状态 |
|-----|-------|-----|
| CalculateSwordSingleDamage | DamageOnEnemy | 待填充 |
| CalculateSwordAOEDamage | DamageOnEnemy | 待填充 |
| CalculateSpellSingleDamage | DamageOnEnemy | 待填充 |
| CalculateSpellAOEDamage | DamageOnEnemy | 待填充 |
| RollSwordSingleCritical | DamageOnEnemy | 待填充 |
| RollSpellSingleCritical | DamageOnEnemy | 待填充 |

---

## 8. 扩展建议（可选）

- 添加伤害数字飘字（DamagePopup）
- 添加受击动画触发接口
- 添加伤害结算事件广播给 UI 系统更新血条
