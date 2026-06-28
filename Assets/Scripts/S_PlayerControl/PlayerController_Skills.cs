using UnityEngine;

public class PlayerController_Skills : MonoBehaviour
{
    // ===================== 状态枚举 =====================
    public enum SelectionState
    {
        None,      // 未进入任何选定状态（默认）
        LeftHold,  // 按住鼠标左键
        RightHold, // 按住鼠标右键
    }

    // ===================== 按键配置 =====================
    [Header("按键设置")]
    public KeyCode LeftMouseKey   = KeyCode.Mouse0;
    public KeyCode RightMouseKey  = KeyCode.Mouse1;
    public KeyCode SkillQ_KeyCode = KeyCode.Q;
    public KeyCode SkillE_KeyCode = KeyCode.E;
    public KeyCode SkillR_KeyCode = KeyCode.R;

    // ===================== 技能冷却配置 =====================
    [Header("技能冷却配置（秒）")]
    [Tooltip("LeftHold+Q 冷却时间")]
    public float LeftHold_Q_Cooldown = 0f;
    [Tooltip("LeftHold+E 冷却时间")]
    public float LeftHold_E_Cooldown = 0f;
    [Tooltip("LeftHold+R 冷却时间")]
    public float LeftHold_R_Cooldown = 0f;
    [Tooltip("RightHold+Q 冷却时间")]
    public float RightHold_Q_Cooldown = 0f;
    [Tooltip("RightHold+E 冷却时间")]
    public float RightHold_E_Cooldown = 0f;
    [Tooltip("RightHold+R 冷却时间")]
    public float RightHold_R_Cooldown = 0f;

    // ===================== 属性引用 =====================
    [Header("属性引用")]
    [Tooltip("将 PlayerProperty 资产拖入此处，用于获取玩家属性并通知伤害系统")]
    public PlayerPropertyTemplate PlayerPropertyAsset;

    // ===================== 内部变量 =====================
    SelectionState m_CurrentState = SelectionState.None;

    /// <summary>当前选定状态（供外部访问）</summary>
    public SelectionState CurrentState => m_CurrentState;

    // 鼠标按住状态（独立于动画锁，外部如相机系统可读取）
    bool m_IsMouseHeld = false;

    /// <summary>鼠标是否按住（不受动画锁影响，供相机等系统使用）</summary>
    public bool IsMouseHeld => m_IsMouseHeld;

    // 6个槽位冷却计时器：LeftHold=0~2, RightHold=3~5
    float[] m_SkillTimers = new float[6];

    // 动画播放锁定：动画播放中禁止触发新技能
    bool m_IsAnimationPlaying = false;

    // ===================== 动画组件 =====================
    Animator m_Animator;

    // ===================== 调试信息 =====================
    string m_DebugMessage = "";
    float m_DebugTimer = 0f;
    const float DEBUG_SHOW_DURATION = 2f;

    // ===================== 事件 =====================
    /// <summary>进入/退出选定状态时触发，参数为新的状态</summary>
    public System.Action<SelectionState> OnSelectionStateChanged;

    /// <summary>
    /// 在选定状态下触发技能时触发
    /// state: 当前选定状态（LeftHold/RightHold）
    /// skillIndex: 0=Q, 1=E, 2=R
    /// </summary>
    public System.Action<SelectionState, int> OnSkillTriggered;

    // ===================== Unity 生命周期 =====================
    void Start()
    {
        // 初始化冷却计时器
        for (int i = 0; i < m_SkillTimers.Length; i++)
            m_SkillTimers[i] = 0f;

        // 获取Animator组件
        m_Animator = GetComponent<Animator>();
        if (m_Animator == null)
            Debug.LogWarning("PlayerController_Skills: 未找到 Animator 组件！");
    }

    void Update()
    {
        UpdateCooldowns();
        HandleMouseButtons();
        HandleSkillKeys();
        UpdateDebugTimer();
    }

    void OnGUI()
    {
        DrawDebugPanel();
    }

    // ===================== 输入处理 =====================
    void HandleMouseButtons()
    {
        // 始终追踪鼠标是否按住（不受动画锁影响，供相机等系统使用）
        bool leftDown = Input.GetKey(LeftMouseKey);
        bool rightDown = Input.GetKey(RightMouseKey);
        m_IsMouseHeld = leftDown || rightDown;

        // 动画播放中不允许切换到 LeftHold/RightHold，但允许切换回 None
        if (m_IsAnimationPlaying)
        {
            // 检测鼠标按键释放 -> 切回 None
            if (m_CurrentState == SelectionState.LeftHold && Input.GetKeyUp(LeftMouseKey))
                TransitionToNone();
            else if (m_CurrentState == SelectionState.RightHold && Input.GetKeyUp(RightMouseKey))
                TransitionToNone();
            return;
        }

        SelectionState newState = m_CurrentState;

        // 检测鼠标按键按下
        if (Input.GetKeyDown(LeftMouseKey))
            newState = SelectionState.LeftHold;
        else if (Input.GetKeyDown(RightMouseKey))
            newState = SelectionState.RightHold;

        // 检测鼠标按键释放
        if (m_CurrentState == SelectionState.LeftHold && Input.GetKeyUp(LeftMouseKey))
            newState = SelectionState.None;
        else if (m_CurrentState == SelectionState.RightHold && Input.GetKeyUp(RightMouseKey))
            newState = SelectionState.None;

        // 状态切换检测
        if (newState != m_CurrentState)
        {
            m_CurrentState = newState;
            OnSelectionStateChanged?.Invoke(m_CurrentState);

            // 调试：打印状态切换
            string from = GetStateName(newState);
            SetDebugMessage($"状态切换 -> {from}");
        }
    }

    void TransitionToNone()
    {
        if (m_CurrentState == SelectionState.None)
            return;
        m_CurrentState = SelectionState.None;
        OnSelectionStateChanged?.Invoke(m_CurrentState);
        SetDebugMessage("状态切换 -> None");
    }

    void HandleSkillKeys()
    {
        // 仅在选定状态下检测Q/E/R，且动画播放中禁止触发
        if (m_CurrentState == SelectionState.None)
            return;
        if (m_IsAnimationPlaying)
            return;

        if (Input.GetKeyDown(SkillQ_KeyCode))
            TryTriggerSkill(0); // Q -> index 0
        else if (Input.GetKeyDown(SkillE_KeyCode))
            TryTriggerSkill(1); // E -> index 1
        else if (Input.GetKeyDown(SkillR_KeyCode))
            TryTriggerSkill(2); // R -> index 2
    }

    // ===================== 动画播放锁定 =====================
    /// <summary>
    /// 由 Animation Event 在动画末尾调用，解除操作锁定
    /// </summary>
    public void OnSkillAnimationEnd()
    {
        m_IsAnimationPlaying = false;
    }

    // ===================== 技能触发 =====================
    void TryTriggerSkill(int skillIndex)
    {
        int slotIndex = GetSlotIndex(m_CurrentState, skillIndex);

        // CD检查：冷却中则忽略
        if (m_SkillTimers[slotIndex] > 0f)
        {
            float remaining = m_SkillTimers[slotIndex];
            SetDebugMessage($"[{GetStateName(m_CurrentState)}+{GetSkillKeyName(skillIndex)}] 冷却中 {remaining:F1}s");
            return;
        }

        // 播放对应动画
        string stateName = GetAnimationStateName(m_CurrentState, skillIndex);
        if (m_Animator != null)
            m_Animator.Play(stateName);

        // 锁定：动画播放中禁止触发新操作
        m_IsAnimationPlaying = true;

        // 触发事件
        OnSkillTriggered?.Invoke(m_CurrentState, skillIndex);

        // 调试：打印技能触发
        float cooldown = GetCooldown(m_CurrentState, skillIndex);
        string cdInfo = cooldown > 0f ? $" | CD: {cooldown:F1}s" : " | 无CD";
        SetDebugMessage($"★★★ 触发技能: [{GetStateName(m_CurrentState)}+{GetSkillKeyName(skillIndex)}] 动画: {stateName}{cdInfo}");

        // 若配置了CD，则启动冷却
        if (cooldown > 0f)
            m_SkillTimers[slotIndex] = cooldown;
    }

    // ===================== 冷却更新 =====================
    void UpdateCooldowns()
    {
        for (int i = 0; i < m_SkillTimers.Length; i++)
        {
            if (m_SkillTimers[i] > 0f)
                m_SkillTimers[i] = Mathf.Max(0f, m_SkillTimers[i] - Time.deltaTime);
        }
    }

    // ===================== 辅助方法 =====================
    /// <summary>
    /// 根据选定状态和技能索引计算槽位索引
    /// LeftHold=0, RightHold=1; Q=0, E=1, R=2
    /// </summary>
    int GetSlotIndex(SelectionState state, int skillIndex)
    {
        int stateOffset = (state == SelectionState.LeftHold) ? 0 : 3;
        return stateOffset + skillIndex;
    }

    float GetCooldown(SelectionState state, int skillIndex)
    {
        return skillIndex switch
        {
            0 => (state == SelectionState.LeftHold) ? LeftHold_Q_Cooldown : RightHold_Q_Cooldown,
            1 => (state == SelectionState.LeftHold) ? LeftHold_E_Cooldown : RightHold_E_Cooldown,
            2 => (state == SelectionState.LeftHold) ? LeftHold_R_Cooldown : RightHold_R_Cooldown,
            _ => 0f,
        };
    }

    // ===================== 公开查询接口 =====================
    /// <summary>获取指定槽位当前剩余冷却时间（秒）</summary>
    public float GetRemainingCooldown(SelectionState state, int skillIndex)
    {
        return m_SkillTimers[GetSlotIndex(state, skillIndex)];
    }

    /// <summary>获取指定槽位是否在冷却中</summary>
    public bool IsOnCooldown(SelectionState state, int skillIndex)
    {
        return m_SkillTimers[GetSlotIndex(state, skillIndex)] > 0f;
    }

    // ===================== 调试相关 =====================
    void SetDebugMessage(string msg)
    {
        m_DebugMessage = msg;
        m_DebugTimer = DEBUG_SHOW_DURATION;
    }

    void UpdateDebugTimer()
    {
        if (m_DebugTimer > 0f)
            m_DebugTimer -= Time.deltaTime;
    }

    void DrawDebugPanel()
    {
        if (m_DebugTimer <= 0f)
            return;

        float x = Screen.width - 370f;
        float y = 10f;
        float boxW = 360f;
        float boxH = 200f;

        // 背景半透明框
        GUI.Box(new Rect(x, y, boxW, boxH), "");

        // 标题
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 14;
        GUI.Label(new Rect(x + 10, y + 4, 340, 20), "=== Skill Debug ===", titleStyle);

        // 当前状态
        GUIStyle stateStyle = new GUIStyle(GUI.skin.label);
        stateStyle.fontSize = 13;
        string stateStr = m_CurrentState switch
        {
            SelectionState.LeftHold  => "[LeftHold] 按住左键",
            SelectionState.RightHold => "[RightHold] 按住右键",
            _                        => "[None] 未选定",
        };
        stateStyle.normal.textColor = m_CurrentState == SelectionState.None ? Color.gray :
                                     (m_CurrentState == SelectionState.LeftHold ? Color.cyan : Color.yellow);
        GUI.Label(new Rect(x + 10, y + 28, 340, 20), "状态: " + stateStr, stateStyle);

        // 动画锁定状态
        GUIStyle lockStyle = new GUIStyle(GUI.skin.label);
        lockStyle.fontSize = 11;
        lockStyle.normal.textColor = m_IsAnimationPlaying ? Color.red : Color.green;
        GUI.Label(new Rect(x + 10, y + 46, 340, 16),
            "动画: " + (m_IsAnimationPlaying ? "锁定中" : "可操作"), lockStyle);

        // 调试信息
        if (!string.IsNullOrEmpty(m_DebugMessage))
        {
            GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
            msgStyle.fontSize = 12;
            msgStyle.normal.textColor = new Color(0.3f, 1f, 0.3f);
            GUI.Label(new Rect(x + 10, y + 60, 340, 60), m_DebugMessage, msgStyle);
        }

        // 冷却状态
        GUIStyle cdStyle = new GUIStyle(GUI.skin.label);
        cdStyle.fontSize = 11;
        cdStyle.normal.textColor = Color.white;

        float cdY = y + 120f;
        GUI.Label(new Rect(x + 10, cdY, 160, 16), "--- LeftHold 冷却 ---", cdStyle);
        DrawCooldownRow(0, "Q", LeftHold_Q_Cooldown, x + 10,  cdY + 18, cdStyle);
        DrawCooldownRow(1, "E", LeftHold_E_Cooldown, x + 10,  cdY + 36, cdStyle);
        DrawCooldownRow(2, "R", LeftHold_R_Cooldown, x + 10,  cdY + 54, cdStyle);

        GUI.Label(new Rect(x + 180, cdY, 160, 16), "--- RightHold 冷却 ---", cdStyle);
        DrawCooldownRow(3, "Q", RightHold_Q_Cooldown, x + 180, cdY + 18, cdStyle);
        DrawCooldownRow(4, "E", RightHold_E_Cooldown, x + 180, cdY + 36, cdStyle);
        DrawCooldownRow(5, "R", RightHold_R_Cooldown, x + 180, cdY + 54, cdStyle);
    }

    void DrawCooldownRow(int slotIndex, string keyLabel, float maxCd, float x, float y, GUIStyle style)
    {
        float remaining = m_SkillTimers[slotIndex];
        string text;
        Color color;

        if (maxCd <= 0f)
        {
            text = $"{keyLabel}: 就绪";
            color = Color.green;
        }
        else if (remaining <= 0f)
        {
            text = $"{keyLabel}: 就绪";
            color = Color.green;
        }
        else
        {
            float pct = remaining / maxCd;
            text = $"{keyLabel}: {remaining:F1}s";
            color = Color.Lerp(Color.yellow, Color.red, pct);
        }

        style.normal.textColor = color;
        GUI.Label(new Rect(x, y, 80, 16), text, style);
    }

    string GetSkillKeyName(int skillIndex)
    {
        return skillIndex switch
        {
            0 => "Q",
            1 => "E",
            2 => "R",
            _ => "?",
        };
    }

    string GetStateName(SelectionState state)
    {
        return state switch
        {
            SelectionState.LeftHold  => "LeftHold",
            SelectionState.RightHold => "RightHold",
            _                        => "None",
        };
    }

    string GetAnimationStateName(SelectionState state, int skillIndex)
    {
        string prefix = (state == SelectionState.LeftHold) ? "Sword" : "Spell";
        string key = GetSkillKeyName(skillIndex);
        return $"{prefix}_{key}";
    }
}
