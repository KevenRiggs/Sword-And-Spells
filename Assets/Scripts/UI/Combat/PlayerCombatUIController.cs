using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatUIController : MonoBehaviour
{
    [Header("Bar Fills - 从场景拖拽赋值")]
    public Image healthBarFill;    // PlayerCurrentHealth (绿色)
    public Image staminaBarFill;   // PlayerCurrentPower (黄色)
    public Image manaBarFill;      // PlayerCurrentSpell (蓝色)

    [Header("玩家属性数据")]
    public PlayerPropertyTemplate playerProperty;

    [Header("条形动画配置")]
    [Tooltip("条形变化的平滑速度，值越大变化越快")]
    public float lerpSpeed = 5f;

    // 当前显示的视觉值（用于Lerp插值）
    private float m_HealthVisual;
    private float m_StaminaVisual;
    private float m_ManaVisual;

    // Update is called once per frame
    void Update()
    {
        if (playerProperty == null) return;

        // 初始化视觉值（首次运行或刚启用时）
        if (m_HealthVisual < 0f) m_HealthVisual = playerProperty.Health_Current / playerProperty.Health_Max;
        if (m_StaminaVisual < 0f) m_StaminaVisual = playerProperty.Stamina_Current / playerProperty.Stamina_Max;
        if (m_ManaVisual < 0f) m_ManaVisual = playerProperty.Mana_Current / playerProperty.Mana_Max;

        UpdateHealthBar();
        UpdateStaminaBar();
        UpdateManaBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;

        float targetRatio = playerProperty.Health_Current / playerProperty.Health_Max;
        m_HealthVisual = Mathf.Lerp(m_HealthVisual, targetRatio, lerpSpeed * Time.deltaTime);
        healthBarFill.fillAmount = Mathf.Clamp01(m_HealthVisual);
    }

    private void UpdateStaminaBar()
    {
        if (staminaBarFill == null) return;

        float targetRatio = playerProperty.Stamina_Current / playerProperty.Stamina_Max;
        m_StaminaVisual = Mathf.Lerp(m_StaminaVisual, targetRatio, lerpSpeed * Time.deltaTime);
        staminaBarFill.fillAmount = Mathf.Clamp01(m_StaminaVisual);
    }

    private void UpdateManaBar()
    {
        if (manaBarFill == null) return;

        float targetRatio = playerProperty.Mana_Current / playerProperty.Mana_Max;
        m_ManaVisual = Mathf.Lerp(m_ManaVisual, targetRatio, lerpSpeed * Time.deltaTime);
        manaBarFill.fillAmount = Mathf.Clamp01(m_ManaVisual);
    }
}
