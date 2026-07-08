using UnityEngine;

/// <summary>
/// 玩家实时状态管理器
/// 独立处理玩家属性的自动回复逻辑（血量/体力/法力每秒回复）
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
    // ===== 引用 =====
    [Header("属性引用")]
    [Tooltip("拖入 PlayerProperty 资产")]
    public PlayerPropertyTemplate PlayerPropertyAsset;

    // ===== Unity 生命周期 =====

    void Update()
    {
        if (PlayerPropertyAsset == null)
            return;

        float dt = Time.deltaTime;
        UpdateHealthRegen(dt);
        UpdateStaminaRegen(dt);
        UpdateManaRegen(dt);
    }

    // ===== 回复更新 =====

    void UpdateHealthRegen(float dt)
    {
        float regen = PlayerPropertyAsset.Health_Regen;
        if (regen <= 0f)
            return;

        PlayerPropertyAsset.Health_Current = Mathf.Min(
            PlayerPropertyAsset.Health_Current + regen * dt,
            PlayerPropertyAsset.Health_Max
        );
    }

    void UpdateStaminaRegen(float dt)
    {
        float regen = PlayerPropertyAsset.Stamina_Regen;
        if (regen <= 0f)
            return;

        PlayerPropertyAsset.Stamina_Current = Mathf.Min(
            PlayerPropertyAsset.Stamina_Current + regen * dt,
            PlayerPropertyAsset.Stamina_Max
        );
    }

    void UpdateManaRegen(float dt)
    {
        float regen = PlayerPropertyAsset.Mana_Regen;
        if (regen <= 0f)
            return;

        PlayerPropertyAsset.Mana_Current = Mathf.Min(
            PlayerPropertyAsset.Mana_Current + regen * dt,
            PlayerPropertyAsset.Mana_Max
        );
    }
}
