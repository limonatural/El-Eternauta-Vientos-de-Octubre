// Shader retro de la beta (estilo PS1 / Quake): textura con píxeles duros,
// luz horneada en el color de los vértices, niebla gris afuera y oscuridad adentro,
// linterna en primera persona y colores reducidos.
// Es un shader sin "LightMode", así que URP lo dibuja igual (pasada SRPDefaultUnlit).
Shader "Eternauta/Retro"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Tinte (luz del lugar)", Color) = (1,1,1,1)
        _Resaltado ("Resaltado azul (objeto seleccionado)", Range(0,1)) = 0
        _NieblaMul ("Multiplicador de niebla", Float) = 1
        _Interior ("Forzar interior", Range(0,1)) = 0
        _Brillo ("Emite luz", Range(0,1)) = 0
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull [_Cull]
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Resaltado, _NieblaMul, _Interior, _Brillo;

            // Globales (las pone el juego)
            float4 _EtNieblaColor;
            float _EtNieblaDensidad;
            float _EtLinterna;
            float _EtNiveles;
            float4 _EtNorte; // x = z donde empieza, y = largo de la transición, z = multiplicador lejano

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 extra : TEXCOORD1; float4 color : COLOR; float3 vpos : TEXCOORD2; float wz : TEXCOORD3; };

            v2f vert (appdata v)
            {
                v2f o;
                float3 vp = UnityObjectToViewPos(v.vertex.xyz);
                o.pos = mul(UNITY_MATRIX_P, float4(vp, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.extra = float2(max(v.uv2.x, _Interior), max(v.uv2.y, _Brillo));
                o.color = v.color;
                o.vpos = vp;
                o.wz = mul(unity_ObjectToWorld, v.vertex).z;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                clip(t.a - 0.5);
                float emis = i.extra.y;
                float inter = i.extra.x;
                float3 c = t.rgb * i.color.rgb * 2.0 * lerp(_Color.rgb, float3(1,1,1), emis);

                float d = length(i.vpos);
                float3 dir = i.vpos / max(d, 0.0001);
                float cono = saturate((-dir.z - 0.9) / 0.1);
                c += t.rgb * _EtLinterna * cono * saturate(1.0 - d / 9.0) * 0.9 * (1.0 - emis);

                c = lerp(c, c * 0.6 + float3(0.36, 0.5, 0.62), _Resaltado);

                float norte = lerp(1.0, _EtNorte.z, saturate((i.wz - _EtNorte.x) / max(_EtNorte.y, 0.001)));
                float fe = 1.0 - exp(-d * _EtNieblaDensidad * norte * _NieblaMul);
                float fi = 1.0 - exp(-d * 0.16);
                c = lerp(c, _EtNieblaColor.rgb, fe * (1.0 - inter));
                c = lerp(c, float3(0,0,0), fi * inter * (1.0 - emis * 0.6));

                if (_EtNiveles > 1.0) c = floor(c * _EtNiveles + 0.5) / _EtNiveles;
                return fixed4(c, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
