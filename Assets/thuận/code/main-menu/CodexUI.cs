using System.Security.Cryptography;
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


    private void Start()
    {
        //MusicManager.Instance.PlayMusic("");
    }

    public void OpenMenu()
    {
        menuPanel.SetActive(true);
    }

    public void OpenSaveSlot()
    {
        saveSlotPanel.SetActive(true);
        menuPanel.SetActive(false);
    }
    public void OpenCodex()
    {
        codexPanel.SetActive(true);
        menuPanel.SetActive(false);
    }
    public void OpenCredit()
    {
        CreditPanel.SetActive(true);
        menuPanel.SetActive(false);
    }
    public void OpenSetting()
    {
        SettingPanel.SetActive(true);
        menuPanel.SetActive(false);

    }

    public void OpenAccount()
    {
        if (AccountPanel != null)
            AccountPanel.SetActive(true);
        menuPanel.SetActive(false);
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
        menuPanel.SetActive(false);
    }
    public void CloseCredit()
    {
        CreditPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void CloseSaveSlot()
    {
        saveSlotPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    public void CloseCodex()
    {
        codexPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    public void CloseSetting()
    {
        SettingPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void CloseAccount()
    {
        if (AccountPanel != null)
            AccountPanel.SetActive(false);
        menuPanel.SetActive(true);
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
}