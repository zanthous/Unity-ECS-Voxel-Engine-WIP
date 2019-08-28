Shader "Custom / TextureArray"

{

Properties

{

_Textures("Textures", 2DArray) = "" {}

}

SubShader

{

Pass

{

CGPROGRAM

#pragma vertex vert

#pragma fragment frag

// to use texture arrays we need to target DX10/OpenGLES3 which

// is shader model 3.5 minimum

#pragma target 3.5

#include "UnityCG.cginc"

struct appdata_t

{

float4 vertex : POSITION;

float3 texcoord : TEXCOORD0;

};

struct v2f

{

float3 uv : TEXCOORD0;

float4 vertex : SV_POSITION;

};

UNITY_DECLARE_TEX2DARRAY(_Textures);

v2f vert(appdata_t v)

{

v2f o;

o.vertex = UnityObjectToClipPos(v.vertex);

o.uv.xy = v.texcoord;

o.uv.z = v.texcoord.z;

return o;

}

half4 frag(v2f i) : SV_Target

{

return UNITY_SAMPLE_TEX2DARRAY(_Textures, i.uv);

}

ENDCG

}

}

}