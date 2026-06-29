using UnityEngine;

/// <summary>
/// 弹射物类型枚举
/// </summary>
public enum ProjectileType
{
    Flying = 0,      // 飞行弹射物
    GroundAOE = 1,   // 地面持续范围
    Slash = 2        // 剑气
}

/// <summary>
/// 弹射物模板基类，定义共有字段和行为接口
/// </summary>
public abstract class ProjectileTemplate : ScriptableObject
{
    [Header("通用配置")]
    [Tooltip("视觉 Prefab")]
    public GameObject prefab;

    [Tooltip("命中特效 Prefab（可选）")]
    public GameObject hitVFX;

    [Tooltip("命中音效（可选）")]
    public AudioClip hitSFX;

    [Tooltip("碰撞检测层级（敌人发射填 Player 层，玩家发射填 Enemy 层）")]
    public LayerMask hitLayer;

    /// <summary>
    /// 获取该模板的类型枚举
    /// </summary>
    public abstract ProjectileType GetProjectileType();

    /// <summary>
    /// 注册到管理器的对象池
    /// </summary>
    public abstract void RegisterToPool(Global_ProjectileManager manager, int warmupCount = 0);
}
