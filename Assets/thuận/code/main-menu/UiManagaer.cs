using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject codexPanel;
    public GameObject creditPanel;
    public GameObject settingPanel;
    public GameObject controlsPanel;
    public GameObject saveSlotPanel;

    [Header("Codex")]
    public TMP_Text contentText;

    private void Start()
    {
        CloseAllPanels();
    }

    void CloseAllPanels()
    {
        codexPanel.SetActive(false);
        creditPanel.SetActive(false);
        settingPanel.SetActive(false);
        controlsPanel.SetActive(false);
        saveSlotPanel.SetActive(false);
    }

    // ===== MENU =====

    public void OpenCodex()
    {
        CloseAllPanels();
        codexPanel.SetActive(true);
    }

    public void OpenCredit()
    {
        CloseAllPanels();
        creditPanel.SetActive(true);
    }

    public void OpenSetting()
    {
        CloseAllPanels();
        settingPanel.SetActive(true);
    }

    // ===== SETTINGS =====

    public void OpenControls()
    {
        controlsPanel.SetActive(true);
        saveSlotPanel.SetActive(false);
    }

    public void OpenSaveSlot()
    {
        saveSlotPanel.SetActive(true);
        controlsPanel.SetActive(false);
    }

    // ===== CLOSE =====

    public void CloseCodex()
    {
        codexPanel.SetActive(false);
    }

    public void CloseCredit()
    {
        creditPanel.SetActive(false);
    }

    public void CloseSetting()
    {
        settingPanel.SetActive(false);
        controlsPanel.SetActive(false);
        saveSlotPanel.SetActive(false);
    }

    // ===== CODEX =====

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
}