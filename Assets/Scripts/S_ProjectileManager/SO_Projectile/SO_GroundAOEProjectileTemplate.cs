using UnityEngine;

/// <summary>
/// 地面持续范围弹射物模板 — SpellR 用
/// 生成后不移动，在范围内持续检测敌人并造成伤害
/// </summary>
[CreateAssetMenu(fileName = "SO_GroundAOEProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/GroundAOE")]
public class GroundAOEProjectileTemplate : ProjectileTemplate
{
    [Header("地面AOE配置")]
    [Tooltip("影响范围半径（米）")]
    public float effectRadius = 5f;

    [Tooltip("持续时间（秒）")]
    public float duration = 3f;

    [Tooltip("伤害 tick 间隔（秒）")]
    public float tickInterval = 0.5f;

    [Tooltip("每 tick 伤害值")]
    public float damagePerTick = 10f;

    [Tooltip("地面指示器 Prefab（如圆形范围圈）")]
    public GameObject groundIndicator;

    [Header("发射配置")]
    [Tooltip("发射位置偏移（世界坐标，相对于玩家位置）")]
    public Vector3 spawnOffset = Vector3.zero;

    public override ProjectileType GetProjectileType() => ProjectileType.GroundAOE;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterGroundAoeTemplate(this, warmupCount);
    }
}
