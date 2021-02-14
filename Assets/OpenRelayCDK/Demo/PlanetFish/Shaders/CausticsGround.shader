// Found this on GLSL sandbox. I really liked it, changed a few things and made it tileable.
// :)
// by David Hoskins.


// Water turbulence effect by joltz0r 2013-07-04, improved 2013-07-07


// Redefine below to see the tiling...
//#define SHOW_TILING

// Original Shader Code
// https://www.shadertoy.com/view/MdlXz8
// Created by Dave_Hoskins in 2014-04-07

// Arrenged by FurtherSystem Co,.Ltd 2020-2-15.

Shader "Custom/CausticsGround" {

	Properties{
		_MainTex("MainTex", 2D) = "white" {}
		_SecondTex("SecondTex", 2D) = "white" {}
		_ThirdTex("ThirdTex", 2D) = "white" {}
		_HorizontalBar("HorizontalBar", Vector) = (0,0,0,0)
		_SkyColor("SkyColor", Color) = (0.3, 1.0, 1.0, 1.0)
		_SunLightColor("SunLightColor", Color) = (1.7, 0.65, 0.65, 1.0)
		_SkyLightColor("SkyLightColor", Color) = (0.8, 0.35, 0.15, 1.0)
		_IndLightColor("IndLightColor", Color) = (0.4, 0.3, 0.2, 1.0)
		_HorizonColor("HorizonColor", Color) = (0.0, 0.05, 0.2, 1.0)
		_SunDirection("SunDirection", Vector) = (0.8, 0.8, 0.6)
		_CausticTileSize("CausticTileSize", Float) = 1.1
		_CausticDensity("CausticDensity", Float) = 8
		_CausticSpeed("CausticSpeed", Float) = 0.5
	}

		SubShader{
			Tags { "RenderType" = "Transparent"}

			Pass {
				Tags {"LightMode" = "ForwardBase"}
					Blend SrcAlpha OneMinusSrcAlpha

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "Lighting.cginc"

				#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
				#include "AutoLight.cginc"

				struct VertexInput {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float4 tangent : TANGENT;
					float3 normal : NORMAL;
					float2 tiling_offset_uv : TEXCOORD2;
				};

				struct VertexOutput {
					float2 uv : TEXCOORD0;
					float4 pos : SV_POSITION;
					SHADOW_COORDS(1)
					fixed3 diff : COLOR0;
					fixed3 ambient : COLOR1;
					float2 tiling_offset_uv : TEXCOORD2;
				};

				//Variables
				#define iTime _Time.y
				float4 _HorizontalBar;
				sampler2D _MainTex;
				sampler2D _SecondTex;
				sampler2D _ThirdTex;
				float4 _ThirdTex_ST;
				float4 _SkyColor;
				float4 _SunLightColor;
				float4 _SkyLightColor;
				float4 _IndLightColor;
				float4 _HorizonColor;
				float3 _SunDirection;
				float _CausticTileSize;
				float _CausticDensity;
				float _CausticSpeed;

				#define TAU 6.28318530718
				#define MAX_ITER 5

				float3 caustic(float2 uv)
				{
					float2 p = fmod(uv*TAU, TAU) - 250.0;
					float time = iTime * _CausticSpeed + 23.0;
					float2 i = p;
					float c = 1.0;
					float inten = .005;
					for (int n = 0; n < MAX_ITER; n++)
					{
						float t = time * (1.0 - (3.5 / float(n + 1)));
						i = p + float2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
						c += 1.0 / length(float2(p.x / (sin(i.x + t) / inten),p.y / (cos(i.y + t) / inten)));
					}
					c /= float(MAX_ITER);
					c = 1.17 - pow(c, 1.4);
					float init = pow(abs(c), _CausticDensity);
					float3 color = float3(init, init, init);
					color = clamp(color + float3(0.0, 0.35, 0.5), 0.0, 1.0);
					color = lerp(color, float3(1.0,1.0,1.0),0.3);
					return color;
				}

				// perf increase for god ray, eliminates Y
				fixed causticX(fixed x, fixed power, fixed gtime)
				{
					fixed p = fmod(x*TAU, TAU) - 250.0;
					fixed time = gtime * .5 + 23.0;
					fixed i = p;
					fixed c = 1.0;
					fixed inten = .005;
					for (int n = 0; n < MAX_ITER / 2; n++)
					{
						fixed t = time * (1.0 - (3.5 / fixed(n + 1)));
						i = p + cos(t - i) + sin(t + i);
						c += 1.0 / length(p / (sin(i + t) / inten));
					}
					c /= fixed(MAX_ITER);
					c = 1.17 - pow(c, power);
					return c;
				}

				float noise(in float2 p)
				{
					float height = lerp(tex2D(_MainTex, p / 80.0).x,1.0,0.85);
					float height2 = lerp(tex2D(_SecondTex, p / 700.0).x,0.0,-3.5);
					return height2 - height - 0.179;
				}

				float fBm(in float2 p)
				{
					float sum = 0.0;
					float amp = 1.0;

					[unroll(100)]
					for (int i = 0; i < 4; i++)
					{
						sum += amp * noise(p);
						amp *= 0.5;
						p *= 2.5;
					}
					return sum * 0.5 + 0.15;
				}

				float3 raymarchTerrain(in float3 ro, in float3 rd, in float tmin, in float tmax)
				{
					float t = tmin;
					float3 res = float3(-1.0,-1.0,-1.0);
					for (int i = 0; i < 110; i++)
					{
						float3 p = ro + rd * t;
						res = float3(float2(0.0, p.y - fBm(p.xz)), t);
						float d = res.y;
						if (d < (0.001 * t) || t > tmax)
						{
							break;
						}
						t += 0.5 * d;
					}
					return res;
				}

				float3 getTerrainNormal(in float3 p)
				{
					float eps = 0.025;
					return normalize(float3(fBm(float2(p.x - eps, p.z)) - fBm(float2(p.x + eps, p.z)),
											2.0 * eps,
											fBm(float2(p.x, p.z - eps)) - fBm(float2(p.x, p.z + eps))));
				}

				VertexOutput vert(VertexInput v)
				{
					VertexOutput o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					half3 worldNormal = UnityObjectToWorldNormal(v.normal);
					half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
					o.diff = nl * _LightColor0.rgb;
					o.tiling_offset_uv = float2(o.uv.xy * _ThirdTex_ST.xy + _ThirdTex_ST.zw);
					TRANSFER_SHADOW(o);
					return o;
				}

				float4 frag(VertexOutput i) : SV_Target
				{
					float3 skyColor = _SkyColor;
					float3 sunLightColor = _SunLightColor;
					float3 skyLightColor = _SkyLightColor;
					float3 indLightColor = _IndLightColor;
					float3 horizonColor = _HorizonColor;
					float3 sunDirection = normalize(_SunDirection);
					float2 p = (-1 + 2.0 * i.uv) / 1;
					float3 eye = float3(0.0, 1.0, 0.0);
					float2 rot = 6.2831 * (fixed2(-0.05 + 1 * 0.01, 0.0 - sin(1 * 0.5) * 0.01) + fixed2(1.0, 0.0) * (_HorizontalBar.xy - 1 * 0.25) / 1);
					eye.yz = cos(rot.y) * eye.yz + sin(rot.y) * eye.zy * float2(-1.0, 1.0);
					eye.xz = cos(rot.x) * eye.xz + sin(rot.x) * eye.zx * float2(1.0, -1.0);
					float3 ro = eye;
					float3 ta = float3(0.5, 0.0, 0.0);
					float3 cw = normalize(ta - ro);
					float3 cu = normalize(cross(float3(0.0, 1.0, 0.0), cw));
					float3 cv = normalize(cross(cw, cu));
					float3x3 cam = float3x3(cu, cv, cw);
					float3 rd = mul(normalize(float3(p.x, p.y, 1.0)), cam);
					// background
					float3 color = _SkyColor;
					float sky = 0.0;
					// terrain marching
					float tmin = 0.1;
					float tmax = 20.0;
					float3 res = raymarchTerrain(ro, rd, tmin, tmax);
					float t = res.z;
					float3 pos = ro + rd * t;
					float3 nor;
					// add bumps
					nor = getTerrainNormal(pos);
					nor = normalize(nor + 0.5 * getTerrainNormal(pos * 8.0));
					float sun = clamp(dot(sunDirection, nor), 0.0, 1.0);
					sky = clamp(0.5 + 0.5 * nor.y, 0.0, 1.0);
					float3 diffuse = lerp(tex2D(_ThirdTex, float2(pos.x*pow(pos.y, 0.01)*i.tiling_offset_uv.x, pos.z*pow(pos.y, 0.01)*i.tiling_offset_uv.y)).xyz, float3(1.0, 1.0, 1.0), clamp(1.1 - pos.y, 0.0, 1.0));
					diffuse *= caustic(float2(lerp(pos.x, pos.y, 0.2), lerp(pos.z, pos.y, 0.2))*_CausticTileSize);
					float3 lightColor = 1.0 * sun * sunLightColor;
					lightColor += sky * skyLightColor;
					color *= diffuse * lightColor;
					// gamma correction
					float3 gamma = float3(0.46, 0.46, 0.46);
					float4 col = float4(pow(color, gamma), 1.0);
					fixed shadow = SHADOW_ATTENUATION(i);
					fixed3 lighting = i.diff * shadow + i.ambient;
					col.rgb *= lighting;
					return col;
				}

			ENDCG
			}
			// Projection Support
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
		}
}

