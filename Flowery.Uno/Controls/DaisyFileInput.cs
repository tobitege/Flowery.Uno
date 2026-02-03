using System;
using Flowery.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Flowery.Theming;

namespace Flowery.Controls
{
    /// <summary>
    /// A file input control styled after DaisyUI's File Input component.
    /// Displays a styled button with file name text.
    /// </summary>
    public partial class DaisyFileInput : DaisyBaseContentControl
    {
        private Grid? _rootGrid;
        private DaisyButton? _browseButton;
        private TextBlock? _fileNameTextBlock;
        private object? _userContent;

        public DaisyFileInput()
        {
            DefaultStyleKey = typeof(DaisyFileInput);
            IsTabStop = false;
        }

        #region Dependency Properties

        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register(
                nameof(FileName),
                typeof(string),
                typeof(DaisyFileInput),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("FileInput_NoFileChosen", "No file chosen"), OnFileNameChanged));

        /// <summary>
        /// Gets or sets the displayed file name.
        /// </summary>
        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(
                nameof(Variant),
                typeof(DaisyButtonVariant),
                typeof(DaisyFileInput),
                new PropertyMetadata(DaisyButtonVariant.Default, OnAppearanceChanged));

        public DaisyButtonVariant Variant
        {
            get => (DaisyButtonVariant)GetValue(VariantProperty);
            set => SetValue(VariantProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(DaisySize),
                typeof(DaisyFileInput),
                new PropertyMetadata(DaisySize.Medium, OnAppearanceChanged));

        public DaisySize Size
        {
            get => (DaisySize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(
                nameof(ButtonText),
                typeof(string),
                typeof(DaisyFileInput),
                new PropertyMetadata(FloweryLocalization.GetStringInternal("FileInput_ChooseFile", "Choose File"), OnButtonTextChanged));

        /// <summary>
        /// Gets or sets the text displayed on the browse button.
        /// </summary>
        public string ButtonText
        {
            get => (string)GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        /// <summary>
        /// Event raised when the browse button is clicked.
        /// Handle this to show a file picker dialog.
        /// </summary>
        public event EventHandler? BrowseClicked;

        #endregion

        #region Callbacks

        private static void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFileInput input && input._fileNameTextBlock != null)
            {
                input._fileNameTextBlock.Text = e.NewValue as string ?? FloweryLocalization.GetStringInternal("FileInput_NoFileChosen", "No file chosen");
            }
        }

        private static void OnButtonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFileInput input && input._browseButton != null)
            {
                input._browseButton.Content = e.NewValue as string ?? FloweryLocalization.GetStringInternal("FileInput_ChooseFile", "Choose File");
            }
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DaisyFileInput input)
            {
                input.ApplyAll();
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (_containerBorder != null)
            {
                ApplyAll();
                return;
            }

            // Capture user content (could be custom button content)
            _userContent = Content;
            Content = null;

            BuildVisualTree();
            ApplyAll();
        }

        protected override void OnThemeChanged(string themeName)
        {
            base.OnThemeChanged(themeName);
            ApplyAll();
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

        protected override FrameworkElement? GetNeumorphicHostElement()
        {
            return _containerBorder ?? base.GetNeumorphicHostElement();
        }

        #endregion

        private Border? _containerBorder;

        #region Visual Tree

        private void BuildVisualTree()
        {
            _rootGrid = new Grid();
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Browse button
            _browseButton = new DaisyButton
            {
                Content = _userContent ?? ButtonText,
                Variant = Variant
            };
            _browseButton.Click += OnBrowseButtonClicked;

            Grid.SetColumn(_browseButton, 0);
            _rootGrid.Children.Add(_browseButton);

            // File name display
            _fileNameTextBlock = new TextBlock
            {
                Text = FileName,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Opacity = 0.7
            };

            Grid.SetColumn(_fileNameTextBlock, 1);
            _rootGrid.Children.Add(_fileNameTextBlock);

            // Wrap in a styled container
            _containerBorder = new Border
            {
                Child = _rootGrid
            };

            Content = _containerBorder;
        }

        private void OnBrowseButtonClicked(object sender, RoutedEventArgs e)
        {
            BrowseClicked?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Apply Styling

        private void ApplyAll()
        {
            if (_browseButton == null || _fileNameTextBlock == null || _containerBorder == null)
                return;

            ApplySizing();
            ApplyColors();
        }

        private void ApplySizing()
        {
            if (_browseButton == null || _fileNameTextBlock == null || _containerBorder == null)
                return;

            var resources = Application.Current?.Resources;
            if (resources != null)
                DaisyTokenDefaults.EnsureDefaults(resources);

            string sizeKey = DaisyResourceLookup.GetSizeKeyFull(Size);

            _browseButton.Size = Size;

            double fontSize = resources == null
                ? DaisyResourceLookup.GetDefaultFontSize(Size)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}FontSize",
                    DaisyResourceLookup.GetDefaultFontSize(Size));
            _fileNameTextBlock.FontSize = fontSize;

            double containerHeight = resources == null
                ? DaisyResourceLookup.GetDefaultFloatingInputHeight(Size)
                : DaisyResourceLookup.GetDouble(resources, $"DaisySize{sizeKey}Height",
                    DaisyResourceLookup.GetDefaultFloatingInputHeight(Size));
            _containerBorder.Height = containerHeight;

            var cornerRadius = resources == null
                ? new CornerRadius(8)
                : DaisyResourceLookup.GetCornerRadius(resources, "DaisyRoundedBtn", new CornerRadius(8));
            _containerBorder.CornerRadius = cornerRadius;
        }

        private void ApplyColors()
        {
            if (_browseButton == null || _fileNameTextBlock == null || _containerBorder == null)
                return;

            // Check for lightweight styling overrides
            var bgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyFileInput", "Background");
            var borderOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyFileInput", "BorderBrush");
            var fgOverride = DaisyResourceLookup.TryGetControlBrush(this, "DaisyFileInput", "Foreground");

            _browseButton.Variant = Variant;
            _fileNameTextBlock.Foreground = fgOverride ?? DaisyResourceLookup.GetBrush("DaisyBaseContentBrush");

            // Style the container border
            _containerBorder.Background = bgOverride ?? DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            _containerBorder.BorderBrush = borderOverride ?? DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            _containerBorder.BorderThickness = new Thickness(1);
        }

        #endregion
    }
}
