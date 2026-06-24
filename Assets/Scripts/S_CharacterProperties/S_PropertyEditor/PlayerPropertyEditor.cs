#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色属性编辑器组件。
/// 将 PlayerProperty 资产拖入 PlayerPropertyAsset 字段后，
/// 即可在 Inspector 中查看和编辑该资产的所有属性。
/// </summary>
public class PlayerPropertyEditor : MonoBehaviour
{
    [Header("属性资产")]
    [Tooltip("将 PlayerProperty 资产拖入此处")]
    public PlayerPropertyTemplate PlayerPropertyAsset;

    // ===== 通用属性（仅供参考，从属资产的值以资产自身为准）=====

    [Header("===== 通用属性 =====")]
    [Tooltip("最大生命值")] public float 生命上限;
    [Tooltip("当前生命值")] public float 生命当前;
    [Tooltip("物理强度")] public float 物理强度;
    [Tooltip("物理防御")] public float 物理防御;
    [Tooltip("法术强度")] public float 法术强度;
    [Tooltip("法术防御")] public float 法术防御;

    // ===== 玩家资源属性 =====

    [Header("===== 玩家资源属性 =====")]
    [Tooltip("最大体力值（用于左键Sword消耗）")] public float 体力上限;
    [Tooltip("当前体力值")] public float 体力当前;
    [Tooltip("最大法力值（用于右键Spell消耗）")] public float 法力上限;
    [Tooltip("当前法力值")] public float 法力当前;

    // ===== 回复属性 =====

    [Header("===== 回复属性 =====")]
    [Tooltip("每秒血量回复值")] public float 生命回复;
    [Tooltip("每秒体力回复值")] public float 体力回复;
    [Tooltip("每秒法力回复值")] public float 法力回复;

    // ===== 技能操作：左键+Q（Sword单体攻击）=====

    [Header("===== 技能：左键+Q Sword单体攻击 =====")]
    [Tooltip("基础伤害值")] public float 剑单体伤害基础值;
    [Tooltip("增伤倍率")] public float 剑单体增伤倍率;
    [Tooltip("体力消耗")] public float 剑单体体力消耗;
    [Tooltip("冷却时间")] public float 剑单体冷却时间;
    [Tooltip("暴击概率")] public float 剑单体暴击概率;

    // ===== 技能操作：左键+E（Sword格挡）=====

    [Header("===== 技能：左键+E Sword格挡 =====")]
    [Tooltip("格挡减伤比例")] public float 剑格挡减伤比例;
    [Tooltip("体力消耗")] public float 剑格挡体力消耗;
    [Tooltip("冷却时间")] public float 剑格挡冷却时间;
    [Tooltip("完美格挡概率")] public float 剑格挡完美格挡概率;

    // ===== 技能操作：左键+R（SwordAOE）=====

    [Header("===== 技能：左键+R SwordAOE =====")]
    [Tooltip("基础伤害值")] public float 剑AOE伤害基础值;
    [Tooltip("增伤倍率")] public float 剑AOE增伤倍率;
    [Tooltip("体力消耗")] public float 剑AOE体力消耗;
    [Tooltip("冷却时间")] public float 剑AOE冷却时间;
    [Tooltip("剑气范围")] public float 剑AOE范围;
    [Tooltip("剑气数量")] public float 剑AOE数量;

    // ===== 技能操作：右键+Q（Spell单体攻击）=====

    [Header("===== 技能：右键+Q Spell单体攻击 =====")]
    [Tooltip("基础伤害值")] public float 法单体伤害基础值;
    [Tooltip("增伤倍率")] public float 法单体增伤倍率;
    [Tooltip("法力消耗")] public float 法单体法力消耗;
    [Tooltip("冷却时间")] public float 法单体冷却时间;
    [Tooltip("法球数量")] public float 法单体法球数量;

    // ===== 技能操作：右键+E（Spell格挡）=====

    [Header("===== 技能：右键+E Spell格挡 =====")]
    [Tooltip("格挡减伤比例")] public float 法格挡减伤比例;
    [Tooltip("法力消耗")] public float 法格挡法力消耗;
    [Tooltip("冷却时间")] public float 法格挡冷却时间;
    [Tooltip("完美格挡概率")] public float 法格挡完美格挡概率;

    // ===== 技能操作：右键+R（SpellAOE）=====

    [Header("===== 技能：右键+R SpellAOE =====")]
    [Tooltip("基础伤害值")] public float 法AOE伤害基础值;
    [Tooltip("增伤倍率")] public float 法AOE增伤倍率;
    [Tooltip("法力消耗")] public float 法AOE法力消耗;
    [Tooltip("冷却时间")] public float 法AOE冷却时间;
    [Tooltip("影响范围")] public float 法AOE范围;
    [Tooltip("持续时间")] public float 法AOE持续时间;

    /// <summary>
    /// 从 PlayerPropertyAsset 资产同步所有属性值到本组件。
    /// </summary>
    [ContextMenu("从资产同步属性")]
    public void SyncFromAsset()
    {
        if (PlayerPropertyAsset == null) return;

        生命上限 = PlayerPropertyAsset.Health_Max;
        生命当前 = PlayerPropertyAsset.Health_Current;
        物理强度 = PlayerPropertyAsset.Physical_Strength;
        物理防御 = PlayerPropertyAsset.Physical_Defense;
        法术强度 = PlayerPropertyAsset.Magic_Strength;
        法术防御 = PlayerPropertyAsset.Magic_Defense;

        体力上限 = PlayerPropertyAsset.Stamina_Max;
        体力当前 = PlayerPropertyAsset.Stamina_Current;
        法力上限 = PlayerPropertyAsset.Mana_Max;
        法力当前 = PlayerPropertyAsset.Mana_Current;

        生命回复 = PlayerPropertyAsset.Health_Regen;
        体力回复 = PlayerPropertyAsset.Stamina_Regen;
        法力回复 = PlayerPropertyAsset.Mana_Regen;

        剑单体伤害基础值 = PlayerPropertyAsset.SwordSingle_Damage_Base;
        剑单体增伤倍率 = PlayerPropertyAsset.SwordSingle_Damage_Multiplier;
        剑单体体力消耗 = PlayerPropertyAsset.SwordSingle_Stamina_Cost;
        剑单体冷却时间 = PlayerPropertyAsset.SwordSingle_Cooldown_Time;
        剑单体暴击概率 = PlayerPropertyAsset.SwordSingle_Crit_Chance;

        剑格挡减伤比例 = PlayerPropertyAsset.SwordBlock_Block_Reduction;
        剑格挡体力消耗 = PlayerPropertyAsset.SwordBlock_Stamina_Cost;
        剑格挡冷却时间 = PlayerPropertyAsset.SwordBlock_Cooldown_Time;
        剑格挡完美格挡概率 = PlayerPropertyAsset.SwordBlock_Perfect_Block_Chance;

        剑AOE伤害基础值 = PlayerPropertyAsset.SwordAOE_Damage_Base;
        剑AOE增伤倍率 = PlayerPropertyAsset.SwordAOE_Damage_Multiplier;
        剑AOE体力消耗 = PlayerPropertyAsset.SwordAOE_Stamina_Cost;
        剑AOE冷却时间 = PlayerPropertyAsset.SwordAOE_Cooldown_Time;
        剑AOE范围 = PlayerPropertyAsset.SwordAOE_Range;
        剑AOE数量 = PlayerPropertyAsset.SwordAOE_Count;

        法单体伤害基础值 = PlayerPropertyAsset.SpellSingle_Damage_Base;
        法单体增伤倍率 = PlayerPropertyAsset.SpellSingle_Damage_Multiplier;
        法单体法力消耗 = PlayerPropertyAsset.SpellSingle_Mana_Cost;
        法单体冷却时间 = PlayerPropertyAsset.SpellSingle_Cooldown_Time;
        法单体法球数量 = PlayerPropertyAsset.SpellSingle_Spell_Count;

        法格挡减伤比例 = PlayerPropertyAsset.SpellBlock_Block_Reduction;
        法格挡法力消耗 = PlayerPropertyAsset.SpellBlock_Mana_Cost;
        法格挡冷却时间 = PlayerPropertyAsset.SpellBlock_Cooldown_Time;
        法格挡完美格挡概率 = PlayerPropertyAsset.SpellBlock_Perfect_Block_Chance;

        法AOE伤害基础值 = PlayerPropertyAsset.SpellAOE_Damage_Base;
        法AOE增伤倍率 = PlayerPropertyAsset.SpellAOE_Damage_Multiplier;
        法AOE法力消耗 = PlayerPropertyAsset.SpellAOE_Mana_Cost;
        法AOE冷却时间 = PlayerPropertyAsset.SpellAOE_Cooldown_Time;
        法AOE范围 = PlayerPropertyAsset.SpellAOE_Range;
        法AOE持续时间 = PlayerPropertyAsset.SpellAOE_Duration;
    }

    /// <summary>
    /// 将本组件的属性值同步回 PlayerPropertyAsset 资产。
    /// </summary>
    [ContextMenu("同步属性到资产")]
    public void SyncToAsset()
    {
        if (PlayerPropertyAsset == null) return;

        PlayerPropertyAsset.Health_Max = 生命上限;
        PlayerPropertyAsset.Health_Current = 生命当前;
        PlayerPropertyAsset.Physical_Strength = 物理强度;
        PlayerPropertyAsset.Physical_Defense = 物理防御;
        PlayerPropertyAsset.Magic_Strength = 法术强度;
        PlayerPropertyAsset.Magic_Defense = 法术防御;

        PlayerPropertyAsset.Stamina_Max = 体力上限;
        PlayerPropertyAsset.Stamina_Current = 体力当前;
        PlayerPropertyAsset.Mana_Max = 法力上限;
        PlayerPropertyAsset.Mana_Current = 法力当前;

        PlayerPropertyAsset.Health_Regen = 生命回复;
        PlayerPropertyAsset.Stamina_Regen = 体力回复;
        PlayerPropertyAsset.Mana_Regen = 法力回复;

        PlayerPropertyAsset.SwordSingle_Damage_Base = 剑单体伤害基础值;
        PlayerPropertyAsset.SwordSingle_Damage_Multiplier = 剑单体增伤倍率;
        PlayerPropertyAsset.SwordSingle_Stamina_Cost = 剑单体体力消耗;
        PlayerPropertyAsset.SwordSingle_Cooldown_Time = 剑单体冷却时间;
        PlayerPropertyAsset.SwordSingle_Crit_Chance = 剑单体暴击概率;

        PlayerPropertyAsset.SwordBlock_Block_Reduction = 剑格挡减伤比例;
        PlayerPropertyAsset.SwordBlock_Stamina_Cost = 剑格挡体力消耗;
        PlayerPropertyAsset.SwordBlock_Cooldown_Time = 剑格挡冷却时间;
        PlayerPropertyAsset.SwordBlock_Perfect_Block_Chance = 剑格挡完美格挡概率;

        PlayerPropertyAsset.SwordAOE_Damage_Base = 剑AOE伤害基础值;
        PlayerPropertyAsset.SwordAOE_Damage_Multiplier = 剑AOE增伤倍率;
        PlayerPropertyAsset.SwordAOE_Stamina_Cost = 剑AOE体力消耗;
        PlayerPropertyAsset.SwordAOE_Cooldown_Time = 剑AOE冷却时间;
        PlayerPropertyAsset.SwordAOE_Range = 剑AOE范围;
        PlayerPropertyAsset.SwordAOE_Count = 剑AOE数量;

        PlayerPropertyAsset.SpellSingle_Damage_Base = 法单体伤害基础值;
        PlayerPropertyAsset.SpellSingle_Damage_Multiplier = 法单体增伤倍率;
        PlayerPropertyAsset.SpellSingle_Mana_Cost = 法单体法力消耗;
        PlayerPropertyAsset.SpellSingle_Cooldown_Time = 法单体冷却时间;
        PlayerPropertyAsset.SpellSingle_Spell_Count = 法单体法球数量;

        PlayerPropertyAsset.SpellBlock_Block_Reduction = 法格挡减伤比例;
        PlayerPropertyAsset.SpellBlock_Mana_Cost = 法格挡法力消耗;
        PlayerPropertyAsset.SpellBlock_Cooldown_Time = 法格挡冷却时间;
        PlayerPropertyAsset.SpellBlock_Perfect_Block_Chance = 法格挡完美格挡概率;

        PlayerPropertyAsset.SpellAOE_Damage_Base = 法AOE伤害基础值;
        PlayerPropertyAsset.SpellAOE_Damage_Multiplier = 法AOE增伤倍率;
        PlayerPropertyAsset.SpellAOE_Mana_Cost = 法AOE法力消耗;
        PlayerPropertyAsset.SpellAOE_Cooldown_Time = 法AOE冷却时间;
        PlayerPropertyAsset.SpellAOE_Range = 法AOE范围;
        PlayerPropertyAsset.SpellAOE_Duration = 法AOE持续时间;

#if UNITY_EDITOR
        EditorUtility.SetDirty(PlayerPropertyAsset);
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PlayerPropertyEditor))]
    public class PlayerPropertyEditorEditor : Editor
    {
        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>
        {
            { "通用属性", true },
            { "玩家资源属性", true },
            { "回复属性", true },
            { "技能：左键+Q Sword单体攻击", true },
            { "技能：左键+E Sword格挡", true },
            { "技能：左键+R SwordAOE", true },
            { "技能：右键+Q Spell单体攻击", true },
            { "技能：右键+E Spell格挡", true },
            { "技能：右键+R SpellAOE", true },
        };

        private SerializedProperty _assetProp;

        private void OnEnable()
        {
            _assetProp = serializedObject.FindProperty("PlayerPropertyAsset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_assetProp, new GUIContent("属性资产"));
            EditorGUILayout.Space(5f);

            DrawSection("通用属性", () =>
            {
                DrawField("生命上限");
                DrawField("生命当前");
                DrawField("物理强度");
                DrawField("物理防御");
                DrawField("法术强度");
                DrawField("法术防御");
            });

            DrawSection("玩家资源属性", () =>
            {
                DrawField("体力上限");
                DrawField("体力当前");
                DrawField("法力上限");
                DrawField("法力当前");
            });

            DrawSection("回复属性", () =>
            {
                DrawField("生命回复");
                DrawField("体力回复");
                DrawField("法力回复");
            });

            DrawSection("技能：左键+Q Sword单体攻击", () =>
            {
                DrawField("剑单体伤害基础值");
                DrawField("剑单体增伤倍率");
                DrawField("剑单体体力消耗");
                DrawField("剑单体冷却时间");
                DrawField("剑单体暴击概率");
            });

            DrawSection("技能：左键+E Sword格挡", () =>
            {
                DrawField("剑格挡减伤比例");
                DrawField("剑格挡体力消耗");
                DrawField("剑格挡冷却时间");
                DrawField("剑格挡完美格挡概率");
            });

            DrawSection("技能：左键+R SwordAOE", () =>
            {
                DrawField("剑AOE伤害基础值");
                DrawField("剑AOE增伤倍率");
                DrawField("剑AOE体力消耗");
                DrawField("剑AOE冷却时间");
                DrawField("剑AOE范围");
                DrawField("剑AOE数量");
            });

            DrawSection("技能：右键+Q Spell单体攻击", () =>
            {
                DrawField("法单体伤害基础值");
                DrawField("法单体增伤倍率");
                DrawField("法单体法力消耗");
                DrawField("法单体冷却时间");
                DrawField("法单体法球数量");
            });

            DrawSection("技能：右键+E Spell格挡", () =>
            {
                DrawField("法格挡减伤比例");
                DrawField("法格挡法力消耗");
                DrawField("法格挡冷却时间");
                DrawField("法格挡完美格挡概率");
            });

            DrawSection("技能：右键+R SpellAOE", () =>
            {
                DrawField("法AOE伤害基础值");
                DrawField("法AOE增伤倍率");
                DrawField("法AOE法力消耗");
                DrawField("法AOE冷却时间");
                DrawField("法AOE范围");
                DrawField("法AOE持续时间");
            });

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从资产读取", GUILayout.Height(30f)))
            {
                ((PlayerPropertyEditor)target).SyncFromAsset();
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("写入资产", GUILayout.Height(30f)))
            {
                ((PlayerPropertyEditor)target).SyncToAsset();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(string title, System.Action drawAction)
        {
            if (!_foldouts.ContainsKey(title))
                _foldouts[title] = false;

            EditorGUILayout.Space(3f);
            _foldouts[title] = EditorGUILayout.Foldout(_foldouts[title], title, true);
            if (_foldouts[title])
            {
                EditorGUI.indentLevel++;
                drawAction();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawField(string fieldName)
        {
            var prop = serializedObject.FindProperty(fieldName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }
    }
#endif
}
