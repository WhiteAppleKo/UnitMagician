using UnityEditor;
using UnityEngine;

namespace UnitSystem
{
    [CustomPropertyDrawer(typeof(UnitSetupData))]
    public class UnitSetupDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 라벨 그리기
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var unitTypeProp = property.FindPropertyRelative("unitType");
            var initialValueProp = property.FindPropertyRelative("initialValue");

            // Enum에 따라 공간 분배
            bool hideValue = unitTypeProp.enumValueIndex == (int)UnitType.Vector_Reverse;

            if (hideValue)
            {
                // Vector_Reverse일 때는 UnitType만 전체 너비로 그림
                var typeRect = new Rect(position.x, position.y, position.width, position.height);
                EditorGUI.PropertyField(typeRect, unitTypeProp, GUIContent.none);
            }
            else
            {
                // 일반적인 경우 반반 나눠서 그림
                float halfWidth = position.width / 2f;
                var typeRect = new Rect(position.x, position.y, halfWidth - 2, position.height);
                var valueRect = new Rect(position.x + halfWidth + 2, position.y, halfWidth - 2, position.height);

                EditorGUI.PropertyField(typeRect, unitTypeProp, GUIContent.none);
                
                // 보기 좋게 Value 라벨을 작게 추가
                EditorGUIUtility.labelWidth = 40;
                EditorGUI.PropertyField(valueRect, initialValueProp, new GUIContent("Value"));
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
