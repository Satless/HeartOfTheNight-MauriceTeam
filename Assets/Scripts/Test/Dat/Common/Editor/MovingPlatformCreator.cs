#if UNITY_EDITOR
using HeartOfTheNight.Common;
using UnityEditor;
using UnityEngine;

namespace HeartOfTheNight.CommonEditor
{
    /// <summary>
    /// Spawns a ready-to-use moving platform: a Kinematic Rigidbody2D platform body
    /// plus two standalone waypoints it travels between. The waypoints are siblings
    /// (not children of the moving body) so they stay fixed in the world.
    /// </summary>
    public static class MovingPlatformCreator
    {
        private const string GroundLayerName = "Ground";

        [MenuItem("GameObject/2D Object/Moving Platform", false, 11)]
        public static void CreateMovingPlatform(MenuCommand menuCommand)
        {
            var group = new GameObject("MovingPlatformGroup");

            var platform = new GameObject("Platform");
            platform.transform.SetParent(group.transform);

            var sr = platform.AddComponent<SpriteRenderer>();
            sr.sprite   = GetBuiltinSquareSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size     = new Vector2(3f, 0.5f);
            sr.color    = new Color(0.3f, 0.45f, 0.6f);

            var col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.5f);

            var rb = platform.AddComponent<Rigidbody2D>();
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            if (groundLayer >= 0) platform.layer = groundLayer;

            var wpA = new GameObject("Waypoint A");
            var wpB = new GameObject("Waypoint B");
            wpA.transform.SetParent(group.transform);
            wpB.transform.SetParent(group.transform);
            wpA.transform.localPosition = new Vector3(-3f, 0f, 0f);
            wpB.transform.localPosition = new Vector3( 3f, 0f, 0f);

            var mover = platform.AddComponent<MovingPlatform>();
            AssignWaypoints(mover, wpA.transform, wpB.transform);

            GameObjectUtility.SetParentAndAlign(group, menuCommand.context as GameObject);
            PlaceInFrontOfSceneCamera(group);

            Undo.RegisterCreatedObjectUndo(group, "Create Moving Platform");
            Selection.activeObject = group;
            EditorGUIUtility.PingObject(group);
        }

        private static void AssignWaypoints(MovingPlatform mover, Transform a, Transform b)
        {
            var so   = new SerializedObject(mover);
            var prop = so.FindProperty("waypoints");
            if (prop == null) return;

            prop.arraySize = 2;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = a;
            prop.GetArrayElementAtIndex(1).objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();
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
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
                   ?? Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        }
    }
}
#endif
