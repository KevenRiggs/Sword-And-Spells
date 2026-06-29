using UnityEngine;

/// <summary>
/// 第三人称射击镜头控制器
/// 相机以角色为球心进行球坐标旋转，角色朝向始终跟随相机水平朝向
/// </summary>
public class PlayerContoller_Camera : MonoBehaviour
{
    // ===================== 相机引用 =====================
    [Header("相机组件")]
    [Tooltip("MainCamera 引用")]
    public Camera CameraRef;

    // ===================== 球坐标参数 =====================
    [Header("相机距离")]
    [Tooltip("相机与角色的后方距离（固定方向）")]
    public float BackDistance = 3f;

    [Tooltip("瞄准时拉近的距离")]
    public float AimDistance = 1.5f;

    [Header("右肩偏移")]
    [Tooltip("相机相对于角色正后方的右侧偏移")]
    public float ShoulderOffsetRight = 0.8f;

    [Tooltip("瞄准时右侧偏移")]
    public float AimShoulderOffsetRight = 0.4f;

    [Header("相机高度")]
    [Tooltip("相机高度占角色身高的比例（0.5=腰部，0.6=肩部，0.9=头顶）")]
    public float CameraHeightRatio = 0.6f;

    [Header("碰撞检测")]
    [Tooltip("相机碰撞检测的层")]
    public LayerMask CollisionLayers = ~0;

    [Tooltip("相机碰撞时距离障碍物的最小距离")]
    public float CollisionMinDistance = 0.2f;

    [Header("垂直角度限制")]
    [Tooltip("垂直角度下限（低头）")]
    public float VerticalMin = -15f;

    [Tooltip("垂直角度上限（抬头）")]
    public float VerticalMax = 50f;

    // ===================== FOV 设置 =====================
    [Header("FOV 设置")]
    [Tooltip("正常视野角")]
    public float NormalFOV = 60f;

    [Tooltip("瞄准时视野角")]
    public float AimFOV = 35f;

    // ===================== 旋转速度 =====================
    [Header("旋转速度")]
    [Tooltip("鼠标旋转灵敏度")]
    public float RotateSpeed = 3f;

    // ===================== 平滑参数 =====================
    [Header("平滑参数")]
    [Tooltip("位置插值速度")]
    public float PositionSmooth = 15f;

    [Tooltip("FOV 插值速度")]
    public float FOVSmooth = 10f;

    [Tooltip("角色朝向插值速度")]
    public float CharacterRotateSmooth = 15f;

    // ===================== 状态枚举 =====================
    public enum CameraState
    {
        Normal,
        Aiming,
    }

    // ===================== 内部变量 =====================
    CameraState m_CurrentState = CameraState.Normal;

    // 球坐标角度
    float m_CamTheta = 0f;   // 水平角度（绕 Y 轴）[0, 360）
    float m_CamPhi = 0f;     // 垂直角度（绕 X 轴）[-90, 90]

    // 插值值
    float m_CurrentDistance;       // 当前距离
    float m_CurrentShoulderOffset; // 当前右肩偏移

    // 角色朝向
    float m_CharacterYaw = 0f;

    // PlayerController_Skills 引用
    PlayerController_Skills m_Skills;

    // ===================== UNITY 生命周期 =====================
    void Start()
    {
        // 锁定鼠标到屏幕中心
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 初始化
        m_CurrentDistance = BackDistance;
        m_CurrentShoulderOffset = ShoulderOffsetRight;
        m_CamTheta = transform.eulerAngles.y; // 初始朝向
        m_CharacterYaw = m_CamTheta;

        // 查找相机引用
        if (CameraRef == null)
            CameraRef = GetComponentInChildren<Camera>();

        // 订阅技能系统事件
        m_Skills = FindAnyObjectByType<PlayerController_Skills>();
        if (m_Skills != null)
            m_Skills.OnSelectionStateChanged += OnSkillStateChanged;
    }

    void OnDestroy()
    {
        if (m_Skills != null)
            m_Skills.OnSelectionStateChanged -= OnSkillStateChanged;
    }

    void Update()
    {
        HandleInput();
        UpdateCharacterRotation();
        UpdateCameraPosition();
        UpdateFOV();
    }

    // ===================== 技能状态切换 =====================
    void OnSkillStateChanged(PlayerController_Skills.SelectionState state)
    {
        m_CurrentState = (state == PlayerController_Skills.SelectionState.None)
            ? CameraState.Normal
            : CameraState.Aiming;
    }

    // ===================== 输入处理 =====================
    void HandleInput()
    {
        // Escape 切换鼠标锁定
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // 鼠标 X -> 水平旋转（绕 Y 轴）
        // 鼠标 Y -> 垂直旋转（绕 X 轴）
        float mouseX = Input.GetAxisRaw("Mouse X") * RotateSpeed;
        float mouseY = Input.GetAxisRaw("Mouse Y") * RotateSpeed;

        m_CamTheta += mouseX;
        m_CamTheta = m_CamTheta % 360f;

        m_CamPhi -= mouseY;
        m_CamPhi = Mathf.Clamp(m_CamPhi, VerticalMin, VerticalMax);
    }

    // ===================== 角色朝向 =====================
    void UpdateCharacterRotation()
    {
        // 角色朝向始终跟随相机的水平角度
        m_CharacterYaw = Mathf.LerpAngle(m_CharacterYaw, m_CamTheta, Time.deltaTime * CharacterRotateSmooth);
        transform.rotation = Quaternion.Euler(0f, m_CharacterYaw, 0f);
    }

    // ===================== 相机位置更新 =====================
    void UpdateCameraPosition()
    {
        if (CameraRef == null)
            return;

        // 目标参数
        float targetBackDist = (m_CurrentState == CameraState.Aiming) ? AimDistance : BackDistance;
        float targetShoulder = (m_CurrentState == CameraState.Aiming) ? AimShoulderOffsetRight : ShoulderOffsetRight;

        // 平滑插值
        m_CurrentDistance = Mathf.Lerp(m_CurrentDistance, targetBackDist, Time.deltaTime * PositionSmooth);
        m_CurrentShoulderOffset = Mathf.Lerp(m_CurrentShoulderOffset, targetShoulder, Time.deltaTime * PositionSmooth);

        // 相机基准高度（角色肩部高度）
        float camHeight = transform.lossyScale.y * CameraHeightRatio;

        // 相机目标位置：角色正后方 + 固定右肩偏移 + 可变高度
        // 后方方向始终是角色的 -forward（固定方向，不随鼠标 X 改变）
        Vector3 shoulderPos = transform.position;
        shoulderPos.y += camHeight;                            // 基准高度
        shoulderPos += transform.right * m_CurrentShoulderOffset; // 固定右肩偏移

        Vector3 idealCamPos = shoulderPos
            - transform.forward * m_CurrentDistance;           // 固定后方偏移
        idealCamPos.y += m_CamPhi * 0.05f;                    // 鼠标 Y 上下视角

        // ========== 碰撞检测 ==========
        // 从角色肩部向理想相机位置发射射线，检测障碍物
        Vector3 rayDir = (idealCamPos - shoulderPos).normalized;
        float maxDist = m_CurrentDistance + CollisionMinDistance;

        if (Physics.Raycast(shoulderPos, rayDir, out RaycastHit hit, maxDist, CollisionLayers))
        {
            // 撞到障碍物，相机放在碰撞点向后退 CollisionMinDistance
            idealCamPos = hit.point - rayDir * CollisionMinDistance;
        }

        // 平滑跟随
        CameraRef.transform.position = Vector3.Lerp(
            CameraRef.transform.position,
            idealCamPos,
            Time.deltaTime * PositionSmooth
        );

        // 相机朝向：看向角色前方（朝向方向）
        Vector3 lookAtPos = transform.position + transform.forward * 3f + Vector3.up * camHeight * 0.5f;
        CameraRef.transform.LookAt(lookAtPos);
    }

    // ===================== FOV 更新 =====================
    void UpdateFOV()
    {
        if (CameraRef == null)
            return;

        float targetFOV = (m_CurrentState == CameraState.Aiming) ? AimFOV : NormalFOV;
        CameraRef.fieldOfView = Mathf.Lerp(CameraRef.fieldOfView, targetFOV, Time.deltaTime * FOVSmooth);
    }

    // ===================== 公开接口 =====================
    public Vector3 GetCameraForward() => CameraRef != null ? CameraRef.transform.forward : Vector3.forward;

    public Vector3 GetAimTarget()
    {
        if (CameraRef == null) return transform.position + transform.forward * 10f;
        return CameraRef.transform.position + CameraRef.transform.forward * 10f;
    }

    public CameraState GetCameraState() => m_CurrentState;

    public float GetHorizontalAngle() => m_CamTheta;
}
