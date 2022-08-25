# Velvet.Library.Avalonia.Font.Awesome
A markup extension for Avalonia, that allows you to use Font Awesome. It already contains version 6 (alpha 2). You can combine upto two glyphs. Including style, colors, outline, placement or custom SVG styling.

**Features?**

- already prepackaged version 6 (alpha 2)
- all supported versions (brands, duotone, light, regular, solid, thin)
- up to 2 glyphs :D
- you can choose brushes
- outline color/width
- placement and relative sizing
- automatically detects if element you use it on is enabled/disabled
- alternatively you can just write custom SVG style snippet, that will be inserted into `<path style="{your_style}" />`

**Usage?**

1. Add declaration in your XAML:

`xmlns:a="clr-namespace:Library.Font.Awesome;assembly=Library.Font.Awesome"`

2. Use markup extension and set properties:

`<Image Source="{a:Awesome Major=person, Minor=plus, Secondary=DarkGreen}" />`

3. Additionally you can use global defaults (set in your code - App.axaml.cs),<br/>
   so all the images will have these settings:

```
Awesome.DefaultVersion = FontVersion.Solid;
Awesome.DefaultPrimary = Brushes.White;
```

**Results?**

![image](https://user-images.githubusercontent.com/2457949/186626711-bf3a5102-bc4b-44c6-a5f3-084785dcc802.png)

**Placement explained:**

`Left`    - aligns glyph #2 to the left<br/>
`Right`   - aligns glyph #2 to the right<br/>
`Top`     - aligns glyph #2 to the top<br/>
`Bottom`  - aligns glyph #2 to the bottom<br/>

`Under`   - makes glyph #2 appear behind glyph #1<br/>

`Quarter` - when `X1` is specified it means glyph #2 is 1/4 size, when not glyph #1 covers to 3/4 of glyph #2<br/>
`Half`    - when `X1` is specified it means glyph #2 is 1/2 size, when not glyph #1 covers to 1/2 of glyph #2<br/>
`Full`    - when `X1` is specified it means glyph #2 is full size, when not glyph #1 doesn't cover glyph #2<br/>

`X1`      - scales glyph #2 to full size (see `Full/Half/Quarter`)<br/>
`X2`      - scales glyph #2 to 1/2<br/>
`X3`      - scales glyph #2 to 1/3<br/>
`X4`      - scales glyph #2 to 1/4<br/>

You can combine `X2/X3/X4`. Example: `X2,X3` = 1/2 + 1/3 = 5/6<br/>

By default the placement is `X2,Half,Right,Bottom` (you can see it in the picture)<br/>
