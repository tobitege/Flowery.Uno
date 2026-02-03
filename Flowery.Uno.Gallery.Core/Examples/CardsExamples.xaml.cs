using System;
using System.Collections.Generic;
using System.Linq;
using Flowery.Controls;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Flowery.Uno.Gallery.Examples
{
    public sealed partial class CardsExamples : ScrollableExamplePage
    {
        public Visibility SkiaMatrixVisibility => OperatingSystem.IsBrowser() ? Visibility.Collapsed : Visibility.Visible;

        private Grid? _cardStackContainer;
        private StackPanel? _skiaMatrixContainer;
        private int _currentCardIndex;
        private readonly List<DaisyCard> _cards = [];
        private DaisyButton? _prevCardButton;
        private DaisyButton? _nextCardButton;

        public CardsExamples()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        protected override ScrollViewer? ScrollViewer => MainScrollViewer;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeCardStack();
            InitializeSkiaMatrix();
        }

        private void InitializeCardStack()
        {
            _cardStackContainer = CardStackContainer;
            _prevCardButton = PrevCardBtn;
            _nextCardButton = NextCardBtn;

            if (_cardStackContainer == null || _cards.Count > 0)
                return;

            var variants = new[]
            {
                DaisyColor.Primary,
                DaisyColor.Secondary,
                DaisyColor.Accent,
                DaisyColor.Neutral
            };

            for (var i = 0; i < variants.Length; i++)
            {
                var card = new DaisyCard
                {
                    Width = 260,
                    Height = 340,
                    ColorVariant = variants[i],
                    RenderTransform = new CompositeTransform(),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    Content = new Grid
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = $"CARD {i + 1}",
                                FontWeight = FontWeights.Bold,
                                FontSize = 24,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    }
                };

                _cards.Add(card);
                _cardStackContainer.Children.Add(card);
            }

            UpdateCardStack();
        }

        private void UpdateCardStack()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                var offset = i - _currentCardIndex;

                var zIndex = _cards.Count - Math.Abs(offset);
                var opacity = offset < 0 ? 0 : (1.0 - offset * 0.2);
                var scale = 1.0 - (offset * 0.05);
                var translateY = offset * 20.0;

                Canvas.SetZIndex(card, zIndex);
                card.Visibility = offset >= 0 ? Visibility.Visible : Visibility.Collapsed;
                card.Opacity = opacity;

                if (card.RenderTransform is CompositeTransform transform)
                {
                    transform.ScaleX = scale;
                    transform.ScaleY = scale;
                    transform.TranslateY = translateY;
                }
            }

            // Reorder children based on ZIndex to ensure correct rendering on all platforms (especially Android)
            if (_cardStackContainer != null)
            {
                var sorted = _cards
                    .Select(c => new { Card = c, ZIndex = Canvas.GetZIndex(c) })
                    .OrderBy(x => x.ZIndex)
                    .Select(x => x.Card)
                    .ToList();

                // Only reorder if necessary to avoid flicker/overhead
                bool needsReorder = false;
                if (_cardStackContainer.Children.Count != sorted.Count)
                {
                    needsReorder = true;
                }
                else
                {
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        if (_cardStackContainer.Children[i] != sorted[i])
                        {
                            needsReorder = true;
                            break;
                        }
                    }
                }

                if (needsReorder)
                {
                    _cardStackContainer.Children.Clear();
                    foreach (var card in sorted)
                    {
                        _cardStackContainer.Children.Add(card);
                    }
                }
            }

            UpdateCardStackNavigationButtons();
        }

        private void UpdateCardStackNavigationButtons()
        {
            if (_prevCardButton == null || _nextCardButton == null)
                return;

            var canNavigate = _cards.Count > 1;
            _prevCardButton.Visibility = canNavigate && _currentCardIndex > 0 ? Visibility.Visible : Visibility.Collapsed;
            _nextCardButton.Visibility = canNavigate && _currentCardIndex < _cards.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PrevCard_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (_currentCardIndex <= 0)
                return;

            _currentCardIndex--;
            UpdateCardStack();
        }

        private void NextCard_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;

            if (_currentCardIndex >= _cards.Count - 1)
                return;

            _currentCardIndex++;
            UpdateCardStack();
        }

        private void InitializeSkiaMatrix()
        {
            if (OperatingSystem.IsBrowser())
            {
                return;
            }

            _skiaMatrixContainer = SkiaMatrixContainer;
            if (_skiaMatrixContainer == null || _skiaMatrixContainer.Children.Count > 0)
                return;

            // Header Row (Saturation values)
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headerRow.Children.Add(new Border { Width = 40 }); // Corner spacer

            for (var sat = 0; sat <= 100; sat += 10)
            {
                headerRow.Children.Add(new TextBlock
                {
                    Text = $"S{sat}%",
                    Width = 50,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                    FontWeight = FontWeights.Bold
                });
            }
            _skiaMatrixContainer.Children.Add(headerRow);

            // Rows (Blur values) - limited to B40 for demo purposes
            for (var blur = 0; blur <= 40; blur += 10)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

                // Row Header
                row.Children.Add(new TextBlock
                {
                    Text = $"B{blur}",
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = Microsoft.UI.Xaml.TextAlignment.Right
                });

                for (var sat = 0; sat <= 100; sat += 10)
                {
                    // Background Border with content - this is what DaisyGlass will capture
                    var bgBorder = new Border
                    {
                        Width = 50,
                        Height = 35,
                        Background = new SolidColorBrush(Colors.White),
                        CornerRadius = new CornerRadius(6)
                    };

                    // Background content (text "lorem") inside the border
                    var bgText = new TextBlock
                    {
                        Text = "lorem",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Colors.Black),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    bgBorder.Child = bgText;

                    // Container Grid to layer glass on top of background
                    var tileGrid = new Grid { Width = 50, Height = 35 };
                    tileGrid.Children.Add(bgBorder);

                    // Glass Overlay - will capture bgBorder as its backdrop
                    var glass = new DaisyGlass
                    {
                        GlassBlur = blur,
                        GlassSaturation = sat / 100.0,
                        BlurMode = GlassBlurMode.SkiaSharp,
                        EnableBackdropBlur = true,
                        CornerRadius = new CornerRadius(6),
                        GlassTintOpacity = 0.1,
                        GlassTint = Colors.White,
                        GlassBorderOpacity = 0.2,
                        GlassReflectOpacity = 0.15,
                        Padding = new Thickness(0)
                    };
                    ToolTipService.SetToolTip(glass, $"Blur: {blur}, Saturation: {sat / 100.0:P0}");

                    tileGrid.Children.Add(glass);
                    row.Children.Add(tileGrid);
                }
                _skiaMatrixContainer.Children.Add(row);
            }
        }
    }
}
