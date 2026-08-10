using System.Collections;
using HeartOfTheNight.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D blockerCollider;
    [SerializeField] private Collider2D transitionTrigger;

    [Header("Key Lock")]
    [Tooltip("None = cua thuong. Blue/Red = can dung chia cung mau.")]
    [SerializeField] private KeyType requiredKey = KeyType.None;
    [Tooltip("Tieu thu 1 chia khi mo khoa lan dau.")]
    [SerializeField] private bool consumeKeyOnUnlock = true;
    [Tooltip("ID de luu trang thai da mo. De trong = tu tao theo Scene + ten object.")]
    [SerializeField] private string doorSaveId = "";
    [SerializeField] private string playerTag = "Player";

    [Header("Open Timing")]
    [Tooltip("Thoi gian cho animation Open xong moi cho di qua. < 0 = lay tu clip Animator.")]
    [SerializeField] private float openPassageDelay = -1f;

    private bool isOpen = false;
    private bool keyRequirementMet = true;
    private Coroutine openSequenceRoutine;

    public KeyType RequiredKey => requiredKey;
    public bool IsOpen => isOpen;
    public bool IsKeyRequirementMet => keyRequirementMet;

    private void Awake()
    {
        EnsureUnlockSensor();
        ApplySavedUnlockState();

        // Cua can chia va chua mo khoa: ep dong ngay (tranh lo hong 1 frame truoc Start).
        if (requiredKey != KeyType.None && !keyRequirementMet)
            Close();
    }

    private void EnsureUnlockSensor()
    {
        if (requiredKey == KeyType.None || blockerCollider == null) return;

        var sensor = blockerCollider.GetComponent<DoorUnlockSensor>();
        if (sensor == null)
            sensor = blockerCollider.gameObject.AddComponent<DoorUnlockSensor>();
        sensor.Init(this, playerTag);
    }

    private void ApplySavedUnlockState()
    {
        if (requiredKey == KeyType.None)
        {
            keyRequirementMet = true;
            return;
        }

        keyRequirementMet = PlayerKeyInventory.IsDoorUnlocked(GetDoorSaveId());
        if (keyRequirementMet)
            Open(instant: true);
    }

    private string GetDoorSaveId()
    {
        if (!string.IsNullOrEmpty(doorSaveId))
            return doorSaveId;

        return SceneManager.GetActiveScene().name + "_" + gameObject.name;
    }

    /// <summary>
    /// Goi khi player cham cua khoa. Co chia dung mau thi mo.
    /// </summary>
    public bool TryUnlockWithKey()
    {
        if (requiredKey == KeyType.None)
            return true;

        if (keyRequirementMet)
        {
            if (!isOpen) Open();
            return true;
        }

        if (!PlayerKeyInventory.Has(requiredKey))
        {
            Debug.Log($"[{name}] Can chia {requiredKey} de mo cua.", this);
            return false;
        }

        if (consumeKeyOnUnlock && !PlayerKeyInventory.TryConsume(requiredKey))
            return false;

        keyRequirementMet = true;
        PlayerKeyInventory.MarkDoorUnlocked(GetDoorSaveId());
        Open();
        return true;
    }

    /// <summary>
    /// Mo cua. Mac dinh choi animation truoc, roi moi tat blocker / bat transition.
    /// </summary>
    /// <param name="instant">true = mo ngay (load save / tele toi cua dich).</param>
    public void Open(bool instant = false)
    {
        // Cua khoa chua duoc mo bang chia: khong cho Open() (ke ca RoomSpawnController).
        if (requiredKey != KeyType.None && !keyRequirementMet)
            return;

        if (isOpen) return;
        isOpen = true;

        if (openSequenceRoutine != null)
        {
            StopCoroutine(openSequenceRoutine);
            openSequenceRoutine = null;
        }

        if (anim != null) anim.SetTrigger("Open");

        if (instant)
        {
            SetPassageOpen(true);
            return;
        }

        // Giu blocker trong luc anim: player dung xem cua mo, khong tele xen ngang.
        openSequenceRoutine = StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        yield return new WaitForSeconds(GetOpenPassageDelay());

        openSequenceRoutine = null;
        if (!isOpen) yield break;

        SetPassageOpen(true);
    }

    private float GetOpenPassageDelay()
    {
        if (openPassageDelay >= 0f)
            return openPassageDelay;

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var clips = anim.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip != null && clip.name.IndexOf("Open", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip.length;
            }
        }

        return 1.4f;
    }

    private void SetPassageOpen(bool open)
    {
        if (blockerCollider != null) blockerCollider.enabled = !open;
        if (transitionTrigger != null) transitionTrigger.enabled = open;
    }

    public void Close()
    {
        if (openSequenceRoutine != null)
        {
            StopCoroutine(openSequenceRoutine);
            openSequenceRoutine = null;
        }

        if (isOpen)
        {
            if (anim != null) anim.SetTrigger("Close");
        }

        isOpen = false;
        SetPassageOpen(false);
    }
}
