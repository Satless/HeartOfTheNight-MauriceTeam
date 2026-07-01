using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Thêm [TagSelector] trước biến kiểu chuỗi (string) để biến nó thành một dropdown chọn Tag trong Inspector.
/// LƯU Ý: Nếu project có cài thư viện Cinemachine, thì KHÔNG CẦN dùng file này. Hãy dùng [Cinemachine.TagField] có sẵn để tối ưu.
/// </summary>
public class TagSelectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Hiện dropdown menu lấy danh sách toàn bộ Tag đang có trong game
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            EditorGUI.EndProperty();
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
