using TMPro;
using UnityEngine;

public class CodexUI : MonoBehaviour
{
    public GameObject MenuPanel;
    public GameObject codexPanel;
    public GameObject CreditPanel;
    public GameObject SettingPanel;
    public GameObject ControlsPanel;
    public GameObject SaveLostPanel;
    public TMP_Text contentText;


    public void OpenMenu()
    {
        MenuPanel.SetActive(true);
    }
    public void OpenCodex()
    {
        codexPanel.SetActive(true);
    }
    public void OpenCredit()
    {
        CreditPanel.SetActive(true);
        MenuPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
        ControlsPanel.SetActive(false);
    }
    public void OpenSetting()
    {
        SettingPanel.SetActive(true);
        MenuPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
        ControlsPanel.SetActive(false);
    }
    public void OpenControls()
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
    }


    public void CloseMenu()
    {
        MenuPanel.SetActive(false);
    }
    public void CloseCredit()
    {
        CreditPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }
    public void CloseCodex()
    {
        codexPanel.SetActive(false);
    }
    public void CloseSetting()
    {
        MenuPanel.SetActive(true);
        SettingPanel.SetActive(false);
        ControlsPanel.SetActive(false);
        SaveLostPanel.SetActive(false);
    }
    public void CloseControls()
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
    }

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