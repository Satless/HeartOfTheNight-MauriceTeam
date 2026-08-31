using UnityEngine;

// Tầng 3: Danh sách Clip âm thanh cho từng hành động
[System.Serializable]
public struct SoundAction
{
    public string actionID;     // Tên chức năng (Attack, Walk, Jump, Die...)
    public AudioClip[] clips;   // Các file audio (chọn ngẫu nhiên)
}

// Tầng 2: Tên đối tượng cụ thể trong danh mục
[System.Serializable]
public struct SoundSubCategory
{
    public string subCategoryID; // Tên quái/nhân vật (Goblin, Orc, Dragon, Knight...)
    public SoundAction[] actions; // Danh sách các hành động của đối tượng này
}

// Tầng 1: Danh mục tổng quát
[System.Serializable]
public struct SoundCategory
{
    public string categoryID;   // Nhóm lớn (Player, Monster, UI, Environment...)
    public SoundSubCategory[] subCategories; // Danh sách các đối tượng thuộc nhóm
}

public class SoundLibrary_New : MonoBehaviour
{
    public SoundCategory[] categories;

    // Tìm clip theo đủ 3 tầng: Category -> SubCategory -> Action
    public AudioClip GetClipFromName(string categoryID, string subCategoryID, string actionName)
    {
        foreach (var category in categories)
        {
            if (category.categoryID == categoryID)
            {
                if (category.subCategories == null) continue;

                foreach (var subCategory in category.subCategories)
                {
                    if (subCategory.subCategoryID == subCategoryID)
                    {
                        if (subCategory.actions == null) continue;

                        foreach (var action in subCategory.actions)
                        {
                            if (action.actionID == actionName)
                            {
                                if (action.clips == null || action.clips.Length == 0)
                                    return null;

                                return action.clips[Random.Range(0, action.clips.Length)];
                            }
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"[SoundLibrary] Không tìm thấy SFX cho đường dẫn: '{categoryID}/{subCategoryID}/{actionName}'!");
        return null;
    }
}