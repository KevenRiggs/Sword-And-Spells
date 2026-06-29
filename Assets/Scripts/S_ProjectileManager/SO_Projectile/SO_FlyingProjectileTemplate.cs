using UnityEngine;

/// <summary>
/// 飞行弹射物模板 — SpellQ 用
/// 直线飞行，撞到第一个敌人后消失
/// </summary>
[CreateAssetMenu(fileName = "SO_FlyingProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/Flying")]
public class FlyingProjectileTemplate : ProjectileTemplate
{
    [Header("飞行弹射物配置")]
    [Tooltip("飞行速度（米/秒）")]
    public float speed = 20f;

    [Tooltip("碰撞体积半径（用于 SphereCast 检测）")]
    public float radius = 0.5f;

    [Tooltip("最大存活时间（秒），超时自动回收")]
    public float maxLifetime = 5f;

    [Header("发射配置")]
    [Tooltip("发射位置偏移（世界坐标，相对于玩家位置）")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0.5f);

    public override ProjectileType GetProjectileType() => ProjectileType.Flying;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterFlyingTemplate(this, warmupCount);
    }
}
