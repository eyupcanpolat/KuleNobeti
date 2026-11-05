Shader "KuleNobeti/ServiceSurface"
{
    Properties
    {
        _Color ("Yüzey rengi", Color) = (1,1,1,1)
        _MainTex ("Temel renk", 2D) = "white" {}
        [Normal] _BumpMap ("Yüzey normali", 2D) = "bump" {}
        _ArmMap ("AO / Pürüzlülük / Metal", 2D) = "white" {}
        _NormalStrength ("Normal etkisi", Range(0,2)) = 1
        _Saturation ("Doku renk doygunluğu", Range(0,1)) = 1
        _AlbedoGain ("Doku renk çarpanı", Range(0,4)) = 1
        _Smoothness ("Parlaklık sınırı", Range(0,1)) = 1
        _Metallic ("Metal taban değeri", Range(0,1)) = 0
        _EmissionColor ("Gösterge ışığı", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _ArmMap;
        fixed4 _Color, _EmissionColor;
        half _NormalStrength, _Saturation, _AlbedoGain, _Smoothness, _Metallic;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 baseColor = tex2D(_MainTex, IN.uv_MainTex).rgb;
            half grey = dot(baseColor, half3(.2126, .7152, .0722));
            o.Albedo = saturate(lerp(grey.xxx, baseColor, _Saturation) * _Color.rgb * _AlbedoGain);
            half3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Normal = normalize(lerp(half3(0,0,1), normal, _NormalStrength));
            fixed3 arm = tex2D(_ArmMap, IN.uv_MainTex).rgb;
            o.Occlusion = arm.r;
            o.Smoothness = saturate((1 - arm.g) * _Smoothness);
            o.Metallic = saturate(arm.b + _Metallic);
            o.Emission = _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
