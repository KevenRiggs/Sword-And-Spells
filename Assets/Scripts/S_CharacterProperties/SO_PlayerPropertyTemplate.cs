using UnityEngine;

[CreateAssetMenu(fileName = "PlayerProperty", menuName = "Scriptable Objects/PlayerProperty")]
public class PlayerPropertyTemplate : CharacterPropertyTemplate
{
    // ===== 玩家资源属性 =====

    // 最大体力值（用于左键Sword消耗）
    public float Stamina_Max;
    // 当前体力值
    public float Stamina_Current;
    // 最大法力值（用于右键Spell消耗）
    public float Mana_Max;
    // 当前法力值
    public float Mana_Current;

    // ===== 回复属性 =====

    // 每秒血量回复值
    public float Health_Regen;
    // 每秒体力回复值
    public float Stamina_Regen;
    // 每秒法力回复值
    public float Mana_Regen;

    // ===== 技能操作：左键+Q（Sword单体攻击）=====
    // 基础伤害值
    public float SwordSingle_Damage_Base;
    // 增伤倍率
    public float SwordSingle_Damage_Multiplier;
    // 体力消耗
    public float SwordSingle_Stamina_Cost;
    // 冷却时间
    public float SwordSingle_Cooldown_Time;
    // 暴击概率
    public float SwordSingle_Crit_Chance;

    // ===== 技能操作：左键+E（Sword格挡）=====
    // 格挡减伤比例
    public float SwordBlock_Block_Reduction;
    // 体力消耗
    public float SwordBlock_Stamina_Cost;
    // 冷却时间
    public float SwordBlock_Cooldown_Time;
    // 完美格挡概率
    public float SwordBlock_Perfect_Block_Chance;

    // ===== 技能操作：左键+R（SwordAOE）=====
    // 基础伤害值
    public float SwordAOE_Damage_Base;
    // 增伤倍率
    public float SwordAOE_Damage_Multiplier;
    // 体力消耗
    public float SwordAOE_Stamina_Cost;
    // 冷却时间
    public float SwordAOE_Cooldown_Time;
    // 剑气范围
    public float SwordAOE_Range;
    // 剑气数量
    public float SwordAOE_Count;

    // ===== 技能操作：右键+Q（Spell单体攻击）=====
    // 基础伤害值
    public float SpellSingle_Damage_Base;
    // 增伤倍率
    public float SpellSingle_Damage_Multiplier;
    // 法力消耗
    public float SpellSingle_Mana_Cost;
    // 冷却时间
    public float SpellSingle_Cooldown_Time;
    // 法球数量
    public float SpellSingle_Spell_Count;

    // ===== 技能操作：右键+E（Spell格挡）=====
    // 格挡减伤比例
    public float SpellBlock_Block_Reduction;
    // 法力消耗
    public float SpellBlock_Mana_Cost;
    // 冷却时间
    public float SpellBlock_Cooldown_Time;
    // 完美格挡概率
    public float SpellBlock_Perfect_Block_Chance;

    // ===== 技能操作：右键+R（SpellAOE）=====
    // 基础伤害值
    public float SpellAOE_Damage_Base;
    // 增伤倍率
    public float SpellAOE_Damage_Multiplier;
    // 法力消耗
    public float SpellAOE_Mana_Cost;
    // 冷却时间
    public float SpellAOE_Cooldown_Time;
    // 影响范围
    public float SpellAOE_Range;
    // 持续时间
    public float SpellAOE_Duration;
}