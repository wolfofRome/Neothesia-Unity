// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "HOLO/Holo2"
{
	Properties
	{
		_originalDiffuse("Custom Diffuse Map", 2D) = "white" {}
		_Diffuse("Diffuse Map", 2D) = "white" {}
		[HDR] _diff_Color ("Diffuse Color Mult", Color) = (1,1,1,1)
		_N_map("Noise", 2D) = "white" {}
		_M_map("Mask", 2D) = "white" {}
		[Toggle] _mask_type("Use Map as Mask", Float) = 1
		_intensity("Intensity", float) = 0
		_deform("Deformation Intensity", float) = 1
		[HDR] _Color ("Outline Color Mult", Color) = (1,1,1,1)
		_Opacity("Base Opacity", range (0,1)) = 0
		_Bias("Bias", range (0,1)) = 0
		_Scale ("Scale ", range (0,10)) = 0
		_Power("Power", range (0,3)) = 0
		//_Opacity_gl("Glitch Opacity", range (0,1)) = 0
		_Speed("Speed", range (0,1)) = 0
		_t("Extra Option", range (0,1)) = 0
		_noise_details("G/H Noise Details Amount ", range (1,16)) = 0
		[Toggle] _X("Active X Axe", Float) = 1
		[Toggle] _Y("Active X Axe", Float) = 1
		[Toggle] _glitchColor("Display G/H Color", Float) = 1
		[Toggle] _monochrom("Monochromatic", Float) = 1
		[Toggle] _OriginalUVSwitch("Use Custom Diffuse Texture", Float) = 0

		_Distance("Distance", float) = 0
		_Amplitude("Amplitude", float) = 0
		_Speed_Up("_Speed_Up", float) = 0
		_Amount("Amount", Range(0, 1)) = 0

		_cut_level("Cut Level", range (0,1) ) = 0



	}

	Subshader
	{
		//http://docs.unity3d.com/462/Documentation/Manual/SL-SubshaderTags.html
	    // Background : 1000     -        0 - 1499 = Background
        // Geometry   : 2000     -     1500 - 2399 = Geometry
        // AlphaTest  : 2450     -     2400 - 2699 = AlphaTest
        // Transparent: 3000     -     2700 - 3599 = Transparent
        // Overlay    : 4000     -     3600 - 5000 = Overlay
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Zwrite on
			ZTest on
			//cull off
			CGPROGRAM
			//http://docs.unity3d.com/Manual/SL-ShaderPrograms.html
			#pragma vertex vert
			#pragma fragment frag


			#include "UnityCG.cginc"

			//http://docs.unity3d.com/ru/current/Manual/SL-ShaderPerformance.html
			//http://docs.unity3d.com/Manual/SL-ShaderPerformance.html

			// VARIABLES ///////////////////////////////////////////////////////////////////////////////////////////
			uniform sampler2D _originalDiffuse;
			uniform half4 _Color;
			uniform half4 _diff_Color ;
			uniform sampler2D _Diffuse;
			uniform sampler2D _N_map;
			uniform sampler2D _M_map;

			uniform float4 _Diffuse_ST;
			uniform float4 _N_map_ST;
			uniform float4 _M_map_ST;
			uniform float _intensity;
			uniform float _deform ;
			uniform float _Bias ;
			uniform float _Scale ;
			uniform float _Power ;
			uniform float _Speed ;
			uniform float _t ;
			uniform float _X ;
			uniform float _Y ;
			uniform float _glitchColor ;
			uniform float _monochrom ;
			uniform float _Opacity ;
			uniform float _noise_details ;

			uniform float _Distance ;
			uniform float _Amplitude ;
			uniform float _Speed_Up ;
			uniform float _Amount ;
			uniform float _mask_type ;
			//uniform float _Opacity_gl ;
			uniform float _cut_level ;
			uniform float _OriginalUVSwitch ;


			////////////////////////////////////////////////////////////////////////////////////////////////////////


			//https://msdn.microsoft.com/en-us/library/windows/desktop/bb509647%28v=vs.85%29.aspx#VS

			// STRUCTURS  //////////////////////////////////////////////////////////////////////////////////////////
			struct vertexInput
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float4 normal : NORMAL;

			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float4 texcoord1 : TEXCOORD1;
				float3 uv_matcap : TEXCOORD2;
				float3 normalDir : TEXCOORD3;
				float4 R : TEXCOORD4;
				float4 normal : NORMAL;
				float4 mpos : TEXCOORD6;

			};
			////////////////////////////////////////////////////////////////////////////////////////////////////////


			// FUNCTIONS /////////////////////////////////////////////////////////////////////////////////////////////
			float4 glitch(float2 coord)
			{
				float4 c = float4(coord,0,1);
			    float4 d;
			    float4 e;
			    for (int j=_noise_details;j>0;j--) {
			        e=floor(c);
			        d+=(sin(e*e.yxyx+e*(_Time/10)));
			       	c*=2.5;
			    }
				float4 glitch_res = d;
				return glitch_res;
			}
			//////////////////////////////////////////////////////////////////////////////////////////////////////////


			// VERTEX OPERATIONS ////////////////////////////////////////////////////////////////////////////////////
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;
				UNITY_INITIALIZE_OUTPUT(vertexOutput, o);
				fixed T = _Time * _Speed*30;
				fixed offset_sin =cos(T*2);
				fixed offset_cos = sin(T*2);

				float4 mask_map = tex2Dlod(_M_map, float4((v.texcoord.xy * ((((_M_map_ST.y),(_M_map_ST.y * offset_cos)))  ) + (((_M_map_ST.z ),(_M_map_ST.w * offset_sin)))),0,0));
				float4 glitch_ver = glitch(v.texcoord.xy)*(0.001 * _deform) * (0.1*_intensity) * mask_map.x ;

				float3 axe = float3(_X,_Y,1);


				float Animation_Sinus = sin(_Time.y * _Speed_Up + (v.vertex.y*axe.y) +(v.vertex.x*axe.x)* _Amplitude)  *_Distance * _Amount ;



				//v.vertex.xyz += (glitch_ver.xyz * axe * v.normal.xyz * 10) ;
				v.vertex.x += Animation_Sinus *axe.x *glitch_ver.x * v.normal.x ;
				v.vertex.y += Animation_Sinus *axe.y *glitch_ver.y * v.normal.y;

				// PROJECTIONS
				o.mpos = v.vertex;
				o.pos = UnityObjectToClipPos( v.vertex);

				o.uv = v.texcoord;
				float3 posWorld = mul(unity_ObjectToWorld, v.vertex).xyz;
				//float3 normWorld = normalize(mul(float3x3(unity_ObjectToWorld), v.normal));


				float3 normalVS = mul(UNITY_MATRIX_MV,v.normal);
				normalVS = normalize(normalVS);

				o.normalDir = UnityObjectToWorldNormal(v.normal);
				o.uv_matcap.xy = normalVS.xy * 0.5 + float2(0.5,0.5);

				float3 I = normalize(posWorld - _WorldSpaceCameraPos.xyz);
				fixed temp1 =  dot(I, o.normalDir) + _t;
				o.R.x = _Bias * _Scale * pow(temp1, _Power);

			


				return o;
			}

			////////////////////////////////////////////////////////////////////////////////////////////////////////


			// FRAGMENTS OPERATIONS ///////////////////////////////////////////////////////////////////////////////
			half4 frag(vertexOutput i , float facing : VFACE) : COLOR
			{

				/// TIME ANIMATION
				fixed T = _Time * _Speed*30	 ;
				fixed offset_sin = cos(T*2);
				fixed offset_cos = sin(T*2);
				/// GET DIFF / MASK / NOISE FROM MAPS
				float4 temp_noise = tex2D(_N_map, float4((i.uv.xy * (_N_map_ST.xy  ) + (_N_map_ST.zw  * T)),0,0));
				float4 mask = tex2D(_M_map, float4((i.uv.xy * ((((_M_map_ST.y),(_M_map_ST.y * offset_cos)))  ) + (((_M_map_ST.z ),(_M_map_ST.w * offset_sin)))),0,0));
				//float4 diff = tex2D(_Diffuse, i.uv);
				//float4 c_1 = tex2D(_Diffuse ,float4((i.uv_matcap.xy * ((_Diffuse_ST.x), _Diffuse_ST.y  ) + ((_Diffuse_ST.z ),_Diffuse_ST.w )), 0,0)) ;
				float4 c_1 = float4(0,0,0,0);

				///////- Switch original UV

				if (_OriginalUVSwitch == 0)
				{
				float4 diff = tex2D(_Diffuse, i.uv);
				//i.uv.xy += (temp * sin(1)* i.uv.x)*(_intensity) ;

				c_1 = tex2D(_Diffuse ,float4((i.uv_matcap.xy * ((_Diffuse_ST.x), _Diffuse_ST.y  ) + ((_Diffuse_ST.z ),_Diffuse_ST.w )), 0, 0 )) ;
				}

				if (_OriginalUVSwitch == 1)
				{
				float4 diff = tex2D(_originalDiffuse, i.uv);
				//i.uv.xy += (temp * sin(1)* i.uv.x)*(_intensity) ;
				c_1 = tex2D(_originalDiffuse ,i.uv) ;
				}


				// Procedural Animation
				/// Distortion Uvs and Colors
				fixed temp = temp_noise.x * mask.x  ;
				i.uv.xy += (temp * sin(T)* i.uv.x)*(_intensity) ;
				float Animation_Sinus_frag = sin(_Time.y * _Speed_Up + (i.uv.y) * _Amplitude)  *_Distance * _Amount;


				//////////// Procedural noise glitch

				float4 glitch_res = glitch(i.uv.xy) * _glitchColor * Animation_Sinus_frag;


				//// Color distortion
				float4 c_2 = _Color;
				c_2.r += (temp * sin(T*1) * glitch_res.x * (_intensity)*0.25) *0.75 ;
				c_2.g +=(temp * sin(T*2) * glitch_res.y * (_intensity)*0.5)  *0.75;
				c_2.b += (temp * sin(T*4)  * glitch_res.x * (_intensity)) *0.75;

				/// main color opacity and color override
				c_1.a *= _Opacity ;
				c_1 *= _diff_Color ;
			


				/// Switch to Monchromatic Mode
				if (_monochrom == 1)
				{
					fixed c_2_to_grey = (c_2.r +c_2.g + c_2.b) /3;
					fixed c_1_to_grey = (c_1.r +c_1.g + c_1.b) /3;
					c_2.rgb = float3(c_2_to_grey,c_2_to_grey,c_2_to_grey);
					c_1.rgb = float3(c_1_to_grey,c_1_to_grey,c_1_to_grey);
				}




				/// Results
				return lerp(c_1,c_2,i.R.x);
			}
			///////////////////////////////////////////////////////////////////////////////////////////////////////

			ENDCG
		}
	}
	CustomEditor "Glitch_Editor_lite"
}
