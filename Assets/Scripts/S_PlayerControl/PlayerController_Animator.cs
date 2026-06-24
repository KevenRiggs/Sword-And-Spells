using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController_Animator : MonoBehaviour
{
    [Header("动画参数")]
    [Tooltip("前后方向速度参数名（对应Blend Tree的SpeedX）")]
    public string speedXParameter = "SpeedX";

    [Tooltip("左右方向速度参数名（对应Blend Tree的SpeedY）")]
    public string speedYParameter = "SpeedY";

    [Tooltip("速度缩放系数，用于调整动画混合的灵敏度")]
    public float speedMultiplier = 1f;

    [Header("引用")]
    [Tooltip("角色移动脚本引用")]
    public PlayerController_Movement movementScript;

    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (movementScript == null)
        {
            movementScript = GetComponent<PlayerController_Movement>();
        }

        if (movementScript == null)
        {
            Debug.LogWarning("未找到 PlayerController_Movement 脚本，动画参数将无法更新。");
        }
    }

    void Update()
    {
        if (animator == null)
            return;

        // 读取输入方向
        float h = 0f;
        float v = 0f;
        if (Input.GetKey(movementScript.Forward_KeyCode))   v += 1f;
        if (Input.GetKey(movementScript.Backward_KeyCode))  v -= 1f;
        if (Input.GetKey(movementScript.Rightward_KeyCode)) h += 1f;
        if (Input.GetKey(movementScript.Leftward_KeyCode))  h -= 1f;

        // 归一化防止斜向输入时 magnitude > 1
        Vector2 inputDir = new Vector2(h, v);
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        // 直接使用输入值作为动画参数（范围已经是 -1 ~ 1）
        // SpeedX: 前后方向（对应 v）
        // SpeedY: 左右方向（对应 h）
        animator.SetFloat(speedXParameter, inputDir.y);
        animator.SetFloat(speedYParameter, inputDir.x);
    }
}
