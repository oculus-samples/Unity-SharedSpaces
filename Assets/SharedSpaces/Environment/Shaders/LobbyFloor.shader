// Copyright (c) Facebook, Inc. and its affiliates.

Shader "Unlit/LobbyFloor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FadeOut ("Fade Out", Range(0,1)) = 0.1
        _FadeOutPow ("Fade Out Power", Range(0,3)) = 2
        _Color ("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FadeOut;
            float _FadeOutPow;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                col.a = saturate(min(min(i.uv.x, 1 - i.uv.x), min(i.uv.y, 1 - i.uv.y)) / _FadeOut);
                col.a = pow(col.a, _FadeOutPow);
                return col * _Color;
            }
            ENDCG
        }
    }
}
