using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class TesselationLEADRGUI : ShaderGUI {

    enum TessellationMode
    {
        Uniform, Edge
    }

	static GUIContent staticLabel = new GUIContent();

	Material target;
	MaterialEditor editor;
	MaterialProperty[] properties;
	bool shouldShowAlphaCutoff;

	public override void OnGUI (
		MaterialEditor editor, MaterialProperty[] properties
	) {
		this.target = editor.target as Material;
		this.editor = editor;
		this.properties = properties;
        if (target.HasProperty("_TessellationUniform")) {
            DoTessellation();
        }
        if (target.HasProperty("_Lean1")) {
            DoMain();
            //DoLEAN
        }

		DoMain();

	}

    void DoTessellation() {
        GUILayout.Label("Tessellation", EditorStyles.boldLabel);
        EditorGUI.indentLevel += 2;

        TessellationMode mode = TessellationMode.Uniform;
        if (IsKeywordEnabled("_TESSELLATION_EDGE")) {
            mode = TessellationMode.Edge;
        }
        EditorGUI.BeginChangeCheck();
        mode = (TessellationMode)EditorGUILayout.EnumPopup(
            MakeLabel("Mode"), mode
        );
        if (EditorGUI.EndChangeCheck()) {
            RecordAction("Tessellation Mode");
            SetKeyword("_TESSELLATION_EDGE", mode == TessellationMode.Edge);
        }

        if (mode == TessellationMode.Uniform) {
            editor.ShaderProperty(
                FindProperty("_TessellationUniform"),
                MakeLabel("Uniform")
            );
        }
        else {
            editor.ShaderProperty(
                FindProperty("_TessellationEdgeLength"),
                MakeLabel("Edge Length")
            );
        }
        EditorGUI.indentLevel -= 2;
    }


	void DoMain () {
		GUILayout.Label("Main Maps", EditorStyles.boldLabel);

		MaterialProperty mainTex = FindProperty("_MainTex");
		editor.TexturePropertySingleLine(
			MakeLabel(mainTex, "Albedo (RGB)"), mainTex, FindProperty("_Color")
		);

		if (shouldShowAlphaCutoff) {
			DoAlphaCutoff();
		}
		DoMetallic();
		DoNormals();
		DoParallax();
		editor.TextureScaleOffsetProperty(mainTex);
	}

	void DoAlphaCutoff () {
		MaterialProperty slider = FindProperty("_Cutoff");
		EditorGUI.indentLevel += 2;
		editor.ShaderProperty(slider, MakeLabel(slider));
		EditorGUI.indentLevel -= 2;
	}

	void DoNormals () {
		MaterialProperty map = FindProperty("_NormalMap");
		Texture tex = map.textureValue;
		EditorGUI.BeginChangeCheck();
		editor.TexturePropertySingleLine(
			MakeLabel(map), map,
			tex ? FindProperty("_BumpScale") : null
		);
		if (EditorGUI.EndChangeCheck() && tex != map.textureValue) {
			SetKeyword("_NORMAL_MAP", map.textureValue);
		}
	}

	void DoMetallic () {
		MaterialProperty map = FindProperty("_MetallicMap");
		Texture tex = map.textureValue;
		EditorGUI.BeginChangeCheck();
		editor.TexturePropertySingleLine(
			MakeLabel(map, "Metallic (R)"), map,
			tex ? null : FindProperty("_Metallic")
		);
		if (EditorGUI.EndChangeCheck() && tex != map.textureValue) {
			SetKeyword("_METALLIC_MAP", map.textureValue);
		}
	}


	void DoParallax () {
		MaterialProperty map = FindProperty("_ParallaxMap");
		Texture tex = map.textureValue;
		EditorGUI.BeginChangeCheck();
		editor.TexturePropertySingleLine(
			MakeLabel(map, "Parallax (G)"), map,
			tex ? FindProperty("_ParallaxStrength") : null
		);
		if (EditorGUI.EndChangeCheck() && tex != map.textureValue) {
			SetKeyword("_PARALLAX_MAP", map.textureValue);
		}
	}

	void DoOcclusion () {
		MaterialProperty map = FindProperty("_OcclusionMap");
		Texture tex = map.textureValue;
		EditorGUI.BeginChangeCheck();
		editor.TexturePropertySingleLine(
			MakeLabel(map, "Occlusion (G)"), map,
			tex ? FindProperty("_OcclusionStrength") : null
		);
		if (EditorGUI.EndChangeCheck() && tex != map.textureValue) {
			SetKeyword("_OCCLUSION_MAP", map.textureValue);
		}
	}


	MaterialProperty FindProperty (string name) {
		return FindProperty(name, properties);
	}

	static GUIContent MakeLabel (string text, string tooltip = null) {
		staticLabel.text = text;
		staticLabel.tooltip = tooltip;
		return staticLabel;
	}

	static GUIContent MakeLabel (
		MaterialProperty property, string tooltip = null
	) {
		staticLabel.text = property.displayName;
		staticLabel.tooltip = tooltip;
		return staticLabel;
	}

	void SetKeyword (string keyword, bool state) {
		if (state) {
			foreach (Material m in editor.targets) {
				m.EnableKeyword(keyword);
			}
		}
		else {
			foreach (Material m in editor.targets) {
				m.DisableKeyword(keyword);
			}
		}
	}

	bool IsKeywordEnabled (string keyword) {
		return target.IsKeywordEnabled(keyword);
	}

	void RecordAction (string label) {
		editor.RegisterPropertyChangeUndo(label);
	}
}