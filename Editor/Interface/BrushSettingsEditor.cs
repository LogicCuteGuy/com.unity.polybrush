using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditor.Polybrush
{
    /// <summary>
    /// The default editor for BrushSettings.
    /// </summary>
    [CustomEditor(typeof(BrushSettings))]
	internal class BrushSettingsEditor : Editor
	{
		internal bool showSettingsBounds = false;
		internal bool showImageBrushSettings = false;

        GUIContent m_GCRadius = new GUIContent("Outer Radius", "Radius: The distance from the center of a brush to it's outer edge.\n\nShortcut: 'Ctrl + Mouse Wheel'");
        GUIContent m_GCFalloff = new GUIContent("Inner Radius", "Inner Radius: The distance from the center of a brush at which the strength begins to linearly taper to 0.  This value is normalized, 1 means the entire brush gets full strength, 0 means the very center point of a brush is full strength and the edges are 0.\n\nShortcut: 'Shift + Mouse Wheel'");
        GUIContent m_GCFalloffCurve = new GUIContent("Falloff Curve", "Falloff: Sets the Falloff Curve.");
        GUIContent m_GCSculptPower = new GUIContent("Sculpt Power", "Sculpt Power controls how much vertices are displaced during sculpting operations. Higher values produce more dramatic displacement effects, while lower values create subtle changes. The actual displacement also depends on the brush radius and falloff settings.\n\nShortcut: 'Ctrl + Shift + Mouse Wheel'");
        GUIContent m_GCRadiusMin = new GUIContent("Brush Radius Min", "The minimum value the brush radius slider can access");
        GUIContent m_GCRadiusMax = new GUIContent("Brush Radius Max", "The maximum value the brush radius slider can access");
        GUIContent m_GCAllowUnclampedFalloff = new GUIContent("Unclamped Falloff", "If enabled, the falloff curve will not be limited to values between 0 and 1.");
        GUIContent m_GCBrushSettingsMinMax = new GUIContent("Brush Radius Min / Max", "Set the minimum and maximum brush radius values");
        
        // Image Brush UI content
        GUIContent m_GCImageBrushEnabled = new GUIContent("Enable Image Brush", "Use a grayscale texture to define brush shape and intensity");
        GUIContent m_GCImageBrushTexture = new GUIContent("Brush Texture", "Grayscale texture that defines the brush shape. Lighter values = stronger influence");
        GUIContent m_GCImageBrushRotation = new GUIContent("Rotation", "Rotation angle of the brush texture in degrees (0-360)");
        GUIContent m_GCImageBrushAspectRatio = new GUIContent("Preserve Aspect Ratio", "Maintain the texture's aspect ratio in world space");
        GUIContent m_GCImageBrushSamplingMode = new GUIContent("Sampling Mode", "Texture filtering mode for sampling");
        GUIContent m_GCImageBrushSettings = new GUIContent("Image Brush Settings", "Configure image-based brush behavior");
        GUIContent m_GCImageBrushPreset = new GUIContent("Default Brushes", "Select from built-in brush patterns");
        
        // Cached preset names for dropdown
        private string[] m_PresetPatternNames;
        private int m_SelectedPresetIndex = -1;

		private static readonly Rect RECT_ONE = new Rect(0,0,1,1);

        private const float k_BrushSizeMaxValue = 10000f;

		SerializedProperty 	radius,
							falloff,
							strength,
							brushRadiusMin,
							brushRadiusMax,
							brushStrengthMin,
							brushStrengthMax,
							curve,
							allowNonNormalizedFalloff;

		internal void OnEnable()
		{
			/// User settable
			radius = serializedObject.FindProperty("_radius");
			falloff = serializedObject.FindProperty("_falloff");
			curve = serializedObject.FindProperty("_curve");
			strength = serializedObject.FindProperty("_strength");

			/// Bounds
			brushRadiusMin = serializedObject.FindProperty("brushRadiusMin");
			brushRadiusMax = serializedObject.FindProperty("brushRadiusMax");
			allowNonNormalizedFalloff = serializedObject.FindProperty("allowNonNormalizedFalloff");
		}

		private bool approx(float lhs, float rhs)
		{
			return Mathf.Abs(lhs-rhs) < .0001f;
		}

		public override void OnInspectorGUI()
		{
            serializedObject.Update();

            // Manually show the settings header in PolyEditor so that the preset selector can be included in the block
            // if(PolyGUILayout.HeaderWithDocsLink(PolyGUI.TempContent("Brush Settings")))
            // 	Application.OpenURL("http://procore3d.github.io/polybrush/brushSettings/");

            showSettingsBounds = PolyGUILayout.Foldout(showSettingsBounds, m_GCBrushSettingsMinMax);

            if (showSettingsBounds)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new GUILayout.VerticalScope())
                    {
                        brushRadiusMin.floatValue = PolyGUILayout.FloatField(m_GCRadiusMin, brushRadiusMin.floatValue);
                        brushRadiusMin.floatValue = Mathf.Clamp(brushRadiusMin.floatValue, .0001f, k_BrushSizeMaxValue);

                        brushRadiusMax.floatValue = PolyGUILayout.FloatField(m_GCRadiusMax, brushRadiusMax.floatValue);
                        brushRadiusMax.floatValue = Mathf.Clamp(brushRadiusMax.floatValue, brushRadiusMin.floatValue + .001f, k_BrushSizeMaxValue);

                        allowNonNormalizedFalloff.boolValue = PolyGUILayout.Toggle(m_GCAllowUnclampedFalloff, allowNonNormalizedFalloff.boolValue);
                    }
                }
            }

            radius.floatValue = PolyGUILayout.FloatFieldWithSlider(m_GCRadius, radius.floatValue, brushRadiusMin.floatValue, brushRadiusMax.floatValue);
            falloff.floatValue = PolyGUILayout.FloatFieldWithSlider(m_GCFalloff, falloff.floatValue, 0f, 1f);
            
            // Enhanced Sculpt Power slider with visual feedback
            DrawSculptPowerSlider(strength);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(m_GCFalloffCurve, GUILayout.Width(100));

                if (allowNonNormalizedFalloff.boolValue)
                    curve.animationCurveValue = EditorGUILayout.CurveField(curve.animationCurveValue, GUILayout.MinHeight(22));
                else
                    curve.animationCurveValue = EditorGUILayout.CurveField(curve.animationCurveValue, Color.green, RECT_ONE, GUILayout.MinHeight(22));
            }

            Keyframe[] keys = curve.animationCurveValue.keys;

            if ((approx(keys[0].time, 0f) && approx(keys[0].value, 0f) && approx(keys[1].time, 1f) && approx(keys[1].value, 1f)))
            {
                Keyframe[] rev = new Keyframe[keys.Length];

                for (int i = 0; i < keys.Length; i++)
                    rev[keys.Length - i - 1] = new Keyframe(1f - keys[i].time, keys[i].value, -keys[i].outTangent, -keys[i].inTangent);

                curve.animationCurveValue = new AnimationCurve(rev);
            }

            // Image Brush Settings Section
            EditorGUILayout.Space();
            showImageBrushSettings = PolyGUILayout.Foldout(showImageBrushSettings, m_GCImageBrushSettings);

            if (showImageBrushSettings)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    BrushSettings brushSettings = (BrushSettings)target;
                    ImageBrushSettings imageBrush = brushSettings.imageBrushSettings;

                    if (imageBrush != null)
                    {
                        // Enable/Disable toggle
                        bool wasEnabled = imageBrush.enabled;
                        imageBrush.enabled = PolyGUILayout.Toggle(m_GCImageBrushEnabled, imageBrush.enabled);

                        if (imageBrush.enabled)
                        {
                            // Default brush preset dropdown
                            DrawDefaultBrushPresetDropdown(brushSettings, imageBrush);
                            
                            EditorGUILayout.Space(4);
                            
                            // Texture field
                            Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(
                                m_GCImageBrushTexture,
                                imageBrush.brushTexture,
                                typeof(Texture2D),
                                false
                            );

                            // Validate texture when changed using centralized error handling
                            if (newTexture != imageBrush.brushTexture)
                            {
                                if (newTexture != null)
                                {
                                    string errorMessage;
                                    if (ErrorHandling.ValidateTextureFormat(newTexture, out errorMessage))
                                    {
                                        imageBrush.brushTexture = newTexture;
                                    }
                                    else
                                    {
                                        ErrorHandling.ShowTextureFormatWarning(newTexture, errorMessage);
                                    }
                                }
                                else
                                {
                                    imageBrush.brushTexture = null;
                                }
                            }

                            // Show warning if texture is assigned but not valid
                            if (imageBrush.brushTexture != null)
                            {
                                string validationError;
                                if (!ErrorHandling.ValidateTextureFormat(imageBrush.brushTexture, out validationError))
                                {
                                    EditorGUILayout.HelpBox(
                                        validationError + "\nThe brush will fall back to standard circular mode.",
                                        MessageType.Warning
                                    );
                                }
                            }

                            // Rotation slider
                            imageBrush.rotation = PolyGUILayout.FloatFieldWithSlider(
                                m_GCImageBrushRotation,
                                imageBrush.rotation,
                                0f,
                                360f
                            );

                            // Aspect ratio toggle
                            imageBrush.preserveAspectRatio = PolyGUILayout.Toggle(
                                m_GCImageBrushAspectRatio,
                                imageBrush.preserveAspectRatio
                            );

                            // Sampling mode dropdown
                            imageBrush.samplingMode = (FilterMode)EditorGUILayout.EnumPopup(
                                m_GCImageBrushSamplingMode,
                                imageBrush.samplingMode
                            );
                        }

                        // Mark as dirty if anything changed
                        if (GUI.changed || wasEnabled != imageBrush.enabled)
                        {
                            EditorUtility.SetDirty(target);
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Create a New BrushSettings Asset
        /// </summary>
        /// <returns>the newly created BrushSettings</returns>
		internal static BrushSettings AddNew(BrushSettings prevSettings = null)
        {
            string path = PolyEditorUtility.UserAssetDirectory + "Brush Settings";

			if(string.IsNullOrEmpty(path))
				path = "Assets";

			path = AssetDatabase.GenerateUniqueAssetPath(path + "/New Brush.asset");

			if(!string.IsNullOrEmpty(path))
			{
				BrushSettings settings = ScriptableObject.CreateInstance<BrushSettings>();
                if (prevSettings != null) {
                    string name = settings.name;
                    prevSettings.CopyTo(settings);
                    settings.name = name;	// want to retain the unique name generated by AddNew()
                }
                else
                {
                    settings.SetDefaultValues();
                }

				AssetDatabase.CreateAsset(settings, path);
				AssetDatabase.Refresh();

				EditorGUIUtility.PingObject(settings);

				return settings;
			}

			return null;
		}

        static internal BrushSettings LoadBrushSettingsAssets(string guid)
        {
            BrushSettings settings;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            settings = AssetDatabase.LoadAssetAtPath<BrushSettings>(path);
            return settings;
        }

        static internal BrushSettings[] GetAvailableBrushes()
        {
            List<BrushSettings> brushes = PolyEditorUtility.GetAll<BrushSettings>();

            if (brushes.Count < 1)
                brushes.Add(PolyEditorUtility.GetFirstOrNew<BrushSettings>());

            return brushes.ToArray();
        }

        /// <summary>
        /// Draw enhanced sculpt power slider with visual feedback
        /// </summary>
        private void DrawSculptPowerSlider(SerializedProperty strengthProperty)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(m_GCSculptPower, GUILayout.Width(100));
                
                float oldStrength = strengthProperty.floatValue;
                float newStrength = EditorGUILayout.Slider(oldStrength, 0f, 1f);
                
                if (!Mathf.Approximately(oldStrength, newStrength))
                {
                    strengthProperty.floatValue = Mathf.Clamp(newStrength, 0f, 1f);
                }
            }
            
            // Visual feedback bar showing displacement magnitude
            DrawDisplacementPreview(strengthProperty.floatValue);
        }

        /// <summary>
        /// Draw dropdown for selecting default brush patterns
        /// </summary>
        private void DrawDefaultBrushPresetDropdown(BrushSettings brushSettings, ImageBrushSettings imageBrush)
        {
            // Initialize preset names if needed
            if (m_PresetPatternNames == null)
            {
                m_PresetPatternNames = DefaultImageBrushLibrary.GetBrushPatternNames();
            }
            
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(m_GCImageBrushPreset, GUILayout.Width(100));
                
                // Create options array with "Custom" as first option
                string[] options = new string[m_PresetPatternNames.Length + 1];
                options[0] = "Custom";
                for (int i = 0; i < m_PresetPatternNames.Length; i++)
                {
                    options[i + 1] = m_PresetPatternNames[i];
                }
                
                // Determine current selection based on texture
                int currentIndex = 0; // Default to "Custom"
                if (imageBrush.brushTexture != null)
                {
                    string textureName = imageBrush.brushTexture.name;
                    for (int i = 0; i < m_PresetPatternNames.Length; i++)
                    {
                        if (m_PresetPatternNames[i] == textureName)
                        {
                            currentIndex = i + 1;
                            break;
                        }
                    }
                }
                
                int newIndex = EditorGUILayout.Popup(currentIndex, options);
                
                if (newIndex != currentIndex && newIndex > 0)
                {
                    // Apply selected default brush pattern
                    int patternIndex = newIndex - 1;
                    DefaultImageBrushLibrary.BrushPattern pattern = 
                        (DefaultImageBrushLibrary.BrushPattern)patternIndex;
                    
                    Texture2D newTexture = DefaultImageBrushLibrary.GetBrushTexture(pattern);
                    if (newTexture != null)
                    {
                        imageBrush.brushTexture = newTexture;
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }

        /// <summary>
        /// Draw visual feedback showing the expected displacement magnitude
        /// </summary>
        private void DrawDisplacementPreview(float strength)
        {
            const float PREVIEW_HEIGHT = 20f;
            const float PREVIEW_MARGIN = 4f;
            
            Rect previewRect = GUILayoutUtility.GetRect(
                EditorGUIUtility.currentViewWidth - 120f, 
                PREVIEW_HEIGHT
            );
            
            previewRect.x += 105f;
            previewRect.width -= 110f;
            
            // Draw background
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            // Draw strength indicator
            Rect strengthRect = new Rect(
                previewRect.x + PREVIEW_MARGIN,
                previewRect.y + PREVIEW_MARGIN,
                (previewRect.width - PREVIEW_MARGIN * 2) * strength,
                previewRect.height - PREVIEW_MARGIN * 2
            );
            
            // Color gradient from green (low) to yellow (medium) to red (high)
            Color strengthColor;
            if (strength < 0.5f)
            {
                // Green to yellow
                strengthColor = Color.Lerp(new Color(0.2f, 0.8f, 0.2f), Color.yellow, strength * 2f);
            }
            else
            {
                // Yellow to red
                strengthColor = Color.Lerp(Color.yellow, new Color(1f, 0.3f, 0.2f), (strength - 0.5f) * 2f);
            }
            
            EditorGUI.DrawRect(strengthRect, strengthColor);
            
            // Draw text label
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 10;
            
            string strengthLabel = string.Format("Displacement: {0:P0}", strength);
            GUI.Label(previewRect, strengthLabel, labelStyle);
        }
    }
}
