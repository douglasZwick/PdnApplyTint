using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using CheckboxControl = System.Boolean;
using ColorWheelControl = PaintDotNet.ColorBgra;
using DoubleSliderControl = System.Double;
using IntSliderControl = System.Int32;

[assembly: AssemblyTitle("ApplyTint plugin for Paint.NET")]
[assembly: AssemblyDescription("Multiplicatively tints the selection with the given color")]
[assembly: AssemblyConfiguration("apply tint")]
[assembly: AssemblyCompany("Doug Zwick")]
[assembly: AssemblyProduct("ApplyTint")]
[assembly: AssemblyCopyright("Copyright ©2020 by Doug Zwick")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]

namespace ApplyTintEffect
{
  public class PluginSupportInfo : IPluginSupportInfo
  {
    public string Author
    {
      get
      {
        return base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
      }
    }

    public string Copyright
    {
      get
      {
        return base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
      }
    }

    public string DisplayName
    {
      get
      {
        return base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
      }
    }

    public Version Version
    {
      get
      {
        return base.GetType().Assembly.GetName().Version;
      }
    }

    public Uri WebsiteUri
    {
      get
      {
        return new Uri("https://www.getpaint.net/redirect/plugins.html");
      }
    }
  }

  [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Apply Tint")]
  public class ApplyTintEffectPlugin : PropertyBasedEffect
  {
    public static string StaticName
    {
      get
      {
        return "Apply Tint";
      }
    }

    public static Image StaticIcon
    {
      get
      {
        return null;
      }
    }

    public static string SubmenuName
    {
      get
      {
        return "Color";
      }
    }

    public ApplyTintEffectPlugin()
        : base(StaticName, StaticIcon, SubmenuName, new EffectOptions() { Flags = EffectFlags.Configurable })
    {
    }

    public enum PropertyNames
    {
      M_TintStrength,
      M_UseRgbPicker,
      M_RgbColor,
      M_H,
      M_S,
      M_V
    }


    protected override PropertyCollection OnCreatePropertyCollection()
    {
      ColorBgra PrimaryColor = EnvironmentParameters.PrimaryColor.NewAlpha(byte.MaxValue);
      ColorBgra SecondaryColor = EnvironmentParameters.SecondaryColor.NewAlpha(byte.MaxValue);

      List<Property> props = new List<Property>();

      props.Add(new DoubleProperty(PropertyNames.M_TintStrength, 1, 0, 1));
      props.Add(new BooleanProperty(PropertyNames.M_UseRgbPicker, true));
      props.Add(new Int32Property(PropertyNames.M_RgbColor, ColorBgra.ToOpaqueInt32(PrimaryColor), 0, 0xffffff));
      props.Add(new Int32Property(PropertyNames.M_H, 0, 0, 360));
      props.Add(new Int32Property(PropertyNames.M_S, 100, 0, 100));
      props.Add(new Int32Property(PropertyNames.M_V, 100, 0, 100));

      List<PropertyCollectionRule> propRules = new List<PropertyCollectionRule>();

      propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.M_RgbColor, PropertyNames.M_UseRgbPicker, true));
      propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.M_H, PropertyNames.M_UseRgbPicker, false));
      propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.M_S, PropertyNames.M_UseRgbPicker, false));
      propRules.Add(new ReadOnlyBoundToBooleanRule(PropertyNames.M_V, PropertyNames.M_UseRgbPicker, false));

      return new PropertyCollection(props, propRules);
    }

    protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
    {
      ControlInfo configUI = CreateDefaultConfigUI(props);

      configUI.SetPropertyControlValue(PropertyNames.M_TintStrength, ControlInfoPropertyNames.DisplayName, "Tint Strength");
      configUI.SetPropertyControlValue(PropertyNames.M_TintStrength, ControlInfoPropertyNames.SliderLargeChange, 0.25);
      configUI.SetPropertyControlValue(PropertyNames.M_TintStrength, ControlInfoPropertyNames.SliderSmallChange, 0.05);
      configUI.SetPropertyControlValue(PropertyNames.M_TintStrength, ControlInfoPropertyNames.UpDownIncrement, 0.01);
      configUI.SetPropertyControlValue(PropertyNames.M_TintStrength, ControlInfoPropertyNames.DecimalPlaces, 3);
      configUI.SetPropertyControlValue(PropertyNames.M_UseRgbPicker, ControlInfoPropertyNames.DisplayName, string.Empty);
      configUI.SetPropertyControlValue(PropertyNames.M_UseRgbPicker, ControlInfoPropertyNames.Description, "Use RGB Picker");
      configUI.SetPropertyControlValue(PropertyNames.M_RgbColor, ControlInfoPropertyNames.DisplayName, "Color");
      configUI.SetPropertyControlType(PropertyNames.M_RgbColor, PropertyControlType.ColorWheel);
      configUI.SetPropertyControlValue(PropertyNames.M_H, ControlInfoPropertyNames.DisplayName, "Hue");
      configUI.SetPropertyControlValue(PropertyNames.M_H, ControlInfoPropertyNames.ControlStyle, SliderControlStyle.Hue);
      configUI.SetPropertyControlValue(PropertyNames.M_H, ControlInfoPropertyNames.RangeWraps, true);
      configUI.SetPropertyControlValue(PropertyNames.M_S, ControlInfoPropertyNames.DisplayName, "Saturation");
      configUI.SetPropertyControlValue(PropertyNames.M_V, ControlInfoPropertyNames.DisplayName, "Value");

      return configUI;
    }

    protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
    {
      // Change the effect's window title
      props[ControlInfoPropertyNames.WindowTitle].Value = "Apply Tint";
      // Add help button to effect UI
      props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
      props[ControlInfoPropertyNames.WindowHelpContent].Value = "Apply Tint v1.0\nCopyright ©2020 by Doug Zwick\nAll rights reserved.";
      base.OnCustomizeConfigUIWindowProperties(props);
    }

    protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
    {
      m_TintStrength = token.GetProperty<DoubleProperty>(PropertyNames.M_TintStrength).Value;
      m_UseRgbPicker = token.GetProperty<BooleanProperty>(PropertyNames.M_UseRgbPicker).Value;
      m_RgbColor = ColorBgra.FromOpaqueInt32(token.GetProperty<Int32Property>(PropertyNames.M_RgbColor).Value);
      m_H = token.GetProperty<Int32Property>(PropertyNames.M_H).Value;
      m_S = token.GetProperty<Int32Property>(PropertyNames.M_S).Value;
      m_V = token.GetProperty<Int32Property>(PropertyNames.M_V).Value;

      base.OnSetRenderInfo(token, dstArgs, srcArgs);
    }

    protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
    {
      if (length == 0) return;
      for (int i = startIndex; i < startIndex + length; ++i)
      {
        Render(DstArgs.Surface, SrcArgs.Surface, rois[i]);
      }
    }

    #region User Entered Code
    // Name: Apply Tint
    // Submenu: Color
    // Author: Doug Zwick
    // Title: Apply Tint
    // Version: 1.0.0
    // Desc: Multiplicatively tints the selection with the given color
    // Keywords:
    // URL:
    // Help:
    #region UICode
    DoubleSliderControl m_TintStrength = 1; // [0,1] Tint Strength
    CheckboxControl m_UseRgbPicker = true; // Use RGB Picker
    ColorWheelControl m_RgbColor = ColorBgra.FromBgr(0, 0, 0); // [PrimaryColor] {m_UseRgbPicker} Color
    IntSliderControl m_H = 0; // [0,360,1] {!m_UseRgbPicker} Hue
    IntSliderControl m_S = 100; // [0,100] {!m_UseRgbPicker} Saturation
    IntSliderControl m_V = 100; // [0,100] {!m_UseRgbPicker} Value
    #endregion

    void Render(Surface dst, Surface src, Rectangle rect)
    {
      HsvColor hsv = new HsvColor(m_H, m_S, m_V);
      var bgra = m_UseRgbPicker ? m_RgbColor : ColorBgra.FromColor(hsv.ToColor());
      var white = ColorBgra.FromColor(Color.White);
      var colorToUse = ColorBgra.Lerp(white, bgra, m_TintStrength);
      ColorBgra currentPixel;
      for (int y = rect.Top; y < rect.Bottom; y++)
      {
        if (IsCancelRequested) return;
        for (int x = rect.Left; x < rect.Right; x++)
        {
          currentPixel = src[x, y];
          byte r = currentPixel.R;
          byte g = currentPixel.G;
          byte b = currentPixel.B;
          currentPixel.R = (byte)((int)r * (int)colorToUse.R / 255);
          currentPixel.G = (byte)((int)g * (int)colorToUse.G / 255);
          currentPixel.B = (byte)((int)b * (int)colorToUse.B / 255);

          dst[x, y] = currentPixel;
        }
      }
    }

    #endregion
  }
}
