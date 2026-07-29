using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Thêm [ReadOnly] trước biến để hiển thị nó trên Inspector (nhằm mục đích theo dõi thông số realtime) nhưng KHÔNG cho phép chỉnh sửa.
/// Rất an toàn để bảo vệ các biến đếm giờ (timers) và trạng thái FSM (states).
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Vô hiệu hóa GUI (làm mờ ô nhập liệu, chặn tương tác chuột/phím)
        GUI.enabled = false;
        
        // 2. Vẽ property đó ra Inspector như bình thường.
        // Tham số "true" ở cuối giúp hỗ trợ vẽ đúng các kiểu dữ liệu phức tạp như Mảng (Array), List, Struct, Class.
        EditorGUI.PropertyField(position, property, label, true); 
        
        // 3. Quan trọng: Phải bật lại GUI để các biến bên dưới nó không bị khóa lây!
        GUI.enabled = true;
    }

    // Ghi đè hàm này để Unity tính toán đúng chiều cao của property trên Inspector.
    // Nếu thiếu hàm này, khi gắn [ReadOnly] cho Array hoặc Vector3, nó sẽ bị đè chữ lên biến bên dưới.
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
