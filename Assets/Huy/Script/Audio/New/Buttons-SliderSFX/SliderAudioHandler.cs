using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderAudioHandler : MonoBehaviour, IPointerUpHandler
{
    [Header("Audio Settings")]
    public string audioCategory = "UI";
    public string audioGroup = "Slider";
    public string audioName = "StopDrag";

    public void OnPointerUp(PointerEventData eventData)
    {
        // Kiểm tra SoundManager tồn tại
        if (SoundManager_New.Instance != null)
        {
            AudioEvents.TriggerSound2D(audioCategory, audioGroup, audioName);

            // Tìm và bắt AudioSource vừa phát sound UI phải ignore trạng thái Pause
            AudioSource[] sources = SoundManager_New.Instance.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source.isPlaying)
                {
                    source.ignoreListenerPause = true;
                }
            }
        }
    }
}