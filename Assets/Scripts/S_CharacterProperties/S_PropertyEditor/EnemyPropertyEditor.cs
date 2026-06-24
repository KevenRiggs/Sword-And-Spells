#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人属性编辑器组件。
/// 将 EnemyProperty 资产拖入 EnemyPropertyAsset 字段后，
/// 即可在 Inspector 中查看和编辑该资产的所有属性。
/// </summary>
public class EnemyPropertyEditor : MonoBehaviour
{
    [Header("属性资产")]
    [Tooltip("将 EnemyProperty 资产拖入此处")]
    public EnemyPropertyTemplate EnemyPropertyAsset;

    // ===== 通用属性 =====

    [Header("===== 通用属性 =====")]
    [Tooltip("最大生命值")] public float Health_Max;
    [Tooltip("当前生命值")] public float Health_Current;
    [Tooltip("物理强度")] public float Physical_Strength;
    [Tooltip("物理防御")] public float Physical_Defense;
    [Tooltip("法术强度")] public float Magic_Strength;
    [Tooltip("法术防御")] public float Magic_Defense;

    // ===== 敌人伤害属性 =====

    [Header("===== 敌人伤害属性 =====")]
    [Tooltip("物理伤害基础值")] public float Enemy_Physical_Damage_Base;
    [Tooltip("法术伤害基础值")] public float Enemy_Magic_Damage_Base;

    /// <summary>
    /// 从 EnemyPropertyAsset 资产同步所有属性值到本组件。
    /// </summary>
    [ContextMenu("从资产同步属性")]
    public void SyncFromAsset()
    {
        if (EnemyPropertyAsset == null) return;

        Health_Max = EnemyPropertyAsset.Health_Max;
        Health_Current = EnemyPropertyAsset.Health_Current;
        Physical_Strength = EnemyPropertyAsset.Physical_Strength;
        Physical_Defense = EnemyPropertyAsset.Physical_Defense;
        Magic_Strength = EnemyPropertyAsset.Magic_Strength;
        Magic_Defense = EnemyPropertyAsset.Magic_Defense;

        Enemy_Physical_Damage_Base = EnemyPropertyAsset.Enemy_Physical_Damage_Base;
        Enemy_Magic_Damage_Base = EnemyPropertyAsset.Enemy_Magic_Damage_Base;
    }

    /// <summary>
    /// 将本组件的属性值同步回 EnemyPropertyAsset 资产。
    /// </summary>
    [ContextMenu("同步属性到资产")]
    public void SyncToAsset()
    {
        if (EnemyPropertyAsset == null) return;

        EnemyPropertyAsset.Health_Max = Health_Max;
        EnemyPropertyAsset.Health_Current = Health_Current;
        EnemyPropertyAsset.Physical_Strength = Physical_Strength;
        EnemyPropertyAsset.Physical_Defense = Physical_Defense;
        EnemyPropertyAsset.Magic_Strength = Magic_Strength;
        EnemyPropertyAsset.Magic_Defense = Magic_Defense;

        EnemyPropertyAsset.Enemy_Physical_Damage_Base = Enemy_Physical_Damage_Base;
        EnemyPropertyAsset.Enemy_Magic_Damage_Base = Enemy_Magic_Damage_Base;

#if UNITY_EDITOR
        EditorUtility.SetDirty(EnemyPropertyAsset);
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EnemyPropertyEditor))]
    public class EnemyPropertyEditorEditor : Editor
    {
        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>
        {
            { "通用属性", true },
            { "敌人伤害属性", true },
        };

        private SerializedProperty _assetProp;

        private void OnEnable()
        {
            _assetProp = serializedObject.FindProperty("EnemyPropertyAsset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_assetProp, new GUIContent("属性资产"));
            EditorGUILayout.Space(5f);

            DrawSection("通用属性", () =>
            {
                DrawField("Health_Max");
                DrawField("Health_Current");
                DrawField("Physical_Strength");
                DrawField("Physical_Defense");
                DrawField("Magic_Strength");
                DrawField("Magic_Defense");
            });

            DrawSection("敌人伤害属性", () =>
            {
                DrawField("Enemy_Physical_Damage_Base");
                DrawField("Enemy_Magic_Damage_Base");
            });

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从资产读取", GUILayout.Height(30f)))
            {
                ((EnemyPropertyEditor)target).SyncFromAsset();
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("写入资产", GUILayout.Height(30f)))
            {
                ((EnemyPropertyEditor)target).SyncToAsset();
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
