Shader "Custom/TestShader"
{
    Properties
    {
        _Color ("Outer Color", Color) = (1,1,1,1)
        _InnerColor ("Inner Color", Color) = (1,1,1,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            float4 _Color;
            float4 _InnerColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * 2 - 1; // centre = (0,0)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = length(i.uv);
                float alpha = saturate(dist); // plus transparent vers le centre
                return lerp(_InnerColor, _Color, alpha);
            }
            ENDCG
        }
    }
}
