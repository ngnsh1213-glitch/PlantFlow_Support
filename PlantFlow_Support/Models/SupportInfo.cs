// Decompiled with JetBrains decompiler
// Type: PlantFlow_Support.SupportInfo
// Assembly: PlantFlow_Support, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 503E85E1-3D1B-40F1-A567-5E4C05655DB9
// Assembly location: C:\Temp\CADLIB\PlantFlow_Support.dll

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.PnP3dOrthoDrawingsUI;
using System.Collections.Generic;
using Autodesk.ProcessPower.DataObjects;

#nullable disable
namespace PlantFlow_Support
{
  public class ViewSpec
  {
    public string ViewName { get; set; }

    public PlantFlow_Support.Commands.ViewType ViewType { get; set; }

    public Matrix3d UCS { get; set; }

    public Vector3d ViewDirection { get; set; }

    public Vector3d UpVector { get; set; }

    public Point3d PaperCenter { get; set; }
  }

  public class SupportInfo
  {
    public string Name { get; set; }
    public PnPDatabase PnPDatabase { get; set; }

    public PlantFlow_Support.Commands.ViewType ViewType { get; set; }

    public List<ViewSpec> ViewSpecs { get; set; } = new List<ViewSpec>();

    public int NumberOfView { get; set; }

    public ObjectId[] Ids { get; set; }

    public Extents3d Extents { get; set; }

    public Matrix3d UCS { get; set; }

    public Vector3d ViewDirection { get; set; }

    public Vector3d UpVector { get; set; }

    public string CPYDesignStd { get; set; }

    public List<AttachmentInfo> AttachmentList { get; set; }

    public List<ObjectId> PipeList { get; set; }

    public Point3d DatumPoint { get; set; }

    public Vector3d SupportDirection { get; set; }

    public PortCollection PPoints { get; set; }

    public string PipeLineNo { get; set; }

    public string SupportCoding { get; set; }

    public string DwgProjectNo { get; set; }

    public string DwgRevision { get; set; }

    public string BOP { get; set; }

    public string COP { get; set; }

    public string Size { get; set; }

    public string StdName { get; set; }

    public bool GenerateBOM { get; set; } = true;

    public Point3d? CustomInsertionPoint { get; set; } = null;
  }
}
