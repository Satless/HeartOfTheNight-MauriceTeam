using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Collections;
using HeartOfTheNight.Common;

public class AutoUniversalHealthBar : MonoBehaviour
{
    [Header("Cài đặt UI Minimalist")]
    public Canvas canvas;
    public Image fillImage;

    [Header("Màu sắc & Nhấp nháy")]
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color hitFlashColor = Color.white;

    [Header("Cài đặt Thời gian")]
    public float catchupSpeed = 10f;
    public float timeToHide = 3f;

    private static readonly string[] CurrentHpNames = { "currentHealth", "health", "_currentHealth" };
    private static readonly string[] MaxHpNames = { "maxHealth", "_maxHealth" };
    private const BindingFlags HpFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private Quaternion startRotation;
    private Vector3 originalCanvasScale;

    private Component enemyScript;
    private FieldInfo currentHpField;
    private FieldInfo maxHpField;

    private float targetFill = 1f;
    private float hideTimer = 0f;
    private Color currentBaseColor;

    private bool isFlashing = false;
    private bool isDying = false;
    private bool hasBaseline = false;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (canvas != null)
        {
            startRotation = canvas.transform.rotation;
            originalCanvasScale = canvas.transform.localScale;
            if (originalCanvasScale.x <= 0.0001f || originalCanvasScale.y <= 0.0001f)
                originalCanvasScale = new Vector3(0.01f, 0.01f, 0.01f);

            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 20;

            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        enemyScript = GetComponentInParent<IDamageable>() as Component;
        if (enemyScript != null)
        {
            System.Type type = enemyScript.GetType();
            currentHpField = FindField(type, CurrentHpNames);
            maxHpField = FindField(type, MaxHpNames);
        }
    }

    void Update()
    {
        if (isDying || enemyScript == null || currentHpField == null) return;

        if (!TryReadHp(out int curHp, out int maxHp) || maxHp <= 0) return;

        float newFill = Mathf.Clamp01((float)curHp / maxHp);

        // Freeze spawn tắt script quái trước Start() → currentHealth còn 0.
        // Đừng tưởng quái chết rồi co canvas về (0,0,z).
        if (!hasBaseline)
        {
            if (curHp <= 0) return;

            targetFill = newFill;
            if (fillImage != null) fillImage.fillAmount = newFill;
            hasBaseline = true;
            return;
        }

        if (newFill < targetFill - 0.0001f)
        {
            ShowBar();
            hideTimer = 0f;

            if (curHp <= 0)
            {
                StartCoroutine(TVEffectRoutine());
                targetFill = newFill;
                return;
            }

            StartCoroutine(HitFlashRoutine());
        }

        targetFill = newFill;

        if (targetFill > 0.6f) currentBaseColor = highHealthColor;
        else if (targetFill > 0.3f) currentBaseColor = mediumHealthColor;
        else currentBaseColor = lowHealthColor;

        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * catchupSpeed);
            if (!isFlashing) fillImage.color = currentBaseColor;
        }

        if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= timeToHide)
                canvasGroup.alpha = 0f;
        }
    }

    void LateUpdate()
    {
        if (canvas == null || canvasGroup == null || canvasGroup.alpha <= 0f) return;

        canvas.transform.rotation = startRotation;
        if (isDying) return;

        Vector3 fixScale = canvas.transform.localScale;
        Transform parent = transform.parent;
        if (parent != null && parent.localScale.x < 0)
            fixScale.x = -Mathf.Abs(fixScale.x);
        else
            fixScale.x = Mathf.Abs(fixScale.x);
        canvas.transform.localScale = fixScale;
    }

    private void ShowBar()
    {
        if (canvas == null || canvasGroup == null) return;
        if (canvasGroup.alpha > 0f) return;

        canvas.transform.localScale = originalCanvasScale;
        canvasGroup.alpha = 1f;
    }

    private bool TryReadHp(out int curHp, out int maxHp)
    {
        curHp = 0;
        maxHp = 0;
        if (!TryReadInt(currentHpField, out curHp)) return false;

        if (maxHpField != null && TryReadInt(maxHpField, out maxHp) && maxHp > 0)
            return true;

        maxHp = Mathf.Max(curHp, 1);
        return true;
    }

    private bool TryReadInt(FieldInfo field, out int value)
    {
        value = 0;
        if (field == null || enemyScript == null) return false;

        object raw = field.GetValue(enemyScript);
        if (raw == null) return false;

        try
        {
            value = Convert.ToInt32(raw);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static FieldInfo FindField(System.Type type, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            FieldInfo field = type.GetField(names[i], HpFlags);
            if (field != null) return field;
        }
        return null;
    }

    private IEnumerator HitFlashRoutine()
    {
        isFlashing = true;
        if (fillImage != null) fillImage.color = hitFlashColor;
        yield return new WaitForSeconds(0.08f);
        isFlashing = false;
    }

    private IEnumerator TVEffectRoutine()
    {
        isDying = true;
        float timer = 0;
        Vector3 startScale = canvas != null ? canvas.transform.localScale : originalCanvasScale;
        Vector3 flatScale = new Vector3(startScale.x, 0f, startScale.z);

        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            if (canvas != null)
                canvas.transform.localScale = Vector3.Lerp(startScale, flatScale, timer / 0.1f);
            yield return null;
        }

        timer = 0;
        Vector3 dotScale = new Vector3(0f, 0f, startScale.z);

        while (timer < 0.15f)
        {
            timer += Time.deltaTime;
            if (canvas != null)
                canvas.transform.localScale = Vector3.Lerp(flatScale, dotScale, timer / 0.15f);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }
}
