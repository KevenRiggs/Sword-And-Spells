using UnityEngine;

public class CharacterPropertyTemplate : ScriptableObject
{
    // ===== 通用属性 =====

    // 最大生命值
    public float Health_Max;
    // 当前生命值
    public float Health_Current;
    // 物理强度
    public float Physical_Strength;
    // 物理防御
    public float Physical_Defense;
    // 法术强度
    public float Magic_Strength;
    // 法术防御
    public float Magic_Defense;
}