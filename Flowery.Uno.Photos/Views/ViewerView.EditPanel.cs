using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;
using static Flowery.Uno.Photos.Views.UiHelpers;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Side panel. Skia: only the Info tab for now; the Crop, Adjust, Filters and Markup tabs come with the editing phase,
    /// so the tab bar and the edit actions stay hidden.
    /// </summary>
    public sealed partial class ViewerView
    {
        private enum EditTab
        {
            Crop,
            Adjust,
            Filters,
            Markup,
            Info
        }

        private EditTab _tab = EditTab.Info;
        private EditTab _lastEditTab = EditTab.Adjust;
        private bool _panelOpen;
        private int _infoVersion;

        private void BuildEditPanel()
        {
            UpdateTabVisuals();
        }

        private void SetPanelOpen(bool open, EditTab? tab = null)
        {
            _panelOpen = open;
            if (tab is { } t)
                _tab = t;
            if (_tab != EditTab.Info)
                _lastEditTab = _tab;

            EditPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            PhotosSettings.EditPanelOpen = open;

            InfoButton.Variant = open && _tab == EditTab.Info ? DaisyButtonVariant.Primary : DaisyButtonVariant.Ghost;
            InfoButton.ButtonStyle = open && _tab == EditTab.Info ? DaisyButtonStyle.Soft : DaisyButtonStyle.Default;

            // Mode changed: drop any crop/markup cursor until the pointer moves again.
            CanvasHost.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);

            UpdateTabVisuals();
            OnTabShown();
            Canvas.Invalidate();
        }

        private void UpdateTabVisuals()
        {
            CropPanel.Visibility = _tab == EditTab.Crop ? Visibility.Visible : Visibility.Collapsed;
            AdjustPanel.Visibility = _tab == EditTab.Adjust ? Visibility.Visible : Visibility.Collapsed;
            FilterPanel.Visibility = _tab == EditTab.Filters ? Visibility.Visible : Visibility.Collapsed;
            MarkupPanel.Visibility = _tab == EditTab.Markup ? Visibility.Visible : Visibility.Collapsed;
            InfoPanel.Visibility = _tab == EditTab.Info ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTabShown()
        {
            if (!_panelOpen)
                return;
            if (_tab == EditTab.Info)
                _ = RefreshInfoAsync();
            UpdateZoomUi();
        }

        private void OnImageLoadedForPanel() => OnTabShown();

        #region Info

        private async Task RefreshInfoAsync()
        {
            var version = ++_infoVersion;
            InfoPanel.Children.Clear();
            if (Current is not { } item)
                return;

            var culture = PhotosLocalization.CurrentCulture;
            AddInfoHeader(item.FileName);
            AddInfoRow("Photos_Info_Folder", item.FolderPath, () => FileOperations.ShowInExplorer(item.Path));
            AddInfoRow("Photos_Info_FileSize", FileOperations.FormatFileSize(item.FileSize));
            var w = _image?.Width ?? Math.Max(0, item.PixelWidth);
            var h = _image?.Height ?? Math.Max(0, item.PixelHeight);
            if (w > 0)
                AddInfoRow("Photos_Info_Dimensions", $"{w} × {h}  ·  {(long)w * h / 1_000_000.0:0.0} MP");
            AddInfoRow("Photos_Info_Modified", item.DateModified.ToString("f", culture));

            // Skia: StorageFile.Properties is not implemented on the Uno desktop heads, so the EXIF block is parsed directly.
            var exif = await Task.Run(() => ExifDetails.Read(item.Path));
            if (version != _infoVersion)
                return;

            if (exif != null)
            {
                if (exif.DateTaken is { } taken)
                    AddInfoRow("Photos_Info_DateTaken", taken.ToString("f", culture));
                if (!string.IsNullOrWhiteSpace(exif.Camera))
                    AddInfoRow("Photos_Info_Camera", exif.Camera);
                if (!string.IsNullOrWhiteSpace(exif.Lens))
                    AddInfoRow("Photos_Info_Lens", exif.Lens);

                var exposure = ViewerFormat.Exposure(exif, culture);
                if (exposure.Length > 0)
                    AddInfoRow("Photos_Info_Exposure", exposure);

                if (exif.Latitude is { } lat && exif.Longitude is { } lon)
                {
                    var coords = string.Format(CultureInfo.InvariantCulture, "{0:0.00000}, {1:0.00000}", lat, lon);
                    AddInfoRow("Photos_Info_Location", coords, () =>
                        FileOperations.OpenUri(string.Format(CultureInfo.InvariantCulture, "https://www.openstreetmap.org/?mlat={0}&mlon={1}#map=15/{0}/{1}", lat, lon)));
                }
            }

            var albums = PhotoLibrary.Instance.Albums.Where(a => a.Paths.Contains(item.Path, PathRules.Comparer)).Select(a => a.Name).ToList();
            if (albums.Count > 0)
                AddInfoRow("Photos_Info_Albums", string.Join(", ", albums));
        }

        private void AddInfoHeader(string text)
        {
            InfoPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });
        }

        private void AddInfoRow(string labelKey, string value, Action? onClick = null)
        {
            var panel = new StackPanel { Spacing = 2 };
            panel.Children.Add(Caption(labelKey, new Thickness(0)));
            if (onClick != null)
            {
                // A TextBlock hyperlink wraps and grows with its text (fixed-height buttons would clip).
                var hyperlink = new Microsoft.UI.Xaml.Documents.Hyperlink();
                // Skia: the default hyperlink color is unreadable on the dark themes.
                hyperlink.Foreground = DaisyResourceLookup.GetBrush("DaisyPrimaryBrush");
                hyperlink.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = value });
                hyperlink.Click += (_, _) => onClick();
                var linkText = new TextBlock { TextWrapping = TextWrapping.Wrap };
                linkText.Inlines.Add(hyperlink);
                FlowerySizeManager.SetResponsiveFont(linkText, ResponsiveFontTier.Primary);
                panel.Children.Add(linkText);
            }
            else
            {
                panel.Children.Add(new TextBlock
                {
                    Text = value,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                });
            }
            InfoPanel.Children.Add(panel);
        }

        #endregion
    }
}
