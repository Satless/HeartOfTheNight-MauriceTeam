using UnityEngine;
using HeartOfTheNight.Common;

/// <summary>
/// Ngọn lửa cháy trên Player. Tự tắt theo số nhịp, dash, hoặc mất mục tiêu.
/// Chạy trên chính object lửa — không phụ thuộc quái còn sống hay coroutine trên quái.
/// </summary>
public class BurnEffectLifetime : MonoBehaviour
{
    private IDamageable _target;
    private Rigidbody2D _playerRb;
    private int _ticksRemaining;
    private float _tickInterval;
    private int _tickDamage;
    private float _dashSpeedThreshold;
    private float _tickTimer;
    private bool _running;

    public static void ApplyOn(
        Transform host,
        GameObject prefab,
        Vector2 offset,
        IDamageable target,
        Rigidbody2D playerRb,
        int burnTicks,
        float timeBetweenTicks,
        int burnDamagePerTick,
        float dashSpeedThreshold)
    {
        if (host == null) return;

        BurnEffectLifetime burn = host.GetComponentInChildren<BurnEffectLifetime>(true);
        if (burn == null)
        {
            ClearOrphanBurnVisuals(host);

            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab, host);
                go.transform.localPosition = new Vector3(offset.x, offset.y, -1f);
            }
            else
            {
                go = new GameObject("BurnEffect");
                go.transform.SetParent(host, false);
                go.transform.localPosition = new Vector3(offset.x, offset.y, -1f);
            }

            go.transform.localRotation = Quaternion.identity;
            burn = go.GetComponent<BurnEffectLifetime>();
            if (burn == null) burn = go.AddComponent<BurnEffectLifetime>();
            AlignSorting(go, host);
        }

        burn.Restart(target, playerRb, burnTicks, timeBetweenTicks, burnDamagePerTick, dashSpeedThreshold);
    }

    private void Restart(
        IDamageable target,
        Rigidbody2D playerRb,
        int burnTicks,
        float timeBetweenTicks,
        int burnDamagePerTick,
        float dashSpeedThreshold)
    {
        _target = target;
        _playerRb = playerRb;
        _ticksRemaining = Mathf.Max(0, burnTicks);
        _tickInterval = Mathf.Max(0.01f, timeBetweenTicks);
        _tickDamage = burnDamagePerTick;
        _dashSpeedThreshold = dashSpeedThreshold;
        _tickTimer = _tickInterval;
        _running = _ticksRemaining > 0;
        if (!_running) Extinguish();
    }

    private void Update()
    {
        if (!_running) return;

        if (_playerRb != null && Mathf.Abs(_playerRb.linearVelocity.x) >= _dashSpeedThreshold)
        {
            Extinguish();
            return;
        }

        var targetBehaviour = _target as Component;
        if (targetBehaviour == null)
        {
            Extinguish();
            return;
        }

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;

        _tickTimer = _tickInterval;
        _target.TakeDamage(_tickDamage);
        _ticksRemaining--;
        if (_ticksRemaining <= 0) Extinguish();
    }

    private void Extinguish()
    {
        _running = false;
        Destroy(gameObject);
    }

    private static void ClearOrphanBurnVisuals(Transform host)
    {
        for (int i = host.childCount - 1; i >= 0; i--)
        {
            Transform child = host.GetChild(i);
            if (child.GetComponent<BurnEffectLifetime>() != null) continue;
            if (child.name.IndexOf("BurnEffect", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            Destroy(child.gameObject);
        }
    }

    private static void AlignSorting(GameObject fire, Transform playerRoot)
    {
        SpriteRenderer fireSr = fire.GetComponent<SpriteRenderer>();
        if (fireSr == null) fireSr = fire.GetComponentInChildren<SpriteRenderer>();
        if (fireSr == null) return;

        SpriteRenderer[] playerSprites = playerRoot.GetComponentsInChildren<SpriteRenderer>();
        int maxOrder = int.MinValue;
        string layerName = fireSr.sortingLayerName;
        bool found = false;

        for (int i = 0; i < playerSprites.Length; i++)
        {
            SpriteRenderer sr = playerSprites[i];
            if (sr == null || sr.gameObject == fire) continue;
            if (sr.sortingOrder > maxOrder)
            {
                maxOrder = sr.sortingOrder;
                layerName = sr.sortingLayerName;
                found = true;
            }
        }

        if (found)
        {
            fireSr.sortingLayerName = layerName;
            fireSr.sortingOrder = maxOrder + 50;
        }
        else
        {
            fireSr.sortingOrder = 32000;
        }
    }
}
