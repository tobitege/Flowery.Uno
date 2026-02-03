using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;
using Flowery.Services;

namespace Flowery.Controls
{
    /// <summary>
    /// A group container for avatars with overlapping display.
    /// Supports MaxVisible to show "+N" overflow indicator.
    /// </summary>
    public partial class DaisyAvatarGroup : DaisyBaseContentControl
    {
        private StackPanel? _avatarsPanel;
        private DaisyAvatar? _overflowAvatar;
        private readonly List<DaisyAvatar> _avatarItems = [];

        public DaisyAvatarGroup()
        {
            DefaultStyleKey = typeof(DaisyAvatarGroup);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty OverlapProperty =
            DependencyProperty.Register(
                nameof(Overlap),
                typeof(double),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(24.0, OnAppearanceChanged));

        public double Overlap
        {
            get => (double)GetValue(OverlapProperty);
            set => SetValue(OverlapProperty, value);
        }

        public static readonly DependencyProperty MaxVisibleProperty =
            DependencyProperty.Register(
                nameof(MaxVisible),
                typeof(int),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(0, OnAppearanceChanged));

        /// <summary>
        /// Maximum number of avatars to show before displaying "+N" overflow.
        /// Set to 0 to show all avatars.
        /// </summary>
        public int MaxVisible
        {
            get => (int)GetValue(MaxVisibleProperty);
            set => SetValue(MaxVisibleProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(Orientation.Horizontal, OnAppearanceChanged));

        /// <summary>
        /// Controls whether avatars stack horizontally or vertically.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty OverflowCountProperty =
            DependencyProperty.Register(
                nameof(OverflowCount),
                typeof(int),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(0));

        /// <summary>
        /// The number of hidden avatars (displayed as "+N").
        /// </summary>
        public int OverflowCount
        {
            get => (int)GetValue(OverflowCountProperty);
            private set => SetValue(OverflowCountProperty, value);
        }

        public static readonly DependencyProperty InitialsProperty =
            DependencyProperty.Register(
                nameof(Initials),
                typeof(string),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(null, OnInitialsChanged));

        /// <summary>
        /// Comma-separated list of initials for "batteries included" avatar generation.
        /// Example: "AB,CD,EF,GH" will create 4 avatars with cycling ring colors.
        /// This is an alternative to manually specifying DaisyAvatar children.
        /// </summary>
        public string? Initials
        {
            get => (string?)GetValue(InitialsProperty);
            set => SetValue(InitialsProperty, value);
        }

        public static readonly DependencyProperty ShowRingsProperty =
            DependencyProperty.Register(
                nameof(ShowRings),
                typeof(bool),
                typeof(DaisyAvatarGroup),
                new PropertyMetadata(true, OnAppearanceChanged));

        /// <summary>
        /// Whether to show colored rings on avatars generated via Initials property.
        /// Default is true for visual distinction between overlapping avatars.
        /// </summary>
        public bool ShowRings
        {
            get => (bool)GetValue(ShowRingsProperty);
            set => SetValue(ShowRingsProperty, value);
        }

        #endregion

        #region Callbacks

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAvatarGroup g) g.RebuildAvatars();
        }

        private static void OnInitialsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyAvatarGroup g)
            {
                g.GenerateAvatarsFromInitials();
                g.RebuildAvatars();
            }
        }

        // Ring colors to cycle through for auto-generated avatars
        private static readonly DaisyColor[] RingColors =
        [
            DaisyColor.Primary,
            DaisyColor.Secondary,
            DaisyColor.Accent,
            DaisyColor.Success,
            DaisyColor.Warning,
            DaisyColor.Info,
            DaisyColor.Error
        ];

        private void GenerateAvatarsFromInitials()
        {
            if (string.IsNullOrWhiteSpace(Initials))
                return;

            // Clear any existing avatars
            _avatarItems.Clear();

            // Parse comma-separated initials
            var parts = Initials.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            for (int i = 0; i < parts.Length; i++)
            {
                // Create avatar with all properties in initializer to avoid timing issues
                var avatar = new DaisyAvatar
                {
                    IsPlaceholder = true,
                    Initials = parts[i].Trim(),
                    HasRing = ShowRings,
                    RingColor = ShowRings ? RingColors[i % RingColors.Length] : DaisyColor.Primary,
                    Size = Size  // Set Size last since it may trigger ApplyAll
                };

                _avatarItems.Add(avatar);
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();

            if (_avatarsPanel != null)
            {
                RebuildAvatars();
                return;
            }

            // Collect any avatars that were added as children before we set up
            CollectChildAvatars();
            BuildVisualTree();
            RebuildAvatars();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            RebuildAvatars();
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

        #endregion

        #region Visual Tree

        private void CollectChildAvatars()
        {
            // If Initials is set, generate avatars from that (batteries-included mode)
            if (!string.IsNullOrWhiteSpace(Initials))
            {
                GenerateAvatarsFromInitials();
                return;
            }

            _avatarItems.Clear();
            
            // Check if Content is already a collection of avatars or a single avatar
            if (Content is DaisyAvatar singleAvatar)
            {
                _avatarItems.Add(singleAvatar);
                Content = null; // Detach
            }
            else if (Content is Panel panel)
            {
                // If content is a panel, extract avatars from its children
                var avatarsToCollect = new List<DaisyAvatar>();
                foreach (var child in panel.Children)
                {
                    if (child is DaisyAvatar avatar)
                    {
                        avatarsToCollect.Add(avatar);
                    }
                }
                
                // Clear the panel to detach the avatars
                panel.Children.Clear();
                
                foreach (var avatar in avatarsToCollect)
                {
                    _avatarItems.Add(avatar);
                }
            }
            else if (Content is IEnumerable<object> enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is DaisyAvatar avatar)
                    {
                        _avatarItems.Add(avatar);
                    }
                }
            }
        }

        private void BuildVisualTree()
        {
            _avatarsPanel = new StackPanel
            {
                Orientation = this.Orientation
            };

            Content = _avatarsPanel;
        }

        private void RebuildAvatars()
        {
            if (_avatarsPanel == null) return;

            _avatarsPanel.Children.Clear();

            var count = _avatarItems.Count;
            if (count == 0)
            {
                OverflowCount = 0;
                return;
            }

            var maxVisible = MaxVisible > 0 ? MaxVisible : count;

            // Calculate how many to show and overflow count
            int showCount = Math.Min(count, maxVisible);
            if (MaxVisible > 0 && count > MaxVisible)
            {
                // Show MaxVisible - 1 items, then the overflow indicator
                showCount = MaxVisible - 1;
                OverflowCount = count - showCount;
            }
            else
            {
                OverflowCount = 0;
            }

            // Calculate overlap margin based on orientation
            double overlapAmount = -Overlap;
            bool isHorizontal = Orientation == Orientation.Horizontal;

            // Update panel orientation in case it changed
            _avatarsPanel.Orientation = this.Orientation;

            // Add visible avatars (no borders - z-ordering provides visual separation)
            for (int i = 0; i < showCount && i < count; i++)
            {
                var avatar = _avatarItems[i];
                avatar.Size = Size;

                // Apply overlap margin (not for first item) based on orientation
                if (i > 0)
                {
                    avatar.Margin = isHorizontal 
                        ? new Thickness(overlapAmount, 0, 0, 0) 
                        : new Thickness(0, overlapAmount, 0, 0);
                }
                else
                {
                    avatar.Margin = new Thickness(0);
                }

                // Set z-index so earlier items appear on top
                Canvas.SetZIndex(avatar, count - i);

                _avatarsPanel.Children.Add(avatar);
            }

            // Add overflow indicator if needed
            if (OverflowCount > 0)
            {
                // Get border brush for overflow indicator (matches page background)
                var overflowBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase100Brush") 
                    ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                
                _overflowAvatar = new DaisyAvatar
                {
                    Size = Size,
                    IsPlaceholder = true,
                    Initials = $"+{OverflowCount}",
                    Margin = isHorizontal 
                        ? new Thickness(overlapAmount, 0, 0, 0) 
                        : new Thickness(0, overlapAmount, 0, 0),
                    // Use HasRing to add proper border with sizing
                    HasRing = true,
                    RingColor = DaisyColor.Neutral  // Will be overridden below
                };
                
                // Apply a border that matches the page background (visual separation)
                ApplyOverflowBorder(_overflowAvatar, overflowBorderBrush, Size);
                
                Canvas.SetZIndex(_overflowAvatar, 0);
                _avatarsPanel.Children.Add(_overflowAvatar);
            }
        }

        /// <summary>
        /// Applies a border to the overflow avatar indicator.
        /// </summary>
        private static void ApplyOverflowBorder(DaisyAvatar avatar, Brush borderBrush, DaisySize size)
        {
            avatar.Loaded += (s, e) =>
            {
                if (avatar.Content is Grid grid && grid.Children.Count > 0)
                {
                    if (grid.Children[0] is Border outerBorder)
                    {
                        outerBorder.BorderBrush = borderBrush;
                        // Use thinner border for XS size to look proportional
                        outerBorder.BorderThickness = new Thickness(size == DaisySize.ExtraSmall ? 1 : 2);
                    }
                }
            };
        }

        #endregion

        /// <summary>
        /// Adds an avatar to the group programmatically.
        /// </summary>
        public void AddAvatar(DaisyAvatar avatar)
        {
            _avatarItems.Add(avatar);
            RebuildAvatars();
        }

        /// <summary>
        /// Clears all avatars from the group.
        /// </summary>
        public void ClearAvatars()
        {
            _avatarItems.Clear();
            RebuildAvatars();
        }
    }
}
