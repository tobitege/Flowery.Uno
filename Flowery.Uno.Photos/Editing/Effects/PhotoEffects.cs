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

        // Highlights, fitted to Win2D on flat colors: out = u * (1 + sum of c * y^i * u^j * max^k * min^l) per channel,
        // with y the luminance of the mask, u the channel, max and min the largest and smallest channel of the pixel.
        // Win2D treats a saturated color differently from a grey of the same luminance, which y alone cannot tell apart.
        // The terms with power 16 reach the white end, where Win2D keeps white white while light grey darkens. Each
        // coefficient is a polynomial in the amount (powers 1 to 3, one row each), so 0 changes nothing.
        private static readonly (int Y, int U, int Max, int Min)[] HighlightsTerms = CreateHighlightsTerms();
        private static readonly float[] HighlightsUp =
        [
            -0.02263f, 0.44652f, 1.94409f, -0.60611f, 1.25591f, 1.57994f, -1.83734f, -2.35399f, -0.64738f, 3.05034f,
            -2.25524f, 2.84067f, -1.38081f, -5.4298f, 0.98319f, 2.89499f, 7.29312f, -2.11176f, -1.25573f, -2.67868f,
            6.35542f, -7.32893f, 1.74607f, 2.75736f, -0.68868f, -9.60229f, -5.4046f, -0.32316f, 4.15598f, 0.28264f,
            -5.91402f, 5.34791f, 11.9642f, 1.20061f, -6.12441f, 1.1177f, 0.34974f, -0.83359f, -0.7812f,
            2.04422f, 0.58965f, -3.7454f, 1.20344f, -11.26894f, -1.07647f, 3.52854f, 22.68205f, -1.57966f,
            -16.99927f, -0.57264f, -5.03546f, 1.03833f, 6.19873f, 3.58458f, 0.38528f, -4.64138f, 2.77197f, -2.50793f,
            3.72805f, -2.27358f, 6.8833f, -1.50471f, 0.75873f, 0.97149f, 5.59358f, 5.87417f, -3.2829f, -10.57916f,
            -0.34445f, -6.74445f, -4.19408f, -6.02742f, 4.52987f, 9.68475f, -2.77622f, -0.99129f, 2.18074f, 1.93446f,
            -1.50163f, -0.58795f, 1.66056f, -0.37f, 8.88491f, 0.0652f, -0.78362f, -17.7187f, 1.77465f, 12.22088f,
            0.27543f, 2.47482f, -0.44022f, -2.28371f, -3.76237f, -1.7106f, 2.39433f, -0.29083f, 3.41207f, -3.48568f,
            -0.36435f, -1.75285f, -0.67601f, -1.70364f, -1.71637f, 0.11266f, -2.06498f, 2.21653f, 5.2845f, -0.25159f,
            6.16949f, 2.18487f, -0.43771f, -2.74817f, -4.28411f, 1.6131f, 0.84616f, -1.26643f, -1.40012f
        ];
        private static readonly float[] HighlightsDown =
        [
            -0.04482f, -0.50693f, 0.33308f, 0.61759f, -0.72569f, -1.77151f, -0.15042f, 1.42186f, 0.59728f, -0.59958f,
            2.0694f, -0.5669f, 0.12898f, 0.18348f, -1.41903f, -0.22964f, -4.19225f, 1.11415f, 1.00676f, 1.32147f,
            -4.46817f, 3.50685f, -1.46731f, 1.40381f, 3.08298f, -1.4733f, 4.81178f, 0.1253f, -0.973f, -0.19322f,
            0.64738f, -3.60987f, 0.36901f, -3.28738f, 2.92883f, -0.62526f, -0.03605f, 0.04122f, 0.59919f,
            -0.1178f, 0.28822f, 1.91047f, -0.83369f, 0.83782f, 1.25064f, 0.84551f, -1.60956f, -0.85162f, 0.65575f,
            -1.49329f, 0.14592f, -0.2654f, 0.71271f, 1.00563f, -0.49642f, 2.65759f, -0.76971f, -1.19925f, -0.43049f,
            3.6372f, -5.33667f, -1.56144f, -0.84109f, -2.0468f, 2.26058f, -2.80928f, 0.5441f, 2.29352f, -0.73233f,
            -6.15406f, 5.9629f, -3.6429f, 2.16927f, 4.00496f, 1.82129f, 0.01355f, -0.02125f, -1.7223f,
            0.10544f, 0.02983f, -1.24458f, 0.27439f, -0.45262f, -0.39565f, -0.07121f, 0.92284f, 0.62425f, -0.38985f,
            0.47346f, 0.11025f, 0.1913f, -1.11182f, 0.0694f, 0.78265f, -0.08109f, -0.14715f, 0.47605f, -0.43735f,
            -1.35455f, 2.06862f, 0.97f, 0.55598f, -0.98743f, -1.59041f, -0.10013f, -0.28642f, -1.28592f, 1.10026f,
            4.42379f, -1.34871f, 3.08384f, -0.62664f, -4.70432f, -0.83563f, 0.00687f, 0.00366f, 1.12048f
        ];

        // Polynomials of the shadows curves, fitted to Win2D output on a test card and a photo.
        // First row: the term linear in the amount, second row: the quadratic term; powers 0 to 5 of the luminance.
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
        private static readonly Lazy<SKRuntimeEffect> HighlightsShadowsEffect = new(() => CompileShader(CreateHighlightsShadowsSksl()));
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

        // HighlightsAndShadowsEffect, fitted to its output. Highlights multiply each channel by the polynomial above, written
        // as a polynomial in u whose coefficients q0 to q3 and q16 depend on y, max and min only, so they are computed once
        // per pixel. Shadows multiply the pixel by exp(polynomial) of the luminance of the mask, mixed with the channel
        // itself. Clarity adds the difference to the mask, which is a small blur.
        private static string CreateHighlightsShadowsSksl() => $$"""
            uniform shader image;
            uniform shader blurred;
            uniform float highlights[{{HighlightsTerms.Length}}];
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
                float mx = max(u.r, max(u.g, u.b));
                float mn = min(u.r, min(u.g, u.b));
                float y16 = pow(y, 16);
                float mx16 = pow(mx, 16);
                float q0 = {{HighlightsCoefficientOfU(0)}};
                float q1 = {{HighlightsCoefficientOfU(1)}};
                float q2 = {{HighlightsCoefficientOfU(2)}};
                float q3 = {{HighlightsCoefficientOfU(3)}};
                float q16 = {{HighlightsCoefficientOfU(16)}};
                float3 h = 1 + q0 + u * (q1 + u * (q2 + u * q3)) + q16 * pow(u, float3(16));
                float3 lnShadows = mix(float3(curve(y, shadowsLow, shadowsHigh)), curves(u, shadowsLow, shadowsHigh), shadowsChannel);
                float3 o = u * h * exp(lnShadows) + (u - bu) * clarity;
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

        // VignetteEffect, fitted to its output on four image sizes: the distances to the nearest side and to the nearest top or
        // bottom edge, in units of the short side, enter a transition band of width "band" and combine as a superellipse
        // (exponent "power"); the weight rises from "start" in that combined distance to "edge" at the middle of a side.
        private const string VignetteSksl = """
            uniform shader image;
            uniform float2 size;
            uniform half4 color;
            uniform float band;
            uniform float power;
            uniform float start;
            uniform float edge;
            uniform float curve;
            half4 main(float2 p) {
                half4 c = image.eval(p);
                float2 e = min(p, size - p) / min(size.x, size.y);
                float2 a = max(band - e, 0);
                float d = pow(pow(a.x, power) + pow(a.y, power), 1 / power);
                half w = half(clamp(edge * pow(max(d - start, 0) / (band - start), curve), 0, 1));
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
            var (shadowsCurve, shadowsChannel) = shadows >= 0 ? (ShadowsUp, 0.25f) : (ShadowsDown, 0.25f);
            var s = Polynomial(shadowsCurve, Math.Abs(shadows));
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["highlights"] = HighlightsCoefficients(highlights),
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
            var band = 0.075f + 0.375f * strength;
            using var uniforms = new SKRuntimeEffectUniforms(effect)
            {
                ["size"] = new[] { width, height },
                ["color"] = new[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, 1f },
                ["band"] = band,
                ["power"] = 2.33208f + 0.04449f * strength,
                ["start"] = band * (0.32021f + 0.29799f * strength),
                ["edge"] = 0.95683f - 0.44068f * strength,
                ["curve"] = 1.0997f + 0.41252f * strength - 0.2689f * strength * strength
            };
            using var children = new SKRuntimeEffectChildren(effect) { ["image"] = image };
            return effect.ToShader(uniforms, children);
        }

        /// <summary>The coefficients of <see cref="HighlightsTerms"/> for an amount from -1 to 1.</summary>
        private static float[] HighlightsCoefficients(float amount)
        {
            var table = amount >= 0 ? HighlightsUp : HighlightsDown;
            var a = Math.Abs(amount);
            var n = HighlightsTerms.Length;
            var result = new float[n];
            for (var t = 0; t < n; t++)
                result[t] = a * table[t] + a * a * table[n + t] + a * a * a * table[2 * n + t];
            return result;
        }

        /// <summary>The terms of the highlights polynomial: all powers of y, u, max and min up to degree 3, then the white end.</summary>
        private static (int Y, int U, int Max, int Min)[] CreateHighlightsTerms()
        {
            var terms = new List<(int, int, int, int)>();
            for (var i = 0; i <= 3; i++)
                for (var j = 0; i + j <= 3; j++)
                    for (var k = 0; i + j + k <= 3; k++)
                        for (var l = 0; i + j + k + l <= 3; l++)
                            terms.Add((i, j, k, l));
            terms.Add((16, 0, 0, 0));
            terms.Add((0, 16, 0, 0));
            terms.Add((0, 0, 16, 0));
            terms.Add((16, 1, 0, 0));
            return [.. terms];
        }

        /// <summary>
        /// The SkSL sum of the highlights terms with <paramref name="uPower"/> as power of u, without u, for example
        /// <c>highlights[14] * y * mx</c>; "0" when there is none.
        /// </summary>
        private static string HighlightsCoefficientOfU(int uPower)
        {
            static IEnumerable<string> Factors(string name, int power) =>
                power == 16 ? [name + "16"] : Enumerable.Repeat(name, power);

            var terms = HighlightsTerms
                .Select((t, i) => (Term: t, Index: i))
                .Where(x => x.Term.U == uPower)
                .Select(x => string.Join(" * ", new[] { $"highlights[{x.Index}]" }
                    .Concat(Factors("y", x.Term.Y))
                    .Concat(Factors("mx", x.Term.Max))
                    .Concat(Factors("mn", x.Term.Min))))
                .ToList();
            return terms.Count == 0 ? "0" : string.Join(" + ", terms);
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
