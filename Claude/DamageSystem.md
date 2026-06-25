# DamageSystem 设计方案

## 1. 架构概述

伤害系统分为两个层次：

```
DamageTemplate（基类）
└── DamageOnEnemy（敌人受击结算）
```

**职责划分：**
- `DamageTemplate`：伤害计算公式基类，定义枚举、字段、虚方法、核心计算流程
- `DamageOnEnemy`：订阅来自 `PlayerController_Skills` 的技能触发事件，实现6种技能的伤害计算与结算

---

## 2. 枚举定义

### 2.1 SkillType（技能类型）

```csharp
public enum SkillType
{
    SwordSingle,   // 左键+Q：剑单体攻击
    SwordBlock,    // 左键+E：剑格挡（不造成伤害）
    SwordAOE,      // 左键+R：剑AOE
    SpellSingle,   // 右键+Q：法单体攻击
    SpellBlock,    // 右键+E：法格挡（不造成伤害）
    SpellAOE,      // 右键+R：法AOE
}
```

### 2.2 DamageSource（伤害来源）

```csharp
public enum DamageSource
{
    Physical,
    Magic
}
```

### 2.3 技能槽位映射

| SelectionState | Q (index=0) | E (index=1) | R (index=2) |
|----------------|-------------|-------------|-------------|
| LeftHold       | SwordSingle | SwordBlock  | SwordAOE    |
| RightHold      | SpellSingle | SpellBlock  | SpellAOE    |

---

## 3. 防御减免公式

### 公式

```
防御减免比例 = (100 - √Defense) %
最终伤害 = 原始伤害 × (100 - √Defense) / 100
```

### 数学含义

| 防御值 | √Defense | 100 - √Defense | 实际受到伤害比例 |
|--------|----------|----------------|-----------------|
| 0      | 0        | 100            | 100% (无减免)   |
| 25     | 5        | 95             | 95%             |
| 100    | 10       | 90             | 90%             |
| 400    | 20       | 80             | 80%             |
| 900    | 30       | 70             | 70%             |
| 10000  | 100      | 0              | 0% (完全免伤)   |

### 设计特点

1. **收益递减** - 防御从0到100比从100到400需要更多防御值，但减免效果相同
2. **无法满减** - 即使防御趋向无穷大，只能接近0%受到伤害，永远不会达到0
3. **物理/法术分离** - 物理技能用 `Physical_Defense`，法术技能用 `Magic_Defense`

---

## 4. DamageTemplate 基类设计

### 4.1 Inspector 字段

| 字段 | 类型 | 说明 |
|-----|-----|-----|
| `PlayerPropertyAsset` | PlayerPropertyTemplate | 玩家属性资产（用于获取技能伤害参数） |

### 4.2 Protected 字段

| 字段 | 类型 | 默认值 | 说明 |
|-----|-----|-------|-----|
| `PlayerProperty` | PlayerPropertyTemplate | - | 玩家属性引用 |
| `EnemyProperty` | EnemyPropertyTemplate | - | 敌人属性引用（子类赋值） |
| `CurrentDamageSource` | DamageSource | - | 当前伤害来源 |
| `CurrentSkillType` | SkillType | - | 当前技能类型 |
| `FinalDamage` | float | - | 最终伤害值 |
| `IsCriticalHit` | bool | false | 是否暴击 |
| `CriticalMultiplier` | float | 1.5f | 暴击倍率 |

### 4.3 核心虚方法

| 方法 | 说明 |
|-----|-----|
| `CalculateRawDamage(SkillType skillType, PlayerPropertyTemplate playerProp)` | 计算原始伤害（防御减免前） |
| `ApplyDefenseReduction(float rawDamage, DamageSource source, EnemyPropertyTemplate enemyProp)` | 应用防御减伤 |
| `SettleDamage(float finalDamage)` | 结算伤害（子类重写实现具体结算逻辑） |

### 4.4 辅助方法

| 方法 | 说明 |
|-----|-----|
| `MapToSkillType(SelectionState state, int skillIndex)` | 将 SelectionState + skillIndex 映射为 SkillType |
| `GetSkillDamageParams(SkillType skillType)` | 从 PlayerProperty 提取指定技能的基础伤害和倍率 |

### 4.5 伤害计算流程

```
接收技能通知 (SelectionState, int)
    ↓
MapToSkillType() → SkillType
    ↓
过滤格挡技能（SwordBlock/SpellBlock 直接返回）
    ↓
GetSkillDamageParams() → (BaseDamage, Multiplier, CritChance)
    ↓
CalculateRawDamage() → 原始伤害
    ↓
RollCriticalHit() → 判定暴击 → 计算暴击伤害
    ↓
ApplyDefenseReduction() → 防御减免后伤害
    ↓
SettleDamage() → 结算到 EnemyProperty.Health_Current
```

---

## 5. DamageOnEnemy 实现方案

### 5.1 Inspector 字段

| 字段 | 类型 | 说明 |
|-----|-----|-----|
| `EnemyPropertyAsset` | EnemyPropertyTemplate | 敌人属性资产 |
| `PlayerPropertyAsset` | PlayerPropertyTemplate | 玩家属性资产（从基类继承） |
| `CriticalMultiplier` | float | 暴击倍率（默认1.5） |

### 5.2 事件订阅

订阅 `PlayerController_Skills.OnSkillTriggered` 事件：

```csharp
private void OnEnable()
{
    var player = FindObjectOfType<PlayerController_Skills>();
    if (player != null)
    {
        player.OnSkillTriggered += HandleSkillTriggered;
    }
}

private void OnDisable()
{
    var player = FindObjectOfType<PlayerController_Skills>();
    if (player != null)
    {
        player.OnSkillTriggered -= HandleSkillTriggered;
    }
}
```

### 5.3 技能伤害计算公式

#### SwordSingle（剑单体攻击）- 有暴击

```
原始伤害 = (SwordSingle_Damage_Base + 1.5 × Physical_Strength + 0.4 × Magic_Strength) × SwordSingle_Damage_Multiplier
暴击伤害 = 原始伤害 × CriticalMultiplier (如果暴击)
最终伤害 = 暴击伤害 × (100 - √Physical_Defense) / 100
```

#### SwordBlock（剑格挡）

不造成伤害，本次不实现。

#### SwordAOE（剑AOE）

```
最终伤害 = (SwordAOE_Damage_Base + 1.2 × Physical_Strength + 0.6 × Magic_Strength) × SwordAOE_Damage_Multiplier × (100 - √Physical_Defense) / 100
```

#### SpellSingle（法单体攻击）

```
最终伤害 = (SpellSingle_Damage_Base + 1.6 × Magic_Strength + 0.2 × Physical_Strength) × SpellSingle_Damage_Multiplier × (100 - √Magic_Defense) / 100
```

#### SpellBlock（法格挡）

不造成伤害，本次不实现。

#### SpellAOE（法AOE）

```
最终伤害 = (SpellAOE_Damage_Base + 1.4 × Magic_Strength + 0.2 × Physical_Strength) × SpellAOE_Damage_Multiplier × (100 - √Magic_Defense) / 100
```

### 5.4 伤害结算

```csharp
protected override void SettleDamage(float finalDamage)
{
    if (EnemyProperty == null) return;

    EnemyProperty.Health_Current = Mathf.Max(
        EnemyProperty.Health_Current - finalDamage,
        0f
    );
    // 死亡逻辑由其他系统处理
}
```

---

## 6. 文件结构

```
Assets/Scripts/S_DamageSystem/
├── DamageTemplate.cs      // 基类（定义枚举、字段、虚方法）
└── DamageOnEnemy.cs       // 敌人受击结算（实现6种技能计算公式）
```

---

## 7. 依赖关系

```
DamageTemplate.cs
├── PlayerPropertyTemplate (Assets/Scripts/S_CharacterProperties/)
├── EnemyPropertyTemplate  (Assets/Scripts/S_CharacterProperties/)
└── PlayerController_Skills (事件来源，待实现)

DamageOnEnemy.cs
└── DamageTemplate.cs (基类)
```

---

## 8. 实现顺序

### 第一步：实现 DamageTemplate.cs
1. 定义枚举 `SkillType`、`DamageSource`
2. 定义 `protected` 字段
3. 实现 `MapToSkillType(SelectionState, int)` 辅助方法
4. 实现 `GetSkillDamageParams(SkillType)` 辅助方法
5. 实现 `CalculateRawDamage(SkillType, PlayerPropertyTemplate)` 虚方法
6. 实现 `ApplyDefenseReduction(float, DamageSource, EnemyPropertyTemplate)` 虚方法
7. 实现 `SettleDamage(float)` 虚方法（默认实现）

### 第二步：实现 DamageOnEnemy.cs
1. 定义 Inspector 字段 `EnemyPropertyAsset`、`CriticalMultiplier`
2. 实现 `OnEnable()` / `OnDisable()` 事件订阅
3. 实现 `HandleSkillTriggered(SelectionState, int)` 处理方法
4. 重写6种技能的具体计算公式
5. 重写 `SettleDamage(float)` 结算到 `EnemyProperty.Health_Current`

---

## 9. 确认事项

- [x] `PlayerController_Skills` 中的 `SelectionState` 枚举定义保持一致
- [x] `EnemyPropertyTemplate` 已确认：继承自 `CharacterPropertyTemplate`
- [x] 死亡检查 `OnEnemyDefeated` 不在伤害系统中实现，由其他系统处理