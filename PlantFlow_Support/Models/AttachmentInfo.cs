using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.PnP3dObjects;

#nullable disable
namespace PlantFlow_Support
{
  public class AttachmentInfo
  {
    public string Name { get; set; }

    public ObjectId Id { get; set; }

    public string BOP { get; set; }

    public string COP { get; set; }

    public string Size { get; set; }

    public string Description { get; set; }

    public string LineNumber { get; set; }

    public PortCollection PPoints { get; set; }

    public Point3d PortZero { get; set; }
  }
}
