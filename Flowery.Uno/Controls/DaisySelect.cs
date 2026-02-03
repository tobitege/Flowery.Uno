using System;
using System.Collections.Generic; // Added for IList
using System.Collections.ObjectModel; // Added for ObservableCollection
using System.Collections.Specialized; // Added for INotifyCollectionChanged
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Flowery.Helpers;
using Flowery.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup; // ContentProperty
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Flowery.Controls
{
    /// <summary>
    /// A ComboBox control styled after DaisyUI's Select component (Uno/WinUI).
    /// </summary>
    [ContentProperty(Name = nameof(Items))]
    public partial class DaisySelect : DaisyComboBoxBase
    {
        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisySelectVariant),
                typeof(DaisySelect),
                new PropertyMetadata(DaisySelectVariant.Bordered, OnStylePropertyChanged));

        public static readonly DependencyProperty LocalizationSourceProperty =
            DependencyProperty.Register(
                nameof(LocalizationSource),
                typeof(object),
                typeof(DaisySelect),
                new PropertyMetadata(null, OnLocalizationSourceChanged));

        public DaisySelectVariant Variant
        {
            get => (DaisySelectVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public object? LocalizationSource
        {
            get => GetValue(LocalizationSourceProperty);
            set => SetValue(LocalizationSourceProperty, value);
        }

        // Items property logic wrapper around ItemsSource for XAML support
        // Note: DaisyComboBoxBase has Items (ObservableCollection).
        // We just need to ensure the ContentProperty works.
        // DaisyComboBoxBase already has [ContentProperty] and Items.
        // But we need to make sure DaisySelect exposes it cleanly or uses Base's.
        // Base defines public IList<object> Items { get; }

        public event EventHandler<object?>? ManualSelectionChanged;
        private INotifyPropertyChanged? _localizationNotifier;

        public DaisySelect() : base(true)
        {
            // Constructor
            SelectionChanged += (s, e) =>
            {
                 // Propagate to ManualSelectionChanged for compatibility
                 if (e.AddedItems.Count > 0)
                    ManualSelectionChanged?.Invoke(this, e.AddedItems[0]);
            };

            if (Items is ObservableCollection<object> items)
            {
                items.CollectionChanged += OnItemsCollectionChanged;
            }
        }

        private static void OnStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySelect select)
            {
                select.ApplyAll();
            }
        }

        private static void OnLocalizationSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisySelect select)
            {
                select.HookLocalizationSource(e.OldValue, e.NewValue);
            }
        }

        protected override void OnBeforeLoaded()
        {
            base.OnBeforeLoaded();
        }

        protected override void OnAfterLoaded()
        {
            base.OnAfterLoaded();
            ApplyLocalizationToItems();
        }

        protected override void ApplyTheme(ResourceDictionary? resources)
        {
            var transparent = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            var base100 = GetBrush(resources, "DaisyBase100Brush", new SolidColorBrush(Microsoft.UI.Colors.White));
            var base300 = GetBrush(resources, "DaisyBase300Brush", new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)));
            var baseContent = GetBrush(resources, "DaisyBaseContentBrush", new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)));

            string variantName = Variant switch
            {
                DaisySelectVariant.Primary => "Primary",
                DaisySelectVariant.Secondary => "Secondary",
                DaisySelectVariant.Accent => "Accent",
                DaisySelectVariant.Info => "Info",
                DaisySelectVariant.Success => "Success",
                DaisySelectVariant.Warning => "Warning",
                DaisySelectVariant.Error => "Error",
                DaisySelectVariant.Ghost => "Ghost",
                _ => "Bordered"
            };

            var borderOverride = !string.IsNullOrEmpty(variantName) && variantName != "Bordered"
                ? DaisyResourceLookup.TryGetControlBrush(this, "DaisySelect", $"{variantName}BorderBrush")
                : null;
            borderOverride ??= DaisyResourceLookup.TryGetControlBrush(this, "DaisySelect", "BorderBrush");

             var variantBorderKey = Variant switch
            {
                DaisySelectVariant.Primary => "DaisyPrimaryBrush",
                DaisySelectVariant.Secondary => "DaisySecondaryBrush",
                DaisySelectVariant.Accent => "DaisyAccentBrush",
                DaisySelectVariant.Info => "DaisyInfoBrush",
                DaisySelectVariant.Success => "DaisySuccessBrush",
                DaisySelectVariant.Warning => "DaisyWarningBrush",
                DaisySelectVariant.Error => "DaisyErrorBrush",
                _ => ""
            };

            Brush background;
            Brush foreground;
            Brush border;
            Thickness borderThickness;

            // Logic similar to original DaisySelect
             switch (Variant)
            {
                case DaisySelectVariant.Primary:
                case DaisySelectVariant.Secondary:
                case DaisySelectVariant.Accent:
                case DaisySelectVariant.Info:
                case DaisySelectVariant.Success:
                case DaisySelectVariant.Warning:
                case DaisySelectVariant.Error:
                    background = base100;
                    foreground = baseContent;
                    border = borderOverride ?? GetBrush(resources, variantBorderKey, base300);
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
                case DaisySelectVariant.Ghost:
                    background = transparent;
                    foreground = baseContent;
                    border = transparent;
                    borderThickness = new Thickness(0);
                    break;
                case DaisySelectVariant.Bordered:
                default:
                    background = base100;
                    foreground = baseContent;
                    border = borderOverride ?? base300;
                    borderThickness = GetThickness(resources, "DaisyBorderThicknessThin", new Thickness(1));
                    break;
            }

            Background = background;
            Foreground = foreground;
            BorderBrush = border;
            BorderThickness = borderThickness;

            // Ensure internal popup/list visuals receive theme colors.
            base.ApplyTheme(resources);
        }

        protected override void ApplySizing(ResourceDictionary? resources)
        {
            base.ApplySizing(resources);

             FontSize = GetSizeDouble(resources, "DaisySize", Size, "FontSize", DaisyResourceLookup.GetDefaultFontSize(Size));
        }

        protected override void OnAfterApplyAll(ResourceDictionary? resources)
        {
            // Sync properties to Visual Tree parts if not bound
            // Since _rootGrid is private in Base, Base needs to handle it.
            // Assuming Base implementation ApplyTheme/ApplySizing does the right thing or binds.
            // I need to update Base to be robust.
        }

        public void RefreshLocalization()
        {
            ApplyLocalizationToItems();
        }

        private void HookLocalizationSource(object? oldValue, object? newValue)
        {
            if (oldValue is INotifyPropertyChanged oldNotifier)
            {
                oldNotifier.PropertyChanged -= OnLocalizationSourcePropertyChanged;
            }

            if (newValue is INotifyPropertyChanged newNotifier)
            {
                newNotifier.PropertyChanged += OnLocalizationSourcePropertyChanged;
                _localizationNotifier = newNotifier;
            }
            else
            {
                _localizationNotifier = null;
            }

            ApplyLocalizationToItems();
        }

        private void OnLocalizationSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ApplyLocalizationToItems();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (LocalizationSource == null)
            {
                return;
            }

            ApplyLocalizationToItems();
        }

        private void ApplyLocalizationToItems()
        {
            if (LocalizationSource is not { } source)
            {
                return;
            }

            foreach (var item in EnumerateDaisySelectItems())
            {
                var key = item.LocalizationKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                var text = ResolveLocalizationText(source, key);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    item.Text = text;
                }
            }
        }

        private IEnumerable<DaisySelectItem> EnumerateDaisySelectItems()
        {
            if (ItemsSource is System.Collections.IEnumerable enumerable && ItemsSource is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is DaisySelectItem selectItem)
                    {
                        yield return selectItem;
                    }
                }
                yield break;
            }

            foreach (var item in Items)
            {
                if (item is DaisySelectItem selectItem)
                {
                    yield return selectItem;
                }
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2075:DynamicallyAccessedMembers",
            Justification = "Localization sources may use indexers; reflection is required and handled defensively.")]
        private static string? ResolveLocalizationText(object source, string key)
        {
            if (source is Func<string, string> resolver)
            {
                return resolver(key);
            }

            if (source is IReadOnlyDictionary<string, string> roDict && roDict.TryGetValue(key, out var roValue))
            {
                return roValue;
            }

            if (source is IDictionary<string, string> dict && dict.TryGetValue(key, out var value))
            {
                return value;
            }

            var indexer = source.GetType().GetProperty("Item", new[] { typeof(string) });
            if (indexer == null)
            {
                return null;
            }

            try
            {
                return indexer.GetValue(source, new object[] { key }) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
