Shader "Custom/TopFaceGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        LOD 100
        
        // Additive Blending: Altındaki orijinal rengi bozmadan sadece üstüne ışık ekler
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
                float3 worldNormal : TEXCOORD0;
            };

            float4 _GlowColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Normali dünya koordinatlarına çeviriyoruz
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Yüzeyin ne kadar yukarı baktığını hesapla (Y ekseniyle olan açı)
                float upDot = dot(normalize(i.worldNormal), float3(0, 1, 0));
                
                // Eğer yüzey yukarı (top face) bakıyorsa, glow rengini ver
                // 0.5 eşiği, düz veya hafif eğimli üst yüzeyleri seçmek içindir.
                if (upDot > 0.5)
                {
                    return _GlowColor;
                }
                
                // Yukarı bakmayan (yan duvarlar, alt vs.) kısımları siyah (şeffaf) yap
                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
