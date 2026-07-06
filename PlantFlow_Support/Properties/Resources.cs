using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

#nullable disable
namespace PlantFlow_Support.Properties
{
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
  [DebuggerNonUserCode]
  [CompilerGenerated]
  internal class Resources
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    internal Resources()
    {
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (PlantFlow_Support.Properties.Resources.resourceMan == null)
          PlantFlow_Support.Properties.Resources.resourceMan = new ResourceManager("PlantFlow_Support.Properties.Resources", typeof (PlantFlow_Support.Properties.Resources).Assembly);
        return PlantFlow_Support.Properties.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get => PlantFlow_Support.Properties.Resources.resourceCulture;
      set => PlantFlow_Support.Properties.Resources.resourceCulture = value;
    }

    internal static Bitmap settings
    {
      get
      {
        return (Bitmap) PlantFlow_Support.Properties.Resources.ResourceManager.GetObject(nameof (settings), PlantFlow_Support.Properties.Resources.resourceCulture);
      }
    }
  }
}
