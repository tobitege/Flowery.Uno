using Flowery.Uno.Photos.Localization;
using Flowery.Uno.Photos.Services;

namespace Flowery.Uno.Photos.Views
{
    /// <summary>
    /// Skia: the entry list of <see cref="CollectionsView"/> (albums or folders, with the search filter) moved out of
    /// the view so the logic can be tested without UI. The WinUI view builds the same list inline.
    /// </summary>
    public static class CollectionEntryBuilder
    {
        public static List<CollectionEntry> Build(PhotoLibrary library, CollectionEntryKind kind, string? search, CultureInfo culture)
        {
            List<CollectionEntry> entries;
            if (kind == CollectionEntryKind.Album)
            {
                entries = [.. library.Albums.Select(a =>
                {
                    var items = library.GetAlbumItems(a).ToList();
                    return new CollectionEntry
                    {
                        Kind = CollectionEntryKind.Album,
                        Key = a.Name,
                        Title = a.Name,
                        Subtitle = items.Count > 0
                            ? FormatDateRange(items.Min(i => i.SortDate), items.Max(i => i.SortDate), culture)
                            : PhotosLocalization.GetString("Photos_EmptyAlbum"),
                        Count = items.Count,
                        Cover = items.FirstOrDefault()
                    };
                })];
            }
            else
            {
                entries = [.. library.GetFolderSummaries().Select(f => new CollectionEntry
                {
                    Kind = CollectionEntryKind.Folder,
                    Key = f.Path,
                    Title = f.Name,
                    Subtitle = f.Path,
                    Count = f.Count,
                    Cover = f.Cover,
                    IsRootFolder = f.IsRoot
                })];
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                entries = [.. entries.Where(e =>
                    e.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    (e.Subtitle?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))];
            }

            return entries;
        }

        public static string FormatDateRange(DateTime from, DateTime to, CultureInfo culture)
        {
            var a = from.ToString("Y", culture);
            var b = to.ToString("Y", culture);
            return a == b ? a : $"{a} \u2013 {b}";
        }
    }
}
