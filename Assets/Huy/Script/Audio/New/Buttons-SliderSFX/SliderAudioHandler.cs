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
        AudioEvents.TriggerSound2D(audioCategory, audioGroup, audioName);
    }
}
