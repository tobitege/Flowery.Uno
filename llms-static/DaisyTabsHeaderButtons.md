<!-- Supplementary documentation for DaisyTabsHeaderButtons -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

## Overview

DaisyTabsHeaderButtons provides optional previous and next buttons and a menu of named views.
It works in a DaisyTabs header or as a separate toolbar control.
The buttons use the Default variant and Square shape. The application handles navigation, view selection, and view management through events or commands.
Ported from Flowery.NET 3.4.0.

## Options

| Property | Default | Behavior |
| -------- | ------- | -------- |
| ShowNavigationButtons | false | Shows previous and next buttons |
| ShowViewMenuButton | false | Shows the view menu button |
| Views | null | Supplies an `IList<DaisyTabView>` for the menu |
| ActiveViewId | null | Identifies the checked view through object equality |
| ShowManageViewsEntry | true | Adds the localized management entry after the views |
| Size | Auto | Sets the size of the three buttons and icons; Auto follows the global size, with Small as the opt-out fallback |

Each `DaisyTabView` has a required `object Id` and a required `string Name`.
Use unique identifiers. The control reads the list and active identifier each time the menu opens.

The view menu is a WinUI `MenuFlyout`: views are `ToggleMenuFlyoutItem` entries, and the active view shows a check mark.
The management entry has a separator when view entries precede it.
With no views, the menu contains only the management entry.
If the view list and management entry are absent, the menu button is disabled unless `ViewMenuOpening` has a handler.

## Events and Commands

| Event | Command | Command parameter |
| ----- | ------- | ----------------- |
| PreviousRequested | PreviousCommand | null |
| NextRequested | NextCommand | null |
| ViewSelected | ViewSelectedCommand | Selected view Id |
| ManageViewsRequested | ManageViewsCommand | null |

`ViewSelected` supplies `DaisyTabViewEventArgs` with `Id` and `Name`.
The other events use `EventArgs`. If an event handler and command are both assigned, both receive the action.
Commands use the standard WinUI `CanExecute` behavior: a command that cannot execute disables its button or menu entry.

The control does not change `ActiveViewId` when a user selects a view.
The application sets this property after it accepts the request.
The control does not change tab visibility, persist views, or open a management dialog.

## Quick Examples

### Navigation in a tab header

```xml
<daisy:DaisyTabs>
    <daisy:DaisyTabs.HeaderTrailingContent>
        <daisy:DaisyTabsHeaderButtons ShowNavigationButtons="True"
                                      PreviousCommand="{x:Bind ViewModel.PreviousCommand}"
                                      NextCommand="{x:Bind ViewModel.NextCommand}" />
    </daisy:DaisyTabs.HeaderTrailingContent>
    <StackPanel>
        <TabViewItem Header="Records"><TextBlock Text="Record content" /></TabViewItem>
    </StackPanel>
</daisy:DaisyTabs>
```

### View menu with MVVM

```xml
<daisy:DaisyTabsHeaderButtons ShowViewMenuButton="True"
                              Views="{x:Bind ViewModel.Views}"
                              ActiveViewId="{x:Bind ViewModel.ActiveViewId, Mode=OneWay}"
                              ViewSelectedCommand="{x:Bind ViewModel.SelectViewCommand}"
                              ManageViewsCommand="{x:Bind ViewModel.ManageViewsCommand}"
                              ShowManageViewsEntry="{x:Bind ViewModel.CanManageViews, Mode=OneWay}" />
```

### Built-in group

```xml
<daisy:DaisyTabs x:Name="RecordTabs" ShowHeaderButtons="True">
    <StackPanel>
        <TabViewItem Header="Records"><TextBlock Text="Record content" /></TabViewItem>
    </StackPanel>
</daisy:DaisyTabs>
```

```csharp
var buttons = RecordTabs.HeaderButtons!;
buttons.ShowNavigationButtons = true;
buttons.ShowViewMenuButton = true;
buttons.Views = new ObservableCollection<DaisyTabView>
{
    new() { Id = 1, Name = "Overview" },
    new() { Id = 2, Name = "Details" },
    new() { Id = 3, Name = "Archive" }
};
buttons.ActiveViewId = 2;
buttons.ViewSelected += OnViewSelected;

void OnViewSelected(object? sender, DaisyTabViewEventArgs e)
{
    if (sender is DaisyTabsHeaderButtons source)
        source.ActiveViewId = e.Id;
}
```

### Separate toolbar control

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
    <TextBlock Text="Records" VerticalAlignment="Center" />
    <daisy:DaisyTabsHeaderButtons x:Name="RecordButtons"
                                  ShowNavigationButtons="True"
                                  ShowViewMenuButton="True" />
</StackPanel>
```

Subscribe to `PreviousRequested`, `NextRequested`, `ViewSelected` and `ManageViewsRequested` in code.

## Load Views Before Opening

`ViewMenuOpening` uses `EventHandler<CancelEventArgs>` and fires before the control builds the menu entries.
The handler can load `Views`, set `ActiveViewId`, and update `ShowManageViewsEntry` after it checks permissions.
Setting `Cancel` to true prevents both the menu rebuild and the menu display.

WinUI's `FlyoutBase.Opening` cannot be cancelled, so the view menu button raises `ViewMenuOpening` itself and only then opens the `MenuFlyout`.
A subscribed handler keeps the menu button available even when the view list is initially empty.
If the handler leaves no entries, the menu does not open.
The event is synchronous. Load the list in the handler, or prepare data asynchronously before the user opens the menu.

```csharp
buttons.ViewMenuOpening += OnViewMenuOpening;

void OnViewMenuOpening(object? sender, CancelEventArgs e)
{
    if (!CanReadViews())
    {
        e.Cancel = true;
        return;
    }

    if (sender is DaisyTabsHeaderButtons source)
    {
        source.Views = LoadViews();
        source.ShowManageViewsEntry = CanManageViews();
    }
}
```

The application supplies `CanReadViews`, `LoadViews`, and `CanManageViews` in this example.
`CancelEventArgs` is in `System.ComponentModel`.

## Collection Updates

The menu always reads the current `Views` list when it opens.
An `ObservableCollection<DaisyTabView>` also updates button availability immediately when its contents change.
A plain list updates availability on the next layout pass or when `Views` is replaced.
Collection changes and property assignments must occur on the UI thread.

## Size and Scaling

Read `ResolvedSize` for the effective tier; see [size precedence](FlowerySizeManager.md#size-ownership-and-precedence).

`Size` supports Auto (default), ExtraSmall, Small, Medium, Large, and ExtraLarge.
Auto follows the global size; the opt-out fallback is Small. Explicit sizes are preserved.
The icons use the Daisy icon size for the current `ResolvedSize` (`DaisyResourceLookup.GetDefaultIconSize`).
The group implements `IScalableControl`: `ApplyScaleFactor` scales the icon size. Button dimensions follow the normal button size resources.

## Localization and Accessibility

`Tabs_ManageViews` supplies the management entry text, and `Tabs_Views` the view button label.
The navigation buttons use `Accessibility_PreviousItem` and `Accessibility_NextItem`. All four keys exist in every supported language.
Each icon button has a localized tooltip and automation name.
Button labels follow culture changes, and menu labels use the culture at opening time.

---
