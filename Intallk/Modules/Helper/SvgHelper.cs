using Svg;
using System.Drawing;
using System.Xml;

namespace Intallk.Modules;

public static class SvgHelper
{
    public static void DrawAsSvg(this string svg, string path)
    {
        XmlDocument xml = new XmlDocument();
        xml.Load(svg);
        SvgDocument doc = SvgDocument.Open(xml);
        doc.Width *= 2; doc.Height *= 2;
        doc.Overflow = SvgOverflow.Inherit;
        doc.FontFamily = "HarmonyOS Sans SC Medium";
        Bitmap bitmap = new Bitmap((int)doc.Width.Value, (int)doc.Height.Value);
        doc.Draw(bitmap);
        bitmap.Save(path);
        bitmap.Dispose();
    }
}
