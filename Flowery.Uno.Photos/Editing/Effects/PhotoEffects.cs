using SkiaSharp;

namespace Flowery.Uno.Photos.Editing.Effects
{
    /// <summary>
    /// Stand-ins for the Win2D photo effects that have no direct SkiaSharp equivalent, calibrated on the output of Win2D 1.3.2.
    /// The color effects return an <see cref="SKColorFilter"/>; the effects that read neighbouring pixels return an
    /// <see cref="SKShader"/> in the pixel space of their input. All effects keep the alpha channel.
    /// This folder depends on SkiaSharp only, so it can move to its own library. The caller owns what is returned.
    /// </summary>
    public static class PhotoEffects
    {
        /// <summary>Sigma in pixels of the copy that <see cref="HighlightsShadows"/> takes as mask (Win2D default).</summary>
        public const float MaskBlurSigma = 1.25f;

        // Polynomials of the highlights and shadows curves, fitted to Win2D output on a test card and a photo.
        // First row: the term linear in the amount, second row: the quadratic term; powers 0 to 5 of the luminance.
        private static readonly float[] HighlightsUp =
        [
            0.65198f, -4.56394f, 42.10039f, -112.09221f, 117.11166f, -43.27298f,
            -0.31146f, 3.93748f, -32.41418f, 81.37665f, -82.53969f, 29.99451f
        ];
        private static readonly float[] HighlightsDown =
        [
            -0.89936f, 7.91638f, -46.98113f, 96.99147f, -80.76501f, 23.73750f,
            0.81610f, -9.51002f, 45.30219f, -94.61410f, 80.99550f, -22.99641f
        ];
        private static readonly float[] ShadowsUp =
        [
            1.70364f, -11.91761f, 37.25936f, -60.85061f, 49.69315f, -15.90808f,
            -0.27206f, 2.23361f, -8.53839f, 15.90398f, -13.85432f, 4.52924f
        ];
        private static readonly float[] ShadowsDown =
        [
            -2.63189f, 11.92832f, -20.17846f, 14.22661f, -2.33649f, -1.01146f,
            1.21604f, -7.01320f, 16.05946f, -18.77218f, 11.42708f, -2.91993f
        ];

        // Compiled once; SKRuntimeEffect is immutable, so renders can run on any thread. The SKRuntime*Builder classes dispose
        // the effect with them and would crash the next render, so uniforms and children are passed directly.
        private static readonly Lazy<SKRuntimeEffect> ExposureEffect = new(() => CompileColorFilter(ExposureSksl));
        private static readonly Lazy<SKRuntimeEffect> ContrastEffect = new(() => CompileColorFilter(ContrastSksl));
        private static readonly Lazy<SKRuntimeEffect> HighlightsShadowsEffect = new(() => CompileShader(HighlightsShadowsSksl));
        private static readonly Lazy<SKRuntimeEffect> SharpenEffect = new(() => CompileShader(SharpenSksl));
        private static readonly Lazy<SKRuntimeEffect> VignetteEffect = new(() => CompileShader(VignetteSksl));

        // ExposureEffect: output = input * 2^EV.
        private const string ExposureSksl = """
            uniform half ev;
            half4 main(half4 c) {
                return half4(clamp(c.rgb * exp2(ev), 0, c.a), c.a);
            }
            """;

        // ContrastEffect, measured: y = x - 0.0975 * amount * sin(2 * pi * x) per channel.
        private const string ContrastSksl = """
            uniform half amount;
            half4 main(half4 c) {
                half a = max(c.a, 0.0001);
                half3 u = c.rgb / a;
                half3 s = u - 0.0975 * amount * sin(6.2831853 * u);
                return half4(clamp(s, 0, 1) * a, c.a);
            }
            """;

        // HighlightsAndShadowsEffect, fitted to its output: each adjustment multiplies the pixel by exp(polynomial) of the luminance
        // of the mask, mixed with the channel itself. Clarity adds the difference to the mask, which is a small blur.
        private const string HighlightsShadowsSksl = """
            uniform shader image;
            uniform shader blurred;
            uniform float3 highlightsLow;
            uniform float3 highlightsHigh;
            uniform float highlightsChannel;
            uniform float3 shadowsLow;
            uniform float3 shadowsHigh;
            uniform float shadowsChannel;
            uniform float clarity;
            float curve(float x, float3 low, float3 high) {
                float x3 = x * x * x;
                return dot(low, float3(1, x, x * x)) + x3 * dot(high, float3(1, x, x * x));
            }
            float3 curves(float3 x, float3 low, float3 high) {
                return float3(curve(x.r, low, high), curve(x.g, low, high), curve(x.b, low, high));
            }
            half4 main(float2 p) {
                half4 c = image.eval(p);
                half4 b = blurred.eval(p);
                float a = max(c.a, 0.0001);
                float3 u = c.rgb / a;
                float3 bu = b.rgb / max(b.a, 0.0001);
                float y = dot(bu, float3(0.2126, 0.7152, 0.0722));
                float3 lnGain = mix(float3(curve(y, highlightsLow, highlightsHigh)), curves(u, highlightsLow, highlightsHigh), highlightsChannel)
                              + mix(float3(curve(y, shadowsLow, shadowsHigh)), curves(u, shadowsLow, shadowsHigh), shadowsChannel);
                float3 o = u * exp(lnGain) + (u - bu) * clarity;
                return half4(clamp(o, 0, 1) * a, c.a);
            }
            """;

        // SharpenEffect, measured: a 3x3 high pass of which the amount is a linear factor.
        private const string SharpenSksl = """
            uniform shader image;
            uniform half amount;
            half4 main(float2 p) {
                half4 c = image.eval(p);
                half4 cross = image.eval(p + float2(1, 0)) + image.eval(p - float2(1, 0))
                            + image.eval(p + float2(0, 1)) + image.eval(p - float2(0, 1));
                half4 corners = image.eval(p + float2(1, 1)) + image.eval(p + float2(-1, 1))
                              + image.eval(p + float2(1, -1)) + image.eval(p + float2(-1, -1));
                half4 r = c + (0.59 * c - 0.1075 * cross - 0.04 * corners) * amount;
                half alpha = clamp(r.a, 0, 1);
                return half4(clamp(r.rgb, 0, alpha), alpha);
            }
            """;

        // VignetteEffect, fitted: not round but a rounded rectangle, whose corner radius follows the short side.
        private const string VignetteSksl = """
            uniform shader image;
            uniform float2 size;
            uniform half4 color;
            uniform float radius;
            uniform float power;
            uniform float scale;
            half4 main(float2 p) {
                half4 c = image.eval(p);
                float2 half_size = size * 0.5;
                float r = radius * min(half_size.x, half_size.y);
                float2 q = max(abs(p - half_size) - (half_size - r), 0);
                half w = half(clamp(scale * pow(length(q) / r, power), 0, 1));
                return mix(c, half4(color.rgb * c.a, c.a), w);
            }
            """;

        /// <param name="ev">Exposure in stops.</param>
        public static SKColorFilter Exposure(float ev)
        {
            var effect = ExposureEffect.Value;
            using var uniforms = new SKRuntimeEffectUniforms(effect) { ["ev"] = ev };
            return effect.ToColorFilter(uniforms);
        }

        /// <param name="amount">-1 (flat) to 1 (hard).</param>
        public static SKColorFilter Contrast(float amount)
        {
            var effect = ContrastEffect.Value;
            using var uniforms = new SKRuntimeEffectUniforms(effect) { ["amount"] = amount };
            return effect.ToColorFilter(uniforms);
        }

        /// <summary>TemperatureAndTintEffect, measured: gains on red and blue, green stays. Each gain is exponential in the amount.</summary>
        /// <param name="temperature">-1 (cool) to 1 (warm).</param>
        /// <param name="tint">-1 (green) to 1 (magenta).</param>
        public static SKColorFilter TemperatureTint(float temperature, float tint)
        {
            var red = MathF.Exp(temperature * (temperature > 0 ? 0.347f : 0.695f) - 0.231f * tint);
            var blue = MathF.Exp(-temperature * (temperature > 0 ? 0.552f : 1.1f) - 0.203f * tint);
            return SKColorFilter.CreateColorMatrix(
            [
                red, 0, 0, 0, 0,
                0, 1, 0, 0, 0,
                0, 0, blue, 0, 0,
                0, 0, 0, 1, 0
            ]);
        }

        /// <param name="image">The photo.</param>
        /// <param name="blurred">The photo blurred by <see cref="MaskBlurSigma"/>, in the same pixel space.</param>
        /// <param name="highlights">-1 to 1.</param>
        /// <param name="shadows">-1 to 1.</param>
        /// <param name="clarity">-1 to 1.</param>
        public static SKShader HighlightsShadows(SKShader image, SKShader blurred, float highlights, float shadows, float clarity)
        {
            var effect = HighlightsShadowsEffect.Value;
            var (highlightsCurve, highlightsChannel) = highlights >= 0 ? (HighlightsUp, 0f) : (HighlightsDown, 0.25f);
            var (shadowsCurve, shadowsChannel) = shadows >= 0 ? (ShadowsUp, 0.25f) : (ShadowsDown, 0.25f);
            var h = Polynomial(highlightsCurve, Math.Abs(highlights));
            var s = Polynomial(shadowsCurve, Math.Abs(shadows));
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["highlightsLow"] = h[..3],
                ["highlightsHigh"] = h[3..],
                ["highlightsChannel"] = highlightsChannel,
                ["shadowsLow"] = s[..3],
                ["shadowsHigh"] = s[3..],
                ["shadowsChannel"] = shadowsChannel,
                // Measured: taking detail away works at half the strength.
                ["clarity"] = clarity * (clarity >= 0 ? 0.78f : 0.395f)
            };
            using var children = new SKRuntimeEffectChildren(effect) { ["image"] = image, ["blurred"] = blurred };
            return effect.ToShader(uniforms, children);
        }

        /// <param name="image">The photo.</param>
        /// <param name="amount">Strength; 0 changes nothing.</param>
        public static SKShader Sharpen(SKShader image, float amount)
        {
            var effect = SharpenEffect.Value;
            using var uniforms = new SKRuntimeEffectUniforms(effect) { ["amount"] = amount };
            using var children = new SKRuntimeEffectChildren(effect) { ["image"] = image };
            return effect.ToShader(uniforms, children);
        }

        /// <param name="image">The photo.</param>
        /// <param name="width">Width of the photo in pixels.</param>
        /// <param name="height">Height of the photo in pixels.</param>
        /// <param name="color">Color of the edges.</param>
        /// <param name="strength">0 (a thin edge) to 1 (a wide, soft edge).</param>
        public static SKShader Vignette(SKShader image, float width, float height, SKColor color, float strength)
        {
            var effect = VignetteEffect.Value;
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["size"] = new[] { width, height },
                ["color"] = new[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f },
                ["radius"] = 0.1f + 0.64f * strength,
                ["power"] = 1 + 2.5f * strength,
                ["scale"] = 0.95f - 0.4f * strength
            };
            using var children = new SKRuntimeEffectChildren(effect) { ["image"] = image };
            return effect.ToShader(uniforms, children);
        }

        /// <summary>The six powers of the luminance for an adjustment: linear and quadratic in the amount.</summary>
        private static float[] Polynomial(float[] curve, float amount)
        {
            var result = new float[6];
            for (var i = 0; i < 6; i++)
                result[i] = amount * curve[i] + amount * amount * curve[i + 6];
            return result;
        }

        private static SKRuntimeEffect CompileColorFilter(string sksl) =>
            SKRuntimeEffect.CreateColorFilter(sksl, out var errors) ?? throw new InvalidOperationException(errors);

        private static SKRuntimeEffect CompileShader(string sksl) =>
            SKRuntimeEffect.CreateShader(sksl, out var errors) ?? throw new InvalidOperationException(errors);
    }
}
