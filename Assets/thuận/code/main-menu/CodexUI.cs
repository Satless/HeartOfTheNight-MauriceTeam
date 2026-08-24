using TMPro;
using UnityEngine;

public class CodexUI : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject saveSlotPanel;
    public GameObject codexPanel;
    public GameObject CreditPanel;
    public GameObject SettingPanel;
    public GameObject ControlsPanel;
    
    public TMP_Text contentText;
    public GameObject AccountPanel;

    private void Awake()
    {
        if (saveSlotPanel == null)
            saveSlotPanel = FindChildByName("LoadSavePanel");
        if (AccountPanel == null)
            AccountPanel = FindChildByName("AccountPanel");
        if (codexPanel == null)
            codexPanel = FindChildByName("CodexPanel");
    }

    public void OpenMenu()
    {
        SetActiveSafe(menuPanel, true);
    }

    public void OpenSaveSlot()
    {
        ShowPanel(saveSlotPanel);
    }
    public void OpenCodex()
    {
        ShowPanel(codexPanel);
    }
    public void OpenCredit()
    {
        ShowPanel(CreditPanel);
    }
    public void OpenSetting()
    {
        ShowPanel(SettingPanel);
    }

    public void OpenAccount()
    {
        ShowPanel(AccountPanel);
    }
    /*public void OpenControls()
    {
        SettingPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
        ControlsPanel.SetActive(true);
    }
    public void OpenSaveLost()
    {
        ControlsPanel.SetActive(false);
        SettingPanel.SetActive(false);
        SaveLostPanel.SetActive(true);
    }*/




    public void CloseMenu()
    {
        SetActiveSafe(menuPanel, false);
    }
    public void CloseCredit()
    {
        ReturnToMenu(CreditPanel);
    }

    public void CloseSaveSlot()
    {
        ReturnToMenu(saveSlotPanel);
    }
    public void CloseCodex()
    {
        ReturnToMenu(codexPanel);
    }
    public void CloseSetting()
    {
        ReturnToMenu(SettingPanel);
    }

    public void CloseAccount()
    {
        ReturnToMenu(AccountPanel);
    }
   /* public void CloseControls()
    {
        SettingPanel.SetActive(false);
        ControlsPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
    }
    public void CloseSaveLost()
    {
        SettingPanel.SetActive(false);
        ControlsPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
    }*/

    public void ShowCharacters()
    {
        contentText.text =
            "Main Character\n" +
            "Ghost Girl\n" +
            "Nurse";
    }

    public void ShowMonsters()
    {
        contentText.text =
            "Shadow Walker\n" +
            "Crawler\n" +
            "Lost Soul";
    }

    public void ShowDocuments()
    {
        contentText.text =
            "Patient Report #01\n" +
            "Diary Page\n" +
            "Medical Record";
    }

    public void ShowLocations()
    {
        contentText.text =
            "Hospital\n" +
            "Basement\n" +
            "Church";
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[CodexUI] Panel chưa được gán trên UIManager.", this);
            return;
        }

        SetActiveSafe(panel, true);
        SetActiveSafe(menuPanel, false);
    }

    private void ReturnToMenu(GameObject panel)
    {
        SetActiveSafe(panel, false);
        SetActiveSafe(menuPanel, true);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

    private GameObject FindChildByName(string objectName)
    {
        Transform root = menuPanel != null ? menuPanel.transform.root : transform;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Trim() == objectName)
                return children[i].gameObject;
        }

        return null;
    }
}