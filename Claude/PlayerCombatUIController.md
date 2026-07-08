# PlayerCombatUIController 设计方案

## 1. 概述

`PlayerCombatUIController` 负责监听玩家属性变化，实时更新战斗UI中的血量(Health)、体力(Stamina)、法力(Mana)三条状态条的FillAmount显示。

## 2. 现有UI层级结构（已存在于场景中）

```
Canvas (PlayerCombatUI) - fileID: 167880924
└── PlayerState (嵌套Canvas) - fileID: 372287113
    ├── PlayerHealth (BG, Type=Sliced) - fileID: 1730772837
    │   └── PlayerCurrentHealth (Fill, Type=Filled, 绿色) - fileID: 2133013332
    ├── PlayerPower (BG, Type=Sliced) - fileID: 1864294804
    │   └── PlayerCurrentPower (Fill, Type=Filled, 黄色) - fileID: 1082453447
    └── PlayerSpell (BG, Type=Sliced) - fileID: 405671747
        └── PlayerCurrentSpell (Fill, Type=Filled, 蓝色) - fileID: 2038500644

以及 PlayerCombatUIController 挂载在空GameObject上 - fileID: 1389785860
```

**Fill对应关系：**
- `PlayerCurrentHealth.fillAmount` → Health_Current / Health_Max（绿色）
- `PlayerCurrentPower.fillAmount` → Stamina_Current / Stamina_Max（黄色）
- `PlayerCurrentSpell.fillAmount` → Mana_Current / Mana_Max（蓝色）

**注意：** Health属性定义在 `CharacterPropertyTemplate` 基类中，而非 `PlayerPropertyTemplate`。血量字段为 `Health_Max` 和 `Health_Current`。

## 3. PlayerCombatUIController 实现方案

### 3.1 依赖引用

通过Unity拖拽实现Inspector注入：

```csharp
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatUIController : MonoBehaviour
{
    [Header("Bar Fills - 从场景拖拽赋值")]
    public Image healthBarFill;    // PlayerCurrentHealth
    public Image staminaBarFill;   // PlayerCurrentPower
    public Image manaBarFill;      // PlayerCurrentSpell

    [Header("玩家属性数据")]
    public PlayerPropertyTemplate playerProperty;
}
```

### 3.2 数据流向

```
PlayerPropertyTemplate (ScriptableObject, SO)
    ↓
PlayerCombatUIController 读取 SO 字段
    ↓
Update() 每帧计算比例
    ↓
设置 Image.fillAmount
```

### 3.3 核心Update逻辑

```csharp
void Update()
{
    if (playerProperty == null) return;

    // Health (基类继承来的字段)
    float healthRatio = playerProperty.Health_Current / playerProperty.Health_Max;
    healthBarFill.fillAmount = Mathf.Clamp01(healthRatio);

    // Stamina (体力)
    float staminaRatio = playerProperty.Stamina_Current / playerProperty.Stamina_Max;
    staminaBarFill.fillAmount = Mathf.Clamp01(staminaRatio);

    // Mana (法力)
    float manaRatio = playerProperty.Mana_Current / playerProperty.Mana_Max;
    manaBarFill.fillAmount = Mathf.Clamp01(manaRatio);
}
```

### 3.4 可选：平滑过渡效果

如需渐变动画效果，可用 `Mathf.Lerp(currentFill, targetFill, smoothing * Time.deltaTime)` 替代直接赋值。

## 4. Inspector配置清单

| 字段 | 类型 | 对应场景对象 |
|------|------|-------------|
| `healthBarFill` | Image | PlayerCurrentHealth (绿色) |
| `staminaBarFill` | Image | PlayerCurrentPower (黄色) |
| `manaBarFill` | Image | PlayerCurrentSpell (蓝色) |
| `playerProperty` | PlayerPropertyTemplate | Player0上的PlayerPropertyEditor组件中的PlayerPropertyAsset |

**注意：** PlayerPropertyTemplate 中没有 Health_Max/Current 字段，这些字段在 CharacterPropertyTemplate 基类中。通过多态访问 `playerProperty.Health_Current` 即可读取。

## 5. 注意事项

- **Health来源**：Health_Max/Current定义在 `CharacterPropertyTemplate` 基类中，读取时用 `playerProperty.Health_Max` 通过多态访问
- **FillAmount方向**：现有Image已配置为Type=Filled，FillMethod=Horizontal (FillMethod=0)，FillOrigin=0 (左到右)
- **Clamp01**：防止超出0~1范围导致Unity渲染异常
- **Canvas Render Mode**：当前为Screen Space - Overlay (Mode=0)，无需Camera引用
- **PlayerCombatUIController** 已存在于场景中(fileID: 1389785860)，但目前没有序列化字段，需要添加上述代码后重新配置Inspector
