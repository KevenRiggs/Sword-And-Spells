using UnityEngine;

/// <summary>
/// 敌人受击结算：订阅技能命中事件，实现6种技能的伤害计算与结算
/// </summary>
public class Damage_OnEnemy : DamageTemplate
{
    // ===== Inspector 字段 =====

    [Header("敌人属性")]
    [Tooltip("敌人属性资产（用于结算伤害）")]
    [SerializeField]
    private EnemyPropertyTemplate EnemyPropertyAsset;

    // ===== 私有字段 =====

    private bool m_IsCriticalHit;
    private PlayerController_Skills m_PlayerCtrl;

    // ===== 生命周期 =====

    private void Awake()
    {
        EnemyProperty = EnemyPropertyAsset;
        m_PlayerCtrl = FindAnyObjectByType<PlayerController_Skills>();
    }

    private void OnEnable()
    {
        // 订阅 PlayerSkillFunctions 的直接命中事件（SwordQ 等扇形检测技能）
        if (m_PlayerCtrl != null)
        {
            var skillFunctions = m_PlayerCtrl.GetComponent<PlayerSkillFunctions>();
            if (skillFunctions != null)
            {
                skillFunctions.OnSkillHit += HandleSkillHit;
            }
        }

        // 订阅 Global_ProjectileManager 的弹射物命中事件（SpellQ 等弹射物技能）
        if (Global_ProjectileManager.Instance != null)
        {
            Global_ProjectileManager.Instance.OnSkillHit += HandleSkillHit;
        }
    }

    private void OnDisable()
    {
        // 取消订阅 PlayerSkillFunctions 的直接命中事件
        if (m_PlayerCtrl != null)
        {
            var skillFunctions = m_PlayerCtrl.GetComponent<PlayerSkillFunctions>();
            if (skillFunctions != null)
            {
                skillFunctions.OnSkillHit -= HandleSkillHit;
            }
        }

        // 取消订阅 Global_ProjectileManager 的弹射物命中事件
        if (Global_ProjectileManager.Instance != null)
        {
            Global_ProjectileManager.Instance.OnSkillHit -= HandleSkillHit;
        }
    }

    // ===== 事件处理 =====

    /// <summary>
    /// 处理技能命中事件
    /// </summary>
    private void HandleSkillHit(GameObject enemy, SkillType skillType, PlayerPropertyTemplate playerProp)
    {
        // 校验：只有被命中的敌人才处理伤害
        if (enemy != this.gameObject)
            return;

        if (EnemyProperty == null || playerProp == null) return;

        // 过滤格挡技能（不造成伤害）
        if (skillType == SkillType.SwordBlock || skillType == SkillType.SpellBlock)
            return;

        float damage = CalculateDamageForSkill(skillType, playerProp, EnemyProperty);
        SettleDamage(damage);

        // ===== 调试打印 =====
        string critTag = m_IsCriticalHit ? "【暴击！】" : "";
        Debug.Log(
            $"[伤害结算] 敌人: {enemy.name} | 技能: {skillType} | " +
            $"伤害: {damage:F1} | 剩余血量: {EnemyProperty.Health_Current:F1} {critTag}"
        );

        // 通知调试显示
        OnDamageDebug?.Invoke(enemy.name, skillType.ToString(), damage, m_IsCriticalHit, EnemyProperty.Health_Current);
    }

    /// <summary>
    /// 伤害调试事件，供 PlayerSkillFunctions 订阅用于屏幕显示
    /// </summary>
    public System.Action<string, string, float, bool, float> OnDamageDebug;

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
