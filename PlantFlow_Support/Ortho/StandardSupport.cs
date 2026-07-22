using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.PnP3dObjects;
using Autodesk.ProcessPower.PnP3dOrthoDrawingsUI;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace PlantFlow_Support
{
  internal class StandardSupport
  {
    private bool IsBaseplate;
    private Matrix3d UCS;
    private Dictionary<string, string> SupportParams;
    private Vector3d SupportDirection;
    private PlantFlow_Support.Commands.ViewType ViewType;
    private string StandardName;
    private PortCollection PPorts;
    public Extents3d Extents3D = new Extents3d();
    public Dictionary<string, Point3d[]> TaggingPoints = new Dictionary<string, Point3d[]>();
    public Dictionary<string, Point3d[]> WeldingPoints = new Dictionary<string, Point3d[]>();

    public StandardSupport(
      Extents3d extents_3D,
      bool is_baseplate,
      Dictionary<string, string> support_params,
      Vector3d support_dir,
      PlantFlow_Support.Commands.ViewType view_type,
      PortCollection PPoints,
      Matrix3d ucs)
    {
      this.Extents3D = extents_3D;
      this.Extents3D.TransformBy(ucs);
      this.IsBaseplate = is_baseplate;
      this.SupportParams = support_params;
      this.SupportDirection = support_dir;
      this.ViewType = view_type;
      this.PPorts = PPoints;
      this.UCS = ucs;
    }


    // 출하 표준(PFS STANDARD)과 기존 모델(HANTEC)을 함께 지원한다.
    // 고객사 표준은 이 목록에만 추가한다.
    private static readonly string[] SupportedDesignStandards = { "PFS STANDARD", "HANTEC" };

    internal static bool IsSupportedStandard(string designStd)
    {
      string normalized = (designStd ?? string.Empty).Trim();
      bool supported = SupportedDesignStandards.Any(value => string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase));
      // 진단은 파일 로그가 표준이다. PSUtil.Log는 명령행 출력이라 사후 검증이 불가능하다.
      PlantOrthoView.FileDiag("STANDARDSUPPORT design standard value='" + normalized + "' supported=" + supported);
      return supported;
    }
    public void StandardInformation(string standard_name)
    {
      this.StandardName = standard_name;
      if (standard_name == "RS12")
      {
        switch (this.SupportParams["TY"])
        {
          case "-1":
            standard_name = "RS12A";
            break;
          case "-2":
            standard_name = "RS12B";
            break;
          case "-3":
            standard_name = "RS12C";
            break;
          case "-4":
            standard_name = "RS12D";
            break;
        }
      }
      if (standard_name == null)
        return;
      switch (standard_name.Length)
      {
        case 2:
          switch (standard_name[0])
          {
            case 'F':
              if (!(standard_name == "FS"))
                return;
              this.FS();
              return;
            case 'T':
              if (!(standard_name == "TR"))
                return;
              this.TR();
              return;
            default:
              return;
          }
        case 3:
          switch (standard_name[2])
          {
            case '1':
              switch (standard_name)
              {
                case "GD1":
                  this.GD1();
                  return;
                case "RC1":
                  goto label_66;
                case "RS1":
                  goto label_72;
                default:
                  return;
              }
            case '2':
              switch (standard_name)
              {
                case "GD2":
                  break;
                case "RS2":
                  goto label_63;
                case "RC2":
                  goto label_66;
                default:
                  return;
              }
              break;
            case '3':
              switch (standard_name)
              {
                case "GD3":
                  break;
                case "RS3":
                  goto label_64;
                case "RC3":
                  goto label_66;
                default:
                  return;
              }
              break;
            case '4':
              switch (standard_name)
              {
                case "RS4":
                  goto label_64;
                case "RC4":
                  this.RC4();
                  return;
                default:
                  return;
              }
            case '5':
              switch (standard_name)
              {
                case "RS5":
                  goto label_62;
                case "RC5":
                  this.RC5();
                  return;
                default:
                  return;
              }
            case '6':
              switch (standard_name)
              {
                case "RS6":
                  goto label_62;
                case "RC6":
                  goto label_66;
                default:
                  return;
              }
            case '7':
              switch (standard_name)
              {
                case "RC7":
                  goto label_63;
                case "RS7":
                  goto label_64;
                default:
                  return;
              }
            case '8':
              switch (standard_name)
              {
                case "RS8":
                  goto label_64;
                case "RC8":
                  this.RC8();
                  return;
                default:
                  return;
              }
            case '9':
              switch (standard_name)
              {
                case "RS9":
                  goto label_64;
                case "RC9":
                  this.RC9();
                  return;
                default:
                  return;
              }
            case 'S':
              if (!(standard_name == "TRS"))
                return;
              this.TRS();
              return;
            default:
              return;
          }
          this.GD2();
          return;
label_63:
          this.RS2();
          return;
label_66:
          this.RC1();
          return;
        case 4:
          switch (standard_name[3])
          {
            case '0':
              if (!(standard_name == "RS10"))
                return;
              goto label_64;
            case '1':
              if (!(standard_name == "RS11"))
                return;
              goto label_72;
            case '2':
              return;
            case '3':
              if (!(standard_name == "RS13"))
                return;
              this.RS13();
              return;
            case '4':
              if (!(standard_name == "RS14"))
                return;
              goto label_72;
            case '5':
              if (!(standard_name == "RS15"))
                return;
              this.RS15();
              return;
            default:
              return;
          }
        case 5:
          switch (standard_name[4])
          {
            case 'A':
              if (!(standard_name == "RS12A"))
                return;
              break;
            case 'B':
              if (!(standard_name == "RS12B"))
                return;
              goto label_64;
            case 'C':
              if (!(standard_name == "RS12C"))
                return;
              goto label_72;
            case 'D':
              if (!(standard_name == "RS12D"))
                return;
              goto label_64;
            default:
              return;
          }
          break;
        default:
          return;
      }
label_62:
      this.RS12A();
      return;
label_64:
      this.RS12B();
      return;
label_72:
      this.RS12C();
    }

    private void GD1()
    {
      this.TaggingPoints.Clear();
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d1
      });
      if (Convert.ToInt32(this.SupportParams["TY"]) != -2)
        return;
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point2,
        point3d2
      });
    }

    private void GD2()
    {
      this.TaggingPoints.Clear();
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d1
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d2
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d3 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F3", new Point3d[2]
      {
        point3,
        point3d3
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d4 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F4", new Point3d[2]
      {
        point4,
        point3d4
      });
    }

    private void RS2()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      if (!(this.StandardName == "RC7"))
        return;
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point3,
        point3d5
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_1", new Point3d[2]
      {
        point4,
        point3d6
      });
    }

    private void RS12A()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      double x1 = this.Extents3D.MaxPoint.X;
      double num1 = numArray[0];
      double x2 = this.Extents3D.MinPoint.X;
      double num2 = numArray[1];
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F3", new Point3d[2]
      {
        point3,
        point3d5
      });
    }

    private void RS12B()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      double x1 = this.Extents3D.MaxPoint.X;
      double num1 = numArray[0];
      double x2 = this.Extents3D.MinPoint.X;
      double num2 = numArray[1];
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      if (!(this.StandardName == "RC1"))
        return;
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point3,
        point3d5
      });
    }

    private void RS12C()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      double x1 = this.Extents3D.MaxPoint.X;
      double num1 = this.Extents3D.MaxPoint.Y;
      double x2 = this.Extents3D.MinPoint.X;
      double num2 = this.Extents3D.MinPoint.Y;
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d3 = new Point3d(point.X + 50.0, point.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point,
        point3d3
      });
    }

    private void RS13()
    {
      this.TaggingPoints.Clear();
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[5]);
      Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d1
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d2
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d3 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F3", new Point3d[2]
      {
        point3,
        point3d3
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d4 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F4", new Point3d[2]
      {
        point4,
        point3d4
      });
      Point3d point5 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d5 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F5", new Point3d[2]
      {
        point5,
        point3d5
      });
    }

    private void TRS()
    {
      this.TaggingPoints.Clear();
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d1
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d2
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d3 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F3", new Point3d[2]
      {
        point3,
        point3d3
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d4 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("Q1", new Point3d[2]
      {
        point4,
        point3d4
      });
      Point3d point5 = this.ConvertPortToPoint(this.PPorts[5]);
      Point3d point3d5 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
      this.TaggingPoints.Add("Q2", new Point3d[2]
      {
        point5,
        point3d5
      });
      Point3d point6 = this.ConvertPortToPoint(this.PPorts[6]);
      Point3d point3d6 = new Point3d(point6.X + 50.0, point6.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point6,
        point3d6
      });
      Point3d point7 = this.ConvertPortToPoint(this.PPorts[7]);
      Point3d point3d7 = new Point3d(point7.X + 50.0, point7.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_1", new Point3d[2]
      {
        point7,
        point3d7
      });
    }

    private void TR()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      double x1 = this.Extents3D.MaxPoint.X;
      double num1 = this.Extents3D.MaxPoint.Y;
      double x2 = this.Extents3D.MinPoint.X;
      double num2 = this.Extents3D.MinPoint.Y;
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      string supportParam = this.SupportParams["TY"];
      if (supportParam == null)
        return;
      switch (supportParam.Length)
      {
        case 2:
          switch (supportParam[1])
          {
            case '1':
              if (!(supportParam == "-1"))
                return;
              break;
            case '2':
              if (!(supportParam == "-2"))
                return;
              break;
            case '3':
              if (!(supportParam == "-3"))
                return;
              Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point1,
                point3d3
              });
              Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
              this.TaggingPoints.Add("G1", new Point3d[2]
              {
                point2,
                point3d4
              });
              Point3d point3 = this.ConvertPortToPoint(this.PPorts[4]);
              Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
              this.TaggingPoints.Add("S1", new Point3d[2]
              {
                point3,
                point3d5
              });
              return;
            case '4':
              if (!(supportParam == "-4"))
                return;
              Point3d point4 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point4,
                point3d6
              });
              Point3d point5 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d7 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
              this.TaggingPoints.Add("S1", new Point3d[2]
              {
                point5,
                point3d7
              });
              return;
            case '5':
              if (!(supportParam == "-5"))
                return;
              Point3d point6 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d8 = new Point3d(point6.X + 50.0, point6.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point6,
                point3d8
              });
              Point3d point7 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d9 = new Point3d(point7.X + 50.0, point7.Y + 50.0, 0.0);
              this.TaggingPoints.Add("G1", new Point3d[2]
              {
                point7,
                point3d9
              });
              return;
            case '6':
              if (!(supportParam == "-6"))
                return;
              Point3d point8 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d10 = new Point3d(point8.X + 50.0, point8.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point8,
                point3d10
              });
              Point3d point9 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d11 = new Point3d(point9.X + 50.0, point9.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q2", new Point3d[2]
              {
                point9,
                point3d11
              });
              Point3d point10 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d12 = new Point3d(point10.X + 50.0, point10.Y + 50.0, 0.0);
              this.TaggingPoints.Add("G1", new Point3d[2]
              {
                point10,
                point3d12
              });
              Point3d point11 = this.ConvertPortToPoint(this.PPorts[5]);
              Point3d point3d13 = new Point3d(point11.X + 50.0, point11.Y + 50.0, 0.0);
              this.TaggingPoints.Add("S1", new Point3d[2]
              {
                point11,
                point3d13
              });
              return;
            case '7':
              if (!(supportParam == "-7"))
                return;
              Point3d point12 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d14 = new Point3d(point12.X + 50.0, point12.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point12,
                point3d14
              });
              Point3d point13 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d15 = new Point3d(point13.X + 50.0, point13.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q2", new Point3d[2]
              {
                point13,
                point3d15
              });
              Point3d point14 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d16 = new Point3d(point14.X + 50.0, point14.Y + 50.0, 0.0);
              this.TaggingPoints.Add("S1", new Point3d[2]
              {
                point14,
                point3d16
              });
              return;
            case '8':
              if (!(supportParam == "-8"))
                return;
              Point3d point15 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d17 = new Point3d(point15.X + 50.0, point15.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point15,
                point3d17
              });
              Point3d point16 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d18 = new Point3d(point16.X + 50.0, point16.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q2", new Point3d[2]
              {
                point16,
                point3d18
              });
              Point3d point17 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d19 = new Point3d(point17.X + 50.0, point17.Y + 50.0, 0.0);
              this.TaggingPoints.Add("G1", new Point3d[2]
              {
                point17,
                point3d19
              });
              return;
            case '9':
              if (!(supportParam == "-9"))
                return;
              Point3d point18 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d20 = new Point3d(point18.X + 50.0, point18.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point18,
                point3d20
              });
              Point3d point19 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d21 = new Point3d(point19.X + 50.0, point19.Y + 50.0, 0.0);
              this.TaggingPoints.Add("UB", new Point3d[2]
              {
                point19,
                point3d21
              });
              return;
            default:
              return;
          }
          Point3d point20 = this.ConvertPortToPoint(this.PPorts[1]);
          Point3d point3d22 = new Point3d(point20.X + 50.0, point20.Y + 50.0, 0.0);
          this.TaggingPoints.Add("Q1", new Point3d[2]
          {
            point20,
            point3d22
          });
          Point3d point21 = this.ConvertPortToPoint(this.PPorts[2]);
          Point3d point3d23 = new Point3d(point21.X + 50.0, point21.Y + 50.0, 0.0);
          this.TaggingPoints.Add("P1_0", new Point3d[2]
          {
            point21,
            point3d23
          });
          break;
        case 3:
          switch (supportParam[2])
          {
            case '0':
              if (!(supportParam == "-10"))
                return;
              Point3d point22 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d24 = new Point3d(point22.X + 50.0, point22.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point22,
                point3d24
              });
              Point3d point23 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d25 = new Point3d(point23.X + 50.0, point23.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q2", new Point3d[2]
              {
                point23,
                point3d25
              });
              Point3d point24 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d26 = new Point3d(point24.X + 50.0, point24.Y + 50.0, 0.0);
              this.TaggingPoints.Add("UB", new Point3d[2]
              {
                point24,
                point3d26
              });
              return;
            case '1':
              if (!(supportParam == "-11"))
                return;
              Point3d point25 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d27 = new Point3d(point25.X + 50.0, point25.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point25,
                point3d27
              });
              return;
            case '2':
              if (!(supportParam == "-12"))
                return;
              Point3d point26 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d28 = new Point3d(point26.X + 50.0, point26.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q1", new Point3d[2]
              {
                point26,
                point3d28
              });
              Point3d point27 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d29 = new Point3d(point27.X + 50.0, point27.Y + 50.0, 0.0);
              this.TaggingPoints.Add("Q2", new Point3d[2]
              {
                point27,
                point3d29
              });
              return;
            default:
              return;
          }
      }
    }

    private void RC1()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point3,
        point3d5
      });
      if (!(this.StandardName == "RC6"))
        return;
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_1", new Point3d[2]
      {
        point4,
        point3d6
      });
    }

    private void RC4()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      if (this.SupportParams["TY"] == "-2")
      {
        Point3d point3 = this.ConvertPortToPoint(this.PPorts[4]);
        Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
        this.TaggingPoints.Add("F3", new Point3d[2]
        {
          point3,
          point3d5
        });
      }
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point4,
        point3d6
      });
    }

    private void RC5()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      // PPorts[0]은 RC5 하단 가로재 위 기준점이다. PPorts[2]는 상단 플레이트이므로 F1에 쓰지 않는다.
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[0]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      // PPorts[1]은 S2(세로 기둥 자유단)와 일치한다.
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point3,
        point3d5
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_1", new Point3d[2]
      {
        point4,
        point3d6
      });
    }

    private void RC8()
    {
      this.TaggingPoints.Clear();
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d1
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point2,
        point3d2
      });
    }

    private void RC9()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      Point3d point1 = this.ConvertPortToPoint(this.PPorts[3]);
      Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F1", new Point3d[2]
      {
        point1,
        point3d3
      });
      Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
      Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F2", new Point3d[2]
      {
        point2,
        point3d4
      });
      Point3d point3 = this.ConvertPortToPoint(this.PPorts[2]);
      Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
      this.TaggingPoints.Add("F3", new Point3d[2]
      {
        point3,
        point3d5
      });
      Point3d point4 = this.ConvertPortToPoint(this.PPorts[4]);
      Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_0", new Point3d[2]
      {
        point4,
        point3d6
      });
      Point3d point5 = this.ConvertPortToPoint(this.PPorts[5]);
      Point3d point3d7 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
      this.TaggingPoints.Add("P1_1", new Point3d[2]
      {
        point5,
        point3d7
      });
    }

    private void FS()
    {
      this.TaggingPoints.Clear();
      double[] numArray = new double[2];
      Point3d maxPoint1 = this.Extents3D.MaxPoint;
      numArray[0] = Math.Abs(maxPoint1.Y);
      Point3d minPoint1 = this.Extents3D.MinPoint;
      numArray[1] = Math.Abs(minPoint1.Y);
      double[] source = numArray;
      Point3d maxPoint2 = this.Extents3D.MaxPoint;
      double x1 = maxPoint2.X;
      double num1 = ((IEnumerable<double>) source).Max();
      Point3d point3d1 = new Point3d(x1, num1, 0.0);
      Point3d minPoint2 = this.Extents3D.MinPoint;
      double x2 = minPoint2.X;
      double num2 = ((IEnumerable<double>) source).Min();
      Point3d point3d2 = new Point3d(x2, num2, 0.0);
      this.Extents3D.Set(point3d2, point3d1);
      string supportParam1 = this.SupportParams["TY"];
      string supportParam2 = this.SupportParams["SM"];
      switch (supportParam1)
      {
        case "-1":
          switch (supportParam2)
          {
            case "-1":
              Point3d point1 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d3 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point1,
                point3d3
              });
              return;
            case "-2":
              Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d4 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point2,
                point3d4
              });
              Point3d point3 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d5 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_0", new Point3d[2]
              {
                point3,
                point3d5
              });
              return;
            default:
              return;
          }
        case "-2":
        case "-4":
          switch (supportParam2)
          {
            case "-1":
              Point3d point4 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d6 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point4,
                point3d6
              });
              Point3d point5 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d7 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point5,
                point3d7
              });
              return;
            case "-2":
              Point3d point6 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d8 = new Point3d(point6.X + 50.0, point6.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point6,
                point3d8
              });
              Point3d point7 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d9 = new Point3d(point7.X + 50.0, point7.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point7,
                point3d9
              });
              Point3d point8 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d10 = new Point3d(point8.X + 50.0, point8.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_0", new Point3d[2]
              {
                point8,
                point3d10
              });
              return;
            default:
              return;
          }
        case "-3":
          switch (supportParam2)
          {
            case "-1":
              Point3d point9 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d11 = new Point3d(point9.X + 50.0, point9.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point9,
                point3d11
              });
              Point3d point10 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d12 = new Point3d(point10.X + 50.0, point10.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point10,
                point3d12
              });
              Point3d point11 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d13 = new Point3d(point11.X + 50.0, point11.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F3", new Point3d[2]
              {
                point11,
                point3d13
              });
              return;
            case "-2":
              Point3d point12 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d14 = new Point3d(point12.X + 50.0, point12.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point12,
                point3d14
              });
              Point3d point13 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d15 = new Point3d(point13.X + 50.0, point13.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point13,
                point3d15
              });
              Point3d point14 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d16 = new Point3d(point14.X + 50.0, point14.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F3", new Point3d[2]
              {
                point14,
                point3d16
              });
              Point3d point15 = this.ConvertPortToPoint(this.PPorts[4]);
              Point3d point3d17 = new Point3d(point15.X + 50.0, point15.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_0", new Point3d[2]
              {
                point15,
                point3d17
              });
              Point3d point16 = this.ConvertPortToPoint(this.PPorts[5]);
              Point3d point3d18 = new Point3d(point16.X + 50.0, point16.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_1", new Point3d[2]
              {
                point16,
                point3d18
              });
              return;
            default:
              return;
          }
        case "-5":
          switch (supportParam2)
          {
            case "-1":
              Point3d point17 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d19 = new Point3d(point17.X + 50.0, point17.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point17,
                point3d19
              });
              Point3d point18 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d20 = new Point3d(point18.X + 50.0, point18.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point18,
                point3d20
              });
              return;
            case "-2":
              Point3d point19 = this.ConvertPortToPoint(this.PPorts[1]);
              Point3d point3d21 = new Point3d(point19.X + 50.0, point19.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F1", new Point3d[2]
              {
                point19,
                point3d21
              });
              Point3d point20 = this.ConvertPortToPoint(this.PPorts[2]);
              Point3d point3d22 = new Point3d(point20.X + 50.0, point20.Y + 50.0, 0.0);
              this.TaggingPoints.Add("F2", new Point3d[2]
              {
                point20,
                point3d22
              });
              Point3d point21 = this.ConvertPortToPoint(this.PPorts[3]);
              Point3d point3d23 = new Point3d(point21.X + 50.0, point21.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_0", new Point3d[2]
              {
                point21,
                point3d23
              });
              Point3d point22 = this.ConvertPortToPoint(this.PPorts[4]);
              Point3d point3d24 = new Point3d(point22.X + 50.0, point22.Y + 50.0, 0.0);
              this.TaggingPoints.Add("P1_1", new Point3d[2]
              {
                point22,
                point3d24
              });
              return;
            default:
              return;
          }
      }
    }

    private void RS15()
    {
      this.TaggingPoints.Clear();
      string supportParam1 = this.SupportParams["TY"];
      string supportParam2 = this.SupportParams["BI"];
      switch (supportParam1)
      {
        case "-1":
          Point3d point1 = this.ConvertPortToPoint(this.PPorts[2]);
          Point3d point3d1 = new Point3d(point1.X + 50.0, point1.Y + 50.0, 0.0);
          this.TaggingPoints.Add("F1", new Point3d[2]
          {
            point1,
            point3d1
          });
          Point3d point2 = this.ConvertPortToPoint(this.PPorts[1]);
          Point3d point3d2 = new Point3d(point2.X + 50.0, point2.Y + 50.0, 0.0);
          this.TaggingPoints.Add("F2", new Point3d[2]
          {
            point2,
            point3d2
          });
          break;
        case "-2":
          Point3d point3 = this.ConvertPortToPoint(this.PPorts[2]);
          Point3d point3d3 = new Point3d(point3.X + 50.0, point3.Y + 50.0, 0.0);
          this.TaggingPoints.Add("F1", new Point3d[2]
          {
            point3,
            point3d3
          });
          Point3d point4 = this.ConvertPortToPoint(this.PPorts[1]);
          Point3d point3d4 = new Point3d(point4.X + 50.0, point4.Y + 50.0, 0.0);
          this.TaggingPoints.Add("F2", new Point3d[2]
          {
            point4,
            point3d4
          });
          Point3d point5 = this.ConvertPortToPoint(this.PPorts[3]);
          Point3d point3d5 = new Point3d(point5.X + 50.0, point5.Y + 50.0, 0.0);
          this.TaggingPoints.Add("F3", new Point3d[2]
          {
            point5,
            point3d5
          });
          if (!(supportParam2 == "315"))
            break;
          Point3d point6 = this.ConvertPortToPoint(this.PPorts[4]);
          Point3d point3d6 = new Point3d(point6.X + 50.0, point6.Y + 50.0, 0.0);
          this.TaggingPoints.Add("P1_0", new Point3d[2]
          {
            point6,
            point3d6
          });
          break;
      }
    }

    public Point3d ConvertPortToPoint(Port port)
    {
      Point3d position1 = port.Position;
      switch ((int)this.ViewType - 1)
      {
        case 2:
          position1 = new Point3d(position1.X, 0.0, -position1.Z);
          break;
        case 3:
          position1 = new Point3d(position1.X, 0.0, position1.Z);
          break;
        case 4:
          position1 = new Point3d(-position1.Z, 0.0, position1.Y);
          break;
        case 5:
          position1 = new Point3d(position1.Z, 0.0, position1.Y);
          break;
      }
      return position1.TransformBy(this.UCS);
    }

    public static int TrunnionProfile(int Dn)
    {
      return new Dictionary<int, int>()
      {
        {
          40,
          25
        },
        {
          50,
          40
        },
        {
          80,
          50
        },
        {
          100,
          50
        },
        {
          125,
          80
        },
        {
          150,
          100
        },
        {
          200,
          100
        },
        {
          250,
          150
        },
        {
          300,
          200
        },
        {
          350,
          200
        },
        {
          400,
          250
        },
        {
          450,
          250
        },
        {
          500,
          300
        },
        {
          550,
          300
        },
        {
          600,
          300
        },
        {
          650,
          350
        },
        {
          700,
          350
        },
        {
          750,
          400
        },
        {
          800,
          400
        },
        {
          850,
          450
        },
        {
          900,
          450
        },
        {
          950,
          500
        },
        {
          1000,
          500
        },
        {
          1050,
          500
        },
        {
          1100,
          500
        },
        {
          1250,
          500
        },
        {
          1350,
          700
        }
      }[Dn];
    }

    public static string AnchorBoltDetail(string std_type, string BI)
    {
      return new Dictionary<string, Dictionary<string, string>>()
      {
        {
          "GD1",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "TR",
          new Dictionary<string, string>()
          {
            {
              "25",
              "(M10)3/8\""
            },
            {
              "40",
              "(M10)3/8\""
            },
            {
              "50",
              "(M10)3/8\""
            },
            {
              "80",
              "(M10)3/8\""
            },
            {
              "100",
              "(M10)3/8\""
            },
            {
              "150",
              "(M12)1/2\""
            },
            {
              "200",
              "(M12)1/2\""
            },
            {
              "250",
              "(M16)5/8\""
            },
            {
              "300",
              "(M16)5/8\""
            },
            {
              "350",
              "(M16)5/8\""
            },
            {
              "400",
              "(M16)5/8\""
            },
            {
              "450",
              "(M20)3/4\""
            },
            {
              "500",
              "(M20)3/4\""
            }
          }
        },
        {
          "RC1",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC2",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC3",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M10)3/8\""
            },
            {
              "16",
              "(M10)3/8\""
            },
            {
              "17",
              "(M10)3/8\""
            },
            {
              "110",
              "(M10)3/8\""
            },
            {
              "210",
              "(M12)1/2\""
            },
            {
              "212",
              "(M12)1/2\""
            },
            {
              "215",
              "(M12)1/2\""
            },
            {
              "220",
              "(M12)1/2\""
            },
            {
              "310",
              "(M16)5/8\""
            },
            {
              "312",
              "(M16)5/8\""
            },
            {
              "315",
              "(M16)5/8\""
            },
            {
              "317",
              "(M16)5/8\""
            },
            {
              "320",
              "(M16)5/8\""
            }
          }
        },
        {
          "RC4",
          new Dictionary<string, string>()
          {
            {
              "110",
              "(M10)3/8\""
            },
            {
              "215",
              "(M12)1/2\""
            },
            {
              "220",
              "(M12)1/2\""
            },
            {
              "315",
              "(M16)5/8\""
            }
          }
        },
        {
          "RC5",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            },
            {
              "215",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC6",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            },
            {
              "215",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC7",
          new Dictionary<string, string>()
          {
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            },
            {
              "215",
              "(M16)5/8\"x150"
            },
            {
              "312",
              "(M16)5/8\"x150"
            },
            {
              "315",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC8",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "RC9",
          new Dictionary<string, string>()
          {
            {
              "15",
              "(M12)1/2\"x125"
            },
            {
              "16",
              "(M12)1/2\"x125"
            },
            {
              "17",
              "(M12)1/2\"x125"
            },
            {
              "110",
              "(M16)5/8\"x150"
            },
            {
              "210",
              "(M16)5/8\"x150"
            },
            {
              "212",
              "(M16)5/8\"x150"
            },
            {
              "215",
              "(M16)5/8\"x150"
            },
            {
              "220",
              "(M16)5/8\"x150"
            },
            {
              "310",
              "(M16)5/8\"x150"
            },
            {
              "312",
              "(M16)5/8\"x150"
            },
            {
              "315",
              "(M16)5/8\"x150"
            }
          }
        },
        {
          "FS",
          new Dictionary<string, string>()
          {
            {
              "17",
              "(M10)3/8\"x108"
            }
          }
        }
      }[std_type][BI];
    }

    public static string DetailProfile(string BI)
    {
      return new Dictionary<string, string>()
      {
        {
          "15",
          "50x50x6"
        },
        {
          "16",
          "65x65x6"
        },
        {
          "17",
          "75x75x9"
        },
        {
          "19",
          "90x90x10"
        },
        {
          "110",
          "100x100x10"
        },
        {
          "113",
          "130x130x12"
        },
        {
          "115",
          "150x150x15"
        },
        {
          "117",
          "175x175x15"
        },
        {
          "120",
          "200x200x20"
        },
        {
          "125",
          "250x250x25"
        },
        {
          "210",
          "100x50x5x7.5"
        },
        {
          "212",
          "125x65x6x8"
        },
        {
          "215",
          "150x75x6.5x10"
        },
        {
          "220",
          "200x90x8x13.5"
        },
        {
          "225",
          "250x90x9x13"
        },
        {
          "230",
          "300x90x9x13"
        },
        {
          "310",
          "100x100x6x8"
        },
        {
          "312",
          "125x125x6.5x9"
        },
        {
          "315",
          "150x150x7x10"
        },
        {
          "317",
          "175x175x7.5x11"
        },
        {
          "320",
          "200x200x8x12"
        },
        {
          "325",
          "250x250x9x14"
        },
        {
          "330",
          "300x200x9x14"
        },
        {
          "425",
          "25x6"
        },
        {
          "450",
          "50x6"
        },
        {
          "475",
          "75x9"
        },
        {
          "4100",
          "100x9"
        }
      }[BI];
    }

    private static Dictionary<string, string> BeamProfileMap()
    {
      return new Dictionary<string, string>()
      {
        {
          "15",
          "ANGLE A5"
        },
        {
          "16",
          "ANGLE A6"
        },
        {
          "17",
          "ANGLE A7"
        },
        {
          "19",
          "ANGLE A9"
        },
        {
          "110",
          "ANGLE A10"
        },
        {
          "113",
          "ANGLE A13"
        },
        {
          "115",
          "ANGLE A15"
        },
        {
          "117",
          "ANGLE A17"
        },
        {
          "120",
          "ANGLE A20"
        },
        {
          "125",
          "ANGLE A25"
        },
        {
          "210",
          "CHANNEL C10"
        },
        {
          "212",
          "CHANNEL C12"
        },
        {
          "215",
          "CHANNEL C15"
        },
        {
          "220",
          "CHANNEL C20"
        },
        {
          "225",
          "CHANNEL C25"
        },
        {
          "230",
          "CHANNEL C30"
        },
        {
          "310",
          "H-BEAM H10"
        },
        {
          "312",
          "H-BEAM H12"
        },
        {
          "315",
          "H-BEAM H15"
        },
        {
          "317",
          "H-BEAM H17"
        },
        {
          "320",
          "H-BEAM H20"
        },
        {
          "325",
          "H-BEAM H25"
        },
        {
          "330",
          "H-BEAM H30"
        },
        {
          "425",
          "FLAT BAR F25"
        },
        {
          "450",
          "FLAT BAR F50"
        },
        {
          "475",
          "FLAT BAR F75"
        },
        {
          "4100",
          "FLAT BAR F100"
        }
      };
    }

    public static string BeamProfile(string indicator)
    {
      return BeamProfileMap()[indicator];
    }

    // BOM col[2]("CHANNEL C15") -> BI("215"). 값은 고유하므로 충돌 없음.
    public static string BeamProfileBI(string beamValue)
    {
      if (string.IsNullOrWhiteSpace(beamValue))
        return string.Empty;

      string key = System.Text.RegularExpressions.Regex.Replace(beamValue.Trim(), "\\s+", " ").ToUpperInvariant();
      foreach (KeyValuePair<string, string> kv in BeamProfileMap())
      {
        string value = System.Text.RegularExpressions.Regex.Replace(kv.Value.Trim(), "\\s+", " ").ToUpperInvariant();
        if (string.Equals(value, key, System.StringComparison.Ordinal))
          return kv.Key;
      }
      return string.Empty;
    }

    public static string PlateProfile(string standard_type, string BI)
    {
      return new Dictionary<string, Dictionary<string, string>>()
      {
        {
          "GD1",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "110",
              "260x260x6mm"
            },
            {
              "212",
              "260x260x6mm"
            }
          }
        },
        {
          "RC1",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "210",
              "260x260x6mm"
            }
          }
        },
        {
          "RC2",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "210",
              "260x260x6mm"
            }
          }
        },
        {
          "RC3",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "110",
              "210x210x9mm"
            },
            {
              "210",
              "190x190x9mm"
            },
            {
              "212",
              "230x230x9mm"
            },
            {
              "215",
              "230x230x9mm"
            },
            {
              "220",
              "280x280x12mm"
            },
            {
              "310",
              "210x210x12mm"
            },
            {
              "312",
              "260x260x12mm"
            },
            {
              "315",
              "260x260x12mm"
            },
            {
              "317",
              "310x310x12mm"
            },
            {
              "320",
              "310x310x12mm"
            }
          }
        },
        {
          "RC4",
          new Dictionary<string, string>()
          {
            {
              "110",
              "210x210x9mm"
            },
            {
              "215",
              "230x230x9mm"
            },
            {
              "220",
              "280x280x12mm"
            },
            {
              "315",
              "260x260x12mm"
            }
          }
        },
        {
          "RC5",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "210",
              "260x260x9mm"
            },
            {
              "212",
              "260x260x9mm"
            },
            {
              "215",
              "300x300x9mm"
            }
          }
        },
        {
          "RC6",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "210",
              "260x260x9mm"
            },
            {
              "212",
              "260x260x9mm"
            },
            {
              "215",
              "300x300x9mm"
            }
          }
        },
        {
          "RC7",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "210",
              "260x260x9mm"
            },
            {
              "212",
              "260x260x9mm"
            },
            {
              "215",
              "300x300x9mm"
            },
            {
              "312",
              "300x300x9mm"
            },
            {
              "315",
              "300x300x9mm"
            }
          }
        },
        {
          "RC8",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "110",
              "260x260x9mm"
            },
            {
              "210",
              "260x260x9mm"
            },
            {
              "212",
              "260x260x9mm"
            },
            {
              "215",
              "300x300x9mm"
            },
            {
              "220",
              "340x340x12m"
            }
          }
        },
        {
          "RC9",
          new Dictionary<string, string>()
          {
            {
              "15",
              "180x180x6mm"
            },
            {
              "16",
              "180x180x6mm"
            },
            {
              "17",
              "180x180x6mm"
            },
            {
              "110",
              "260x260x9mm"
            },
            {
              "210",
              "260x260x9mm"
            },
            {
              "212",
              "260x260x9mm"
            },
            {
              "215",
              "300x300x9mm"
            },
            {
              "310",
              "300x300x9mm"
            },
            {
              "220",
              "340x340x12mm"
            },
            {
              "312",
              "340x340x12mm"
            },
            {
              "315",
              "340x340x12mm"
            }
          }
        },
        {
          "TR",
          new Dictionary<string, string>()
          {
            {
              "25",
              "140x140x6mm"
            },
            {
              "40",
              "140x140x6mm"
            },
            {
              "50",
              "140x140x6mm"
            },
            {
              "80",
              "140x140x6mm"
            },
            {
              "100",
              "160x160x9mm"
            },
            {
              "150",
              "220x220x12mm"
            },
            {
              "200",
              "270x270x12mm"
            },
            {
              "250",
              "320x320x16mm"
            },
            {
              "300",
              "370x370x16mm"
            },
            {
              "350",
              "400x400x16mm"
            },
            {
              "400",
              "450x450x16mm"
            },
            {
              "450",
              "500x500x20mm"
            },
            {
              "500",
              "550x550x20mm"
            }
          }
        },
        {
          "TRPT2",
          new Dictionary<string, string>()
          {
            {
              "25",
              "90x90x6mm"
            },
            {
              "40",
              "90x90x6mm"
            },
            {
              "50",
              "140x140x6mm"
            },
            {
              "80",
              "140x140x6mm"
            },
            {
              "100",
              "160x140x9mm"
            },
            {
              "150",
              "220x220x12mm"
            },
            {
              "200",
              "270x270x12mm"
            },
            {
              "250",
              "320x320x12mm"
            },
            {
              "300",
              "370x370x12mm"
            },
            {
              "350",
              "400x400x16mm"
            },
            {
              "400",
              "450x450x16mm"
            },
            {
              "450",
              "500x500x22mm"
            },
            {
              "500",
              "550x550x22mm"
            },
            {
              "600",
              "650x650x25mm"
            },
            {
              "700",
              "760x760x28mm"
            }
          }
        },
        {
          "FS",
          new Dictionary<string, string>() { { "17", "160x160x6mm" } }
        }
      }[standard_type][BI];
    }
  }
}
