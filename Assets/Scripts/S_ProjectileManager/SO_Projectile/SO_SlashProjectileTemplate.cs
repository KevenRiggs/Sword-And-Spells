using UnityEngine;

/// <summary>
/// 剑气弹射物模板 — SwordR 用
/// 直线飞行，路径上所有敌人受击（不停止）
/// </summary>
[CreateAssetMenu(fileName = "SO_SlashProjectileTemplate", menuName = "Scriptable Objects/ProjectileTemplates/Slash")]
public class SlashProjectileTemplate : ProjectileTemplate
{
    [Header("剑气配置")]
    [Tooltip("剑气飞行速度（米/秒）")]
    public float speed = 30f;

    [Tooltip("剑气长度（米）")]
    public float slashLength = 8f;

    [Tooltip("剑气宽度（米）")]
    public float slashWidth = 1.5f;

    [Tooltip("剑气最大存活时间（秒），0表示不限制，按飞行距离自动计算")]
    public float maxLifetime = 0f;

    [Header("发射配置")]
    [Tooltip("发射位置偏移（世界坐标，相对于玩家位置）")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    public override ProjectileType GetProjectileType() => ProjectileType.Slash;

    public override void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0)
    {
        manager.RegisterSlashTemplate(this, warmupCount);
    }
}
