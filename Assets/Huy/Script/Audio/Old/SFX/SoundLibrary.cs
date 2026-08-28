//using UnityEngine;



//// level 1: object Name / Category
//[System.Serializable]
//public struct SoundCategory
//{
//    public string categoryID;   // name of the object (Demon, Player, UI,...)
//    public SoundEffect[] soundEffects;
//}

//// Level 2: SFX Action and random clips option 
//[System.Serializable]
//public struct SoundEffect
//{
//    public string groupID;      // name for sfx action/feature (Walk, attack,...)
//    public AudioClip[] clips;   // randomly choose 1 sfx from list
//}


//public class SoundLibrary : MonoBehaviour
//{
//    public SoundCategory[] categories;

//    // find clips based on 2 levels: Target_Name & SFX_Name
//    public AudioClip GetClipFromName(string categoryID, string soundName)
//    {
//        foreach (var category in categories)
//        {
//            if (category.categoryID == categoryID)
//            {
//                foreach (var soundEffect in category.soundEffects)
//                {
//                    if (soundEffect.groupID == soundName)
//                    {
//                        if (soundEffect.clips != null && soundEffect.clips.Length > 0)
//                        {
//                            // randomly choose 1 clip
//                            return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
//                        }
//                    }
//                }
//            }
//        }

//        Debug.LogWarning($"[SoundLibrary] SFX not found '{soundName}' belongs to category '{categoryID}'!");
//        return null;
//    }
//}

////change the volume with this 
//// https://assetstore.unity.com/packages/tools/audio/easy-audio-cutter-316085
////thank god i dont have to use another app or website to just simply cut out and adjust the volume of the files

////oh also use: 
//// SoundManager.Instance.PlaySound3D("Name", "Action/Feature",transform.position);
////to play the sfx where you want, put it somewhere ideal