#if UNITY_EDITOR
using HeartOfTheNight.Common;
using UnityEditor;
using UnityEngine;

namespace HeartOfTheNight.CommonEditor
{
    /// <summary>
    /// Adds a menu item that spawns a fully configured one-way platform in the scene:
    /// sprite, BoxCollider2D (used by effector), PlatformEffector2D (one-way),
    /// the OneWayPlatform script, the Ground layer, and the Player layer mask.
    /// </summary>
    public static class OneWayPlatformCreator
    {
        private const string GroundLayerName = "Ground";

        [MenuItem("GameObject/2D Object/One Way Platform", false, 10)]
        public static void CreateOneWayPlatform(MenuCommand menuCommand)
        {
            var go = new GameObject("OneWayPlatform");

            // Sprite so the platform is visible in the editor and at runtime.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetBuiltinSquareSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(4f, 0.4f);
            sr.color = new Color(0.55f, 0.4f, 0.25f);

            // Collider sized to match the visible sprite, driven by the effector.
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(4f, 0.4f);
            col.usedByEffector = true;

            // One-way effector lets the player pass through from below.
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useColliderMask = false;

            // Marks the platform as one-way; drop-through is handled by the player controller.
            go.AddComponent<OneWayPlatform>();

            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            if (groundLayer >= 0) go.layer = groundLayer;

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            PlaceInFrontOfSceneCamera(go);

            Undo.RegisterCreatedObjectUndo(go, "Create One Way Platform");
            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private static void PlaceInFrontOfSceneCamera(GameObject go)
        {
            var view = SceneView.lastActiveSceneView;
            if (view != null && view.camera != null)
            {
                Vector3 p = view.camera.transform.position + view.camera.transform.forward * 10f;
                p.z = 0f;
                go.transform.position = p;
            }
            else
            {
                go.transform.position = Vector3.zero;
            }
        }

        private static Sprite GetBuiltinSquareSprite()
        {
            // Unity ships a built-in unit square sprite used by 2D primitives.
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
                   ?? Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        }
    }
}
#endif
