using UnityEngine;

[CreateAssetMenu(fileName = "EnemyProperty", menuName = "Scriptable Objects/EnemyProperty")]
public class EnemyPropertyTemplate : CharacterPropertyTemplate
{
    // ===== 敌人伤害属性 =====

    // 物理伤害基础值
    public float Enemy_Physical_Damage_Base;
    // 法术伤害基础值
    public float Enemy_Magic_Damage_Base;
}