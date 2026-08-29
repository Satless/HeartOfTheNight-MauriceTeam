using UnityEngine;
using TMPro;

public class PaperInteraction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject readPrompt;
    [SerializeField] private GameObject paperDialog;
    [SerializeField] private TMP_Text dialogText;

    [Header("Nội dung")]
    [TextArea(3, 10)]
    [SerializeField] private string message;

    [Header("Tốc độ chữ")]
    [SerializeField] private float textSpeed = 0.04f;

    private bool playerInside = false;
    private bool isReading = false;

    private Coroutine typingCoroutine;

    private void Start()
    {
        readPrompt.SetActive(false);
        paperDialog.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.E))
        {
            if (!isReading)
            {
                OpenPaper();
            }
            else
            {
                ClosePaper();
            }
        }
    }

    private void OpenPaper()
    {
        isReading = true;

        paperDialog.SetActive(true);
        readPrompt.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

    private void ClosePaper()
    {
        isReading = false;

        paperDialog.SetActive(false);

        if (playerInside)
            readPrompt.SetActive(true);
    }

    private System.Collections.IEnumerator TypeText()
    {
        dialogText.text = "";

        foreach (char letter in message)
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (!isReading)
            readPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        readPrompt.SetActive(false);

        if (isReading)
        {
            isReading = false;
            paperDialog.SetActive(false);
        }
    }
}