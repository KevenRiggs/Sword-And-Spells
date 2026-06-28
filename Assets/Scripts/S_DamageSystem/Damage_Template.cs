using UnityEngine;

/// <summary>
/// 伤害计算模板基类（极简空壳，具体实现下沉到子类）
/// </summary>
public abstract class DamageTemplate : MonoBehaviour
{
    // ===== 枚举定义 =====

    /// <summary>技能类型（对应6种技能操作）</summary>
    public enum SkillType
    {
        SwordSingle,   // 左键+Q：剑单体攻击
        SwordBlock,    // 左键+E：剑格挡（不造成伤害）
        SwordAOE,      // 左键+R：剑AOE
        SpellSingle,   // 右键+Q：法单体攻击
        SpellBlock,    // 右键+E：法格挡（不造成伤害）
        SpellAOE,      // 右键+R：法AOE
    }

    /// <summary>伤害来源（物理/法术）</summary>
    public enum DamageSource
    {
        Physical,
        Magic
    }

    // ===== Protected 字段 =====

    protected EnemyPropertyTemplate EnemyProperty;
    protected float FinalDamage;

    // ===== 核心虚方法 =====

    /// <summary>计算原始伤害（防御减免前）</summary>
    protected virtual float CalculateRawDamage(SkillType skillType, PlayerPropertyTemplate playerProp) => 0f;

    /// <summary>应用防御减免</summary>
    protected virtual float ApplyDefenseReduction(float rawDamage, DamageSource source, EnemyPropertyTemplate enemyProp) => rawDamage;

    /// <summary>结算伤害（子类重写实现具体结算逻辑）</summary>
    protected virtual void SettleDamage(float finalDamage) { }

    // ===== 属性 =====

    /// <summary>暴击倍率</summary>
    protected float CriticalMultiplier { get; private set; } = 1.5f;
}
