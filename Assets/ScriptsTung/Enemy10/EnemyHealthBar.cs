using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using HeartOfTheNight.Common;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Cài đặt UI")]
    public Image fillImage;
    public Canvas canvas;
    private Quaternion startRotation;

    private Component enemyScript;
    private object currentHealthSource;
    private object maxHealthSource;
    private FieldInfo currentHealthField;
    private FieldInfo maxHealthField;

    private static readonly BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] CurrentHealthNames =
    {
        "currentHealth", "health", "_currentHealth", "hp", "currentHP", "_health"
    };

    private static readonly string[] MaxHealthNames =
    {
        "maxHealth", "_maxHealth", "maxHP", "MaxHealth"
    };

    private static readonly string[] NestedStatsNames =
    {
        "stats", "_data", "data", "_stats"
    };

    void Start()
    {
        if (canvas != null) startRotation = canvas.transform.rotation;

        enemyScript = GetComponentInParent<IDamageable>() as Component;
        if (enemyScript == null)
        {
            Debug.LogWarning($"[{name}] EnemyHealthBar: không tìm thấy IDamageable trên parent.", this);
            return;
        }

        Type type = enemyScript.GetType();
        currentHealthField = FindField(type, CurrentHealthNames);
        currentHealthSource = enemyScript;

        maxHealthField = FindField(type, MaxHealthNames);
        maxHealthSource = enemyScript;

        // Boss / MauTest: maxHealth nằm trong stats / _data
        if (maxHealthField == null)
        {
            foreach (string nestedName in NestedStatsNames)
            {
                FieldInfo nestedField = type.GetField(nestedName, FieldFlags);
                if (nestedField == null) continue;

                object nestedObj = nestedField.GetValue(enemyScript);
                if (nestedObj == null) continue;

                FieldInfo nestedMax = FindField(nestedObj.GetType(), MaxHealthNames);
                if (nestedMax == null) continue;

                maxHealthField = nestedMax;
                maxHealthSource = nestedObj;
                break;
            }
        }

        if (currentHealthField == null)
            Debug.LogWarning($"[{name}] Không tìm thấy biến máu trên {enemyScript.GetType().Name}", this);
        if (maxHealthField == null)
            Debug.LogWarning($"[{name}] Không tìm thấy maxHealth trên {enemyScript.GetType().Name}", this);
    }

    void Update()
    {
        if (enemyScript == null || currentHealthField == null || maxHealthField == null)
            return;
        if (fillImage == null)
            return;

        int curHp = Convert.ToInt32(currentHealthField.GetValue(currentHealthSource));
        int maxHp = Convert.ToInt32(maxHealthField.GetValue(maxHealthSource));
        if (maxHp <= 0) return;

        fillImage.fillAmount = (float)curHp / maxHp;

        if (canvas != null)
            canvas.gameObject.SetActive(curHp > 0);
    }

    void LateUpdate()
    {
        if (canvas == null) return;

        canvas.transform.rotation = startRotation;

        Vector3 fixScale = canvas.transform.localScale;
        if (transform.parent != null && transform.parent.localScale.x < 0)
            fixScale.x = -Mathf.Abs(fixScale.x);
        else
            fixScale.x = Mathf.Abs(fixScale.x);

        canvas.transform.localScale = fixScale;
    }

    private static FieldInfo FindField(Type type, string[] names)
    {
        foreach (string name in names)
        {
            FieldInfo field = type.GetField(name, FieldFlags);
            if (field != null) return field;
        }
        return null;
    }
}
