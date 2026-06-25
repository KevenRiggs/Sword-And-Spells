using UnityEngine;

/// <summary>
/// 敌人受击结算：订阅技能触发事件，实现6种技能的伤害计算与结算
/// </summary>
public class DamageOnEnemy : DamageTemplate
{
    // ===== Inspector 字段 =====

    [Header("敌人属性")]
    [Tooltip("敌人属性资产（用于结算伤害）")]
    [SerializeField]
    private EnemyPropertyTemplate EnemyPropertyAsset;

    // ===== 私有字段 =====

    private PlayerPropertyTemplate m_PlayerProperty;
    private bool m_IsCriticalHit;

    // ===== 生命周期 =====

    private void Awake()
    {
        EnemyProperty = EnemyPropertyAsset;
    }

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

    // ===== 事件处理 =====

    /// <summary>
    /// 处理技能触发事件
    /// 注意：通知方应通过扩展事件传递 PlayerPropertyTemplate
    /// </summary>
    private void HandleSkillTriggered(PlayerController_Skills.SelectionState state, int skillIndex)
    {
        // TODO: 通过扩展事件接收 playerProp 参数
        // 当前临时方案：从 PlayerController_Skills 获取
        var player = FindObjectOfType<PlayerController_Skills>();
        m_PlayerProperty = player != null ? player.PlayerPropertyAsset : null;

        if (EnemyProperty == null || m_PlayerProperty == null) return;

        SkillType skillType = MapToSkillType(state, skillIndex);

        // 过滤格挡技能（不造成伤害）
        if (skillType == SkillType.SwordBlock || skillType == SkillType.SpellBlock)
            return;

        float damage = CalculateDamageForSkill(skillType, m_PlayerProperty, EnemyProperty);
        SettleDamage(damage);
    }

    // ===== 技能伤害计算 =====

    private float CalculateDamageForSkill(SkillType skillType, PlayerPropertyTemplate playerProp, EnemyPropertyTemplate enemyProp)
    {
        var (baseDamage, multiplier, critChance) = GetSkillDamageParams(skillType, playerProp);
        float strengthContrib = GetStrengthContribution(skillType, playerProp);

        float rawDamage = (baseDamage + strengthContrib) * multiplier;
        RollCriticalHit(rawDamage, critChance);

        DamageSource source = (skillType == SkillType.SwordSingle || skillType == SkillType.SwordAOE)
            ? DamageSource.Physical
            : DamageSource.Magic;

        return ApplyDefenseReduction(FinalDamage, source, enemyProp);
    }

    // ===== 辅助方法 =====

    /// <summary>
    /// 将 SelectionState + skillIndex 映射为 SkillType
    /// </summary>
    private SkillType MapToSkillType(PlayerController_Skills.SelectionState state, int skillIndex)
    {
        if (state == PlayerController_Skills.SelectionState.LeftHold)
        {
            return skillIndex switch
            {
                0 => SkillType.SwordSingle,
                1 => SkillType.SwordBlock,
                2 => SkillType.SwordAOE,
                _ => SkillType.SwordSingle
            };
        }
        else if (state == PlayerController_Skills.SelectionState.RightHold)
        {
            return skillIndex switch
            {
                0 => SkillType.SpellSingle,
                1 => SkillType.SpellBlock,
                2 => SkillType.SpellAOE,
                _ => SkillType.SpellSingle
            };
        }

        return SkillType.SwordSingle;
    }

    /// <summary>
    /// 从 PlayerProperty 提取指定技能的基础伤害、倍率、暴击率
    /// </summary>
    private (float baseDamage, float multiplier, float critChance) GetSkillDamageParams(
        SkillType skillType, PlayerPropertyTemplate playerProp)
    {
        return skillType switch
        {
            SkillType.SwordSingle => (playerProp.SwordSingle_Damage_Base,
                                      playerProp.SwordSingle_Damage_Multiplier,
                                      playerProp.SwordSingle_Crit_Chance),

            SkillType.SwordAOE => (playerProp.SwordAOE_Damage_Base,
                                   playerProp.SwordAOE_Damage_Multiplier,
                                   0f),

            SkillType.SpellSingle => (playerProp.SpellSingle_Damage_Base,
                                      playerProp.SpellSingle_Damage_Multiplier,
                                      0f),

            SkillType.SpellAOE => (playerProp.SpellAOE_Damage_Base,
                                   playerProp.SpellAOE_Damage_Multiplier,
                                   0f),

            _ => (0f, 0f, 0f)
        };
    }

    /// <summary>
    /// 根据技能类型获取力量加成（Physical_Strength 和 Magic_Strength 的加权组合）
    /// </summary>
    private float GetStrengthContribution(SkillType skillType, PlayerPropertyTemplate playerProp)
    {
        return skillType switch
        {
            SkillType.SwordSingle => 1.5f * playerProp.Physical_Strength + 0.4f * playerProp.Magic_Strength,
            SkillType.SwordAOE    => 1.2f * playerProp.Physical_Strength + 0.6f * playerProp.Magic_Strength,
            SkillType.SpellSingle => 1.6f * playerProp.Magic_Strength + 0.2f * playerProp.Physical_Strength,
            SkillType.SpellAOE    => 1.4f * playerProp.Magic_Strength + 0.2f * playerProp.Physical_Strength,
            _ => 0f
        };
    }

    /// <summary>
    /// 暴击判定
    /// </summary>
    private void RollCriticalHit(float rawDamage, float critChance)
    {
        m_IsCriticalHit = Random.value < critChance;
        FinalDamage = m_IsCriticalHit ? rawDamage * CriticalMultiplier : rawDamage;
    }

    // ===== 防御减免（重写基类） =====

    protected override float ApplyDefenseReduction(float rawDamage, DamageSource source, EnemyPropertyTemplate enemyProp)
    {
        if (enemyProp == null) return rawDamage;

        float defense = source == DamageSource.Physical
            ? enemyProp.Physical_Defense
            : enemyProp.Magic_Defense;

        float reductionRatio = (100f - Mathf.Sqrt(defense)) / 100f;
        return rawDamage * reductionRatio;
    }

    // ===== 伤害结算 =====

    protected override void SettleDamage(float finalDamage)
    {
        if (EnemyProperty == null) return;

        EnemyProperty.Health_Current = Mathf.Max(
            EnemyProperty.Health_Current - finalDamage,
            0f
        );

        // 死亡逻辑由其他系统处理
    }
}
