using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Vong tron mau canh bao duoi chan player truoc khi cot lua xuat hien.
    /// Vong quay nhanh dan theo thoi gian charge; tu sinh hinh anh bang LineRenderer.
    /// Boss tu huy object nay ngay khi cot lua xuat hien.
    /// </summary>
    public class HeartOfTheNightTelegraph : MonoBehaviour
    {
        private const int RingSegments = 48;
        private const int SpokeCount = 3;

        private static readonly Color RingColor = new(0.8f, 0.05f, 0.12f, 0.9f);
        private static readonly Color FillColor = new(1f, 0.1f, 0.2f, 0.45f);

        private Transform spokes;
        private float radius;
        private float chargeTime;
        private float spinStart;
        private float spinEnd;
        private float timer;
        private float currentAngle;

        public void Configure(float ringRadius, float charge, float spinStartSpeed, float spinEndSpeed)
        {
            radius = Mathf.Max(0.1f, ringRadius);
            chargeTime = Mathf.Max(0.05f, charge);
            spinStart = spinStartSpeed;
            spinEnd = spinEndSpeed;
            timer = 0f;
            currentAngle = 0f;

            BuildRing();
            BuildSpokes();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / chargeTime);

            float spinSpeed = Mathf.Lerp(spinStart, spinEnd, t * t);
            currentAngle += spinSpeed * Time.deltaTime;
            if (spokes != null)
                spokes.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            float pulse = 1f + 0.08f * Mathf.Sin(timer * 18f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t) * pulse;
        }

        private LineRenderer NewLine(string childName, int positionCount, bool loop, float width, Color color)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader != null) lr.material = new Material(shader);
            lr.useWorldSpace = false;
            lr.loop = loop;
            lr.positionCount = positionCount;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = color;
            lr.numCapVertices = 2;
            return lr;
        }

        private void BuildRing()
        {
            var ring = NewLine("Ring", RingSegments, true, radius * 0.07f, RingColor);
            for (int i = 0; i < RingSegments; i++)
            {
                float a = (i / (float)RingSegments) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
        }

        private void BuildSpokes()
        {
            var holder = new GameObject("Spokes");
            holder.transform.SetParent(transform, false);
            spokes = holder.transform;

            for (int s = 0; s < SpokeCount; s++)
            {
                float baseAngle = (s / (float)SpokeCount) * Mathf.PI * 2f;
                var spoke = NewLine($"Spoke{s}", 2, false, radius * 0.09f, FillColor);
                spoke.transform.SetParent(spokes, false);
                spoke.SetPosition(0, Vector3.zero);
                spoke.SetPosition(1, new Vector3(Mathf.Cos(baseAngle) * radius, Mathf.Sin(baseAngle) * radius, 0f));
            }
        }
    }
}
