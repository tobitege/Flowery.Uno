#if __WASM__ || HAS_UNO_WASM || __ANDROID__ || __IOS__
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
#endif

namespace Flowery.Services;

#if __WASM__ || HAS_UNO_WASM || __ANDROID__ || __IOS__
internal static class WasmCompositionAnimationScheduler
{
    private sealed class AnimationBinding
    {
        public AnimationBinding(CompositionAnimation animation, Visual visual, string propertyName, string subPropertyName)
        {
            Animation = new WeakReference<CompositionAnimation>(animation);
            Visual = new WeakReference<Visual>(visual);
            PropertyName = propertyName;
            SubPropertyName = subPropertyName;
        }

        public WeakReference<CompositionAnimation> Animation { get; }
        public WeakReference<Visual> Visual { get; }
        public string PropertyName { get; }
        public string SubPropertyName { get; }
    }

    private sealed class AnimationState
    {
        public Vector3 Translation;
        public bool HasTranslation;
        public Vector3 Offset;
        public bool HasOffset;
        public Vector3 Scale = new(1f, 1f, 1f);
        public bool HasScale;
        public float Rotation;
        public bool HasRotation;
        public float Opacity = 1f;
        public bool HasOpacity;
        public Vector3 CenterPoint;
        public bool HasCenterPoint;
    }

    private static readonly object Gate = new();
    private static readonly List<AnimationBinding> Bindings = [];
    private static readonly HashSet<UIElement> DirtyElements = [];
    private static readonly ConditionalWeakTable<UIElement, CompositeTransform> Transforms = new();
    private static readonly ConditionalWeakTable<UIElement, AnimationState> States = new();
    private static readonly Action<CompositionAnimation>? RaiseAnimationFrame = CreateRaiseAnimationFrame();
    private static readonly Func<CompositionAnimation, object?>? EvaluateAnimation = CreateEvaluateAnimation();
    private static readonly Func<Visual, object?>? GetNativeOwner = CreateNativeOwnerGetter();
    private static bool _isRenderingHooked;

    public static void Register(CompositionAnimation animation, Visual visual, string property)
    {
        if (RaiseAnimationFrame == null || EvaluateAnimation == null || GetNativeOwner == null)
        {
            return;
        }

        var propertyName = property;
        var subPropertyName = string.Empty;
        var separatorIndex = property.IndexOf('.');
        if (separatorIndex >= 0)
        {
            propertyName = property[..separatorIndex];
            subPropertyName = property[(separatorIndex + 1)..];
        }

        lock (Gate)
        {
            Bindings.Add(new AnimationBinding(animation, visual, propertyName, subPropertyName));
            if (!_isRenderingHooked)
            {
                CompositionTarget.Rendering += OnRendering;
                _isRenderingHooked = true;
            }
        }
    }

    private static void OnRendering(object? sender, object e)
    {
        if (RaiseAnimationFrame == null || EvaluateAnimation == null || GetNativeOwner == null)
        {
            CompositionTarget.Rendering -= OnRendering;
            _isRenderingHooked = false;
            return;
        }

        lock (Gate)
        {
            DirtyElements.Clear();

            for (var i = Bindings.Count - 1; i >= 0; i--)
            {
                var binding = Bindings[i];
                if (!binding.Animation.TryGetTarget(out var animation) ||
                    !binding.Visual.TryGetTarget(out var visual))
                {
                    Bindings.RemoveAt(i);
                    continue;
                }

                RaiseAnimationFrame(animation);
                var value = EvaluateAnimation(animation);
                if (value != null)
                {
                    ApplyAnimationValue(binding, visual, value);
                }
            }

            foreach (var element in DirtyElements)
            {
                if (States.TryGetValue(element, out var state))
                {
                    ApplyState(element, state);
                }
            }

            if (Bindings.Count == 0)
            {
                CompositionTarget.Rendering -= OnRendering;
                _isRenderingHooked = false;
            }
        }
    }

    private static void ApplyAnimationValue(AnimationBinding binding, Visual visual, object value)
    {
        if (GetNativeOwner?.Invoke(visual) is not UIElement element)
        {
            return;
        }

        var state = States.GetValue(element, _ => new AnimationState());
        switch (binding.PropertyName)
        {
            case "Opacity":
                if (TryGetScalar(value, out var opacity))
                {
                    state.HasOpacity = true;
                    state.Opacity = opacity;
                    DirtyElements.Add(element);
                }
                break;
            case "Scale":
                if (ApplyVectorValue(value, binding.SubPropertyName, ref state.Scale, scaleDefaultsToOne: true))
                {
                    state.HasScale = true;
                    state.CenterPoint = GetCenterPoint(visual, element);
                    state.HasCenterPoint = true;
                    DirtyElements.Add(element);
                }
                break;
            case "Translation":
                if (ApplyVectorValue(value, binding.SubPropertyName, ref state.Translation, scaleDefaultsToOne: false))
                {
                    state.HasTranslation = true;
                    DirtyElements.Add(element);
                }
                break;
            case "Offset":
                if (ApplyVectorValue(value, binding.SubPropertyName, ref state.Offset, scaleDefaultsToOne: false))
                {
                    state.HasOffset = true;
                    DirtyElements.Add(element);
                }
                break;
            case "RotationAngle":
                if (TryGetScalar(value, out var rotation))
                {
                    state.HasRotation = true;
                    state.Rotation = rotation;
                    state.CenterPoint = GetCenterPoint(visual, element);
                    state.HasCenterPoint = true;
                    DirtyElements.Add(element);
                }
                break;
        }
    }

    private static Vector3 GetCenterPoint(Visual visual, UIElement element)
    {
        var centerPoint = visual.CenterPoint;
        if (centerPoint != Vector3.Zero)
        {
            return centerPoint;
        }

        if (element is FrameworkElement frameworkElement)
        {
            var width = frameworkElement.ActualWidth;
            var height = frameworkElement.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                if (!double.IsNaN(frameworkElement.Width) && frameworkElement.Width > 0)
                    width = frameworkElement.Width;
                if (!double.IsNaN(frameworkElement.Height) && frameworkElement.Height > 0)
                    height = frameworkElement.Height;
            }

            if (width > 0 && height > 0)
            {
                var origin = frameworkElement.RenderTransformOrigin;
                if (origin.X != 0 || origin.Y != 0)
                {
                    return new Vector3((float)(origin.X * width), (float)(origin.Y * height), 0);
                }

                return new Vector3((float)(width / 2), (float)(height / 2), 0);
            }
        }

        return centerPoint;
    }

    private static bool HasNonDefaultOrigin(FrameworkElement element)
    {
        var origin = element.RenderTransformOrigin;
        return origin.X != 0 || origin.Y != 0;
    }

    private static void ApplyState(UIElement element, AnimationState state)
    {
        var transform = GetOrCreateTransform(element);

        if (state.HasTranslation || state.HasOffset)
        {
            var translation = state.HasTranslation ? state.Translation : Vector3.Zero;
            var offset = state.HasOffset ? state.Offset : Vector3.Zero;
            transform.TranslateX = translation.X + offset.X;
            transform.TranslateY = translation.Y + offset.Y;
        }

        if (state.HasScale)
        {
            transform.ScaleX = state.Scale.X;
            transform.ScaleY = state.Scale.Y;
        }

        if (state.HasRotation)
        {
            transform.Rotation = state.Rotation * (180.0 / Math.PI);
        }

        if (state.HasOpacity)
        {
            element.Opacity = state.Opacity;
        }

        if (state.HasCenterPoint)
        {
            if (state.HasRotation)
            {
                // Rotation is unreliable on Android without an explicit center.
                transform.CenterX = state.CenterPoint.X;
                transform.CenterY = state.CenterPoint.Y;
            }
            else if (state.HasScale && element is FrameworkElement frameworkElement && HasNonDefaultOrigin(frameworkElement))
            {
                // RenderTransformOrigin already applies the pivot; avoid double-offsetting the transform.
                transform.CenterX = 0;
                transform.CenterY = 0;
            }
            else
            {
                transform.CenterX = state.CenterPoint.X;
                transform.CenterY = state.CenterPoint.Y;
            }
        }
    }

    private static bool ApplyVectorValue(object value, string subPropertyName, ref Vector3 target, bool scaleDefaultsToOne)
    {
        if (string.IsNullOrEmpty(subPropertyName))
        {
            if (TryGetVector3(value, out var vector3))
            {
                target = vector3;
                return true;
            }

            if (TryGetVector2(value, out var vector2))
            {
                target = new Vector3(vector2, scaleDefaultsToOne ? 1f : 0f);
                return true;
            }

            if (TryGetScalar(value, out var scalar))
            {
                if (scaleDefaultsToOne)
                {
                    target = new Vector3(scalar, scalar, 1f);
                }
                else
                {
                    target = new Vector3(scalar, 0f, 0f);
                }
                return true;
            }

            return false;
        }

        if (!TryGetScalar(value, out var component))
        {
            return false;
        }

        switch (subPropertyName)
        {
            case "X":
                target.X = component;
                return true;
            case "Y":
                target.Y = component;
                return true;
            case "Z":
                target.Z = component;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetScalar(object value, out float scalar)
    {
        switch (value)
        {
            case float f:
                scalar = f;
                return true;
            case double d:
                scalar = (float)d;
                return true;
            case int i:
                scalar = i;
                return true;
            case uint u:
                scalar = u;
                return true;
            case long l:
                scalar = l;
                return true;
            case short s:
                scalar = s;
                return true;
            case byte b:
                scalar = b;
                return true;
            default:
                scalar = 0f;
                return false;
        }
    }

    private static bool TryGetVector2(object value, out Vector2 vector)
    {
        switch (value)
        {
            case Vector2 v2:
                vector = v2;
                return true;
            case Vector3 v3:
                vector = new Vector2(v3.X, v3.Y);
                return true;
            default:
                vector = default;
                return false;
        }
    }

    private static bool TryGetVector3(object value, out Vector3 vector)
    {
        switch (value)
        {
            case Vector3 v3:
                vector = v3;
                return true;
            case Vector2 v2:
                vector = new Vector3(v2, 0f);
                return true;
            default:
                vector = default;
                return false;
        }
    }

    private static CompositeTransform GetOrCreateTransform(UIElement element)
    {
        if (Transforms.TryGetValue(element, out var existing))
        {
            return existing;
        }

        CompositeTransform transform;
        switch (element.RenderTransform)
        {
            case CompositeTransform composite:
                transform = composite;
                break;
            case TransformGroup group:
                transform = group.Children.OfType<CompositeTransform>().FirstOrDefault() ?? new CompositeTransform();
                if (!group.Children.Contains(transform))
                {
                    group.Children.Add(transform);
                }
                break;
            case null:
            case MatrixTransform { Matrix: { IsIdentity: true } }:
                transform = new CompositeTransform();
                element.RenderTransform = transform;
                break;
            default:
                var existingTransform = element.RenderTransform;
                var newGroup = new TransformGroup();
                if (existingTransform != null)
                {
                    newGroup.Children.Add(existingTransform);
                }
                transform = new CompositeTransform();
                newGroup.Children.Add(transform);
                element.RenderTransform = newGroup;
                break;
        }

        Transforms.Add(element, transform);
        return transform;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(CompositionAnimation))]
    private static Action<CompositionAnimation>? CreateRaiseAnimationFrame()
    {
        var method = typeof(CompositionAnimation).GetMethod("RaiseAnimationFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            return null;
        }

        return (Action<CompositionAnimation>)method.CreateDelegate(typeof(Action<CompositionAnimation>));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicProperties, typeof(Visual))]
    private static Func<Visual, object?>? CreateNativeOwnerGetter()
    {
        var property = typeof(Visual).GetProperty("NativeOwner", BindingFlags.Instance | BindingFlags.NonPublic);
        if (property == null || property.GetGetMethod(true) == null)
        {
            return null;
        }

        return (Func<Visual, object?>)property.GetGetMethod(true)!.CreateDelegate(typeof(Func<Visual, object?>));
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(CompositionAnimation))]
    private static Func<CompositionAnimation, object?>? CreateEvaluateAnimation()
    {
        var method = typeof(CompositionAnimation).GetMethod("Evaluate", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (method == null)
        {
            return null;
        }

        return (Func<CompositionAnimation, object?>)method.CreateDelegate(typeof(Func<CompositionAnimation, object?>));
    }
}
#else
internal static class WasmCompositionAnimationScheduler
{
    public static void Register(CompositionAnimation animation, Visual visual, string property)
    {
    }
}
#endif
