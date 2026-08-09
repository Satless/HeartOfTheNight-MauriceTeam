using UnityEngine;

namespace HeartOfTheNight.Rooms
{
    /// <summary>
    /// Gan len Blocker: player va cham cua khoa thi thu mo bang chia.
    /// </summary>
    public class DoorUnlockSensor : MonoBehaviour
    {
        private RoomDoor door;
        private string playerTag = "Player";

        public void Init(RoomDoor owner, string tag = "Player")
        {
            door = owner;
            playerTag = tag;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryUnlock(collision?.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryUnlock(other);
        }

        private void TryUnlock(Collider2D other)
        {
            if (door == null || other == null) return;
            if (!IsPlayer(other)) return;
            door.TryUnlockWithKey();
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other.CompareTag(playerTag)) return true;
            return other.transform.root.CompareTag(playerTag);
        }
    }
}
