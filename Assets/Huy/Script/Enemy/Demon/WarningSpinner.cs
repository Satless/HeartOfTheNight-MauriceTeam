using UnityEngine;

public class WarningSpinner : MonoBehaviour
{
    private float speed = 100f;

    void Update()
    {
        //speed up the the warning ring
        speed += 200f * Time.deltaTime;
        transform.Rotate(0, 0, speed * Time.deltaTime);
    }
}