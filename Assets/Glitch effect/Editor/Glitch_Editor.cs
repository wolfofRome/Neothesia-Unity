using UnityEngine;
using UnityEditor;
 
public class Glitch_Editor : ShaderGUI
{

	MaterialEditor editor;
	MaterialProperty[] properties;

	//get preperties function
	MaterialProperty FindProperty (string name) 
	{
		return FindProperty(name, properties);
	}
	//
	
	////
	static GUIContent staticLabel = new GUIContent();
	static GUIContent MakeLabel (MaterialProperty property, string tooltip = null) 
	{
		staticLabel.text = property.displayName;
		staticLabel.tooltip = tooltip;
		return staticLabel;
	}
	////

	public override void OnGUI (MaterialEditor editor, MaterialProperty[] properties) 
		{
			this.editor = editor;
			this.properties = properties;
			DoMain();

		}


	// GUI FUNCTION	
		void DoMain() 
		{
			
		 
		Texture2D myGUITexture  = (Texture2D)Resources.Load("holofx");
		GUILayout.Label(myGUITexture,EditorStyles.centeredGreyMiniLabel);


		//LABELS
		GUILayout.Label("/---------------/ ADVANCED VERSION /---------------/", EditorStyles.centeredGreyMiniLabel);
		GUILayout.Label("DIFFUSE MAP", EditorStyles.helpBox);

		// get properties
		MaterialProperty _Diffuse = ShaderGUI.FindProperty("_Diffuse", properties);

		//Add to GUI
		editor.TexturePropertySingleLine(MakeLabel(_Diffuse,"Diffuse Map"), _Diffuse,FindProperty("_diff_Color"));
		editor.TextureScaleOffsetProperty (_Diffuse);



		//----------------
		GUILayout.Label("GLITCH MAP", EditorStyles.helpBox);

		// get properties
		MaterialProperty _N_map = ShaderGUI.FindProperty("_N_map", properties);

		//Add to GUI
		editor.TexturePropertySingleLine(MakeLabel(_N_map,"Noise Map"), _N_map);
		editor.TextureScaleOffsetProperty (_N_map);
		//----------------


		//----------------
		GUILayout.Label("MASK MAP", EditorStyles.helpBox);

		// get properties
		MaterialProperty _M_map = ShaderGUI.FindProperty("_M_map", properties);

		//Add to GUI
		editor.TexturePropertySingleLine(MakeLabel(_M_map,"Mask Map"), _M_map);
		editor.TextureScaleOffsetProperty (_M_map);
		//----------------

		//----------------
		GUILayout.Label("G/H SETTING", EditorStyles.helpBox);
		MaterialProperty _Color = FindProperty("_Color", properties);
		editor.ShaderProperty(_Color, "Aura Color");
		//----------------

		//----------------
		MaterialProperty _intensity = FindProperty("_intensity");
		editor.ShaderProperty(_intensity, MakeLabel(_intensity));

		MaterialProperty _deform = FindProperty("_deform");
		editor.ShaderProperty(_deform, MakeLabel(_deform));

		MaterialProperty _glitchColor_c = FindProperty("_glitchColor_c", properties);
		editor.ShaderProperty(_glitchColor_c, "G/H Color");

		MaterialProperty _dist_chrom = FindProperty("_dist_chrom", properties);
		editor.ShaderProperty(_dist_chrom, "Chromatic");

		MaterialProperty _Opacity = FindProperty("_Opacity");
		editor.ShaderProperty(_Opacity, MakeLabel(_Opacity));

		MaterialProperty _Speed = FindProperty("_Speed");
		editor.ShaderProperty(_Speed, MakeLabel(_Speed));

		MaterialProperty _noise_details = FindProperty("_noise_details");
		editor.ShaderProperty(_noise_details, MakeLabel(_noise_details));


		//----------------


		//---------------
		GUILayout.Label("FRESNEL SETTINGS", EditorStyles.helpBox);

		MaterialProperty _Bias = FindProperty("_Bias");
		editor.ShaderProperty(_Bias, MakeLabel(_Bias));

		MaterialProperty _Scale = FindProperty("_Scale");
		editor.ShaderProperty(_Scale, MakeLabel(_Scale));

		MaterialProperty _Power = FindProperty("_Power");
		editor.ShaderProperty(_Power, MakeLabel(_Power));

		//---------------

		//----------------
		GUILayout.Label("ADVANCED CONTROLS", EditorStyles.helpBox);

		MaterialProperty _cut_level = FindProperty("_cut_level");
		editor.ShaderProperty(_cut_level, MakeLabel(_cut_level));

		MaterialProperty _OrigineX = FindProperty("_OrigineX");
		editor.ShaderProperty(_OrigineX, MakeLabel(_OrigineX));

		MaterialProperty _OrigineY = FindProperty("_OrigineY");
		editor.ShaderProperty(_OrigineY, MakeLabel(_OrigineY));

		MaterialProperty _Circle_wave = FindProperty("_Circle_wave");
		editor.ShaderProperty(_Circle_wave, MakeLabel(_Circle_wave));

		MaterialProperty _Speed_wave = FindProperty("_Speed_wave");
		editor.ShaderProperty(_Speed_wave, MakeLabel(_Speed_wave));



		//----------------

		//--------------
		GUILayout.Label("EXTRA OPTIONS", EditorStyles.helpBox);

		MaterialProperty _glitchColor = FindProperty("_glitchColor");
		editor.ShaderProperty(_glitchColor, MakeLabel(_glitchColor));

		MaterialProperty _X = FindProperty("_X");
		editor.ShaderProperty(_X, MakeLabel(_X));


		MaterialProperty _Y = FindProperty("_Y");
		editor.ShaderProperty(_Y, MakeLabel(_Y));

		MaterialProperty _monochrom = FindProperty("_monochrom");
		editor.ShaderProperty(_monochrom, MakeLabel(_monochrom));

		MaterialProperty _t = FindProperty("_t");
		editor.ShaderProperty(_t, MakeLabel(_t));

		/////////
		GUILayout.Label("ORIGINAL UVs", EditorStyles.helpBox);

		MaterialProperty _OriginalUVSwitch = FindProperty("_OriginalUVSwitch");
		editor.ShaderProperty(_OriginalUVSwitch, MakeLabel(_OriginalUVSwitch));

		// get properties
		MaterialProperty _originalDiffuse = ShaderGUI.FindProperty("_originalDiffuse", properties);

		//Add to GUI
		editor.TexturePropertySingleLine(MakeLabel(_originalDiffuse,"OriginalDiffuse"), _originalDiffuse);
		editor.TextureScaleOffsetProperty (_originalDiffuse);





		//--------------
	}
}