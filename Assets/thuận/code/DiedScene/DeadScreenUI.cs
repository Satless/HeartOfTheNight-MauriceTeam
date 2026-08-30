using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay YOU DIED. Bật khi player chết, bấm R để hồi sinh tại checkpoint.
/// Gắn trên prefab DeadScreen (có thể để inactive sẵn).
/// </summary>
public class DeadScreenUI : MonoBehaviour
{
    private const int OverlaySortingOrder = 600;

    [SerializeField] private KeyCode restartKey = KeyCode.R;

    private bool _waitingForRestart;

    public void Show()
    {
        gameObject.SetActive(true);
        EnsureDrawOnTop();
        transform.SetAsLastSibling();
        _waitingForRestart = true;
        if (PauseUI.Instance != null)
            PauseUI.Instance.DismissForExternalFlow();
        Time.timeScale = 0f;
    }

    /// <summary>
    /// DeadScreen nằm trong KeyHUD (không override sorting) nên HUD Overlay khác
    /// (thanh máu boss, v.v.) có thể vẽ đè lên. Nested Canvas riêng để luôn ở trên.
    /// </summary>
    private void EnsureDrawOnTop()
    {
        var overlay = GetComponent<Canvas>();
        if (overlay == null)
            overlay = gameObject.AddComponent<Canvas>();

        overlay.enabled = true;
        overlay.overrideSorting = true;
        overlay.sortingOrder = OverlaySortingOrder;

        var parents = GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < parents.Length; i++)
        {
            if (parents[i] != overlay)
            {
                overlay.sortingLayerID = parents[i].sortingLayerID;
                break;
            }
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
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
