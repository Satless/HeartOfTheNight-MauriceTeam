using HeartOfTheNight.Hung;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Sau khi bật DEMO, vào màn được chọn thì đủ súng + chìa.</summary>
public sealed class DemoUnlockRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindFirstObjectByType<DemoUnlockRuntime>() != null)
            return;

        var go = new GameObject("DemoUnlockRuntime");
        DontDestroyOnLoad(go);
        go.AddComponent<DemoUnlockRuntime>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryApplyForActiveScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryApplyForActiveScene();
    }

    private void TryApplyForActiveScene()
    {
        if (!DemoUnlock.IsArmed)
            return;
        if (!DataManager.IsLevelScene(SceneManager.GetActiveScene().name))
            return;

        StopAllCoroutines();
        StartCoroutine(ApplyAfterSpawn());
    }

    private System.Collections.IEnumerator ApplyAfterSpawn()
    {
        yield return null;
        yield return null;
        DemoUnlock.ApplyLiveWeapons();
        DemoUnlock.EnsureDemoKeys();

        var data = DataManager.Instance != null ? DataManager.Instance.Data : null;
        if (data != null)
            data.CaptureCheckpointWorldState();
    }
}
