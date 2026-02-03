namespace Flowery.Controls
{
    /// <summary>
    /// Displays modifier key states (Shift, Ctrl, Alt, CapsLock, NumLock, ScrollLock).
    /// On Windows, uses native key state polling to reflect live OS state.
    /// </summary>
    public partial class DaisyModifierKeys : DaisyBaseContentControl
    {
        private sealed class ModifierKeyVisual
        {
            public required DaisyKbd Key { get; init; }
        }

        private StackPanel? _rootPanel;
        private ModifierKeyVisual? _shiftKey;
        private ModifierKeyVisual? _ctrlKey;
        private ModifierKeyVisual? _altKey;
        private Border? _separator;
        private ModifierKeyVisual? _capsLockKey;
        private ModifierKeyVisual? _numLockKey;
        private ModifierKeyVisual? _scrollLockKey;
        private DispatcherTimer? _pollTimer;
        // Icon path data definitions
        private const string ModifierIconShiftKey = "ModifierIconShift";
        private const string ModifierIconCtrlKey = "ModifierIconCtrl";
        private const string ModifierIconAltKey = "ModifierIconAlt";
        private const string ModifierIconCapsLockKey = "ModifierIconCapsLock";
        private const string ModifierIconNumLockKey = "ModifierIconNumLock";
        private const string ModifierIconScrollLockKey = "ModifierIconScrollLock";

        public DaisyModifierKeys()
        {
            DefaultStyleKey = typeof(DaisyModifierKeys);
            IsTabStop = false;
        }

        #region Size and Style

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyModifierKeys),
                new PropertyMetadata(DaisySize.Small, OnLayoutChanged));

        /// <summary>
        /// The size of the modifier key display.
        /// </summary>
        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty KbdStyleProperty =
            DependencyProperty.Register(
                nameof(KbdStyle),
                typeof(DaisyKbdStyle),
                typeof(DaisyModifierKeys),
                new PropertyMetadata(DaisyKbdStyle.ThreeDimensional, OnLayoutChanged));

        /// <summary>
        /// Gets or sets the visual style of the keyboard keys.
        /// </summary>
        public DaisyKbdStyle KbdStyle
        {
            get => (DaisyKbdStyle)GetValue(KbdStyleProperty);
            set => SetValue(KbdStyleProperty, value);
        }

        #endregion

        #region State Properties

        public static readonly DependencyProperty IsShiftPressedProperty =
            DependencyProperty.Register(nameof(IsShiftPressed), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsShiftPressed
        {
            get => (bool)GetValue(IsShiftPressedProperty);
            set => SetValue(IsShiftPressedProperty, value);
        }

        public static readonly DependencyProperty IsCtrlPressedProperty =
            DependencyProperty.Register(nameof(IsCtrlPressed), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsCtrlPressed
        {
            get => (bool)GetValue(IsCtrlPressedProperty);
            set => SetValue(IsCtrlPressedProperty, value);
        }

        public static readonly DependencyProperty IsAltPressedProperty =
            DependencyProperty.Register(nameof(IsAltPressed), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsAltPressed
        {
            get => (bool)GetValue(IsAltPressedProperty);
            set => SetValue(IsAltPressedProperty, value);
        }

        public static readonly DependencyProperty IsCapsLockOnProperty =
            DependencyProperty.Register(nameof(IsCapsLockOn), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsCapsLockOn
        {
            get => (bool)GetValue(IsCapsLockOnProperty);
            set => SetValue(IsCapsLockOnProperty, value);
        }

        public static readonly DependencyProperty IsNumLockOnProperty =
            DependencyProperty.Register(nameof(IsNumLockOn), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsNumLockOn
        {
            get => (bool)GetValue(IsNumLockOnProperty);
            set => SetValue(IsNumLockOnProperty, value);
        }

        public static readonly DependencyProperty IsScrollLockOnProperty =
            DependencyProperty.Register(nameof(IsScrollLockOn), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnStateChanged));

        public bool IsScrollLockOn
        {
            get => (bool)GetValue(IsScrollLockOnProperty);
            set => SetValue(IsScrollLockOnProperty, value);
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyModifierKeys control)
                control.UpdateKeyStyles();
        }

        #endregion

        #region Visibility Properties

        public static readonly DependencyProperty ShowShiftProperty =
            DependencyProperty.Register(nameof(ShowShift), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(true, OnLayoutChanged));

        public bool ShowShift
        {
            get => (bool)GetValue(ShowShiftProperty);
            set => SetValue(ShowShiftProperty, value);
        }

        public static readonly DependencyProperty ShowCtrlProperty =
            DependencyProperty.Register(nameof(ShowCtrl), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(true, OnLayoutChanged));

        public bool ShowCtrl
        {
            get => (bool)GetValue(ShowCtrlProperty);
            set => SetValue(ShowCtrlProperty, value);
        }

        public static readonly DependencyProperty ShowAltProperty =
            DependencyProperty.Register(nameof(ShowAlt), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(true, OnLayoutChanged));

        public bool ShowAlt
        {
            get => (bool)GetValue(ShowAltProperty);
            set => SetValue(ShowAltProperty, value);
        }

        public static readonly DependencyProperty ShowCapsLockProperty =
            DependencyProperty.Register(nameof(ShowCapsLock), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(true, OnLayoutChanged));

        public bool ShowCapsLock
        {
            get => (bool)GetValue(ShowCapsLockProperty);
            set => SetValue(ShowCapsLockProperty, value);
        }

        public static readonly DependencyProperty ShowNumLockProperty =
            DependencyProperty.Register(nameof(ShowNumLock), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(true, OnLayoutChanged));

        public bool ShowNumLock
        {
            get => (bool)GetValue(ShowNumLockProperty);
            set => SetValue(ShowNumLockProperty, value);
        }

        public static readonly DependencyProperty ShowScrollLockProperty =
            DependencyProperty.Register(nameof(ShowScrollLock), typeof(bool), typeof(DaisyModifierKeys),
                new PropertyMetadata(false, OnLayoutChanged));

        public bool ShowScrollLock
        {
            get => (bool)GetValue(ShowScrollLockProperty);
            set => SetValue(ShowScrollLockProperty, value);
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyModifierKeys control)
                control.RebuildVisual();
        }

        #endregion

        protected override void OnLoaded()
        {
            base.OnLoaded();
            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            if (FlowerySizeManager.UseGlobalSizeByDefault && !FlowerySizeManager.GetIgnoreGlobalSize(this))
                Size = FlowerySizeManager.CurrentSize;

            RebuildVisual();
            SyncFromOS();
            UpdateKeyStyles();

            StartPollingIfAvailable();
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
            StopPolling();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            UpdateKeyStyles();
        }

        protected override void OnGlobalSizeChanged(DaisySize size)
        {
            base.OnGlobalSizeChanged(size);
            SetControlSize(size);
        }

        protected override DaisySize? GetControlSize() => Size;

        protected override void SetControlSize(DaisySize size)
        {
            Size = size;
        }

        private void RebuildVisual()
        {
            _rootPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            _shiftKey = null;
            _ctrlKey = null;
            _altKey = null;
            _capsLockKey = null;
            _numLockKey = null;
            _scrollLockKey = null;
            _separator = null;

            if (ShowShift)
            {
                _shiftKey = CreateKey(ModifierIconShiftKey, string.Empty, "Shift");
                ApplySizing(_shiftKey);
                _rootPanel.Children.Add(_shiftKey.Key);
            }

            if (ShowCtrl)
            {
                _ctrlKey = CreateKey(ModifierIconCtrlKey, "Ctrl", "Ctrl");
                ApplySizing(_ctrlKey);
                _rootPanel.Children.Add(_ctrlKey.Key);
            }

            if (ShowAlt)
            {
                _altKey = CreateKey(ModifierIconAltKey, "Alt", "Alt");
                ApplySizing(_altKey);
                _rootPanel.Children.Add(_altKey.Key);
            }

            var anyModifiers = ShowShift || ShowCtrl || ShowAlt;
            var anyLocks = ShowCapsLock || ShowNumLock || ShowScrollLock;

            if (anyModifiers && anyLocks)
            {
                _separator = new Border
                {
                    Width = 1,
                    Height = 20,
                    Margin = new Thickness(4, 0, 4, 0),
                    Background = DaisyResourceLookup.GetBrush(Application.Current?.Resources, "DaisyBase300Brush")
                };
                _rootPanel.Children.Add(_separator);
            }

            if (ShowCapsLock)
            {
                _capsLockKey = CreateKey(ModifierIconCapsLockKey, "A", "Caps");
                _capsLockKey.Key.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                ApplySizing(_capsLockKey);
                _rootPanel.Children.Add(_capsLockKey.Key);
            }

            if (ShowNumLock)
            {
                _numLockKey = CreateKey(ModifierIconNumLockKey, "123", "Num");
                ApplySizing(_numLockKey);
                _rootPanel.Children.Add(_numLockKey.Key);
            }

            if (ShowScrollLock)
            {
                _scrollLockKey = CreateKey(ModifierIconScrollLockKey, "Scr", "Scr");
                ApplySizing(_scrollLockKey);
                _rootPanel.Children.Add(_scrollLockKey.Key);
            }

            Content = _rootPanel;
            UpdateKeyStyles();
        }

        private void ApplySizing(ModifierKeyVisual key)
        {
            var effectiveSize = FlowerySizeManager.ShouldIgnoreGlobalSize(this)
                ? Size
                : (FlowerySizeManager.UseGlobalSizeByDefault ? FlowerySizeManager.CurrentSize : Size);

            key.Key.Size = effectiveSize;
            key.Key.KbdStyle = KbdStyle;
            key.Key.MinWidth = DaisyResourceLookup.GetSizeValue(effectiveSize);
        }

        private static ModifierKeyVisual CreateKey(string iconResourceKey, string text, string tooltip)
        {
            var kbd = new DaisyKbd
            {
                Size = DaisySize.Small,
                Text = text,
                IconData = FloweryPathHelpers.GetIconPathData(iconResourceKey)
            };

            ToolTipService.SetToolTip(kbd, tooltip);

            return new ModifierKeyVisual
            {
                Key = kbd
            };
        }

        private void StartPollingIfAvailable()
        {
            if (!OperatingSystem.IsWindows())
                return;

            if (_pollTimer != null)
                return;

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _pollTimer.Tick += OnPollTick;
            _pollTimer.Start();
        }

        private void StopPolling()
        {
            if (_pollTimer == null)
                return;

            _pollTimer.Tick -= OnPollTick;
            _pollTimer.Stop();
            _pollTimer = null;
        }

        private void OnPollTick(object? sender, object e)
        {
            SyncFromOS();
        }

        private void SyncFromOS()
        {
            if (!OperatingSystem.IsWindows())
                return;

            IsShiftPressed = KeyboardHelper.IsKeyPressed(KeyboardHelper.VK_SHIFT);
            IsCtrlPressed = KeyboardHelper.IsKeyPressed(KeyboardHelper.VK_CONTROL);
            IsAltPressed = KeyboardHelper.IsKeyPressed(KeyboardHelper.VK_MENU);
            IsCapsLockOn = KeyboardHelper.IsKeyToggled(KeyboardHelper.VK_CAPITAL);
            IsNumLockOn = KeyboardHelper.IsKeyToggled(KeyboardHelper.VK_NUMLOCK);
            IsScrollLockOn = KeyboardHelper.IsKeyToggled(KeyboardHelper.VK_SCROLL);
        }

        private void UpdateKeyStyles()
        {
            // Separator brush should track theme changes
            if (_separator != null)
                _separator.Background = DaisyResourceLookup.GetBrush(Application.Current?.Resources, "DaisyBase300Brush");

            ApplyKeyStyle(_shiftKey, IsShiftPressed, isLockKey: false);
            ApplyKeyStyle(_ctrlKey, IsCtrlPressed, isLockKey: false);
            ApplyKeyStyle(_altKey, IsAltPressed, isLockKey: false);

            ApplyKeyStyle(_capsLockKey, IsCapsLockOn, isLockKey: true);
            ApplyKeyStyle(_numLockKey, IsNumLockOn, isLockKey: true);
            ApplyKeyStyle(_scrollLockKey, IsScrollLockOn, isLockKey: true);
        }

        private void ApplyKeyStyle(ModifierKeyVisual? key, bool isActive, bool isLockKey)
        {
            if (key == null)
                return;

            // Sync visual style
            key.Key.KbdStyle = KbdStyle;

            if (!isActive)
            {
                key.Key.Variant = DaisyBadgeVariant.Default;
                key.Key.RenderTransform = null;
                
                // Restore default unpressed thickness based on style
                key.Key.BorderThickness = KbdStyle switch
                {
                    DaisyKbdStyle.ThreeDimensional => new Thickness(0, 0, 0, 3),
                    DaisyKbdStyle.Keycap => new Thickness(0, 0, 0, 5),
                    DaisyKbdStyle.Flat => new Thickness(1),
                    _ => new Thickness(0)
                };
                return;
            }

            key.Key.Variant = isLockKey ? DaisyBadgeVariant.Accent : DaisyBadgeVariant.Primary;

            // Tactile depression effect: Shift face down within the body
            if (KbdStyle == DaisyKbdStyle.ThreeDimensional)
            {
                key.Key.BorderThickness = new Thickness(0, 2, 0, 1);
            }
            else if (KbdStyle == DaisyKbdStyle.Keycap)
            {
                key.Key.BorderThickness = new Thickness(0, 4, 0, 1);
            }
            else
            {
                key.Key.RenderTransform = new TranslateTransform { Y = 1 };
                if (KbdStyle == DaisyKbdStyle.Flat)
                    key.Key.BorderThickness = new Thickness(1);
            }
        }

    }
}
