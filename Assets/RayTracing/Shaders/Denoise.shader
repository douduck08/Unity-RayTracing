Shader "Hidden/Denoise" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Exponent ("Exponent", float) = 1
    }
    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Exponent;

            half4 frag (v2f_img i) : SV_Target {
                float exponent = 1.0;

                half4 center = tex2D(_MainTex, i.uv);

                half4 color = 0;
                float total = 0.0;
                for (float x = -4.0; x <= 4.0; x += 1.0) {
                    for (float y = -4.0; y <= 4.0; y += 1.0) {
                        half4 side = tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy);
                        float weight = 1.0 - abs(dot(side.rgb - center.rgb, 0.25));
                        weight = pow(weight, _Exponent);
                        color += side * weight;
                        total += weight;
                    }
                }
                
                center.rgb = color.rgb / total;
                return center;
            }
            ENDCG
        }
    }
}
