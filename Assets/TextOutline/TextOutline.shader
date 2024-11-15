Shader"Tool/TextOutline"
{
	Properties
	{
		[PerRendererData] _MainTex("Main Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1, 1, 1, 1)
		//_OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
		//_OutlineWidth("Outline Width", Float) = 1
		//_OutlineOffset("Outline Offset", Vector) = (1, 1, 1, 1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		//_ShadowOutlineColor("Shadow Outline Color", Color) = (1, 1, 1, 1)
		//_ShadowOutlineWidth("Shadow Outline Width", Float) = 1

		[Enum(UnityEngine.Rendering.CullMode)]_Cull ("Cull Mode", Int) = 0

		[Toggle(IS_GAMMA_CORRECTION)] _IS_GAMMA_CORRECTION("Is Gamma Correction", Float) = 0

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}

			Stencil
			{
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}

			Cull [_Cull]
			Lighting Off
			ZWrite Off
			ZTest[unity_GUIZTestMode]
			Blend SrcAlpha OneMinusSrcAlpha
			// Blend Off
			ColorMask[_ColorMask]

			Pass
			{
				Name "OUTLINE"

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0

			//Add for RectMask2D
			#include "UnityUI.cginc"
			//End for RectMask2D
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;//或许可以不要
			float4 _MainTex_TexelSize;

			//float4 _OutlineColor;
			float _OutlineWidth;
			float4 _OutlineOffset;

			fixed4 _ShadowOutlineColor;
			float _ShadowOutlineWidth;

			//Add for RectMask2D
			float4 _ClipRect;
			//End for RectMask2D
			float2 uv_MainTex;
			float _IS_GAMMA_CORRECTION;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float4 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				fixed4 color : COLOR;

			};


			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 tangent : TANGENT;
				float4 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float4 uv3 : TEXCOORD3;
				//Add for RectMask2D
				float4 worldPosition : TEXCOORD4;
				//End for RectMask2D
				fixed4 color : COLOR;
			};

			v2f vert(appdata IN)
			{
				v2f o;

				//Add for RectMask2D
				o.worldPosition = IN.vertex;
				//End for RectMask2D

				o.vertex = UnityObjectToClipPos(IN.vertex);
				o.tangent = IN.tangent;
				o.texcoord = IN.texcoord;
				o.color = IN.color;
				o.uv1 = IN.uv1;
				o.uv2 = IN.uv2;
				o.uv3 = IN.uv3;
				o.normal = IN.normal;
				return o;
			}

			fixed IsInRect(float2 pPos, float2 pClipRectMin, float2 pClipRectMax)
			{
				pPos = step(pClipRectMin, pPos) * step(pPos, pClipRectMax);
				return pPos.x * pPos.y;
			}

			fixed OutlineAlpha(v2f IN, float outlineWidth, fixed4 outlineColor, fixed sin, fixed cos)
			{
				float2 pos = IN.texcoord + _MainTex_TexelSize.xy * float2(cos, sin) * outlineWidth;
				return IsInRect(pos, IN.uv1, IN.uv2) * (tex2D(_MainTex, pos) + _TextureSampleAdd).a * outlineColor.a;
			}

			//描边的基础逻辑是根据偏移画12个字围绕本体，然后描边像素在本体内的不显示，这样看起来就是描边了
			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;//默认的文字颜色
				// 使用uv3.x来区分当前顶点是文字还是阴影
				_OutlineWidth = IN.uv3.w;
				if ((IN.uv3.x != -1 && _OutlineWidth > 0) || (IN.uv3.x == -1 && _ShadowOutlineWidth > 0))
				{
					color.w *= IsInRect(IN.texcoord, IN.uv1, IN.uv2);	//uv1 uv2 存着原始字的uv长方形区域大小
					float outlineWidth = IN.uv3.x == -1 ? _ShadowOutlineWidth : _OutlineWidth;
					fixed4 outlineColor = IN.uv3.x == -1 ? _ShadowOutlineColor : IN.uv3;
					if (_IS_GAMMA_CORRECTION == 1)
                    {
                        outlineColor.rgb = GammaToLinearSpace(outlineColor.rgb);
                    }
					half4 val = half4(outlineColor.rgb, 0);//val 是 _OutlineColor的rgb，a是后面计算的
					
					//这里需要避免for循环以及new数组
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0, 1);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0.5, 0.866);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0.866, 0.5);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 1, 0);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0.866, -0.5);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0.5, -0.866);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, 0, -1);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, -0.5, -0.866);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, -0.866, -0.5);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, -1, 0);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, -0.866, 0.5);
					val.w += OutlineAlpha(IN, outlineWidth, outlineColor, -0.5, 0.866);

					color = (val * (1.0 - color.a)) + (color * color.a);
					color.a = saturate(color.a);//限制在0-1之间 clamp类似
					color.a *= IN.color.a;	//字逐渐隐藏时，描边也要隐藏
				}

				//Add for RectMask2D
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
				#endif
					//End for RectMask2D

					return color;
				}

				ENDCG
			}
		}
}
