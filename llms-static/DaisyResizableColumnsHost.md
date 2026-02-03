# Overview

`DaisyResizableColumnsHost` is a reusable horizontal multi-column host that provides a shared resize gripper between columns. It is designed to decouple column widths from content sizing, allowing all columns in a collection to be resized simultaneously.

## Key Features

- **Shared Resizing**: Dragging the gripper between any two columns updates the `ColumnWidth` for all columns.
- **Auto-Discovery**: The resize grip appears automatically when hovering near the gaps between columns.
- **Clamped Width**: Support for `MinColumnWidth` and `MaxColumnWidth` constraints.
- **Orientation Support**: Default is Horizontal, with Vertical support for future expansions.

## Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| **ColumnWidth** | `double` | 250.0 | The shared width of all columns. |
| **MinColumnWidth** | `double` | 100.0 | The minimum allowed width for columns. |
| **MaxColumnWidth** | `double` | 1000.0 | The maximum allowed width for columns. |
| **ColumnSpacing** | `double` | 0.0 | The spacing between columns. |
| **GripVisibility** | `DaisyGripVisibility` | Auto | Visibility mode for the resize grip. |
| **Orientation** | `Orientation` | Horizontal | The layout orientation of the columns. |

## Quick Examples

```xml
<!-- Basic usage with ItemsSource -->
<daisy:DaisyResizableColumnsHost ItemsSource="{Binding Columns}" 
                                 ColumnWidth="300" 
                                 MinColumnWidth="150" />

<!-- Customized spacing and template -->
<daisy:DaisyResizableColumnsHost ItemsSource="{Binding Columns}" 
                                 ColumnSpacing="8" 
                                 ColumnWidth="{Binding SharedWidth, Mode=TwoWay}">
    <daisy:DaisyResizableColumnsHost.ItemTemplate>
        <DataTemplate>
            <Border Background="{DynamicResource DaisyBase200Brush}" 
                    Width="{Binding ColumnWidth, RelativeSource={RelativeSource AncestorType=daisy:DaisyResizableColumnsHost}}">
                <!-- Column Content -->
            </Border>
        </DataTemplate>
    </daisy:DaisyResizableColumnsHost.ItemTemplate>
</daisy:DaisyResizableColumnsHost>
```

## Tips & Best Practices

- **Binding Item Width**: For the columns to resize correctly, the root element of your `ItemTemplate` should bind its `Width` to the `ColumnWidth` of the host using `RelativeSource`.
- **ItemsPanel**: The host uses a `StackPanel` by default. If you need virtualization, you can override the `ItemsPanel` with a `VirtualizingStackPanel`.
