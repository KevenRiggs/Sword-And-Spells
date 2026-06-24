using UnityEngine;

public class PlayerController_Movement : MonoBehaviour
{
    [Header("按键设置")]
    public KeyCode Forward_KeyCode   = KeyCode.W;
    public KeyCode Backward_KeyCode  = KeyCode.S;
    public KeyCode Leftward_KeyCode  = KeyCode.A;
    public KeyCode Rightward_KeyCode = KeyCode.D;
    public KeyCode Jump_KeyCode      = KeyCode.Space;

    [Header("移动参数")]
    [Tooltip("移动速度（米/秒）")]
    public float moveSpeed = 6f;

    [Header("跳跃参数")]
    [Tooltip("跳跃初始向上速度")]
    public float jumpForce = 12f;

    [Tooltip("重力加速度")]
    public float gravity = -20f;

    [Tooltip("地面检测起始位置（相对于自身）的偏移")]
    public Vector3 groundCheckOffset = new Vector3(0f, -0.5f, 0f);

    [Tooltip("地面检测半径")]
    public float groundCheckRadius = 0.3f;

    [Tooltip("地面所在的层级")]
    public LayerMask groundLayer;

    Rigidbody rb;

    /// <summary>获取当前刚体速度（世界坐标）</summary>
    public Vector3 Velocity => rb.linearVelocity;

    /// <summary>角色是否在地面上</summary>
    bool isGrounded;

    /// <summary>是否真正与地面有物理碰撞接触</summary>
    bool isTouchingGround;

    /// <summary>跳跃标志位，Update 检测到按下后设为 true，FixedUpdate 消费后重置</summary>
    bool jumpPressed;

    /// <summary>本帧是否刚执行过跳跃（用于跳过落地速度清零逻辑）</summary>
    bool justJumped;

    void OnCollisionStay(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isTouchingGround = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isTouchingGround = false;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        // 由脚本自己控制重力，禁用Unity内置重力
        rb.useGravity = false;
    }

    void Update()
    {
        // 跳跃输入检测放在 Update 中，避免在 FixedUpdate 中因帧率问题丢失 GetKeyDown 事件
        if (Input.GetKeyDown(Jump_KeyCode))
        {
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        // 1. 地面检测
        Vector3 rayOrigin = transform.position + groundCheckOffset;
        isGrounded = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundCheckRadius + 0.05f,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        // 2. 读取输入
        float h = 0f, v = 0f;
        if (Input.GetKey(Forward_KeyCode))   v += 1f;
        if (Input.GetKey(Backward_KeyCode))  v -= 1f;
        if (Input.GetKey(Rightward_KeyCode)) h += 1f;
        if (Input.GetKey(Leftward_KeyCode))  h -= 1f;

        Vector3 inputDir = new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        Vector3 moveDir = GetCameraRelativeDirection(inputDir);

        // 3. 获取当前速度
        Vector3 vel = rb.linearVelocity;

        // 4. Y轴控制
        if (jumpPressed && isGrounded)
        {
            vel.y = jumpForce;
            jumpPressed = false;
            justJumped = true;
        }
        else
        {
            jumpPressed = false;
            if (isGrounded && !justJumped)
            {
                vel.y = 0f;
            }
            else
            {
                vel.y += gravity * Time.fixedDeltaTime;
            }
            justJumped = false;
        }

        // 5. X/Z轴控制（与Y轴独立）
        vel.x = moveDir.x * moveSpeed;
        vel.z = moveDir.z * moveSpeed;

        // 6. 写入刚体
        rb.linearVelocity = vel;
    }

    /// <summary>
    /// 将基于输入方向的本地向量转换为相对于主摄像机朝向的世界向量。
    /// </summary>
    Vector3 GetCameraRelativeDirection(Vector3 inputDir)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("未找到主摄像机，移动方向将基于世界坐标。");
            return inputDir;
        }

        // 摄像机.forward 在XZ平面的投影
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        // 摄像机.right 在XZ平面的投影
        Vector3 camRight = cam.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        return camForward * inputDir.z + camRight * inputDir.x;
    }

    void OnDrawGizmosSelected()
    {
        // 在 Editor 中可视化地面检测球体
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            transform.position + groundCheckOffset,
            groundCheckRadius
        );
    }
}
