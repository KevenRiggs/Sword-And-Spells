using UnityEngine;

/// <summary>
/// 玩家技能逻辑执行器
/// 订阅 PlayerController_Skills.OnSkillTriggered，执行技能特有逻辑（检测/特效/音效等）
/// 通过 Animation Event 驱动命中检测，检测到命中后触发 OnSkillHit 通知下游
/// </summary>
public class PlayerSkillFunctions : MonoBehaviour
{
    // ===== Inspector 字段 =====

    [Header("SwordQ 配置")]
    [Tooltip("扇形检测半径（米）")]
    public float SwordQ_Radius = 5f;
    [Tooltip("扇形角度（度）")]
    public float SwordQ_Angle = 60f;

    [Header("SpellQ 配置")]
    [Tooltip("法球弹射物模板（.asset）")]
    public FlyingProjectileTemplate SpellQ_Template;
    [Tooltip("多球发射间隔（秒）")]
    public float SpellQ_SpawnInterval = 0.1f;

    [Header("SwordR 配置")]
    [Tooltip("剑气弹射物模板（.asset）")]
    public SlashProjectileTemplate SwordR_Template;
    [Tooltip("多波发射间隔（秒）")]
    public float SwordR_SpawnInterval = 0.15f;

    [Header("引用")]
    [Tooltip("Enemy 层遮罩（拖入 Enemy 层）")]
    public LayerMask EnemyLayer;

    // ===== 私有字段 =====

    PlayerController_Skills m_PlayerCtrl;
    Transform m_PlayerTransform;
    LineRenderer m_FanLineRenderer;
    Global_ProjectileManager m_ProjectileManager;

    // ===== 调试信息 =====
    string m_DebugMessage = "";
    float m_DebugTimer = 0f;
    const float DEBUG_SHOW_DURATION = 3f;

    // 伤害结算调试信息
    string m_DamageDebugLine1 = "";
    string m_DamageDebugLine2 = "";
    float m_DamageDebugTimer = 0f;
    const float DAMAGE_DEBUG_DURATION = 3f;

    // ===== 事件 =====

    /// <summary>
    /// 技能命中事件
    /// 参数：命中的敌人 / 技能类型 / 玩家属性（用于伤害计算）
    /// </summary>
    public System.Action<GameObject, DamageTemplate.SkillType, PlayerPropertyTemplate> OnSkillHit;

    // ===== Unity 生命周期 =====

    void Start()
    {
        m_PlayerCtrl = GetComponent<PlayerController_Skills>();
        m_PlayerTransform = GetComponent<Transform>();
        m_ProjectileManager = FindAnyObjectByType<Global_ProjectileManager>();
    }

    void OnEnable()
    {
        if (m_PlayerCtrl != null)
        {
            m_PlayerCtrl.OnSkillTriggered += HandleSkillTriggered;
        }

        // 订阅伤害结算调试事件
        var damageOnEnemy = FindAnyObjectByType<Damage_OnEnemy>();
        if (damageOnEnemy != null)
        {
            damageOnEnemy.OnDamageDebug += HandleDamageDebug;
        }
    }

    void OnDisable()
    {
        if (m_PlayerCtrl != null)
        {
            m_PlayerCtrl.OnSkillTriggered -= HandleSkillTriggered;
        }

        var damageOnEnemy = FindAnyObjectByType<Damage_OnEnemy>();
        if (damageOnEnemy != null)
        {
            damageOnEnemy.OnDamageDebug -= HandleDamageDebug;
        }
    }

    // ===== 事件处理 =====

    /// <summary>
    /// 响应技能触发事件（仅记录/预处理，检测由 Animation Event 驱动）
    /// </summary>
    private void HandleSkillTriggered(PlayerController_Skills.SelectionState state, int skillIndex)
    {
        string skillName = (state, skillIndex) switch
        {
            (PlayerController_Skills.SelectionState.LeftHold, 0) => "SwordQ",
            (PlayerController_Skills.SelectionState.LeftHold, 1) => "SwordE",
            (PlayerController_Skills.SelectionState.LeftHold, 2) => "SwordR",
            (PlayerController_Skills.SelectionState.RightHold, 0) => "SpellQ",
            (PlayerController_Skills.SelectionState.RightHold, 1) => "SpellE",
            (PlayerController_Skills.SelectionState.RightHold, 2) => "SpellR",
            _ => "Unknown"
        };
        SetDebugMessage($"[触发] {skillName} 动画开始播放");
    }

    // ===== Animation Event 入口 =====

    /// <summary>
    /// 由 Sword_Q 动画的 Animation Event 在命中帧调用
    /// </summary>
    public void OnSwordQHit()
    {
        Debug.Log("[SwordQ] OnSwordQHit 被调用");
        if (m_PlayerTransform == null) return;

        DrawFanInGameView(m_PlayerTransform.position, m_PlayerTransform.forward, SwordQ_Radius, SwordQ_Angle);

        GameObject target = DetectNearestEnemyInFan(
            m_PlayerTransform.position,
            m_PlayerTransform.forward,
            SwordQ_Radius,
            SwordQ_Angle
        );

        if (target != null)
        {
            float dist = Vector3.Distance(m_PlayerTransform.position, target.transform.position);
            SetDebugMessage($"[命中] SwordQ 命中目标: {target.name}，距离: {dist:F2}m");
            Debug.Log($"[SwordQ 命中] 目标: {target.name}，距离: {dist:F2}m");

            PlayerPropertyTemplate playerProp = m_PlayerCtrl != null ? m_PlayerCtrl.PlayerPropertyAsset : null;
            OnSkillHit?.Invoke(target, DamageTemplate.SkillType.SwordSingle, playerProp);
        }
        else
        {
            SetDebugMessage("[未命中] SwordQ 扇形范围内无敌人");
            Debug.Log("[SwordQ 未命中] 扇形范围内无敌人");
        }
    }

    /// <summary>
    /// 由 Spell_Q 动画的 Animation Event 在命中帧调用
    /// </summary>
    public void OnSpellQHit()
    {
        Debug.Log("[SpellQ] OnSpellQHit 被调用");

        if (m_PlayerCtrl == null || m_ProjectileManager == null)
        {
            Debug.LogWarning("[SpellQ] PlayerCtrl 或 ProjectileManager 未找到");
            return;
        }

        if (SpellQ_Template == null)
        {
            Debug.LogWarning("[SpellQ] SpellQ_Template 未配置");
            return;
        }

        // 读取配置的法球数量
        int spellCount = 1;
        if (m_PlayerCtrl.PlayerPropertyAsset != null)
        {
            spellCount = Mathf.RoundToInt(m_PlayerCtrl.PlayerPropertyAsset.SpellSingle_Spell_Count);
        }
        spellCount = Mathf.Max(1, spellCount);

        SetDebugMessage($"[SpellQ] 开始发射 {spellCount} 个法球");
        Debug.Log($"[SpellQ] 开始发射 {spellCount} 个法球，间隔 {SpellQ_SpawnInterval}s");

        // 启动协程依次发射
        StartCoroutine(SpellQ_LaunchLoop(spellCount));
    }

    /// <summary>
    /// SpellQ 多球发射协程
    /// </summary>
    private System.Collections.IEnumerator SpellQ_LaunchLoop(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 计算发射方向（玩家朝向）
            Vector3 direction = m_PlayerTransform.forward;

            // 获取玩家属性
            PlayerPropertyTemplate playerProp = m_PlayerCtrl != null ? m_PlayerCtrl.PlayerPropertyAsset : null;

            // 生成法球（应用发射偏移量，从法杖位置发出）
            Vector3 spawnPos = m_PlayerTransform.position + SpellQ_Template.spawnOffset;
            m_ProjectileManager.SpawnFlyingProjectile(
                SpellQ_Template,
                spawnPos,
                direction,
                DamageTemplate.SkillType.SpellSingle,
                playerProp
            );

            Debug.Log($"[SpellQ] 第 {i + 1}/{count} 个法球已发射");

            // 等待间隔（除最后一个外）
            if (i < count - 1)
            {
                yield return new WaitForSeconds(SpellQ_SpawnInterval);
            }
        }

        SetDebugMessage($"[SpellQ] {count} 个法球发射完毕");
        Debug.Log($"[SpellQ] {count} 个法球发射完毕");
    }

    // ===== SwordR 发射逻辑 =====

    /// <summary>
    /// 由 Sword_R 动画的 Animation Event 在释放帧调用
    /// </summary>
    public void OnSwordRHit()
    {
        Debug.Log("[SwordR] OnSwordRHit 被调用");

        if (m_PlayerCtrl == null || m_ProjectileManager == null)
        {
            Debug.LogWarning("[SwordR] PlayerCtrl 或 ProjectileManager 未找到");
            return;
        }

        if (SwordR_Template == null)
        {
            Debug.LogWarning("[SwordR] SwordR_Template 未配置");
            return;
        }

        // 读取配置的剑气波数
        int slashCount = 1;
        if (m_PlayerCtrl.PlayerPropertyAsset != null)
        {
            slashCount = Mathf.RoundToInt(m_PlayerCtrl.PlayerPropertyAsset.SwordAOE_Count);
        }
        slashCount = Mathf.Max(1, slashCount);

        SetDebugMessage($"[SwordR] 开始释放 {slashCount} 波剑气");
        Debug.Log($"[SwordR] 开始释放 {slashCount} 波剑气，间隔 {SwordR_SpawnInterval}s");

        // 启动协程依次发射
        StartCoroutine(SwordR_LaunchLoop(slashCount));
    }

    /// <summary>
    /// SwordR 多波发射协程
    /// </summary>
    private System.Collections.IEnumerator SwordR_LaunchLoop(int count)
    {
        for (int i = 0; i < count; i++)
        {
            // 计算发射方向（玩家朝向）
            Vector3 direction = m_PlayerTransform.forward;

            // 获取玩家属性
            PlayerPropertyTemplate playerProp = m_PlayerCtrl != null ? m_PlayerCtrl.PlayerPropertyAsset : null;

            // 生成剑气（应用发射偏移量，从玩家位置发出）
            Vector3 spawnPos = m_PlayerTransform.position + SwordR_Template.spawnOffset;
            m_ProjectileManager.SpawnSlashProjectile(
                SwordR_Template,
                spawnPos,
                direction,
                DamageTemplate.SkillType.SwordAOE,
                playerProp
            );

            Debug.Log($"[SwordR] 第 {i + 1}/{count} 波剑气已释放");

            // 等待间隔（除最后一个外）
            if (i < count - 1)
            {
                yield return new WaitForSeconds(SwordR_SpawnInterval);
            }
        }

        SetDebugMessage($"[SwordR] {count} 波剑气释放完毕");
        Debug.Log($"[SwordR] {count} 波剑气释放完毕");
    }

    // ===== 扇形检测 =====

    /// <summary>
    /// 在扇形区域内查找距离最近的敌人
    /// </summary>
    /// <param name="origin">玩家位置</param>
    /// <param name="forward">玩家朝向</param>
    /// <param name="radius">扇形半径</param>
    /// <param name="angle">扇形总角度（度）</param>
    /// <returns>最近的敌人，若无则返回 null</returns>
    private GameObject DetectNearestEnemyInFan(Vector3 origin, Vector3 forward, float radius, float angle)
    {
        // 1. 收集球形范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapSphere(origin, radius, EnemyLayer);

        GameObject nearest = null;
        float nearestSqrDist = float.MaxValue;
        float halfAngle = angle * 0.5f;

        foreach (Collider col in colliders)
        {
            // 2. 方向向量
            Vector3 dirToTarget = (col.transform.position - origin).normalized;

            // 3. 夹角判断（点积）
            float cosAngle = Vector3.Dot(forward, dirToTarget);
            float angleDeg = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

            if (angleDeg > halfAngle)
                continue;

            // 4. 取最近
            float sqrDist = (col.transform.position - origin).sqrMagnitude;
            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = col.gameObject;
            }
        }

        return nearest;
    }

    // ===== 调试辅助 =====

    void SetDebugMessage(string msg)
    {
        m_DebugMessage = msg;
        m_DebugTimer = DEBUG_SHOW_DURATION;
    }

    void HandleDamageDebug(string enemyName, string skillName, float damage, bool isCrit, float remainingHp)
    {
        string critTag = isCrit ? "【暴击】" : "";
        m_DamageDebugLine1 = $"敌人: {enemyName}";
        m_DamageDebugLine2 = $"伤害: {damage:F1}  剩余血量: {remainingHp:F0} {critTag}";
        m_DamageDebugTimer = DAMAGE_DEBUG_DURATION;
    }

    void OnGUI()
    {
        float yBase = 10f;
        float rowH = 18f;
        float boxW = 360f;
        float lineCount = 0;

        // 收集所有要显示的行
        System.Collections.Generic.List<string> allLines = new System.Collections.Generic.List<string>();

        if (m_DebugTimer > 0f && !string.IsNullOrEmpty(m_DebugMessage))
        {
            string[] lines = m_DebugMessage.Split('\n');
            foreach (string l in lines) if (!string.IsNullOrEmpty(l)) allLines.Add(l);
        }
        if (m_DamageDebugTimer > 0f)
        {
            if (!string.IsNullOrEmpty(m_DamageDebugLine1)) allLines.Add(m_DamageDebugLine1);
            if (!string.IsNullOrEmpty(m_DamageDebugLine2)) allLines.Add(m_DamageDebugLine2);
        }

        lineCount = allLines.Count;
        if (lineCount == 0)
            return;

        float boxH = 24f + lineCount * rowH + 8f;
        GUI.Box(new Rect(10, yBase, boxW, boxH), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 13;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(18, yBase + 4f, boxW - 16f, 18f), "=== Skill Debug ===", titleStyle);

        for (int i = 0; i < allLines.Count; i++)
        {
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
            msgStyle.fontSize = 12;
            // 伤害行高亮
            msgStyle.normal.textColor = allLines[i].Contains("伤害:") ? Color.yellow : Color.cyan;
            GUI.Label(new Rect(18, yBase + 24f + i * rowH, boxW - 16f, rowH), allLines[i], msgStyle);
        }
    }

    // ===== 统一 Update =====

    void Update()
    {
        if (m_DebugTimer > 0f)
            m_DebugTimer -= Time.deltaTime;

        if (m_DamageDebugTimer > 0f)
            m_DamageDebugTimer -= Time.deltaTime;

        if (m_FanDrawTimer > 0f)
            m_FanDrawTimer -= Time.deltaTime;
    }

    // ===== 扇形 Gizmos 绘制 =====

    void OnDrawGizmos()
    {
        Transform trans = m_PlayerTransform != null ? m_PlayerTransform : GetComponent<Transform>();
        DrawFanGizmos(trans.position, trans.forward, SwordQ_Radius, SwordQ_Angle, false);
    }

    void OnDrawGizmosSelected()
    {
        Transform trans = m_PlayerTransform != null ? m_PlayerTransform : GetComponent<Transform>();
        DrawFanGizmos(trans.position, trans.forward, SwordQ_Radius, SwordQ_Angle, true);
    }

    void DrawFanGizmos(Vector3 origin, Vector3 forward, float radius, float angle, bool bright)
    {
        float halfAngle = angle * 0.5f;

        Quaternion leftRot = Quaternion.Euler(0f, -halfAngle, 0f);
        Quaternion rightRot = Quaternion.Euler(0f, halfAngle, 0f);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        // 边框线（青/绿）
        Gizmos.color = bright ? Color.green : new Color(0f, 1f, 1f, 0.8f);
        Gizmos.DrawLine(origin, origin + leftDir * radius);
        Gizmos.DrawLine(origin, origin + rightDir * radius);

        // Forward 中线（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + forward * radius);

        // 扇形弧线
        Gizmos.color = bright ? new Color(0f, 1f, 0f, 0.8f) : new Color(0f, 1f, 1f, 0.3f);
        DrawFanArcGizmos(origin, forward, radius, halfAngle, 20);

        // 球形范围边界（淡淡黄）
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(origin, radius);
    }

    void DrawFanArcGizmos(Vector3 origin, Vector3 forward, float radius, float halfAngle, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;
            float a1 = Mathf.Lerp(-halfAngle, halfAngle, t1);
            float a2 = Mathf.Lerp(-halfAngle, halfAngle, t2);
            Vector3 p1 = origin + Quaternion.Euler(0f, a1, 0f) * forward * radius;
            Vector3 p2 = origin + Quaternion.Euler(0f, a2, 0f) * forward * radius;
            Gizmos.DrawLine(p1, p2);
        }
    }

    // ===== 游戏视图扇形绘制 =====

    Material m_FanMat;
    const float FAN_DRAW_DURATION = 0.5f;
    float m_FanDrawTimer = 0f;
    Vector3 m_FanOrigin;
    Vector3 m_FanForward;
    float m_FanRadius;
    float m_FanAngle;

    void DrawFanInGameView(Vector3 origin, Vector3 forward, float radius, float angle)
    {
        if (m_FanMat == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            m_FanMat = new Material(shader);
            m_FanMat.color = new Color(0f, 1f, 1f, 0.5f);
        }

        m_FanDrawTimer = FAN_DRAW_DURATION;
        m_FanOrigin = origin;
        m_FanForward = forward;
        m_FanRadius = radius;
        m_FanAngle = angle;
    }

    void OnPostRender()
    {
        if (m_FanDrawTimer <= 0f || m_FanMat == null)
            return;

        // 画球形检测范围（3D完整球体）
        DrawWireSphere(m_FanOrigin, m_FanRadius, 12, 8);
    }

    void DrawWireSphere(Vector3 center, float radius, int latLines, int lonLines)
    {
        GL.Begin(GL.LINES);
        m_FanMat.SetPass(0);

        // 经线（纵向）
        for (int i = 0; i < lonLines; i++)
        {
            float phi = (float)i / lonLines * Mathf.PI * 2f;
            for (int j = 0; j < latLines; j++)
            {
                float theta1 = (float)j / latLines * Mathf.PI;
                float theta2 = (float)(j + 1) / latLines * Mathf.PI;

                float x1 = radius * Mathf.Sin(theta1) * Mathf.Cos(phi);
                float y1 = radius * Mathf.Cos(theta1);
                float z1 = radius * Mathf.Sin(theta1) * Mathf.Sin(phi);

                float x2 = radius * Mathf.Sin(theta2) * Mathf.Cos(phi);
                float y2 = radius * Mathf.Cos(theta2);
                float z2 = radius * Mathf.Sin(theta2) * Mathf.Sin(phi);

                GL.Vertex(center + new Vector3(x1, y1, z1));
                GL.Vertex(center + new Vector3(x2, y2, z2));
            }
        }

        // 纬线（横向）
        for (int j = 1; j < latLines; j++)
        {
            float theta = (float)j / latLines * Mathf.PI;
            float y = radius * Mathf.Cos(theta);
            float r = radius * Mathf.Sin(theta);

            for (int i = 0; i < lonLines; i++)
            {
                float phi1 = (float)i / lonLines * Mathf.PI * 2f;
                float phi2 = (float)(i + 1) / lonLines * Mathf.PI * 2f;

                GL.Vertex(center + new Vector3(r * Mathf.Cos(phi1), y, r * Mathf.Sin(phi1)));
                GL.Vertex(center + new Vector3(r * Mathf.Cos(phi2), y, r * Mathf.Sin(phi2)));
            }
        }

        GL.End();
    }
}
