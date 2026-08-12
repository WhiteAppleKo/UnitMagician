using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using PipeLine.PipeLineBase;

namespace _03._PipeLine.Editor
{
    /// <summary>
    /// 모든 PipeLineSo<T> 하위 클래스를 자동으로 감지하고 단계(Step)를 구성할 수 있게 도와주는 공용 에디터입니다.
    /// </summary>
    [CustomEditor(typeof(PipeLineSoBase), true)]
    public class GenericPipeLineEditor : UnityEditor.Editor
    {
        private ReorderableList reorderableList;
        private SerializedProperty stepsProp;
        private Type contextType;

        private void OnEnable()
        {
            // 타겟 클래스 계층에서 PipeLineSo<T>의 T(Context) 타입을 추출합니다.
            contextType = GetPipeLineContextType(target.GetType());
            
            stepsProp = serializedObject.FindProperty("steps");
            if (stepsProp == null) return;

            reorderableList = new ReorderableList(serializedObject, stepsProp, true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                string headerName = contextType != null ? $"{contextType.Name} Pipeline" : "Generic Pipeline";
                EditorGUI.LabelField(rect, $"{headerName} Steps (SerializeReference)");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = stepsProp.GetArrayElementAtIndex(index);
                rect.y += 2;

                string label = "Empty Step";
                if (element.managedReferenceValue != null)
                {
                    label = element.managedReferenceValue.GetType().Name;
                }

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), 
                    element, new GUIContent(label), true);
            };

            reorderableList.elementHeightCallback = (index) =>
            {
                return EditorGUI.GetPropertyHeight(stepsProp.GetArrayElementAtIndex(index), true) + 4;
            };

            reorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                if (contextType == null) return;

                var menu = new GenericMenu();
                
                // IPipeLineStep<contextType>를 상속받는 모든 클래스를 동적으로 검색합니다.
                Type stepInterfaceType = typeof(IPipeLineStep<>).MakeGenericType(contextType);
                var stepTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => stepInterfaceType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

                foreach (var type in stepTypes)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        serializedObject.Update();
                        int index = stepsProp.arraySize;
                        stepsProp.InsertArrayElementAtIndex(index);
                        var element = stepsProp.GetArrayElementAtIndex(index);
                        element.managedReferenceValue = Activator.CreateInstance(type);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                
                menu.ShowAsContext();
            };
        }

        public override void OnInspectorGUI()
        {
            if (stepsProp == null)
            {
                DrawDefaultInspector();
                return;
            }

            serializedObject.Update();
            EditorGUILayout.Space();
            reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private Type GetPipeLineContextType(Type type)
        {
            while (type != null && type != typeof(ScriptableObject))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PipeLineSo<>))
                {
                    return type.GetGenericArguments()[0];
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}
