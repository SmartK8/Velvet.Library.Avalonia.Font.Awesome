using System;
using System.IO;
using System.Linq;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO.Compression;
using System.Text;

using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Svg.Skia;
using Avalonia.Visuals.Media.Imaging;

using JetBrains.Annotations;

using Library.Font.Awesome.Properties;

using Svg;
using Svg.Skia;

namespace Library.Font.Awesome
{
    public class Awesome : MarkupExtension, IImage, IAffectsRender
    {
        #region | Constants |

        private static readonly Size DefaultBounds = new Size(512, 512);

        #endregion

        #region | Static fields |

        private static readonly ZipArchive Archive;

        private static readonly ConcurrentDictionary<(FontVersion, String, String), String> SvgCache;

        #endregion

        #region | Properties |

        public FontVersion? Version   { get; set; }
        public Place?       Placement { get; set; }

        public String Major { get; set; }
        public String Minor { get; set; }
        public String Style { get; set; }

        public Double? Width { get; set; }

        public IBrush Primary   { get; set; }
        public IBrush Secondary { get; set; }
        public IBrush Outline   { get; set; }

        private SvgSource PrimarySource   { get; set; }
        private SvgSource SecondarySource { get; set; }

        #endregion

        #region | Calculated properties |

        public Size Size => DefaultBounds;

        public FontVersion ActualVersion   => Version ?? DefaultVersion;
        public Place       ActualPlacement => Placement ?? DefaultPlacement;

        public String ActualMajor => Major ?? DefaultMajor;
        public String ActualMinor => Minor ?? DefaultMinor;
        public String ActualStyle => Style ?? DefaultStyle;

        public Double ActualWidth => Width ?? DefaultWidth;

        public IBrush ActualPrimary   => Primary ?? DefaultPrimary;
        public IBrush ActualSecondary => Secondary ?? DefaultSecondary;
        public IBrush ActualOutline   => Outline ?? DefaultOutline;

        #endregion

        #region | Static properties |

        public static FontVersion DefaultVersion   { get; set; } = FontVersion.Solid;
        public static Place       DefaultPlacement { get; set; } = Place.X2 | Place.Half;

        public static IBrush DefaultPrimary   { get; set; } = Brushes.Black;
        public static IBrush DefaultSecondary { get; set; } = Brushes.Black;
        public static IBrush DefaultOutline   { get; set; } = Brushes.Transparent;

        public static Double DefaultWidth { get; set; } = 0d;

        public static String DefaultMajor { get; set; } = default;
        public static String DefaultMinor { get; set; } = default;
        public static String DefaultStyle { get; set; } = default;

        #endregion

        #region | Events |

        [EditorBrowsable(EditorBrowsableState.Never)] public event EventHandler Invalidated;

        #endregion

        #region | Constructors |

        static Awesome()
        {
            MemoryStream stream = new MemoryStream(Resources.Assets);
            Archive = new ZipArchive(stream, ZipArchiveMode.Read);
            SvgCache = new ConcurrentDictionary<(FontVersion, String, String), String>();
        }

        [UsedImplicitly] public Awesome() {}

        public Awesome(FontVersion? version, Place? placement, String major, String minor, String style, Double? width, IBrush primary, IBrush secondary, IBrush outline)
        {
            Version = version;
            Placement = placement;

            Major = major;
            Minor = minor;
            Style = style;

            Width = width;

            Primary = primary;
            Secondary = secondary;
            Outline = outline;
        }

        #endregion

        #region | Helper methods |

        private static String BrushToSvg(ISolidColorBrush brush) => $"{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";

        private static String BuildStyle(IBrush brush, IBrush outline, Double width, Boolean isEnabled = false)
        {
            String fill = brush is ISolidColorBrush solidBrush ? BrushToSvg(solidBrush) : "000";
            String stroke = outline is ISolidColorBrush solidOutline ? BrushToSvg(solidOutline) : "000";

            Double opacity = isEnabled ? 1d : 0.25d;

            return $"fill:#{fill};fill-opacity:{opacity:F2};stroke:#{stroke};stroke-width:{width};stroke-opacity:{opacity:F2}";
        }

        #endregion

        #region | Main methods |

        private static String GetOrExtract(FontVersion version, String name, IBrush brush, IBrush outline, Double? width, Boolean isEnabled, String style)
        {
            name = name.ToLower();

            style = style ?? BuildStyle(brush, outline, width ?? 0d, isEnabled);

            if (!SvgCache.TryGetValue((version, name, style), out String result))
            {
                String match = version.ToString().ToLower();

                ZipArchiveEntry entry = Archive.Entries.FirstOrDefault(item => match.Equals(Path.GetDirectoryName(item.FullName)) && name.Equals(Path.GetFileNameWithoutExtension(item.FullName)));

                if (entry != null)
                {
                    using (Stream entryStream = entry.Open())
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        entryStream.CopyTo(memoryStream);

                        Byte[] data = memoryStream.ToArray();
                        String text = Encoding.ASCII.GetString(data);
                        String[] parts = text.Split('|');

                        StringBuilder builder = new StringBuilder();
                        builder.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {parts[0]} {parts[1]}\">");
                        if (version == FontVersion.Duotone) builder.Append("<defs><style>.fa-secondary{opacity:.4}</style></defs>");
                        builder.Append($"<path style=\"{style}\" d=\"");
                        if (version == FontVersion.Duotone) parts[2] = parts[2].Replace("class=\"fa-secondary\"/>", $"class=\"fa-secondary\"/><path style=\"{style}\" d=\"");
                        builder.Append(parts[2]);
                        builder.Append("\"/></svg>");
                        result = builder.ToString();

                        SvgCache.AddOrUpdate((version, name, style), _ => result, (_, value) => value);
                    }
                }
            }

            return result;
        }

        private Awesome CreateSvg(Boolean isEnabled)
        {
            String primarySvg = GetOrExtract(ActualVersion, ActualMajor, ActualPrimary, ActualOutline, ActualWidth, isEnabled, ActualStyle);

            if (primarySvg != null)
            {
                SvgSource primarySource = new SvgSource();
                SvgDocument primaryDocument = SvgDocument.FromSvg<SvgDocument>(primarySvg);
                primarySource.FromSvgDocument(primaryDocument);
                PrimarySource = primarySource;

                if (ActualMinor != null)
                {
                    String secondarySvg = GetOrExtract(ActualVersion, ActualMinor, ActualSecondary, ActualOutline, ActualWidth, isEnabled, ActualStyle);

                    if (secondarySvg != null)
                    {
                        SvgSource secondarySource = new SvgSource();
                        SvgDocument secondaryDocument = SvgDocument.FromSvg<SvgDocument>(secondarySvg);
                        secondarySource.FromSvgDocument(secondaryDocument);
                        SecondarySource = secondarySource;
                    }
                }
            }

            return this;
        }

        #endregion

        #region << Base >>

        public override Object ProvideValue(IServiceProvider provider)
        {
            Boolean isEnabled = true;

            if (provider is IAvaloniaXamlIlParentStackProvider stack && provider is IProvideValueTarget target)
            {
                AvaloniaProperty property = (AvaloniaProperty) target.TargetProperty;

                InputElement origin = default;

                foreach (Object parent in stack.Parents)
                {
                    if (parent is InputElement element)
                    {
                        origin = origin ?? element;

                        InputElement capture = origin;

                        element.PropertyChanged += (_, args) =>
                        {
                            if (args.Property.Name == nameof(capture.IsEnabled) && args.NewValue != null)
                            {
                                Awesome awesome = new Awesome(Version, Placement, Major, Minor, Style, Width, Primary, Secondary, Outline);
                                awesome.CreateSvg((Boolean) args.NewValue);
                                capture[property] = awesome;
                            }
                        };

                        if (isEnabled && !element.IsEnabled)
                        {
                            isEnabled = false;
                        }
                    }
                }
            }

            return CreateSvg(isEnabled);
        }

        #endregion

        #region << IImage >>

        private static Size Fit(Rect source, Rect target)
        {
            Double ratioX = target.Width/source.Width;
            Double ratioY = target.Height/source.Height;

            Double ratio = Math.Min(ratioX, ratioY);

            return new Size(source.Width*ratio, source.Height*ratio);
        }

        void IImage.Draw(DrawingContext context, Rect source, Rect target, BitmapInterpolationMode mode)
        {
            if (PrimarySource?.Drawable == null || (SecondarySource != null && SecondarySource.Drawable == null)) return;
            if (PrimarySource.Drawable.Bounds.Width <= 0 || PrimarySource.Drawable.Bounds.Height <= 0) return;

            if (SecondarySource?.Drawable != null)
            {
                // flags

                Int32 value = (Int32) ActualPlacement;
                Place flags = (Place) (value & 0xF0);
                Place place = (Place) (value & 0xF);
                Place shrink = (Place) (value & 0xF00);

                if (!shrink.HasFlag(Place.X1))
                {
                    if (place == 0) place = Place.Bottom | Place.Right;
                    if (place is Place.Left || place is Place.Right) place |= Place.Bottom;
                    if (place is Place.Top || place is Place.Bottom) place |= Place.Right;
                }

                Double factor = 0d;

                if (flags is 0 || flags is Place.Under) factor = 1.25d;
                if (flags.HasFlag(Place.Quarter)) factor += 2d;
                if (flags.HasFlag(Place.Half)) factor += 1.25d;
                if (flags.HasFlag(Place.Full)) factor += 1d;

                Double zoom = 0;

                if (shrink == 0) zoom = 2d;

                if (shrink.HasFlag(Place.X1))
                {
                    if (flags.HasFlag(Place.Quarter)) { zoom += 4d; factor = 1d; }
                    if (flags.HasFlag(Place.Half)) { zoom += 2d; factor = 1d; }
                    if (flags.HasFlag(Place.Full)) { zoom += 1d; factor = 1d; }
                }

                if (shrink.HasFlag(Place.X2)) zoom += 2d;
                if (shrink.HasFlag(Place.X3)) zoom += 3d;
                if (shrink.HasFlag(Place.X4)) zoom += 4d;

                Rect first = PrimarySource.Drawable.Bounds.ToSKRect().ToAvaloniaRect();
                Rect second = SecondarySource.Drawable.Bounds.ToSKRect().ToAvaloniaRect();

                first = new Rect(first.X, first.Y, first.Width + ActualWidth*2, first.Height + ActualWidth*2);
                second = new Rect(second.X, second.Y, second.Width + ActualWidth*2, second.Height + ActualWidth*2);

                // secondary image

                SvgSource secondary = SecondarySource;

                Size secondSize = Fit(second, target);

                Double secondLeft = 0d, secondTop = 0d;
                Double secondFactor = secondSize.Height/second.Height;
                Double secondShift = (second.Height - second.Width)/2d;

                if (shrink.HasFlag(Place.X1) && (flags.HasFlag(Place.Quarter) || flags.HasFlag(Place.Half) || flags.HasFlag(Place.Full)))
                {
                    secondLeft = (second.Width*zoom - second.Width)/2d;
                    secondTop = (second.Height*zoom - second.Height)/2d;
                }

                if (place.HasFlag(Place.Top)) secondTop = 0;
                if (place.HasFlag(Place.Left)) secondLeft= 0;
                if (place.HasFlag(Place.Right)) secondLeft = second.Width*zoom - second.Width;
                if (place.HasFlag(Place.Bottom)) secondTop = second.Height*zoom - second.Height;

                Matrix secondScale = Matrix.CreateScale(secondFactor/zoom, secondFactor/zoom);
                Matrix secondTranslate = Matrix.CreateTranslation(secondLeft + secondShift + second.Left, secondTop + second.Top);

                // primary image

                SvgSource primary = PrimarySource;

                Size firstSize = Fit(first, target);

                Double firstLeft = 0d, firstTop = 0d;
                Double firstFactor = firstSize.Height/first.Height;
                Double firstShiftX = Math.Max(0, (first.Height - first.Width)/2d);
                Double firstShiftY = Math.Max(0, (first.Width - first.Height)/2d);

                if (shrink.HasFlag(Place.X1) && (flags.HasFlag(Place.Quarter) || flags.HasFlag(Place.Half) || flags.HasFlag(Place.Full)))
                {
                    firstLeft = (first.Width*factor - first.Width)/2d;
                    firstTop = (first.Height*factor - first.Height)/2d;
                    firstShiftX = firstShiftY = 0d;
                }

                if (place.HasFlag(Place.Left)) { firstLeft = first.Width*factor - (first.Width - firstShiftX); }
                if (place.HasFlag(Place.Top)) { firstTop = first.Height*factor - (first.Height - firstShiftY); }

                Matrix firstScale = Matrix.CreateScale(firstFactor/factor, firstFactor/factor);
                Matrix firstTranslate = Matrix.CreateTranslation(firstLeft + firstShiftX + first.Left + ActualWidth, firstTop + firstShiftY + first.Top + ActualWidth);

                if (flags.HasFlag(Place.Under)) (primary, firstScale, firstTranslate, secondary, secondScale, secondTranslate) = (secondary, secondScale, secondTranslate, primary, firstScale, firstTranslate);

                // rendering

                using (context.PushClip(target))
                {
                    using (context.PushPreTransform(firstTranslate*firstScale))
                    {
                        context.Custom(new SvgCustomDrawOperation(source, primary));
                    }

                    using (context.PushPreTransform(secondTranslate*secondScale))
                    {
                        context.Custom(new SvgCustomDrawOperation(source, secondary));
                    }
                }
            }
            else
            {
                Rect main = PrimarySource.Drawable.Bounds.ToSKRect().ToAvaloniaRect();
                Size size = Fit(main, target);

                Double factor = size.Height/main.Height;
                Double left = (target.Width - size.Width)/factor/2d;
                Double top = (target.Height - size.Height)/factor/2d;

                Matrix scale = Matrix.CreateScale(factor, factor);
                Matrix translate = Matrix.CreateTranslation(left, top);

                using (context.PushClip(target))
                using (context.PushPreTransform(translate*scale))
                {
                    context.Custom(new SvgCustomDrawOperation(main, PrimarySource));
                }
            }
        }

        #endregion
    }
}