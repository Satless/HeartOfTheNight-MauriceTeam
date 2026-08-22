using UnityEditor;
using UnityEngine;

namespace HeartOfTheNight.UI.Editor
{
    [CustomEditor(typeof(DamagePopup))]
    public class DamagePopupEditor : UnityEditor.Editor
    {
        private SerializedProperty lifeTimeProp;
        private SerializedProperty useUnscaledTimeProp;
        private SerializedProperty spawnJitterRadiusProp;
        private SerializedProperty moveUpSpeedProp;
        private SerializedProperty moveSidewaysRandomProp;
        private SerializedProperty velocityDampingProp;
        
        private SerializedProperty startScaleProp;
        private SerializedProperty punchScaleProp;
        private SerializedProperty normalScaleProp;
        private SerializedProperty punchDurationPercentProp;
        private SerializedProperty settleDurationPercentProp;
        
        private SerializedProperty fadeStartPercentProp;
        
        private SerializedProperty colorModeProp;
        private SerializedProperty singleColorProp;
        private SerializedProperty randomColorsProp;
        private SerializedProperty gradientColorProp;
        private SerializedProperty randomGradientsProp;
        
        private SerializedProperty fontSizeModeProp;
        private SerializedProperty fixedFontSizeProp;
        private SerializedProperty randomFontSizeRangeProp;
        
        private SerializedProperty rotationModeProp;
        private SerializedProperty fixedRotationZProp;
        private SerializedProperty randomRotationRangeProp;

        private void OnEnable()
        {
            lifeTimeProp = serializedObject.FindProperty("lifeTime");
            useUnscaledTimeProp = serializedObject.FindProperty("useUnscaledTime");
            spawnJitterRadiusProp = serializedObject.FindProperty("spawnJitterRadius");
            moveUpSpeedProp = serializedObject.FindProperty("moveUpSpeed");
            moveSidewaysRandomProp = serializedObject.FindProperty("moveSidewaysRandom");
            velocityDampingProp = serializedObject.FindProperty("velocityDamping");

            startScaleProp = serializedObject.FindProperty("startScale");
            punchScaleProp = serializedObject.FindProperty("punchScale");
            normalScaleProp = serializedObject.FindProperty("normalScale");
            punchDurationPercentProp = serializedObject.FindProperty("punchDurationPercent");
            settleDurationPercentProp = serializedObject.FindProperty("settleDurationPercent");

            fadeStartPercentProp = serializedObject.FindProperty("fadeStartPercent");

            colorModeProp = serializedObject.FindProperty("colorMode");
            singleColorProp = serializedObject.FindProperty("singleColor");
            randomColorsProp = serializedObject.FindProperty("randomColors");
            gradientColorProp = serializedObject.FindProperty("gradientColor");
            randomGradientsProp = serializedObject.FindProperty("randomGradients");

            fontSizeModeProp = serializedObject.FindProperty("fontSizeMode");
            fixedFontSizeProp = serializedObject.FindProperty("fixedFontSize");
            randomFontSizeRangeProp = serializedObject.FindProperty("randomFontSizeRange");

            rotationModeProp = serializedObject.FindProperty("rotationMode");
            fixedRotationZProp = serializedObject.FindProperty("fixedRotationZ");
            randomRotationRangeProp = serializedObject.FindProperty("randomRotationRange");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(lifeTimeProp);
            EditorGUILayout.PropertyField(useUnscaledTimeProp);
            EditorGUILayout.PropertyField(spawnJitterRadiusProp);
            EditorGUILayout.PropertyField(moveUpSpeedProp);
            EditorGUILayout.PropertyField(moveSidewaysRandomProp);
            EditorGUILayout.PropertyField(velocityDampingProp);
            
            EditorGUILayout.PropertyField(startScaleProp);
            EditorGUILayout.PropertyField(punchScaleProp);
            EditorGUILayout.PropertyField(normalScaleProp);
            EditorGUILayout.PropertyField(punchDurationPercentProp);
            EditorGUILayout.PropertyField(settleDurationPercentProp);

            EditorGUILayout.PropertyField(fadeStartPercentProp);

            EditorGUILayout.PropertyField(colorModeProp);
            
            // Conditional Color Fields
            DamageColorMode colorMode = (DamageColorMode)colorModeProp.enumValueIndex;
            EditorGUI.indentLevel++;
            if (colorMode == DamageColorMode.Single)
            {
                EditorGUILayout.PropertyField(singleColorProp);
            }
            else if (colorMode == DamageColorMode.Random)
            {
                EditorGUILayout.PropertyField(randomColorsProp);
            }
            else if (colorMode == DamageColorMode.GradientOverTime || colorMode == DamageColorMode.RandomFromGradient)
            {
                EditorGUILayout.PropertyField(gradientColorProp);
            }
            else if (colorMode == DamageColorMode.RandomGradientOverTime)
            {
                EditorGUILayout.PropertyField(randomGradientsProp, true); // true để vẽ danh sách (Array) đầy đủ
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(fontSizeModeProp);

            // Conditional Font Size Fields
            DamageFontSizeMode fontSizeMode = (DamageFontSizeMode)fontSizeModeProp.enumValueIndex;
            EditorGUI.indentLevel++;
            if (fontSizeMode == DamageFontSizeMode.Fixed)
            {
                EditorGUILayout.PropertyField(fixedFontSizeProp);
            }
            else if (fontSizeMode == DamageFontSizeMode.Random)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, new GUIContent(randomFontSizeRangeProp.displayName, randomFontSizeRangeProp.tooltip));
                
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; 

                float width = rect.width / 2f - 2f;
                Rect rect1 = new Rect(rect.x, rect.y, width, rect.height);
                Rect rect2 = new Rect(rect.x + width + 4f, rect.y, width, rect.height);

                Vector2 val = randomFontSizeRangeProp.vector2Value;
                val.x = EditorGUI.FloatField(rect1, val.x);
                val.y = EditorGUI.FloatField(rect2, val.y);
                randomFontSizeRangeProp.vector2Value = val;

                EditorGUI.indentLevel = indent;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(rotationModeProp);

            // Conditional Rotation Fields
            DamageRotationMode rotationMode = (DamageRotationMode)rotationModeProp.enumValueIndex;
            EditorGUI.indentLevel++;
            if (rotationMode == DamageRotationMode.Fixed)
            {
                EditorGUILayout.PropertyField(fixedRotationZProp);
            }
            else if (rotationMode == DamageRotationMode.Random)
            {
                Rect rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, new GUIContent(randomRotationRangeProp.displayName, randomRotationRangeProp.tooltip));
                
                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; // Bỏ thụt lề cho 2 ô nhập

                float width = rect.width / 2f - 2f;
                Rect rect1 = new Rect(rect.x, rect.y, width, rect.height);
                Rect rect2 = new Rect(rect.x + width + 4f, rect.y, width, rect.height);

                Vector2 val = randomRotationRangeProp.vector2Value;
                val.x = EditorGUI.FloatField(rect1, val.x);
                val.y = EditorGUI.FloatField(rect2, val.y);
                randomRotationRangeProp.vector2Value = val;

                EditorGUI.indentLevel = indent;
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
