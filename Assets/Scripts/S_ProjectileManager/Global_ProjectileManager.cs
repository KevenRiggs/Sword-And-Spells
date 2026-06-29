using UnityEngine;
using System.Collections.Generic;

public class Global_ProjectileManager : MonoBehaviour
{
    // ===== 单例 =====
    public static Global_ProjectileManager Instance { get; private set; }

    // ===== 全局限制 =====
    private const int MAX_ACTIVE_PROJECTILES = 50;
    private const float MAX_PROJECTILE_AGE = 10f;

    // ===== Inspector 字段 =====
    [Header("对象池预热")]
    [Tooltip("将 FlyingProjectileTemplate .asset 拖入")]
    public List<FlyingProjectileTemplate> flyingTemplates;
    [Tooltip("将 GroundAOEProjectileTemplate .asset 拖入")]
    public List<GroundAOEProjectileTemplate> groundAoeTemplates;
    [Tooltip("将 SlashProjectileTemplate .asset 拖入")]
    public List<SlashProjectileTemplate> slashTemplates;

    [Header("调试")]
    public bool showDebugGizmos = false;

    // ===== 事件 =====
    public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate, Vector3> OnProjectileHit;
    public System.Action<GameObject> OnProjectileExpired;

    /// <summary>
    /// 内部技能命中事件（转发给 Damage_OnEnemy，不包含命中位置）
    /// </summary>
    internal System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;

    // ===== 对象池 =====
    private Dictionary<FlyingProjectileTemplate, Queue<FlyingProjectile>> m_FlyingPool = new Dictionary<FlyingProjectileTemplate, Queue<FlyingProjectile>>();
    private Dictionary<GroundAOEProjectileTemplate, Queue<GroundAOEProjectile>> m_GroundAoePool = new Dictionary<GroundAOEProjectileTemplate, Queue<GroundAOEProjectile>>();
    private Dictionary<SlashProjectileTemplate, Queue<SlashProjectile>> m_SlashPool = new Dictionary<SlashProjectileTemplate, Queue<SlashProjectile>>();

    // ===== 活跃列表 =====
    private List<FlyingProjectile> m_ActiveFlying = new List<FlyingProjectile>();
    private List<GroundAOEProjectile> m_ActiveGroundAoe = new List<GroundAOEProjectile>();
    private List<SlashProjectile> m_ActiveSlash = new List<SlashProjectile>();

    // ===== 生命周期 =====

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Global_ProjectileManager] Duplicate instance detected. Destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 预热对象池
        WarmupPools();
    }

    void Update()
    {
        UpdateFlyingProjectiles(Time.deltaTime);
        UpdateGroundAOEProjectiles(Time.deltaTime);
        UpdateSlashProjectiles(Time.deltaTime);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ===== 对象池预热 =====

    private void WarmupPools()
    {
        // Flying
        if (flyingTemplates != null)
        {
            foreach (var t in flyingTemplates)
            {
                if (t != null)
                    t.RegisterToPool(this, 5);
            }
        }

        // GroundAOE
        if (groundAoeTemplates != null)
        {
            foreach (var t in groundAoeTemplates)
            {
                if (t != null)
                    t.RegisterToPool(this, 2);
            }
        }

        // Slash
        if (slashTemplates != null)
        {
            foreach (var t in slashTemplates)
            {
                if (t != null)
                    t.RegisterToPool(this, 3);
            }
        }
    }

    // ===== 模板注册接口（由模板类在 RegisterToPool 中调用）=====

    public void RegisterFlyingTemplate(FlyingProjectileTemplate template, int warmupCount)
    {
        if (template == null) return;

        if (!m_FlyingPool.ContainsKey(template))
            m_FlyingPool[template] = new Queue<FlyingProjectile>();

        Queue<FlyingProjectile> pool = m_FlyingPool[template];
        for (int i = 0; i < warmupCount; i++)
        {
            GameObject go = Instantiate(template.prefab);
            go.SetActive(false);
            var p = new FlyingProjectile
            {
                gameObject = go,
                transform = go.transform,
                template = template,
                isActive = false
            };
            pool.Enqueue(p);
        }
    }

    public void RegisterGroundAoeTemplate(GroundAOEProjectileTemplate template, int warmupCount)
    {
        if (template == null) return;

        if (!m_GroundAoePool.ContainsKey(template))
            m_GroundAoePool[template] = new Queue<GroundAOEProjectile>();

        Queue<GroundAOEProjectile> pool = m_GroundAoePool[template];
        for (int i = 0; i < warmupCount; i++)
        {
            GameObject go = Instantiate(template.prefab);
            go.SetActive(false);
            var p = new GroundAOEProjectile
            {
                gameObject = go,
                transform = go.transform,
                template = template,
                isActive = false
            };
            pool.Enqueue(p);
        }
    }

    public void RegisterSlashTemplate(SlashProjectileTemplate template, int warmupCount)
    {
        if (template == null) return;

        if (!m_SlashPool.ContainsKey(template))
            m_SlashPool[template] = new Queue<SlashProjectile>();

        Queue<SlashProjectile> pool = m_SlashPool[template];
        for (int i = 0; i < warmupCount; i++)
        {
            GameObject go = Instantiate(template.prefab);
            go.SetActive(false);
            var p = new SlashProjectile
            {
                gameObject = go,
                transform = go.transform,
                template = template,
                isActive = false
            };
            pool.Enqueue(p);
        }
    }

    // ===== Spawn 接口 =====

    /// <summary>
    /// 生成飞行弹射物（SpellQ）
    /// </summary>
    public void SpawnFlyingProjectile(
        FlyingProjectileTemplate template,
        Vector3 spawnPosition,
        Vector3 direction,
        DamageTemplate.SkillType skillType,
        PlayerPropertyTemplate playerProp)
    {
        if (!CanSpawn()) return;

        FlyingProjectile p = GetFromFlyingPool(template);
        p.transform.position = spawnPosition;
        p.transform.forward = direction.normalized;
        p.direction = direction.normalized;
        p.speed = template.speed;
        p.radius = template.radius;
        p.lifetime = 0f;
        p.maxLifetime = template.maxLifetime;
        p.skillType = skillType;
        p.playerProp = playerProp;
        p.isActive = true;
        p.prevPosition = spawnPosition;
        p.gameObject.SetActive(true);

        m_ActiveFlying.Add(p);
    }

    /// <summary>
    /// 生成地面AOE弹射物（SpellR）
    /// </summary>
    public void SpawnGroundAOEProjectile(
        GroundAOEProjectileTemplate template,
        Vector3 spawnPosition,
        DamageTemplate.SkillType skillType,
        PlayerPropertyTemplate playerProp)
    {
        if (!CanSpawn()) return;

        GroundAOEProjectile p = GetFromGroundAoePool(template);
        p.transform.position = spawnPosition;
        p.elapsed = 0f;
        p.duration = template.duration;
        p.tickInterval = template.tickInterval;
        p.damagePerTick = template.damagePerTick;
        p.skillType = skillType;
        p.playerProp = playerProp;
        p.isActive = true;
        p.gameObject.SetActive(true);

        // 生成地面指示器
        if (template.groundIndicator != null)
        {
            var indicator = Instantiate(template.groundIndicator, spawnPosition, Quaternion.identity);
            p.indicatorObject = indicator;
        }

        m_ActiveGroundAoe.Add(p);
    }

    /// <summary>
    /// 生成剑气弹射物（SwordR）
    /// </summary>
    public void SpawnSlashProjectile(
        SlashProjectileTemplate template,
        Vector3 spawnPosition,
        Vector3 direction,
        DamageTemplate.SkillType skillType,
        PlayerPropertyTemplate playerProp)
    {
        if (!CanSpawn()) return;

        SlashProjectile p = GetFromSlashPool(template);
        p.transform.position = spawnPosition;
        p.transform.forward = direction.normalized;
        p.direction = direction.normalized;
        p.speed = template.speed;
        p.lifetime = 0f;
        p.maxLifetime = template.slashLength / template.speed; // 根据长度和速度计算存活时间
        p.skillType = skillType;
        p.playerProp = playerProp;
        p.isActive = true;
        p.prevPosition = spawnPosition;
        p.gameObject.SetActive(true);

        m_ActiveSlash.Add(p);
    }

    // ===== 更新逻辑 =====

    private void UpdateFlyingProjectiles(float deltaTime)
    {
        for (int i = m_ActiveFlying.Count - 1; i >= 0; i--)
        {
            var p = m_ActiveFlying[i];
            p.lifetime += deltaTime;

            // 超时回收
            if (p.lifetime >= p.maxLifetime)
            {
                DeactivateFlying(p, i);
                continue;
            }

            // 移动
            Vector3 currPos = p.transform.position;
            Vector3 moveDir = p.direction;
            float moveDist = p.speed * deltaTime;
            Vector3 nextPos = currPos + moveDir * moveDist;

            p.transform.position = nextPos;

            // 碰撞检测：SphereCast
            if (Physics.SphereCast(p.prevPosition, p.radius, moveDir, out RaycastHit hit, moveDist, p.template.hitLayer, QueryTriggerInteraction.Ignore))
            {
                // 命中敌人
                OnProjectileHit?.Invoke(hit.collider.gameObject, p.skillType, p.playerProp, hit.point);
                OnSkillHit?.Invoke(hit.collider.gameObject, p.skillType, p.playerProp);

                // 播放命中特效
                if (p.template.hitVFX != null)
                    Instantiate(p.template.hitVFX, hit.point, Quaternion.identity);

                // 播放命中音效
                if (p.template.hitSFX != null)
                    AudioSource.PlayClipAtPoint(p.template.hitSFX, hit.point);

                DeactivateFlying(p, i);
            }

            p.prevPosition = currPos;
        }
    }

    private void UpdateGroundAOEProjectiles(float deltaTime)
    {
        for (int i = m_ActiveGroundAoe.Count - 1; i >= 0; i--)
        {
            var p = m_ActiveGroundAoe[i];
            p.elapsed += deltaTime;

            // 超时销毁
            if (p.elapsed >= p.duration)
            {
                // 销毁指示器
                if (p.indicatorObject != null)
                    Destroy(p.indicatorObject);

                DeactivateGroundAoe(p, i);
                continue;
            }

            // Tick 检测：范围内所有敌人受伤
            Collider[] hits = Physics.OverlapSphere(p.transform.position, p.template.effectRadius, p.template.hitLayer, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                // 每 tick 对每个敌人通知一次伤害（实际结算由 Damage_OnEnemy 控制是否重复受伤）
                OnProjectileHit?.Invoke(hit.gameObject, p.skillType, p.playerProp, hit.ClosestPoint(p.transform.position));
                OnSkillHit?.Invoke(hit.gameObject, p.skillType, p.playerProp);
            }
        }
    }

    private void UpdateSlashProjectiles(float deltaTime)
    {
        for (int i = m_ActiveSlash.Count - 1; i >= 0; i--)
        {
            var p = m_ActiveSlash[i];
            p.lifetime += deltaTime;

            // 超时回收
            if (p.lifetime >= p.maxLifetime)
            {
                DeactivateSlash(p, i);
                continue;
            }

            // 移动
            Vector3 currPos = p.transform.position;
            Vector3 moveDir = p.direction;
            float moveDist = p.speed * deltaTime;
            Vector3 nextPos = currPos + moveDir * moveDist;

            p.transform.position = nextPos;

            // 碰撞检测：BoxCast（剑气较长，用 Box）
            Vector3 center = (currPos + nextPos) * 0.5f;
            Vector3 halfExtents = new Vector3(p.template.slashWidth * 0.5f, 1f, p.template.slashLength * 0.5f);
            Quaternion orientation = Quaternion.LookRotation(moveDir);

            if (Physics.BoxCast(center, halfExtents, moveDir, out RaycastHit hit, orientation, moveDist, p.template.hitLayer, QueryTriggerInteraction.Ignore))
            {
                // 命中敌人
                OnProjectileHit?.Invoke(hit.collider.gameObject, p.skillType, p.playerProp, hit.point);
                OnSkillHit?.Invoke(hit.collider.gameObject, p.skillType, p.playerProp);

                // 播放命中特效
                if (p.template.hitVFX != null)
                    Instantiate(p.template.hitVFX, hit.point, Quaternion.identity);

                // 播放命中音效
                if (p.template.hitSFX != null)
                    AudioSource.PlayClipAtPoint(p.template.hitSFX, hit.point);
            }

            p.prevPosition = currPos;
        }
    }

    // ===== 对象池操作 =====

    private FlyingProjectile GetFromFlyingPool(FlyingProjectileTemplate template)
    {
        if (m_FlyingPool.TryGetValue(template, out var pool) && pool.Count > 0)
            return pool.Dequeue();

        // 池空，新建
        GameObject go = Instantiate(template.prefab);
        return new FlyingProjectile
        {
            gameObject = go,
            transform = go.transform,
            template = template,
            isActive = false
        };
    }

    private void DeactivateFlying(FlyingProjectile p, int listIndex)
    {
        p.isActive = false;
        p.gameObject.SetActive(false);
        m_ActiveFlying.RemoveAt(listIndex);

        if (m_FlyingPool.TryGetValue(p.template, out var pool))
            pool.Enqueue(p);
    }

    private GroundAOEProjectile GetFromGroundAoePool(GroundAOEProjectileTemplate template)
    {
        if (m_GroundAoePool.TryGetValue(template, out var pool) && pool.Count > 0)
            return pool.Dequeue();

        GameObject go = Instantiate(template.prefab);
        return new GroundAOEProjectile
        {
            gameObject = go,
            transform = go.transform,
            template = template,
            isActive = false
        };
    }

    private void DeactivateGroundAoe(GroundAOEProjectile p, int listIndex)
    {
        p.isActive = false;
        p.gameObject.SetActive(false);
        m_ActiveGroundAoe.RemoveAt(listIndex);

        if (m_GroundAoePool.TryGetValue(p.template, out var pool))
            pool.Enqueue(p);
    }

    private SlashProjectile GetFromSlashPool(SlashProjectileTemplate template)
    {
        if (m_SlashPool.TryGetValue(template, out var pool) && pool.Count > 0)
            return pool.Dequeue();

        GameObject go = Instantiate(template.prefab);
        return new SlashProjectile
        {
            gameObject = go,
            transform = go.transform,
            template = template,
            isActive = false
        };
    }

    private void DeactivateSlash(SlashProjectile p, int listIndex)
    {
        p.isActive = false;
        p.gameObject.SetActive(false);
        m_ActiveSlash.RemoveAt(listIndex);

        if (m_SlashPool.TryGetValue(p.template, out var pool))
            pool.Enqueue(p);
    }

    private bool CanSpawn()
    {
        int total = m_ActiveFlying.Count + m_ActiveGroundAoe.Count + m_ActiveSlash.Count;
        if (total >= MAX_ACTIVE_PROJECTILES)
        {
            Debug.LogWarning($"[Global_ProjectileManager] Max projectiles reached ({MAX_ACTIVE_PROJECTILES}). Rejecting spawn.");
            return false;
        }
        return true;
    }

    // ===== Gizmos =====

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // 绘制活跃的飞行弹射物
        Gizmos.color = Color.cyan;
        foreach (var p in m_ActiveFlying)
        {
            if (p.isActive && p.gameObject != null)
                Gizmos.DrawWireSphere(p.transform.position, p.radius);
        }

        // 绘制活跃的地面AOE
        Gizmos.color = Color.magenta;
        foreach (var p in m_ActiveGroundAoe)
        {
            if (p.isActive && p.gameObject != null)
                Gizmos.DrawWireSphere(p.transform.position, p.template.effectRadius);
        }

        // 绘制活跃的剑气
        Gizmos.color = Color.yellow;
        foreach (var p in m_ActiveSlash)
        {
            if (p.isActive && p.gameObject != null)
            {
                Gizmos.DrawRay(p.transform.position, p.direction * p.template.slashLength);
            }
        }
    }
}

// ===== 弹射物实例数据结构 =====

/// <summary>
/// 飞行弹射物实例
/// </summary>
public class FlyingProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public FlyingProjectileTemplate template;
    public Vector3 direction;
    public float speed;
    public float radius;
    public float lifetime;
    public float maxLifetime;
    public Vector3 prevPosition;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
}

/// <summary>
/// 地面AOE弹射物实例
/// </summary>
public class GroundAOEProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public GroundAOEProjectileTemplate template;
    public float elapsed;
    public float duration;
    public float tickInterval;
    public float damagePerTick;
    public GameObject indicatorObject;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
}

/// <summary>
/// 剑气弹射物实例
/// </summary>
public class SlashProjectile
{
    public GameObject gameObject;
    public Transform transform;
    public SlashProjectileTemplate template;
    public Vector3 direction;
    public float speed;
    public float lifetime;
    public float maxLifetime;
    public Vector3 prevPosition;
    public DamageTemplate.SkillType skillType;
    public PlayerPropertyTemplate playerProp;
    public bool isActive;
}
