using System;
using System.Threading.Tasks;
using Flowery.Localization;
using Flowery.Theming;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace Flowery.Uno.Kanban.Controls
{
    /// <summary>
    /// Partial class containing all dialog-related methods for FlowKanban.
    /// All dialogs use themed DaisyButton controls for visual consistency.
    /// </summary>
    public partial class FlowKanban
    {
        #region Dialog Execution Methods

        private async void ExecuteAddColumn()
        {
            var result = await ShowInputDialogAsync(
                FloweryLocalization.GetStringInternal("Kanban_AddSection"),
                string.Empty,
                FloweryLocalization.GetStringInternal("Kanban_NewColumnPlaceholder"),
                closeOnEnter: true);

            if (result != null)
            {
                var column = new FlowKanbanColumnData { Title = result };
                Board.Columns.Add(column);
                UpdateKeyboardColumnIndex(column);
                QueueFocusAction(() => FocusColumnHeader(column));
            }
        }

        private async void ExecuteRemoveColumn(FlowKanbanColumnData? column)
        {
            if (column == null) return;

            if (!ConfirmColumnRemovals)
            {
                Board.Columns.Remove(column);
                return;
            }

            var confirmed = await ShowConfirmDialogAsync(
                FloweryLocalization.GetStringInternal("Common_ConfirmDelete"),
                FloweryLocalization.GetStringInternal("Kanban_DeleteColumnConfirm"),
                FloweryLocalization.GetStringInternal("Common_Delete"));

            if (confirmed)
            {
                Board.Columns.Remove(column);
            }
        }

        private async void ExecuteEditColumn(FlowKanbanColumnData? column)
        {
            if (column == null) return;

            var xamlRoot = XamlRoot;
            if (xamlRoot == null)
            {
                return;
            }

            await FlowKanbanColumnEditorDialog.ShowAsync(column, xamlRoot);
        }

        private async void ExecuteEditBoard()
        {
            var xamlRoot = XamlRoot;
            if (xamlRoot == null)
            {
                return;
            }

            await FlowBoardEditorDialog.ShowAsync(Board, xamlRoot);
        }

        private async void ExecuteRenameBoard()
        {
            var result = await ShowInputDialogAsync(
                FloweryLocalization.GetStringInternal("Kanban_Board_RenameTitle"),
                Board.Title,
                FloweryLocalization.GetStringInternal("Kanban_Board_TitlePlaceholder"),
                closeOnEnter: true);

            if (result != null)
            {
                Board.Title = result;
            }
        }

        private async void ExecuteRemoveCard(FlowTask? task)
        {
            if (task == null) return;

            if (!ConfirmCardRemovals)
            {
                RemoveCard(task);
                return;
            }

            var confirmed = await ShowConfirmDialogAsync(
                FloweryLocalization.GetStringInternal("Common_ConfirmDelete"),
                FloweryLocalization.GetStringInternal("Kanban_DeleteCardConfirm"),
                FloweryLocalization.GetStringInternal("Common_Delete"));

            if (confirmed)
            {
                RemoveCard(task);
            }
        }

        private void RemoveCard(FlowTask task)
        {
            foreach (var col in Board.Columns)
            {
                if (col.Tasks.Contains(task))
                {
                    ExecuteCommand(new DeleteCardCommand(col, task));
                    break;
                }
            }
        }

        private async void ExecuteOpenSettings()
        {
            if (XamlRoot == null) return;
            await FlowKanbanSettingsDialog.ShowAsync(this, XamlRoot);
        }

        private async void ExecuteShowKeyboardHelp()
        {
            if (XamlRoot == null) return;
            var showAdminShortcuts = await IsCurrentUserGlobalAdminAsync();
            await FlowKanbanKeyboardHelpDialog.ShowAsync(XamlRoot, showAdminShortcuts);
        }

        #endregion

        #region Themed Dialog Helpers

        /// <summary>
        /// Shows a themed input dialog with a single text input field.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="initialValue">Initial value for the input.</param>
        /// <param name="placeholder">Placeholder text for the input.</param>
        /// <param name="closeOnEnter">Whether Enter confirms the dialog.</param>
        /// <returns>The entered text if saved, null if cancelled.</returns>
        private Task<string?> ShowInputDialogAsync(string title, string initialValue, string placeholder, bool closeOnEnter = false)
        {
            if (XamlRoot == null)
                return Task.FromResult<string?>(null);

            return FlowKanbanInputDialog.ShowAsync(title, initialValue, placeholder, XamlRoot, closeOnEnter);
        }

        /// <summary>
        /// Shows a themed confirmation dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Confirmation message.</param>
        /// <param name="confirmText">Text for the confirm button (default: "Delete").</param>
        /// <returns>True if confirmed, false if cancelled.</returns>
        private Task<bool> ShowConfirmDialogAsync(string title, string message, string? confirmText = null)
        {
            if (XamlRoot == null)
                return Task.FromResult(false);

            return FlowKanbanConfirmDialog.ShowAsync(
                title,
                message,
                confirmText ?? FloweryLocalization.GetStringInternal("Common_Delete"),
                DaisyButtonVariant.Error,
                XamlRoot);
        }

        /// <summary>
        /// Creates content for a simple dialog with title, content, and action buttons.
        /// </summary>
        private static FrameworkElement CreateSimpleDialogContent(
            string title,
            FrameworkElement mainContent,
            out DaisyButton primaryButton,
            out DaisyButton cancelButton,
            string? primaryText = null,
            DaisyButtonVariant primaryVariant = DaisyButtonVariant.Success)
        {
            var container = new StackPanel
            {
                Spacing = 16,
                MinWidth = 280
            };

            // Title
            container.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
            });

            // Main content
            container.Children.Add(mainContent);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            primaryButton = new DaisyButton
            {
                Content = primaryText ?? FloweryLocalization.GetStringInternal("Common_Save"),
                Variant = primaryVariant,
                MinWidth = 80
            };

            cancelButton = new DaisyButton
            {
                Content = FloweryLocalization.GetStringInternal("Common_Cancel"),
                Variant = DaisyButtonVariant.Error,
                MinWidth = 80
            };

            buttonPanel.Children.Add(primaryButton);
            buttonPanel.Children.Add(cancelButton);

            var sectionBackground = DaisyResourceLookup.GetBrush("DaisyBase200Brush");
            var sectionBorderBrush = DaisyResourceLookup.GetBrush("DaisyBase300Brush");
            var footerCard = new Border
            {
                Background = sectionBackground,
                BorderBrush = sectionBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Child = buttonPanel
            };

            container.Children.Add(footerCard);

            return container;
        }

        #endregion

        #region FloweryDialogBase Dialogs

        private sealed partial class FlowKanbanInputDialog : FloweryDialogBase
        {
            private readonly TaskCompletionSource<string?> _tcs = new();
            private readonly XamlRoot _xamlRoot;
            private Panel? _hostPanel;
            private bool _isClosing;
            private long _isOpenCallbackToken;

            private readonly DaisyInput _input;
            private readonly DaisyButton _saveButton;
            private readonly DaisyButton _cancelButton;

            private FlowKanbanInputDialog(
                string title,
                string initialValue,
                string placeholder,
                XamlRoot xamlRoot,
                bool closeOnEnter)
            {
                _xamlRoot = xamlRoot;

                _input = new DaisyInput
                {
                    Text = initialValue,
                    PlaceholderText = placeholder,
                    Variant = Enums.DaisyInputVariant.Bordered,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                AutomationProperties.SetName(_input, title);

                Content = CreateSimpleDialogContent(title, _input, out _saveButton, out _cancelButton);
                IsCloseOnEnterEnabled = closeOnEnter;
                ApplySmartSizingWithAutoHeight(xamlRoot);

                _saveButton.Click += OnSaveClicked;
                _cancelButton.Click += OnCancelClicked;
                _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            }

            public static Task<string?> ShowAsync(
                string title,
                string initialValue,
                string placeholder,
                XamlRoot xamlRoot,
                bool closeOnEnter)
            {
                if (xamlRoot == null)
                    return Task.FromResult<string?>(null);

                var dialog = new FlowKanbanInputDialog(title, initialValue, placeholder, xamlRoot, closeOnEnter);
                return dialog.ShowInternalAsync();
            }

            private Task<string?> ShowInternalAsync()
            {
                _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
                if (_hostPanel == null)
                {
                    Close(null);
                    return _tcs.Task;
                }

                _hostPanel.Children.Add(this);
                IsOpen = true;
                _input.FocusInput(selectAll: true);
                return _tcs.Task;
            }

            protected override bool OnEnterKeyRequested()
            {
                CommitAndClose();
                return true;
            }

            private void OnSaveClicked(object sender, RoutedEventArgs e)
            {
                CommitAndClose();
            }

            private void OnCancelClicked(object sender, RoutedEventArgs e)
            {
                Close(null);
            }

            private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
            {
                if (!IsOpen && !_isClosing)
                {
                    Close(null);
                }
            }

            private void CommitAndClose()
            {
                var text = _input.Text?.Trim();
                Close(string.IsNullOrEmpty(text) ? null : text);
            }

            private void Close(string? result)
            {
                if (_isClosing)
                    return;

                _isClosing = true;
                if (IsOpen)
                    IsOpen = false;

                if (_isOpenCallbackToken != 0)
                {
                    UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                    _isOpenCallbackToken = 0;
                }

                if (_hostPanel != null)
                {
                    _hostPanel.Children.Remove(this);
                    _hostPanel = null;
                }

                _tcs.TrySetResult(result);
            }
        }

        private sealed partial class FlowKanbanColumnEditorDialog : FloweryDialogBase
        {
            private readonly TaskCompletionSource<bool> _tcs = new();
            private readonly FlowKanbanColumnData _column;
            private readonly XamlRoot _xamlRoot;
            private Panel? _hostPanel;
            private bool _isClosing;
            private long _isOpenCallbackToken;

            private readonly DaisyInput _titleInput;
            private readonly DaisyTextArea _policyBox;
            private readonly DaisyButton _saveButton;
            private readonly DaisyButton _cancelButton;

            private FlowKanbanColumnEditorDialog(FlowKanbanColumnData column, XamlRoot xamlRoot)
            {
                _column = column;
                _xamlRoot = xamlRoot;

                var contentStack = new StackPanel
                {
                    Spacing = 12,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var titleLabel = new TextBlock
                {
                    Text = FloweryLocalization.GetStringInternal("Kanban_RenameSection"),
                    FontSize = 11,
                    Opacity = 0.7,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
                contentStack.Children.Add(titleLabel);

                _titleInput = new DaisyInput
                {
                    Text = column.Title,
                    PlaceholderText = FloweryLocalization.GetStringInternal("Kanban_NewColumnPlaceholder"),
                    Variant = Enums.DaisyInputVariant.Bordered,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                AutomationProperties.SetLabeledBy(_titleInput, titleLabel);
                contentStack.Children.Add(_titleInput);

                var policyLabel = new TextBlock
                {
                    Text = FloweryLocalization.GetStringInternal("Kanban_Policy_Info"),
                    FontSize = 11,
                    Opacity = 0.7,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };
                contentStack.Children.Add(policyLabel);

                _policyBox = new DaisyTextArea
                {
                    Text = column.PolicyText ?? string.Empty,
                    MinRows = 2,
                    MaxRows = 4,
                    Variant = Enums.DaisyInputVariant.Bordered,
                    PlaceholderText = FloweryLocalization.GetStringInternal("Kanban_Policies_Placeholder"),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                AutomationProperties.SetLabeledBy(_policyBox, policyLabel);
                contentStack.Children.Add(_policyBox);

                var dialogTitle = string.IsNullOrWhiteSpace(column.Title)
                    ? FloweryLocalization.GetStringInternal("Kanban_RenameSection")
                    : column.Title;

                Content = CreateSimpleDialogContent(dialogTitle, contentStack, out _saveButton, out _cancelButton);
                ApplySmartSizingWithAutoHeight(xamlRoot);

                _saveButton.Click += OnSaveClicked;
                _cancelButton.Click += OnCancelClicked;
                _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            }

            public static Task<bool> ShowAsync(FlowKanbanColumnData column, XamlRoot xamlRoot)
            {
                if (xamlRoot == null)
                    return Task.FromResult(false);

                var dialog = new FlowKanbanColumnEditorDialog(column, xamlRoot);
                return dialog.ShowInternalAsync();
            }

            private Task<bool> ShowInternalAsync()
            {
                _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
                if (_hostPanel == null)
                {
                    Close(false);
                    return _tcs.Task;
                }

                _hostPanel.Children.Add(this);
                IsOpen = true;
                _titleInput.FocusInput(selectAll: true);
                return _tcs.Task;
            }

            private void OnSaveClicked(object sender, RoutedEventArgs e)
            {
                CommitAndClose();
            }

            private void OnCancelClicked(object sender, RoutedEventArgs e)
            {
                Close(false);
            }

            private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
            {
                if (!IsOpen && !_isClosing)
                {
                    Close(false);
                }
            }

            private void CommitAndClose()
            {
                var title = _titleInput.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    _column.Title = title;
                }

                var policy = _policyBox.Text?.Trim();
                _column.PolicyText = string.IsNullOrWhiteSpace(policy) ? null : policy;

                Close(true);
            }

            private void Close(bool saved)
            {
                if (_isClosing)
                    return;

                _isClosing = true;
                if (IsOpen)
                    IsOpen = false;

                if (_isOpenCallbackToken != 0)
                {
                    UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                    _isOpenCallbackToken = 0;
                }

                if (_hostPanel != null)
                {
                    _hostPanel.Children.Remove(this);
                    _hostPanel = null;
                }

                _tcs.TrySetResult(saved);
            }
        }

        private sealed partial class FlowKanbanConfirmDialog : FloweryDialogBase
        {
            private readonly TaskCompletionSource<bool> _tcs = new();
            private readonly XamlRoot _xamlRoot;
            private Panel? _hostPanel;
            private bool _isClosing;
            private long _isOpenCallbackToken;

            private readonly DaisyButton _confirmButton;
            private readonly DaisyButton _cancelButton;

            private FlowKanbanConfirmDialog(
                string title,
                string message,
                string confirmText,
                DaisyButtonVariant confirmVariant,
                XamlRoot xamlRoot)
            {
                _xamlRoot = xamlRoot;

                var messageBlock = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = DaisyResourceLookup.GetBrush("DaisyBaseContentBrush")
                };

                Content = CreateSimpleDialogContent(
                    title,
                    messageBlock,
                    out _confirmButton,
                    out _cancelButton,
                    confirmText,
                    confirmVariant);

                ApplySmartSizingWithAutoHeight(xamlRoot);

                _confirmButton.Click += OnConfirmClicked;
                _cancelButton.Click += OnCancelClicked;
                _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            }

            public static Task<bool> ShowAsync(
                string title,
                string message,
                string confirmText,
                DaisyButtonVariant confirmVariant,
                XamlRoot xamlRoot)
            {
                if (xamlRoot == null)
                    return Task.FromResult(false);

                var dialog = new FlowKanbanConfirmDialog(title, message, confirmText, confirmVariant, xamlRoot);
                return dialog.ShowInternalAsync();
            }

            private Task<bool> ShowInternalAsync()
            {
                _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
                if (_hostPanel == null)
                {
                    Close(false);
                    return _tcs.Task;
                }

                _hostPanel.Children.Add(this);
                IsOpen = true;
                return _tcs.Task;
            }

            private void OnConfirmClicked(object sender, RoutedEventArgs e)
            {
                Close(true);
            }

            private void OnCancelClicked(object sender, RoutedEventArgs e)
            {
                Close(false);
            }

            private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
            {
                if (!IsOpen && !_isClosing)
                {
                    Close(false);
                }
            }

            private void Close(bool confirmed)
            {
                if (_isClosing)
                    return;

                _isClosing = true;
                if (IsOpen)
                    IsOpen = false;

                if (_isOpenCallbackToken != 0)
                {
                    UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                    _isOpenCallbackToken = 0;
                }

                if (_hostPanel != null)
                {
                    _hostPanel.Children.Remove(this);
                    _hostPanel = null;
                }

                _tcs.TrySetResult(confirmed);
            }
        }

        private sealed partial class FlowKanbanKeyboardHelpDialog : FloweryDialogBase
        {
            private readonly TaskCompletionSource<bool> _tcs = new();
            private readonly XamlRoot _xamlRoot;
            private Panel? _hostPanel;
            private bool _isClosing;
            private long _isOpenCallbackToken;

            private readonly DaisyButton _closeButton;

            private FlowKanbanKeyboardHelpDialog(XamlRoot xamlRoot, bool showAdminShortcuts)
            {
                _xamlRoot = xamlRoot;

                Content = CreateDialogLayout(xamlRoot, showAdminShortcuts, out _closeButton);
                IsDraggable = true;
                ApplySmartSizingWithAutoHeight(xamlRoot);

                _closeButton.Click += OnCloseClicked;
                _isOpenCallbackToken = RegisterPropertyChangedCallback(IsOpenProperty, OnIsOpenChanged);
            }

            public static Task ShowAsync(XamlRoot xamlRoot, bool showAdminShortcuts)
            {
                if (xamlRoot == null)
                    return Task.CompletedTask;

                var dialog = new FlowKanbanKeyboardHelpDialog(xamlRoot, showAdminShortcuts);
                return dialog.ShowInternalAsync();
            }

            private Task ShowInternalAsync()
            {
                _hostPanel = FloweryDialogHost.EnsureHost(_xamlRoot);
                if (_hostPanel == null)
                    return Task.CompletedTask;

                _hostPanel.Children.Add(this);
                IsOpen = true;
                DispatcherQueue?.TryEnqueue(() => _closeButton.Focus(FocusState.Programmatic));
                return _tcs.Task;
            }

            private void OnCloseClicked(object sender, RoutedEventArgs e)
            {
                Close();
            }

            private void OnIsOpenChanged(DependencyObject sender, DependencyProperty dp)
            {
                if (!IsOpen && !_isClosing)
                {
                    Close();
                }
            }

            private void Close()
            {
                if (_isClosing)
                    return;

                _isClosing = true;
                if (IsOpen)
                    IsOpen = false;

                if (_isOpenCallbackToken != 0)
                {
                    UnregisterPropertyChangedCallback(IsOpenProperty, _isOpenCallbackToken);
                    _isOpenCallbackToken = 0;
                }

                if (_hostPanel != null)
                {
                    _hostPanel.Children.Remove(this);
                    _hostPanel = null;
                }

                _tcs.TrySetResult(true);
            }

            private static FrameworkElement CreateDialogLayout(XamlRoot xamlRoot, bool showAdminShortcuts, out DaisyButton closeButton)
            {
                var header = new AutoLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    Padding = new Thickness(16, 16, 16, 0)
                };

                header.Children.Add(CreateTextBlock(
                    FloweryLocalization.GetStringInternal("Kanban_KeyboardHelp_Title"),
                    "TitleTextBlockStyle"));

                var subtitle = CreateTextBlock(
                    FloweryLocalization.GetStringInternal("Kanban_KeyboardHelp_Subtitle"),
                    "BodyTextBlockStyle");
                subtitle.Opacity = 0.7;
                header.Children.Add(subtitle);

                var shortcuts = new AutoLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 12,
                    Padding = new Thickness(16, 0, 16, 0)
                };

                shortcuts.Children.Add(CreateShortcutRow("Tab / Shift+Tab", "Kanban_KeyboardHelp_Tab"));
                shortcuts.Children.Add(CreateShortcutRow("Arrow keys", "Kanban_KeyboardHelp_Arrows"));
                if (showAdminShortcuts)
                {
                    shortcuts.Children.Add(CreateShortcutRow("Ctrl + B", "Kanban_KeyboardHelp_AddSection"));
                    shortcuts.Children.Add(CreateShortcutRow("Ctrl + D", "Kanban_KeyboardHelp_DeleteSection"));
                    shortcuts.Children.Add(CreateShortcutRow("Ctrl + T", "Kanban_KeyboardHelp_RenameSection"));
                }
                shortcuts.Children.Add(CreateShortcutRow("Ctrl + N", "Kanban_KeyboardHelp_AddCard"));
                shortcuts.Children.Add(CreateShortcutRow("Ctrl + F", "Kanban_KeyboardHelp_Search"));
                shortcuts.Children.Add(CreateShortcutRow("Ctrl + Shift + Arrow", "Kanban_KeyboardHelp_MoveCard"));
                shortcuts.Children.Add(CreateShortcutRow("Ctrl + Alt + Left/Right", "Kanban_KeyboardHelp_SwitchSection"));
                shortcuts.Children.Add(CreateShortcutRow("Ctrl + + / -", "Kanban_KeyboardHelp_Zoom"));

                var footer = new AutoLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 12,
                    Padding = new Thickness(16, 0, 24, 16)
                };

                closeButton = new DaisyButton
                {
                    Content = FloweryLocalization.GetStringInternal("Common_Close"),
                    Variant = DaisyButtonVariant.Primary,
                    MinWidth = 80
                };

                footer.Children.Add(closeButton);

                var container = FloweryDialogBase.CreateDialogContent(xamlRoot, header, shortcuts, footer);
                var chromePadding = FloweryDialogBase.ContentDialogChrome;
                var minWidth = FloweryDialogBase.AbsoluteMinWidth - chromePadding;
                container.Width = Math.Max(minWidth, container.Width - chromePadding);
                container.Height = double.NaN;
                if (container.RowDefinitions.Count > 1)
                {
                    container.RowDefinitions[1].Height = GridLength.Auto;
                }
                return container;
            }

            private static Grid CreateShortcutRow(string keys, string descriptionKey)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                    ColumnSpacing = 12
                };

                var keyDisplay = new DaisyKbd
                {
                    Text = keys,
                    Size = Enums.DaisySize.Small,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var description = CreateTextBlock(
                    FloweryLocalization.GetStringInternal(descriptionKey),
                    "BodyTextBlockStyle");
                description.TextWrapping = TextWrapping.Wrap;
                description.VerticalAlignment = VerticalAlignment.Center;

                Grid.SetColumn(keyDisplay, 0);
                Grid.SetColumn(description, 1);

                row.Children.Add(keyDisplay);
                row.Children.Add(description);

                return row;
            }

            private static TextBlock CreateTextBlock(string text, string styleKey)
            {
                var block = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap
                };

                if (DaisyResourceLookup.TryGetResource(Application.Current?.Resources, styleKey, out var style)
                    && style is Style textStyle)
                {
                    block.Style = textStyle;
                }

                return block;
            }
        }

        #endregion
    }
}
