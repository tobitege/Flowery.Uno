using Flowery.Services;
using Microsoft.UI.Xaml;

namespace Flowery.Theming
{
    internal static class DaisyTokenDefaults
    {
        private const string InitializationKey = "Flowery.DaisyTokenDefaultsInitialized";

        public static void EnsureDefaults(ResourceDictionary resources)
        {
            if (resources == null)
                return;

            if (resources.ContainsKey(InitializationKey))
                return;

            // ---- Control heights (desktop baseline) ----
            // Desktop-first compact profile on a 4px grid.
            Ensure(resources, "DaisySizeExtraLargeHeight", 40d);
            Ensure(resources, "DaisySizeLargeHeight", 36d);
            Ensure(resources, "DaisySizeMediumHeight", 32d);
            Ensure(resources, "DaisySizeSmallHeight", 28d);
            Ensure(resources, "DaisySizeExtraSmallHeight", 24d);

            // ---- Floating input heights ----
            Ensure(resources, "DaisyInputFloatingExtraLargeHeight", 48d);
            Ensure(resources, "DaisyInputFloatingLargeHeight", 44d);
            Ensure(resources, "DaisyInputFloatingMediumHeight", 40d);
            Ensure(resources, "DaisyInputFloatingSmallHeight", 36d);
            Ensure(resources, "DaisyInputFloatingExtraSmallHeight", 32d);

            // ---- Font sizes (primary) ----
            // Reduced ~15-20% proportional to control heights; XS floors at 9px for readability
            Ensure(resources, "DaisySizeExtraLargeFontSize", 16d);
            Ensure(resources, "DaisySizeLargeFontSize", 14d);
            Ensure(resources, "DaisySizeMediumFontSize", 12d);
            Ensure(resources, "DaisySizeSmallFontSize", 10d);
            Ensure(resources, "DaisySizeExtraSmallFontSize", 8d);

            // ---- Line heights (primary) ----
            Ensure(resources, "DaisySizeExtraLargeLineHeight", 24d);
            Ensure(resources, "DaisySizeLargeLineHeight", 20d);
            Ensure(resources, "DaisySizeMediumLineHeight", 16d);
            Ensure(resources, "DaisySizeSmallLineHeight", 16d);
            Ensure(resources, "DaisySizeExtraSmallLineHeight", 12d);

            // ---- Font sizes (secondary) ----
            Ensure(resources, "DaisySizeExtraLargeSecondaryFontSize", 14d);
            Ensure(resources, "DaisySizeLargeSecondaryFontSize", 12d);
            Ensure(resources, "DaisySizeMediumSecondaryFontSize", 10d);
            Ensure(resources, "DaisySizeSmallSecondaryFontSize", 9d);
            Ensure(resources, "DaisySizeExtraSmallSecondaryFontSize", 8d);

            // ---- Font sizes (tertiary) ----
            Ensure(resources, "DaisySizeExtraLargeTertiaryFontSize", 12d);
            Ensure(resources, "DaisySizeLargeTertiaryFontSize", 10d);
            Ensure(resources, "DaisySizeMediumTertiaryFontSize", 9d);
            Ensure(resources, "DaisySizeSmallTertiaryFontSize", 8d);
            Ensure(resources, "DaisySizeExtraSmallTertiaryFontSize", 8d);

            // ---- Font sizes (section header) ----
            Ensure(resources, "DaisySizeExtraLargeSectionHeaderFontSize", 17d);
            Ensure(resources, "DaisySizeLargeSectionHeaderFontSize", 15d);
            Ensure(resources, "DaisySizeMediumSectionHeaderFontSize", 13d);
            Ensure(resources, "DaisySizeSmallSectionHeaderFontSize", 12d);
            Ensure(resources, "DaisySizeExtraSmallSectionHeaderFontSize", 10d);

            // ---- Font sizes (header) ----
            Ensure(resources, "DaisySizeExtraLargeHeaderFontSize", 24d);
            Ensure(resources, "DaisySizeLargeHeaderFontSize", 20d);
            Ensure(resources, "DaisySizeMediumHeaderFontSize", 17d);
            Ensure(resources, "DaisySizeSmallHeaderFontSize", 14d);
            Ensure(resources, "DaisySizeExtraSmallHeaderFontSize", 12d);

            // ---- Corner radius ----
            Ensure(resources, "DaisySizeExtraLargeCornerRadius", new CornerRadius(12));
            Ensure(resources, "DaisySizeLargeCornerRadius", new CornerRadius(8));
            Ensure(resources, "DaisySizeMediumCornerRadius", new CornerRadius(8));
            Ensure(resources, "DaisySizeSmallCornerRadius", new CornerRadius(4));
            Ensure(resources, "DaisySizeExtraSmallCornerRadius", new CornerRadius(4));

            // ---- Button padding (horizontal only; height drives vertical) ----
            Ensure(resources, "DaisyButtonExtraLargePadding", new Thickness(16, 0, 16, 0));
            Ensure(resources, "DaisyButtonLargePadding", new Thickness(16, 0, 16, 0));
            Ensure(resources, "DaisyButtonMediumPadding", new Thickness(12, 0, 12, 0));
            Ensure(resources, "DaisyButtonSmallPadding", new Thickness(12, 0, 12, 0));
            Ensure(resources, "DaisyButtonExtraSmallPadding", new Thickness(8, 0, 8, 0));

            // ---- Input padding ----
            Ensure(resources, "DaisyInputExtraLargePadding", new Thickness(16, 8, 16, 8));
            Ensure(resources, "DaisyInputLargePadding", new Thickness(16, 8, 16, 8));
            Ensure(resources, "DaisyInputMediumPadding", new Thickness(12, 8, 12, 8));
            Ensure(resources, "DaisyInputSmallPadding", new Thickness(12, 4, 12, 4));
            Ensure(resources, "DaisyInputExtraSmallPadding", new Thickness(8, 4, 8, 4));

            // ---- Tab padding ----
            Ensure(resources, "DaisyTabExtraLargePadding", new Thickness(12, 4, 12, 4));
            Ensure(resources, "DaisyTabLargePadding", new Thickness(12, 4, 12, 4));
            Ensure(resources, "DaisyTabMediumPadding", new Thickness(12, 4, 12, 4));
            Ensure(resources, "DaisyTabSmallPadding", new Thickness(12, 4, 12, 4));
            Ensure(resources, "DaisyTabExtraSmallPadding", new Thickness(8, 4, 8, 4));

            // ---- Badge padding ----
            Ensure(resources, "DaisyBadgeLargePadding", new Thickness(12, 0, 12, 0));
            Ensure(resources, "DaisyBadgeMediumPadding", new Thickness(8, 0, 8, 0));
            Ensure(resources, "DaisyBadgeSmallPadding", new Thickness(6, 0, 6, 0));
            Ensure(resources, "DaisyBadgeExtraSmallPadding", new Thickness(4, 0, 4, 0));

            // ---- Badge heights (sized for descenders like g, y, p) ----
            Ensure(resources, "DaisyBadgeLargeHeight", 24d);
            Ensure(resources, "DaisyBadgeMediumHeight", 20d);
            Ensure(resources, "DaisyBadgeSmallHeight", 16d);
            Ensure(resources, "DaisyBadgeExtraSmallHeight", 16d);

            // ---- Badge font sizes ----
            Ensure(resources, "DaisyBadgeLargeFontSize", 12d);
            Ensure(resources, "DaisyBadgeMediumFontSize", 10d);
            Ensure(resources, "DaisyBadgeSmallFontSize", 9d);
            Ensure(resources, "DaisyBadgeExtraSmallFontSize", 7d);

            // ---- Card padding ----
            Ensure(resources, "DaisyCardLargePadding", new Thickness(24));
            Ensure(resources, "DaisyCardMediumPadding", new Thickness(20));
            Ensure(resources, "DaisyCardSmallPadding", new Thickness(12));
            Ensure(resources, "DaisyCardCompactPadding", new Thickness(8));

            // ---- Spacing ----
            Ensure(resources, "DaisySpacingXL", 20d);
            Ensure(resources, "DaisySpacingLarge", 16d);
            Ensure(resources, "DaisySpacingMedium", 12d);
            Ensure(resources, "DaisySpacingSmall", 8d);
            Ensure(resources, "DaisySpacingXS", 4d);

            // ---- Border thickness ----
            Ensure(resources, "DaisyBorderThicknessNone", new Thickness(0));
            Ensure(resources, "DaisyBorderThicknessThin", new Thickness(1));
            Ensure(resources, "DaisyBorderThicknessMedium", new Thickness(2));
            Ensure(resources, "DaisyBorderThicknessThick", new Thickness(3));

            // ---- Checkbox and radio indicator sizes ----
            Ensure(resources, "DaisyCheckboxExtraLargeSize", 28d);
            Ensure(resources, "DaisyCheckboxLargeSize", 24d);
            Ensure(resources, "DaisyCheckboxMediumSize", 20d);
            Ensure(resources, "DaisyCheckboxSmallSize", 16d);
            Ensure(resources, "DaisyCheckboxExtraSmallSize", 12d);

            Ensure(resources, "DaisyCheckmarkExtraLargeSize", 20d);
            Ensure(resources, "DaisyCheckmarkLargeSize", 16d);
            Ensure(resources, "DaisyCheckmarkMediumSize", 12d);
            Ensure(resources, "DaisyCheckmarkSmallSize", 8d);
            Ensure(resources, "DaisyCheckmarkExtraSmallSize", 8d);

            Ensure(resources, "DaisyRadioDotExtraLargeSize", 20d);
            Ensure(resources, "DaisyRadioDotLargeSize", 16d);
            Ensure(resources, "DaisyRadioDotMediumSize", 12d);
            Ensure(resources, "DaisyRadioDotSmallSize", 8d);
            Ensure(resources, "DaisyRadioDotExtraSmallSize", 4d);

            // ---- Toggle switch sizes ----
            Ensure(resources, "DaisyToggleExtraLargeWidth", 48d);
            Ensure(resources, "DaisyToggleExtraLargeHeight", 28d);
            Ensure(resources, "DaisyToggleLargeWidth", 40d);
            Ensure(resources, "DaisyToggleLargeHeight", 24d);
            Ensure(resources, "DaisyToggleMediumWidth", 36d);
            Ensure(resources, "DaisyToggleMediumHeight", 20d);
            Ensure(resources, "DaisyToggleSmallWidth", 28d);
            Ensure(resources, "DaisyToggleSmallHeight", 16d);
            Ensure(resources, "DaisyToggleExtraSmallWidth", 24d);
            Ensure(resources, "DaisyToggleExtraSmallHeight", 16d);

            // ---- Toggle knob sizes ----
            Ensure(resources, "DaisyToggleKnobExtraLargeSize", 24d);
            Ensure(resources, "DaisyToggleKnobLargeSize", 20d);
            Ensure(resources, "DaisyToggleKnobMediumSize", 16d);
            Ensure(resources, "DaisyToggleKnobSmallSize", 12d);
            Ensure(resources, "DaisyToggleKnobExtraSmallSize", 12d);

            // ---- Progress bar sizes ----
            Ensure(resources, "DaisyProgressLargeHeight", 16d);
            Ensure(resources, "DaisyProgressMediumHeight", 8d);
            Ensure(resources, "DaisyProgressSmallHeight", 4d);
            Ensure(resources, "DaisyProgressExtraSmallHeight", 2d);

            Ensure(resources, "DaisyProgressLargeCornerRadius", new CornerRadius(8));
            Ensure(resources, "DaisyProgressMediumCornerRadius", new CornerRadius(4));
            Ensure(resources, "DaisyProgressSmallCornerRadius", new CornerRadius(2));
            Ensure(resources, "DaisyProgressExtraSmallCornerRadius", new CornerRadius(1));

            // ---- Avatar sizes ----
            Ensure(resources, "DaisyAvatarExtraLargeSize", 80d);
            Ensure(resources, "DaisyAvatarLargeSize", 56d);
            Ensure(resources, "DaisyAvatarMediumSize", 40d);
            Ensure(resources, "DaisyAvatarSmallSize", 28d);
            Ensure(resources, "DaisyAvatarExtraSmallSize", 20d);

            // Avatar status indicator sizes
            Ensure(resources, "DaisyAvatarStatusExtraLargeSize", 16d);
            Ensure(resources, "DaisyAvatarStatusLargeSize", 14d);
            Ensure(resources, "DaisyAvatarStatusMediumSize", 10d);
            Ensure(resources, "DaisyAvatarStatusSmallSize", 7d);
            Ensure(resources, "DaisyAvatarStatusExtraSmallSize", 5d);

            // ---- Status indicator padding ----
            // Base padding for inline-friendly dot sizes
            Ensure(resources, "DaisyStatusIndicatorExtraLargePadding", 8d);
            Ensure(resources, "DaisyStatusIndicatorLargePadding", 6d);
            Ensure(resources, "DaisyStatusIndicatorMediumPadding", 4d);
            Ensure(resources, "DaisyStatusIndicatorSmallPadding", 3d);
            Ensure(resources, "DaisyStatusIndicatorExtraSmallPadding", 2d);

            // Extra padding for ring/orbit variants that extend beyond the dot
            Ensure(resources, "DaisyStatusIndicatorExtraLargeEffectPadding", 20d);
            Ensure(resources, "DaisyStatusIndicatorLargeEffectPadding", 15d);
            Ensure(resources, "DaisyStatusIndicatorMediumEffectPadding", 12d);
            Ensure(resources, "DaisyStatusIndicatorSmallEffectPadding", 10d);
            Ensure(resources, "DaisyStatusIndicatorExtraSmallEffectPadding", 8d);

            // Avatar placeholder font sizes
            Ensure(resources, "DaisyAvatarFontExtraLargeSize", 20d);
            Ensure(resources, "DaisyAvatarFontLargeSize", 16d);
            Ensure(resources, "DaisyAvatarFontMediumSize", 12d);
            Ensure(resources, "DaisyAvatarFontSmallSize", 9d);
            Ensure(resources, "DaisyAvatarFontExtraSmallSize", 7d);

            // Avatar icon sizes
            Ensure(resources, "DaisyAvatarIconExtraLargeSize", 34d);
            Ensure(resources, "DaisyAvatarIconLargeSize", 24d);
            Ensure(resources, "DaisyAvatarIconMediumSize", 18d);
            Ensure(resources, "DaisyAvatarIconSmallSize", 14d);
            Ensure(resources, "DaisyAvatarIconExtraSmallSize", 10d);

            // ---- Menu sizing ----
            Ensure(resources, "DaisyMenuExtraLargePadding", new Thickness(10, 6, 10, 6));
            Ensure(resources, "DaisyMenuLargePadding", new Thickness(10, 6, 10, 6));
            Ensure(resources, "DaisyMenuMediumPadding", new Thickness(8, 5, 8, 5));
            Ensure(resources, "DaisyMenuSmallPadding", new Thickness(8, 4, 8, 4));
            Ensure(resources, "DaisyMenuExtraSmallPadding", new Thickness(6, 3, 6, 3));

            Ensure(resources, "DaisyMenuExtraLargeFontSize", 15d);
            Ensure(resources, "DaisyMenuLargeFontSize", 13d);
            Ensure(resources, "DaisyMenuMediumFontSize", 12d);
            Ensure(resources, "DaisyMenuSmallFontSize", 10d);
            Ensure(resources, "DaisyMenuExtraSmallFontSize", 9d);

            // ---- Kbd sizing ----
            Ensure(resources, "DaisyKbdExtraLargeHeight", 28d);
            Ensure(resources, "DaisyKbdLargeHeight", 24d);
            Ensure(resources, "DaisyKbdMediumHeight", 20d);
            Ensure(resources, "DaisyKbdSmallHeight", 20d);
            Ensure(resources, "DaisyKbdExtraSmallHeight", 20d);

            Ensure(resources, "DaisyKbdExtraLargePadding", new Thickness(8, 0, 8, 0));
            Ensure(resources, "DaisyKbdLargePadding", new Thickness(6, 0, 6, 0));
            Ensure(resources, "DaisyKbdMediumPadding", new Thickness(4, 0, 4, 0));
            Ensure(resources, "DaisyKbdSmallPadding", new Thickness(3, 0, 3, 0));
            Ensure(resources, "DaisyKbdExtraSmallPadding", new Thickness(2, 0, 2, 0));

            Ensure(resources, "DaisyKbdExtraLargeFontSize", 13d);
            Ensure(resources, "DaisyKbdLargeFontSize", 12d);
            Ensure(resources, "DaisyKbdMediumFontSize", 10d);
            Ensure(resources, "DaisyKbdSmallFontSize", 7d);
            Ensure(resources, "DaisyKbdExtraSmallFontSize", 6d);

            Ensure(resources, "DaisyKbdExtraLargeCornerRadius", new CornerRadius(6));
            Ensure(resources, "DaisyKbdLargeCornerRadius", new CornerRadius(5));
            Ensure(resources, "DaisyKbdMediumCornerRadius", new CornerRadius(4));
            Ensure(resources, "DaisyKbdSmallCornerRadius", new CornerRadius(3));
            Ensure(resources, "DaisyKbdExtraSmallCornerRadius", new CornerRadius(2));

            // ---- TextArea sizing ----
            Ensure(resources, "DaisyTextAreaExtraLargeMinHeight", 128d);
            Ensure(resources, "DaisyTextAreaLargeMinHeight", 96d);
            Ensure(resources, "DaisyTextAreaMediumMinHeight", 64d);
            Ensure(resources, "DaisyTextAreaSmallMinHeight", 48d);
            Ensure(resources, "DaisyTextAreaExtraSmallMinHeight", 40d);

            Ensure(resources, "DaisyTextAreaExtraLargePadding", new Thickness(18));
            Ensure(resources, "DaisyTextAreaLargePadding", new Thickness(14));
            Ensure(resources, "DaisyTextAreaMediumPadding", new Thickness(12));
            Ensure(resources, "DaisyTextAreaSmallPadding", new Thickness(10));
            Ensure(resources, "DaisyTextAreaExtraSmallPadding", new Thickness(6));

            // ---- Input/TextArea vertical alignment ----
            Ensure(resources, "DaisyInputVerticalContentAlignment", VerticalAlignment.Center);
            Ensure(resources, "DaisyTextAreaVerticalContentAlignment", VerticalAlignment.Top);

            // ---- DateTimeline sizing ----
            Ensure(resources, "DaisyDateTimelineExtraLargeHeight", 76d);
            Ensure(resources, "DaisyDateTimelineLargeHeight", 64d);
            Ensure(resources, "DaisyDateTimelineMediumHeight", 52d);
            Ensure(resources, "DaisyDateTimelineSmallHeight", 44d);
            Ensure(resources, "DaisyDateTimelineExtraSmallHeight", 36d);

            Ensure(resources, "DaisyDateTimelineExtraLargeItemWidth", 76d);
            Ensure(resources, "DaisyDateTimelineLargeItemWidth", 64d);
            Ensure(resources, "DaisyDateTimelineMediumItemWidth", 52d);
            Ensure(resources, "DaisyDateTimelineSmallItemWidth", 44d);
            Ensure(resources, "DaisyDateTimelineExtraSmallItemWidth", 36d);

            Ensure(resources, "DaisyDateTimelineItemSpacing", 6d);

            Ensure(resources, "DaisyDateTimelineExtraLargePadding", new Thickness(7, 8, 7, 8));
            Ensure(resources, "DaisyDateTimelineLargePadding", new Thickness(6, 7, 6, 7));
            Ensure(resources, "DaisyDateTimelineMediumPadding", new Thickness(5, 6, 5, 6));
            Ensure(resources, "DaisyDateTimelineSmallPadding", new Thickness(4, 5, 4, 5));
            Ensure(resources, "DaisyDateTimelineExtraSmallPadding", new Thickness(3, 4, 3, 4));

            Ensure(resources, "DaisyDateTimelineExtraLargeCornerRadius", new CornerRadius(14));
            Ensure(resources, "DaisyDateTimelineLargeCornerRadius", new CornerRadius(12));
            Ensure(resources, "DaisyDateTimelineMediumCornerRadius", new CornerRadius(10));
            Ensure(resources, "DaisyDateTimelineSmallCornerRadius", new CornerRadius(8));
            Ensure(resources, "DaisyDateTimelineExtraSmallCornerRadius", new CornerRadius(6));

            Ensure(resources, "DaisyDateTimelineExtraLargeDayNumberFontSize", 16d);
            Ensure(resources, "DaisyDateTimelineLargeDayNumberFontSize", 14d);
            Ensure(resources, "DaisyDateTimelineMediumDayNumberFontSize", 12d);
            Ensure(resources, "DaisyDateTimelineSmallDayNumberFontSize", 10d);
            Ensure(resources, "DaisyDateTimelineExtraSmallDayNumberFontSize", 8d);

            Ensure(resources, "DaisyDateTimelineExtraLargeDayNameFontSize", 14d);
            Ensure(resources, "DaisyDateTimelineLargeDayNameFontSize", 12d);
            Ensure(resources, "DaisyDateTimelineMediumDayNameFontSize", 10d);
            Ensure(resources, "DaisyDateTimelineSmallDayNameFontSize", 9d);
            Ensure(resources, "DaisyDateTimelineExtraSmallDayNameFontSize", 8d);

            Ensure(resources, "DaisyDateTimelineExtraLargeMonthNameFontSize", 14d);
            Ensure(resources, "DaisyDateTimelineLargeMonthNameFontSize", 12d);
            Ensure(resources, "DaisyDateTimelineMediumMonthNameFontSize", 10d);
            Ensure(resources, "DaisyDateTimelineSmallMonthNameFontSize", 9d);
            Ensure(resources, "DaisyDateTimelineExtraSmallMonthNameFontSize", 8d);

            Ensure(resources, "DaisyDateTimelineExtraLargeHeaderFontSize", 18d);
            Ensure(resources, "DaisyDateTimelineLargeHeaderFontSize", 16d);
            Ensure(resources, "DaisyDateTimelineMediumHeaderFontSize", 14d);
            Ensure(resources, "DaisyDateTimelineSmallHeaderFontSize", 12d);
            Ensure(resources, "DaisyDateTimelineExtraSmallHeaderFontSize", 10d);

            // ---- Large display tokens ----
            Ensure(resources, "LargeDisplayFontSize", 36d);
            Ensure(resources, "LargeDisplayElementHeight", 86d);
            Ensure(resources, "LargeDisplayElementWidth", 58d);
            Ensure(resources, "LargeDisplayElementCornerRadius", new CornerRadius(12));
            Ensure(resources, "LargeDisplayElementSpacing", 4d);

            Ensure(resources, "LargeDisplayButtonSize", 42d);
            Ensure(resources, "LargeDisplayButtonFontSize", 20d);
            Ensure(resources, "LargeDisplayButtonCornerRadius", new CornerRadius(8));
            Ensure(resources, "LargeDisplayButtonSpacing", 2d);

            Ensure(resources, "LargeDisplayContainerPadding", new Thickness(16));
            Ensure(resources, "LargeDisplayContainerCornerRadius", new CornerRadius(16));

            // ---- OTP Input sizing ----
            // OTP slots are larger than standard controls for visual prominence
            Ensure(resources, "DaisyOtpExtraLargeSlotSize", 56d);
            Ensure(resources, "DaisyOtpLargeSlotSize", 56d);
            Ensure(resources, "DaisyOtpMediumSlotSize", 48d);
            Ensure(resources, "DaisyOtpSmallSlotSize", 40d);
            Ensure(resources, "DaisyOtpExtraSmallSlotSize", 32d);

            // OTP font sizes (larger than standard for readability in single-digit slots)
            Ensure(resources, "DaisyOtpExtraLargeFontSize", 24d);
            Ensure(resources, "DaisyOtpLargeFontSize", 22d);
            Ensure(resources, "DaisyOtpMediumFontSize", 18d);
            Ensure(resources, "DaisyOtpSmallFontSize", 16d);
            Ensure(resources, "DaisyOtpExtraSmallFontSize", 14d);

            // OTP spacing between slots (non-joined mode)
            Ensure(resources, "DaisyOtpExtraLargeSpacing", 10d);
            Ensure(resources, "DaisyOtpLargeSpacing", 8d);
            Ensure(resources, "DaisyOtpMediumSpacing", 6d);
            Ensure(resources, "DaisyOtpSmallSpacing", 5d);
            Ensure(resources, "DaisyOtpExtraSmallSpacing", 4d);

            // ---- Tag Picker sizing ----
            Ensure(resources, "DaisyTagExtraLargePadding", new Thickness(12, 6, 12, 6));
            Ensure(resources, "DaisyTagLargePadding", new Thickness(10, 5, 10, 5));
            Ensure(resources, "DaisyTagMediumPadding", new Thickness(8, 4, 8, 4));
            Ensure(resources, "DaisyTagSmallPadding", new Thickness(6, 3, 6, 3));
            Ensure(resources, "DaisyTagExtraSmallPadding", new Thickness(4, 2, 4, 2));

            // Tag close icon sizes
            Ensure(resources, "DaisyTagCloseExtraLargeSize", 16d);
            Ensure(resources, "DaisyTagCloseLargeSize", 14d);
            Ensure(resources, "DaisyTagCloseMediumSize", 12d);
            Ensure(resources, "DaisyTagCloseSmallSize", 10d);
            Ensure(resources, "DaisyTagCloseExtraSmallSize", 8d);

            // ---- Divider sizing ----
            // Divider text font size
            Ensure(resources, "DaisyDividerExtraLargeFontSize", 16d);
            Ensure(resources, "DaisyDividerLargeFontSize", 14d);
            Ensure(resources, "DaisyDividerMediumFontSize", 12d);
            Ensure(resources, "DaisyDividerSmallFontSize", 10d);
            Ensure(resources, "DaisyDividerExtraSmallFontSize", 9d);

            // Divider text padding (horizontal divider: left/right, vertical: top/bottom)
            Ensure(resources, "DaisyDividerExtraLargeTextPadding", 18d);
            Ensure(resources, "DaisyDividerLargeTextPadding", 16d);
            Ensure(resources, "DaisyDividerMediumTextPadding", 12d);
            Ensure(resources, "DaisyDividerSmallTextPadding", 10d);
            Ensure(resources, "DaisyDividerExtraSmallTextPadding", 8d);

            // Divider margin (spacing around divider)
            Ensure(resources, "DaisyDividerExtraLargeMargin", 7d);
            Ensure(resources, "DaisyDividerLargeMargin", 5d);
            Ensure(resources, "DaisyDividerMediumMargin", 3d);
            Ensure(resources, "DaisyDividerSmallMargin", 2d);
            Ensure(resources, "DaisyDividerExtraSmallMargin", 1d);

            // ---- Dock sizing ----
            // Item container size (compact sizing, one step smaller than standard)
            Ensure(resources, "DaisyDockExtraLargeItemSize", 64d);
            Ensure(resources, "DaisyDockLargeItemSize", 48d);
            Ensure(resources, "DaisyDockMediumItemSize", 32d);
            Ensure(resources, "DaisyDockSmallItemSize", 24d);
            Ensure(resources, "DaisyDockExtraSmallItemSize", 16d);

            // Dock item spacing
            Ensure(resources, "DaisyDockExtraLargeSpacing", 6d);
            Ensure(resources, "DaisyDockLargeSpacing", 4d);
            Ensure(resources, "DaisyDockMediumSpacing", 4d);
            Ensure(resources, "DaisyDockSmallSpacing", 2d);
            Ensure(resources, "DaisyDockExtraSmallSpacing", 2d);

            // Dock icon size ratio (relative to item size)
            Ensure(resources, "DaisyDockIconSizeRatio", 0.6d);

            // ---- SlideToConfirm sizing (compact scale to match other controls) ----
            // Track min width (control auto-sizes to fit text, this is the minimum text area)
            // Values sized to always fit "SLIDE" at each font size
            Ensure(resources, "DaisySlideToConfirmExtraLargeTrackWidth", 120d);
            Ensure(resources, "DaisySlideToConfirmLargeTrackWidth", 100d);
            Ensure(resources, "DaisySlideToConfirmMediumTrackWidth", 90d);
            Ensure(resources, "DaisySlideToConfirmSmallTrackWidth", 80d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallTrackWidth", 70d);

            // Track height (matches input heights)
            Ensure(resources, "DaisySlideToConfirmExtraLargeTrackHeight", 36d);
            Ensure(resources, "DaisySlideToConfirmLargeTrackHeight", 32d);
            Ensure(resources, "DaisySlideToConfirmMediumTrackHeight", 28d);
            Ensure(resources, "DaisySlideToConfirmSmallTrackHeight", 24d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallTrackHeight", 20d);

            // Handle size (~55-60% of track height for proper proportions)
            Ensure(resources, "DaisySlideToConfirmExtraLargeHandleSize", 22d);
            Ensure(resources, "DaisySlideToConfirmLargeHandleSize", 18d);
            Ensure(resources, "DaisySlideToConfirmMediumHandleSize", 16d);
            Ensure(resources, "DaisySlideToConfirmSmallHandleSize", 14d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallHandleSize", 12d);

            // Icon size (~70-75% of handle for legibility)
            Ensure(resources, "DaisySlideToConfirmExtraLargeIconSize", 16d);
            Ensure(resources, "DaisySlideToConfirmLargeIconSize", 14d);
            Ensure(resources, "DaisySlideToConfirmMediumIconSize", 12d);
            Ensure(resources, "DaisySlideToConfirmSmallIconSize", 10d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallIconSize", 9d);

            // Font size (smaller to fit inside compact track)
            Ensure(resources, "DaisySlideToConfirmExtraLargeFontSize", 10d);
            Ensure(resources, "DaisySlideToConfirmLargeFontSize", 9d);
            Ensure(resources, "DaisySlideToConfirmMediumFontSize", 8d);
            Ensure(resources, "DaisySlideToConfirmSmallFontSize", 7d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallFontSize", 6d);

            // Corner radius (half of track height for pill shape)
            Ensure(resources, "DaisySlideToConfirmExtraLargeCornerRadius", 18d);
            Ensure(resources, "DaisySlideToConfirmLargeCornerRadius", 16d);
            Ensure(resources, "DaisySlideToConfirmMediumCornerRadius", 14d);
            Ensure(resources, "DaisySlideToConfirmSmallCornerRadius", 12d);
            Ensure(resources, "DaisySlideToConfirmExtraSmallCornerRadius", 10d);

            if (PlatformCompatibility.IsMobile)
            {
                // Mobile profile: larger touch targets for finger-first interaction.
                resources["DaisySizeExtraLargeHeight"] = 56d;
                resources["DaisySizeLargeHeight"] = 48d;
                resources["DaisySizeMediumHeight"] = 44d;
                resources["DaisySizeSmallHeight"] = 40d;
                resources["DaisySizeExtraSmallHeight"] = 36d;

                resources["DaisyInputFloatingExtraLargeHeight"] = 64d;
                resources["DaisyInputFloatingLargeHeight"] = 56d;
                resources["DaisyInputFloatingMediumHeight"] = 52d;
                resources["DaisyInputFloatingSmallHeight"] = 48d;
                resources["DaisyInputFloatingExtraSmallHeight"] = 44d;
            }

            resources[InitializationKey] = true;
        }

        private static void Ensure(ResourceDictionary resources, string key, object value)
        {
            if (!resources.ContainsKey(key))
            {
                resources[key] = value;
            }
        }
    }
}
