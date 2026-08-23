Shader "Custom/TopFaceGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _NormalThreshold ("Normal Eşiği", Range(0, 1)) = 0.50
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        LOD 100
        
        Blend One One
        ZWrite Off
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float upDot : TEXCOORD0;
            };

            float4 _GlowColor;
            float _NormalThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // Optimizasyon: Dot product hesaplamasını Fragment yerine Vertex shader'a taşıdık
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.upDot = dot(normalize(worldNormal), float3(0, 1, 0));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // SADECE ve SADECE tam anlamıyla dümdüz yukarı bakan yüzeyler parlayacak.
                if (i.upDot >= _NormalThreshold)
                {
                    return _GlowColor;
                }
                
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
