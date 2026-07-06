using Autodesk.AutoCAD.Windows;
using System;
using System.Drawing;
using System.Windows.Forms;

#nullable disable
namespace PlantFlow_Support
{
  public class CustomPaletteSet : PaletteSet
  {
    public CustomPaletteSet()
      : base("PlantFlow_Support", "PlantFlow_Support", new Guid("ba6361b4-e73c-45e3-a5d6-84039323d0a9"))
    {
      this.Style = (PaletteSetStyles) 14;
      this.MinimumSize = new Size(330, 280);
      this.Add("Tab", (Control) new PaletteTab());
    }
  }
}
