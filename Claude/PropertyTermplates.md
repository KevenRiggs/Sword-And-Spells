角色属性设计：

TIPS；所有属性设计为浮点数类型，方便后续计算。

共有属性：
最大生命值Health_Max;
当前生命值Health_Current;
物理强度Physical_Strength;
物理防御Physical_Defense;
法术强度Magic_Strength;
法术防御Magic_Defense;



玩家角色属性：
最大体力值（用于左键Sword消耗） Stamina_Max;
当前体力值（用于左键Sword消耗） Stamina_Current;
最大法力值（用于右键Spell消耗） Mana_Max;
当前法力值（用于右键Spell消耗） Mana_Current;
每秒血量回复值Health_Regen;
每秒体力回复值Stamina_Regen;
每秒法力回复值Mana_Regen;
玩家施法操作共六种左键+Q（Sword单体攻击），左键+E（Sword格挡），左键+R（SwordAOE），右键+Q（Spell单体攻击），右键+E（Spell格挡），右键+R（SpellAOE），以下是每种操作所需要的属性：
左键+Q（Sword单体攻击）：{
基础伤害值Damage_Base;
增伤倍率Damage_Multiplier;
体力消耗Stamina_Cost;
冷却时间Cooldown_Time;
暴击概率Crit_Chance;
}
左键+E（Sword格挡）：{
格挡减伤比例Block_Reduction;
体力消耗Stamina_Cost;
冷却时间Cooldown_Time;
完美格挡概率Perfect_Block_Chance;
}
左键+R（SwordAOE）：{
基础伤害值Damage_Base;
增伤倍率Damage_Multiplier;
体力消耗Stamina_Cost;
冷却时间Cooldown_Time;
剑气范围AOE_Range;
剑气数量AOE_Count;
}
右键+Q（Spell单体攻击）：{
基础伤害值Damage_Base;
增伤倍率Damage_Multiplier;
法力消耗Mana_Cost;
冷却时间Cooldown_Time;
法球数量Spell_Count;
}
右键+E（Spell格挡）：{
格挡减伤比例Block_Reduction;
法力消耗Mana_Cost;
冷却时间Cooldown_Time;
完美格挡概率Perfect_Block_Chance;
}
右键+R（SpellAOE）：{
基础伤害值Damage_Base;
增伤倍率Damage_Multiplier;
法力消耗Mana_Cost;
冷却时间Cooldown_Time;
影响范围AOE_Range;
持续时间Duration;
}


Enemy角色属性：
物理伤害基础值Enemy_Physical_Damage_Base;
法术伤害基础值Enemy_Magic_Damage_Base;





