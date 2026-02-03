using System;
using System.Collections.Generic;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using Flowery.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Flowery.Helpers
{
    internal static class DaisyNeumorphicRefreshHelper
    {
        private static readonly HashSet<FrameworkElement> PendingRefresh = new();
        private static DispatcherQueueTimer? _refreshTimer;
        private static bool _refreshScheduled;
        private static bool _refreshTimerInitialized;
        private static readonly DependencyProperty NeumorphicRefreshPendingProperty =
            DependencyProperty.RegisterAttached(
                "NeumorphicRefreshPending",
                typeof(bool),
                typeof(DaisyNeumorphicRefreshHelper),
                new PropertyMetadata(false));

        private static bool GetNeumorphicRefreshPending(DependencyObject element)
            => (bool)element.GetValue(NeumorphicRefreshPendingProperty);

        private static void SetNeumorphicRefreshPending(DependencyObject element, bool value)
            => element.SetValue(NeumorphicRefreshPendingProperty, value);

        internal static void RefreshNeumorphicInTree(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is DaisyBaseContentControl baseControl)
                {
                    if (!baseControl.IsLoaded)
                    {
                        EnsureRefreshOnLoaded(baseControl);
                    }
                    else
                    {
                        QueueRefresh(baseControl);
                    }
                }
                else if (current is DaisyButton button)
                {
                    if (!button.IsLoaded)
                    {
                        EnsureRefreshOnLoaded(button);
                    }
                    else
                    {
                        QueueRefresh(button);
                    }
                }
                else if (current is DaisyComboBoxBase comboBox)
                {
                    if (!comboBox.IsLoaded)
                    {
                        EnsureRefreshOnLoaded(comboBox);
                    }
                    else
                    {
                        QueueRefresh(comboBox);
                    }
                }

                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child != null)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        private static void EnsureRefreshOnLoaded(FrameworkElement element)
        {
            if (GetNeumorphicRefreshPending(element))
                return;

            SetNeumorphicRefreshPending(element, true);
            RoutedEventHandler? handler = null;
            handler = (s, e) =>
            {
                element.Loaded -= handler;
                SetNeumorphicRefreshPending(element, false);
                QueueRefresh(element);
            };
            element.Loaded += handler;
        }

        internal static void QueueRefresh(FrameworkElement element)
        {
            if (DaisyBaseContentControl.DisableNeumorphicAutoRefresh)
                return;

            if (!element.IsLoaded)
            {
                EnsureRefreshOnLoaded(element);
                return;
            }

            if (!PendingRefresh.Add(element))
                return;

            var dispatcher = element.DispatcherQueue;
            if (dispatcher == null)
            {
                ProcessPending();
                return;
            }

            _refreshTimer ??= dispatcher.CreateTimer();
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(50);
            if (!_refreshTimerInitialized)
            {
                _refreshTimer.Tick += OnRefreshTimerTick;
                _refreshTimerInitialized = true;
            }
            _refreshScheduled = true;
            _refreshTimer.Start();
        }

        private static void OnRefreshTimerTick(DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            _refreshScheduled = false;
            ProcessPending();
        }

        private static void ProcessPending()
        {
            if (DaisyBaseContentControl.DisableNeumorphicAutoRefresh)
            {
                PendingRefresh.Clear();
                return;
            }

            if (PendingRefresh.Count == 0)
                return;

            var batch = new List<FrameworkElement>(PendingRefresh);
            PendingRefresh.Clear();

            foreach (var element in batch)
            {
                if (!element.IsLoaded)
                    continue;

                if (element is DaisyBaseContentControl baseControl)
                {
                    baseControl.RefreshNeumorphicEffect();
                }
                else if (element is DaisyButton button)
                {
                    button.RefreshNeumorphicEffect();
                }
                else if (element is DaisyComboBoxBase comboBox)
                {
                    comboBox.RefreshNeumorphicEffect();
                }
            }

            if (PendingRefresh.Count > 0 && !_refreshScheduled)
            {
                var dispatcher = batch[0].DispatcherQueue;
                if (dispatcher != null)
                {
                    _refreshTimer ??= dispatcher.CreateTimer();
                    _refreshTimer.Interval = TimeSpan.FromMilliseconds(50);
                    if (!_refreshTimerInitialized)
                    {
                        _refreshTimer.Tick += OnRefreshTimerTick;
                        _refreshTimerInitialized = true;
                    }
                    _refreshScheduled = true;
                    _refreshTimer.Start();
                }
            }
        }
    }
}
