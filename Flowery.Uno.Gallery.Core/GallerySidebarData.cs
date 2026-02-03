using System;
using System.Collections.ObjectModel;
using System.Linq;
using Flowery.Controls;
using Flowery.Localization;

namespace Flowery.Uno.Gallery
{
    /// <summary>
    /// Provides Gallery-specific sidebar categories and languages.
    /// This data is specific to the Flowery.Uno Gallery showcase app.
    /// </summary>
    public static class GallerySidebarData
    {
        /// <summary>
        /// Creates the default categories for the Gallery showcase sidebar.
        /// </summary>
        public static ObservableCollection<SidebarCategory> CreateCategories()
        {
            var categories = new ObservableCollection<SidebarCategory>
            {
                // Home stays at top
                new()
                {
                    Name = "Sidebar_Home",
                    IconKey = "DaisyIconHome",
                    Items = new()
                    {
                        new() { Id = "welcome", Name = "Sidebar_Welcome", TabHeader = "Sidebar_Home" },
                        new GalleryThemeSelectorItem { Id = "theme", Name = "Sidebar_Theme", TabHeader = "Sidebar_Home" },
                        new GalleryLanguageSelectorItem { Id = "language", Name = "Sidebar_Language", TabHeader = "Sidebar_Home" },
                        new GallerySizeSelectorItem { Id = "size", Name = "Sidebar_Size", TabHeader = "Sidebar_Home" }
                    }
                },
                // Alphabetically sorted categories
                new()
                {
                    Name = "Sidebar_Actions",
                    IconKey = "DaisyIconActions",
                    Items = new()
                    {
                        new() { Id = "button", Name = "Sidebar_Button", TabHeader = "Sidebar_Actions" },
                        new() { Id = "button-group", Name = "Sidebar_ButtonGroup", TabHeader = "Sidebar_Actions" },
                        new() { Id = "figma-comment", Name = "Sidebar_FigmaComment", TabHeader = "Sidebar_Actions" },
                        new() { Id = "copybutton", Name = "Sidebar_CopyButton", TabHeader = "Sidebar_Actions" },
                        new() { Id = "dropdown", Name = "Sidebar_Dropdown", TabHeader = "Sidebar_Actions" },
                        new() { Id = "dropdownmenu", Name = "Sidebar_DropdownMenu", TabHeader = "Sidebar_Actions" },
                        new() { Id = "fab", Name = "Sidebar_FAB", TabHeader = "Sidebar_Actions" },
                        new() { Id = "popover", Name = "Sidebar_Popover", TabHeader = "Sidebar_Actions" },
                        new() { Id = "modal", Name = "Sidebar_Modal", TabHeader = "Sidebar_Actions" },
                        new() { Id = "modal-radii", Name = "Sidebar_ModalRadii", TabHeader = "Sidebar_Actions" },
                        new() { Id = "swap", Name = "Sidebar_Swap", TabHeader = "Sidebar_Actions" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Cards",
                    IconKey = "DaisyIconCard",
                    Items = new()
                    {
                        new() { Id = "card", Name = "Sidebar_Card", TabHeader = "Sidebar_Cards" },
                        new() { Id = "card-stack", Name = "Sidebar_CardStack", TabHeader = "Sidebar_Cards" },
                        new() { Id = "expandable-cards", Name = "Showcase_ExpandableCards_Title", TabHeader = "Sidebar_Cards" },
                        new() { Id = "patterned-card", Name = "Sidebar_PatternedCard", TabHeader = "Sidebar_Cards" },
                        new() { Id = "glass-card", Name = "Sidebar_GlassCard", TabHeader = "Sidebar_Cards" },
                        new() { Id = "daisyglass-simulated", Name = "Sidebar_DaisyGlassSimulated", TabHeader = "Sidebar_Cards" },
                        new() { Id = "daisyglass-bitmap-capture", Name = "Sidebar_DaisyGlassBitmapCapture", TabHeader = "Sidebar_Cards" },
                        new() { Id = "daisyglass-full-width", Name = "Sidebar_DaisyGlassFullWidth", TabHeader = "Sidebar_Cards" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Carousel",
                    IconKey = "DaisyIconRepost",
                    Items = new()
                    {
                        new() { Id = "carousel", Name = "Sidebar_Carousel", TabHeader = "Sidebar_Carousel" }
#if __WASM__ // && (HAS_UNO_SKIA || __UNO_SKIA__ || __SKIA__ || HAS_UNO_SKIA_WEBASSEMBLY_BROWSER || __UNO_SKIA_WEBASSEMBLY_BROWSER__)
                        ,new() { Id = "carousel-gl", Name = "Sidebar_CarouselGL", TabHeader = "Sidebar_CarouselGL" }
#endif
#if __WASM__ && FLOWERY_GL_TRANSITIONS
                        ,new() { Id = "carousel-gl-transitions", Name = "Sidebar_CarouselGLTransitions", TabHeader = "Sidebar_CarouselGLTransitions" }
#endif
                    }
                },
                new()
                {
                    Name = "Sidebar_DataDisplay",
                    IconKey = "DaisyIconDataDisplay",
                    Items = new()
                    {
                        new() { Id = "accordion", Name = "Sidebar_Accordion", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "animatednumber", Name = "Sidebar_AnimatedNumber", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "avatar", Name = "Sidebar_Avatar", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "badge", Name = "Sidebar_Badge", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "chat-bubble", Name = "Sidebar_ChatBubble", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "collapse", Name = "Sidebar_Collapse", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "contributiongraph", Name = "Sidebar_ContributionGraph", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "clock", Name = "Sidebar_Clock", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "countdown", Name = "Sidebar_Countdown", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "diff", Name = "Sidebar_Diff", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "hover-gallery", Name = "Sidebar_HoverGallery", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "kbd", Name = "Sidebar_Kbd", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "list", Name = "Sidebar_List", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "numberflow", Name = "Sidebar_NumberFlow", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "stat", Name = "Sidebar_Stat", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "table", Name = "Sidebar_Table", TabHeader = "Sidebar_DataDisplay" },
                        new() { Id = "text-rotate", Name = "Sidebar_TextRotate", TabHeader = "Sidebar_DataDisplay" }
                    }
                },
                new()
                {
                    Name = "Sidebar_DateDisplay",
                    IconKey = "DaisyIconDateDisplay",
                    Items = new()
                    {
                        new() { Id = "date-timeline", Name = "Sidebar_DateTimeline", TabHeader = "Sidebar_DateDisplay" },
                        new() { Id = "timeline", Name = "Sidebar_Timeline", TabHeader = "Sidebar_DateDisplay" }
                    }
                },
                new()
                {
                    Name = "Sidebar_DataInput",
                    IconKey = "DaisyIconDataInput",
                    Items = new()
                    {
                        new() { Id = "checkbox", Name = "Sidebar_Checkbox", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "file-input", Name = "Sidebar_FileInput", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "input", Name = "Sidebar_Input", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "calendar-date-picker", Name = "Sidebar_CalendarDatePicker", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "mask-input", Name = "Sidebar_MaskInput", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "numericupdown", Name = "Sidebar_NumericUpDown", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "otpinput", Name = "Sidebar_OtpInput", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "passwordbox", Name = "Sidebar_PasswordBox", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "radio", Name = "Sidebar_Radio", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "range", Name = "Sidebar_Range", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "rating", Name = "Sidebar_Rating", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "select", Name = "Sidebar_Select", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "slide-to-confirm", Name = "Sidebar_SlideToConfirm", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "tagpicker", Name = "Sidebar_TagPicker", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "textarea", Name = "Sidebar_TextArea", TabHeader = "Sidebar_DataInput" },
                        new() { Id = "toggle", Name = "Sidebar_Toggle", TabHeader = "Sidebar_DataInput" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Divider",
                    IconKey = "DaisyIconDivider",
                    Items = new()
                    {
                        new() { Id = "divider", Name = "Sidebar_DividerItem", TabHeader = "Sidebar_Divider" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Feedback",
                    IconKey = "DaisyIconFeedback",
                    Items = new()
                    {
                        new() { Id = "alert", Name = "Sidebar_Alert", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "loading", Name = "Sidebar_Loading", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "progress", Name = "Sidebar_Progress", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "radial-progress", Name = "Sidebar_RadialProgress", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "status", Name = "Sidebar_Status", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "skeleton", Name = "Sidebar_Skeleton", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "toast", Name = "Sidebar_Toast", TabHeader = "Sidebar_Feedback" },
                        new() { Id = "tooltip", Name = "Sidebar_Tooltip", TabHeader = "Sidebar_Feedback" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Layout",
                    IconKey = "DaisyIconLayout",
                    Items = new()
                    {
                        new() { Id = "drawer", Name = "Sidebar_Drawer", TabHeader = "Sidebar_Layout" },
                        new() { Id = "hero", Name = "Sidebar_Hero", TabHeader = "Sidebar_Layout" },
                        new() { Id = "indicator", Name = "Sidebar_Indicator", TabHeader = "Sidebar_Layout" },
                        new() { Id = "join", Name = "Sidebar_Join", TabHeader = "Sidebar_Layout" },
                        new() { Id = "mask", Name = "Sidebar_Mask", TabHeader = "Sidebar_Layout" },
                        new() { Id = "mockup", Name = "Sidebar_Mockup", TabHeader = "Sidebar_Layout" },
                        new() { Id = "resizablecolumns", Name = "Sidebar_ResizableColumns", TabHeader = "Sidebar_Layout" },
                        new() { Id = "stack", Name = "Sidebar_Stack", TabHeader = "Sidebar_Layout" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Navigation",
                    IconKey = "DaisyIconNavigation",
                    Items = new()
                    {
                        new() { Id = "breadcrumbs", Name = "Sidebar_Breadcrumbs", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "breadcrumbbar", Name = "Sidebar_BreadcrumbBar", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "dock", Name = "Sidebar_Dock", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "menu", Name = "Sidebar_Menu", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "navbar", Name = "Sidebar_Navbar", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "pagination", Name = "Sidebar_Pagination", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "steps", Name = "Sidebar_Steps", TabHeader = "Sidebar_Navigation" },
                        new() { Id = "tabs", Name = "Sidebar_Tabs", TabHeader = "Sidebar_Navigation" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Theming",
                    IconKey = "DaisyIconTheme",
                    Items = new()
                    {
                        new() { Id = "product-themes", Name = "Sidebar_ProductThemes", TabHeader = "Sidebar_ProductThemes" },
                        new() { Id = "theme-controller", Name = "Sidebar_ThemeController", TabHeader = "Sidebar_Theming" },
                        new() { Id = "theme-radio", Name = "Sidebar_ThemeRadio", TabHeader = "Sidebar_Theming" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Effects",
                    IconKey = "DaisyIconEffects",
                    Items = new()
                    {
                        new() { Id = "reveal", Name = "Sidebar_Reveal", TabHeader = "Sidebar_Effects" },
                        new() { Id = "scramble", Name = "Sidebar_Scramble", TabHeader = "Sidebar_Effects" },
                        new() { Id = "wave", Name = "Sidebar_Wave", TabHeader = "Sidebar_Effects" },
                        new() { Id = "typewriter", Name = "Sidebar_Typewriter", TabHeader = "Sidebar_Effects" },
                        new() { Id = "cursor-follow", Name = "Sidebar_CursorFollow", TabHeader = "Sidebar_Effects" }
                    }
                },
                new()
                {
                    Name = "Sidebar_Showcase",
                    IconKey = "DaisyIconEffects",
                    Items = new()
                    {
                        new() { Id = "showcase", Name = "Sidebar_Showcase", TabHeader = "Sidebar_Showcase" }
                    }
                },

                // Custom Controls and Color Picker stay at bottom
                new()
                {
                    Name = "Sidebar_CustomControls",
                    IconKey = "DaisyIconSun",
                    Items = new()
                    {
                        new() { Id = "flow-kanban", Name = "Sidebar_FlowKanban", TabHeader = "Sidebar_FlowKanban" },
                        new() { Id = "scaling", Name = "Sidebar_ScalingItem", TabHeader = "Sidebar_Scaling" },
                        new() { Id = "size-dropdown", Name = "Sidebar_SizeDropdown", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "modifier-keys", Name = "Sidebar_ModifierKeys", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "weather-card", Name = "Sidebar_WeatherCard", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "current-weather", Name = "Sidebar_CurrentWeather", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "weather-forecast", Name = "Sidebar_WeatherForecast", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "weather-metrics", Name = "Sidebar_WeatherMetrics", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "weather-conditions", Name = "Sidebar_WeatherConditions", TabHeader = "Sidebar_CustomControls" },
                        new() { Id = "service-integration", Name = "Sidebar_ServiceIntegration", TabHeader = "Sidebar_CustomControls" }
                    }
                },
                new()
                {
                    Name = "Sidebar_ColorPicker",
                    IconKey = "DaisyIconPalette",
                    Items = new()
                    {
                        new() { Id = "colorwheel", Name = "Sidebar_ColorWheel", TabHeader = "Sidebar_ColorPicker" },
                        new() { Id = "colorgrid", Name = "Sidebar_ColorGrid", TabHeader = "Sidebar_ColorPicker" },
                        new() { Id = "colorslider", Name = "Sidebar_ColorSliders", TabHeader = "Sidebar_ColorPicker" },
                        new() { Id = "coloreditor", Name = "Sidebar_ColorEditor", TabHeader = "Sidebar_ColorPicker" },
                        new() { Id = "screenpicker", Name = "Sidebar_ScreenPicker", TabHeader = "Sidebar_ColorPicker" },
                        new() { Id = "colorpickerdialog", Name = "Sidebar_ColorPickerDialog", TabHeader = "Sidebar_ColorPicker" }
                    }
                }
            };

            if (!OperatingSystem.IsBrowser())
            {
                var cardsCategory = categories.FirstOrDefault(category => category.Name == "Sidebar_Cards");
                if (cardsCategory != null)
                {
                    var insertIndex = cardsCategory.Items
                        .Select((item, index) => new { Item = item, Index = index })
                        .FirstOrDefault(entry => entry.Item.Id == "daisyglass-full-width")
                        ?.Index ?? cardsCategory.Items.Count;

                    cardsCategory.Items.Insert(insertIndex,
                        new SidebarItem
                        {
                            Id = "daisyglass-skia-matrix",
                            Name = "Sidebar_DaisyGlassSkiaMatrix",
                            TabHeader = "Sidebar_Cards"
                        });
                }
            }

            ApplyLocalization(categories);

            return categories;
        }

        /// <summary>
        /// Creates the default languages for the Gallery showcase app.
        /// Uses the centralized language data from the library.
        /// </summary>
        public static ObservableCollection<SidebarLanguage> CreateLanguages()
        {
            return SidebarLanguage.CreateAll();
        }

        private static void ApplyLocalization(ObservableCollection<SidebarCategory> categories)
        {
            foreach (var category in categories)
            {
                category.DisplayName = FloweryLocalization.GetString(category.Name);

                foreach (var item in category.Items)
                {
                    item.DisplayName = FloweryLocalization.GetString(item.Name);
                }
            }
        }
    }
}
