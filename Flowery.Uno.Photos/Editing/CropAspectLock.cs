namespace Flowery.Uno.Photos.Editing
{
    /// <summary>
    /// The aspect ratio the crop is locked to, remembered with the rotation it was chosen at. A quarter turn, also one that
    /// is undone or redone, swaps the ratio (16:9 becomes 9:16) along with the crop, so the lock keeps fitting the photo.
    /// The default value is the free crop.
    /// </summary>
    /// <param name="Preset">The chosen preset: null for free, <see cref="OriginalPreset"/>, or the preset's width / height.</param>
    /// <param name="Aspect">The output width / height the crop keeps at <paramref name="Rotation"/>; null for free.</param>
    /// <param name="Rotation">Quarter turns of the edit when the preset was chosen.</param>
    public readonly record struct CropAspectLock(double? Preset, double? Aspect, int Rotation)
    {
        /// <summary>Preset value of "Original": the aspect of the photo itself.</summary>
        public const double OriginalPreset = -1;

        /// <summary>The output width / height the crop keeps at <paramref name="rotation"/>, or null for free.</summary>
        public double? AspectAt(int rotation) => Swap(Aspect, rotation);

        /// <summary>Whether the button of <paramref name="preset"/> shows as chosen at <paramref name="rotation"/>.</summary>
        public bool IsSelected(double? preset, int rotation)
        {
            if (preset is not { } p || Preset is not { } chosen)
                return preset is null && Preset is null;
            // "Original" follows the photo through every turn; a ratio preset turns into its portrait or landscape twin.
            var shown = chosen == OriginalPreset ? chosen : Swap(chosen, rotation)!.Value;
            return Math.Abs(shown - p) < 1e-9;
        }

        private double? Swap(double? ratio, int rotation) =>
            ratio is { } r && (rotation - Rotation) % 2 != 0 ? 1 / r : ratio;
    }
}
