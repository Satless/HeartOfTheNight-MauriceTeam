using UnityEngine;

namespace HeartOfTheNight.Player
{
    public class WeaponEventRelay : MonoBehaviour
{
    private PlayerAttack _playerAttack;

    private void Awake()
    {
        // PlayerAttack nằm ở object cha (hoặc cha của cha)
        _playerAttack = GetComponentInParent<PlayerAttack>();
    }

    /// <summary>
    /// Hàm này sẽ được gọi bằng Animation Event từ trong clip bắn súng.
    /// </summary>
    public void OnAnimationFireEvent()
    {
        if (_playerAttack != null)
        {
            _playerAttack.ExecuteShot();
        }
    }
}
}
