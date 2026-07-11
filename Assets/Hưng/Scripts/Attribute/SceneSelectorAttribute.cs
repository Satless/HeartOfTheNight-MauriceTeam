using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.IO;
#endif

/// <summary>
/// Thêm [SceneSelector] trước biến kiểu chuỗi (string) để tạo Dropdown chọn tên Màn chơi.
/// Tự động lấy danh sách các Scene đã được tick xanh trong cửa sổ Build Settings.
/// </summary>
public class SceneSelectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneSelectorAttribute))]
public class SceneSelectorPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Chỉ hoạt động trên biến kiểu chuỗi (String)
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1. Lấy danh sách các Scene đã được nạp vào Build Settings
            List<string> sceneNames = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    // Lấy tên file, bỏ đi phần đường dẫn và đuôi ".unity"
                    sceneNames.Add(Path.GetFileNameWithoutExtension(scene.path));
                }
            }

            // 2. Nếu chưa có Scene nào trong Build Settings thì báo lỗi nhẹ
            if (sceneNames.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, "Lỗi: Chưa add Scene vào Build Settings!");
            }
            else
            {
                // 3. Tìm vị trí của tên Scene đang lưu trong mảng (mặc định là 0 nếu không thấy)
                int index = Mathf.Max(0, sceneNames.IndexOf(property.stringValue));

                // 4. Vẽ giao diện Popup Dropdown
                index = EditorGUI.Popup(position, label.text, index, sceneNames.ToArray());

                // 5. Gán lại giá trị vừa chọn cho biến
                property.stringValue = sceneNames[index];
            }

            EditorGUI.EndProperty();
        }
        else
        {
            // Báo lỗi nếu gắn [SceneSelector] vào biến int, float...
            EditorGUI.LabelField(position, label.text, "Lỗi: [SceneSelector] chỉ dùng cho kiểu String!");
        }
    }
}
#endif
