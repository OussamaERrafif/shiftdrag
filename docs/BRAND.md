# ShiftDrag identity

![ShiftDrag mark](../assets/brand/icon-256.png)

The open orange ring references the radial wheel. A solid white arrow moves through the opening, connecting the drag gesture to taking a file to its next format. The violet tile matches the app's selection color and gives the orange and white shapes contrast in both light and dark Windows themes.

The mark uses broad filled shapes, transparent outer corners, and restrained gradients. There are no tiny details or letters inside the icon. The source is an editable vector; exports cover tray, title bar, Explorer, and documentation use.

## Assets

| Asset | Purpose |
|---|---|
| [mark.svg](assets/mark.svg) | Canonical editable vector, also displayed in the README |
| [icon-512.png](../assets/brand/icon-512.png) | Large transparent PNG |
| [icon-256.png](../assets/brand/icon-256.png) | Embedded app/sidebar image |
| [ShiftDrag.ico](../assets/brand/ShiftDrag.ico) | Native multi-resolution Windows icon: 16, 20, 24, 32, 40, 48, 64, 128, 256 px |
| `assets/brand/icon-*.png` | Individual transparent exports, 16–512 px |

## Palette

| Element | Colors |
|---|---|
| Violet tile | `#885BFA` → `#4B2AB8` |
| Orange ring | `#FFB45E` → `#FF754B` |
| Drag arrow | `#FFFFFF` |

Keep the ring and arrow together. Preserve the transparent outer margin and avoid adding a second container, outline, or shadow at tray sizes. Screenshots in the research archive can show earlier versions of the identity.

## Regenerate exports

After editing the vector, run on Windows:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File scripts/export-brand.ps1
.\build.ps1
```

The export script renders the vector's filled path geometry with WPF, then writes PNGs and a PNG-backed ICO. Its supported SVG subset is filled paths and diagonal linear gradients. Exported binaries are checked in so a normal source build needs no graphics tool or Python. The build embeds the PNG/ICO and assigns the ICO to the executable, so the app has no extra loose branding-file dependency.

Original ShiftDrag artwork is covered by the repository's [MIT license](../LICENSE). The mark identifies this independent project and does not imply affiliation with the reference Mac app.
