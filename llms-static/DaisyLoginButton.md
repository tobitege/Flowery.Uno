<!-- Supplementary documentation for DaisyLoginButton -->
<!-- This content is merged into auto-generated docs by generate_docs.py -->

# Overview

DaisyLoginButton is a **batteries-included** login button derived from DaisyButton. It bundles a brand icon, localized label, and optional brand styling in a single control. Use it to avoid verbose XAML when rendering OAuth-style login buttons.

## Brand Presets

| Brand | Notes |
| --- | --- |
| Email | Outline envelope icon |
| GitHub | Solid icon |
| Google | Multi-color icon |
| Facebook | Solid icon |
| X | Solid icon |
| Kakao | Solid icon, Korean label |
| Apple | Solid icon |
| Amazon | Solid icon |
| Microsoft | Multi-color icon |
| Line | Solid icon, Japanese label |
| Slack | Multi-color icon |
| LinkedIn | Solid icon |
| VK | Solid icon |
| WeChat | Solid icon |
| MetaMask | Multi-color icon |

## Key Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Brand` | `DaisyLoginBrand` | `Email` | Selects the icon and default label |
| `UseBrandColors` | `bool` | `true` | When true, applies brand background/foreground/border colors |
| `LoginText` | `string?` | `null` | Overrides the default label (no localization lookup) |
| `IconSize` | `double` | `NaN` | Overrides icon size (falls back to size token) |
| `IconSpacing` | `double` | `NaN` | Overrides icon/text spacing (falls back to size token) |

## Localization

Default labels are resolved via `FloweryLocalization.GetStringInternal` using the keys below. If `LoginText` is set, localization is skipped.

```txt
LoginButton_Email
LoginButton_GitHub
LoginButton_Google
LoginButton_Facebook
LoginButton_X
LoginButton_Kakao
LoginButton_Apple
LoginButton_Amazon
LoginButton_Microsoft
LoginButton_Line
LoginButton_Slack
LoginButton_LinkedIn
LoginButton_VK
LoginButton_WeChat
LoginButton_MetaMask
```

Runtime language switching is supported; the control refreshes its label when `FloweryLocalization.CultureChanged` fires.

## Theme Behavior

- When `UseBrandColors="true"` (default), the button overrides its own background, foreground, and border to match brand colors.
- When `UseBrandColors="false"`, the control uses the current theme's foreground colors and does **not** override button brushes. This is useful when you want brand icons with theme-consistent text.

## Examples

```xml
<!-- Default brand styling -->
<controls:DaisyLoginButton Brand="GitHub" />

<!-- Use theme colors instead of brand colors -->
<controls:DaisyLoginButton Brand="GitHub" UseBrandColors="False" />

<!-- Override label and size -->
<controls:DaisyLoginButton Brand="Google"
                           LoginText="Continue with Google"
                           IconSize="14"
                           IconSpacing="6" />

<!-- Compact layout -->
<controls:DaisyLoginButton Brand="Apple" Size="Small" />
```

## Tips

- If you want a uniform look across all themes, set `UseBrandColors="false"` and rely on theme resources.
- To fully customize the label, set `LoginText` and keep localization keys untouched.
