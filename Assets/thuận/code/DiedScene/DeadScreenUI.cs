using UnityEngine;

/// <summary>
/// Overlay YOU DIED. Bật khi player chết, bấm R để hồi sinh tại checkpoint.
/// Gắn trên prefab DeadScreen (có thể để inactive sẵn).
/// </summary>
public class DeadScreenUI : MonoBehaviour
{
    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private bool _waitingForRestart;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _waitingForRestart = true;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!_waitingForRestart) return;
        if (!Input.GetKeyDown(restartKey)) return;

        ConfirmRestart();
    }

    private void ConfirmRestart()
    {
        if (!_waitingForRestart) return;
        _waitingForRestart = false;
        Time.timeScale = 1f;

        if (HeartOfTheNight.Hung.DataManager.Instance != null)
            HeartOfTheNight.Hung.DataManager.Instance.RespawnAtCheckpoint();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
