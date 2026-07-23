using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using ClosedXML.Excel;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace PlantFlow_Support
{
  internal class BOMs : PSUtil
  {
    private ObjectId id;
    private List<string[]> bom_contents = new List<string[]>();
    private List<AttachmentInfo> AttachmentInfos;
    public Dictionary<string, string> SupportParams;
    public bool IsBaseplate;
    public string StandardName;
    public string frame_material;
    private PnPTable support;
    public Dictionary<string, Dictionary<string, List<ObjectId>>> Supports;
    public Dictionary<string, List<ObjectId>> Frameworks;

    public BOMs(ObjectId id, List<AttachmentInfo> attachments)
    {
      this.id = id;
      this.AttachmentInfos = attachments;
      this.SupportParams = this.GetSupportDimension(id);
    }

    public BOMs()
    {
    }

    public List<string[]> ContentsByDesignStd(string design_std)
    {
      if (StandardSupport.IsSupportedStandard(design_std))
        this.StandardSupportContents();
      return this.bom_contents;
    }

    public void StandardSupportContents()
    {
      StringCollection properties = this.dl_manager.GetProperties(this.id, new StringCollection()
      {
        "ShortDescription",
        "Material"
      }, true);
      this.StandardName = properties[0];
      this.frame_material = properties[1];
      this.bom_contents.Clear();
      this.FrameBOM();
      this.AttaBOM();
    }

    private void AttaBOM()
    {
      foreach (AttachmentInfo attachmentInfo in this.AttachmentInfos)
        this.bom_contents.Add(new string[9]
        {
          attachmentInfo.Name,
          "ATTACHMENT",
          attachmentInfo.Size,
          "ASTM A36",
          "1",
          attachmentInfo.Description,
          attachmentInfo.BOP,
          attachmentInfo.COP,
          attachmentInfo.LineNumber
        });
    }

    private void FrameBOM()
    {
      string standardName1 = this.StandardName;
      if (standardName1 == null)
        return;
      int int32_1;
      switch (standardName1.Length)
      {
        case 2:
          switch (standardName1[0])
          {
            case 'F':
              if (!(standardName1 == "FS"))
                return;
              string str1 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
              double num1 = Convert.ToDouble(this.SupportParams["A"]);
              int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
              double num2 = Convert.ToDouble(this.SupportParams["A1"]);
              string supportParam1 = this.SupportParams["TY"];
              string supportParam2 = this.SupportParams["SM"];
              double num3 = Math.Ceiling(num1 + num2);
              string str2 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
              string str3 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
              switch (supportParam1)
              {
                case "-1":
                  switch (supportParam2)
                  {
                    case "-1":
                      this.bom_contents.Add(new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      });
                      return;
                    case "-2":
                      string[] strArray1 = new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      };
                      string[] strArray2 = new string[6]
                      {
                        "P1",
                        "BASE PLATE",
                        str2,
                        this.frame_material,
                        "1",
                        ""
                      };
                      string[] strArray3 = new string[6]
                      {
                        "J1",
                        "SET ANCHOR",
                        str3,
                        this.frame_material,
                        "4ea",
                        ""
                      };
                      this.bom_contents.Add(strArray1);
                      this.bom_contents.Add(strArray2);
                      this.bom_contents.Add(strArray3);
                      return;
                    default:
                      return;
                  }
                case "-2":
                case "-4":
                case "-5":
                  switch (supportParam2)
                  {
                    case "-1":
                      double num4 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                      if (supportParam1 == "-5")
                        num4 = Math.Ceiling((num3 - 200.0) / (Math.Sqrt(3.0) / 2.0));
                      string[] strArray4 = new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      };
                      string[] strArray5 = new string[6]
                      {
                        "F2",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num4.ToString(),
                        ""
                      };
                      this.bom_contents.Add(strArray4);
                      this.bom_contents.Add(strArray5);
                      return;
                    case "-2":
                      double num5 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                      string str4 = "1";
                      if (supportParam1 == "-5")
                      {
                        num5 = Math.Ceiling((num3 - 200.0) / (Math.Sqrt(3.0) / 2.0));
                        str4 = "2";
                      }
                      string[] strArray6 = new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      };
                      string[] strArray7 = new string[6]
                      {
                        "F2",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num5.ToString(),
                        ""
                      };
                      string[] strArray8 = new string[6]
                      {
                        "P1",
                        "BASE PLATE",
                        str2,
                        this.frame_material,
                        str4,
                        ""
                      };
                      string[] strArray9 = new string[6]
                      {
                        "J1",
                        "SET ANCHOR",
                        str3,
                        this.frame_material,
                        "4ea",
                        ""
                      };
                      this.bom_contents.Add(strArray6);
                      this.bom_contents.Add(strArray7);
                      this.bom_contents.Add(strArray8);
                      this.bom_contents.Add(strArray9);
                      return;
                    default:
                      return;
                  }
                case "-3":
                  switch (supportParam2)
                  {
                    case "-1":
                      double num6 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                      string[] strArray10 = new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      };
                      string[] strArray11 = new string[6]
                      {
                        "F2",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num6.ToString(),
                        ""
                      };
                      string[] strArray12 = new string[6]
                      {
                        "F3",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num6.ToString(),
                        ""
                      };
                      this.bom_contents.Add(strArray10);
                      this.bom_contents.Add(strArray11);
                      this.bom_contents.Add(strArray12);
                      return;
                    case "-2":
                      double num7 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                      string[] strArray13 = new string[6]
                      {
                        "F1",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num3.ToString(),
                        ""
                      };
                      string[] strArray14 = new string[6]
                      {
                        "F2",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num7.ToString(),
                        ""
                      };
                      string[] strArray15 = new string[6]
                      {
                        "F3",
                        "FRAMEWORK",
                        str1,
                        this.frame_material,
                        num7.ToString(),
                        ""
                      };
                      string[] strArray16 = new string[6]
                      {
                        "P1",
                        "BASE PLATE",
                        str2,
                        this.frame_material,
                        "2",
                        ""
                      };
                      string[] strArray17 = new string[6]
                      {
                        "J1",
                        "SET ANCHOR",
                        str3,
                        this.frame_material,
                        "8ea",
                        ""
                      };
                      this.bom_contents.Add(strArray13);
                      this.bom_contents.Add(strArray14);
                      this.bom_contents.Add(strArray15);
                      this.bom_contents.Add(strArray16);
                      this.bom_contents.Add(strArray17);
                      return;
                    default:
                      return;
                  }
                default:
                  return;
              }
            case 'T':
              if (!(standardName1 == "TR"))
                return;
              int int32_2 = Convert.ToInt32(this.SupportParams["TY"]);
              double a = Convert.ToDouble(this.SupportParams["A"]);
              int int32_3 = Convert.ToInt32(this.SupportParams["Dn"]);
              string BI = StandardSupport.TrunnionProfile(int32_3).ToString();
              StandardSupport.BeamProfile("15");
              string str5 = StandardSupport.BeamProfile("210");
              switch (int32_2)
              {
                case -14:
                case -13:
                  string str6 = StandardSupport.PlateProfile("TRPT2", BI);
                  double num8 = Math.Ceiling(a + PSUtil.PipeSize(int32_3) * 1.5);
                  if (int32_2 == -14)
                    num8 = Math.Ceiling(a);
                  string[] strArray18 = new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num8.ToString(),
                    ""
                  };
                  string[] strArray19 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str6,
                    this.frame_material,
                    "1",
                    ""
                  };
                  this.bom_contents.Add(strArray18);
                  this.bom_contents.Add(strArray19);
                  return;
                case -12:
                case -11:
                  double num9 = Math.Ceiling(a);
                  this.bom_contents.Add(new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num9.ToString(),
                    ""
                  });
                  if (int32_2 != -12)
                    return;
                  double num10 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A2"]));
                  this.bom_contents.Add(new string[6]
                  {
                    "Q2",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num10.ToString(),
                    ""
                  });
                  return;
                case -10:
                case -9:
                  double num11 = Math.Ceiling(a);
                  this.bom_contents.Add(new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num11.ToString(),
                    ""
                  });
                  string str7 = "1";
                  if (int32_2 == -10)
                  {
                    str7 = "2";
                    double num12 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A2"]));
                    this.bom_contents.Add(new string[6]
                    {
                      "Q2",
                      "FRAMEWORK",
                      BI + "A",
                      this.frame_material,
                      num12.ToString(),
                      ""
                    });
                  }
                  this.bom_contents.Add(new string[6]
                  {
                    "UB",
                    "U-BOLT",
                    BI + "A",
                    this.frame_material,
                    str7,
                    ""
                  });
                  return;
                case -8:
                case -7:
                case -6:
                  double num13 = Math.Ceiling(a);
                  double num14 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A2"]));
                  double num15 = Math.Ceiling(Convert.ToDouble(this.SupportParams["S2"]));
                  double num16 = Math.Ceiling(Convert.ToDouble(this.SupportParams["G2"]));
                  string[] strArray20 = new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num13.ToString(),
                    ""
                  };
                  string[] strArray21 = new string[6]
                  {
                    "Q2",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num14.ToString(),
                    ""
                  };
                  string[] strArray22 = new string[6]
                  {
                    "S1",
                    "STOPPER",
                    str5,
                    this.frame_material,
                    num15.ToString(),
                    "x4 Quan."
                  };
                  string[] strArray23 = new string[6]
                  {
                    "G1",
                    "GUIDE",
                    str5,
                    this.frame_material,
                    num16.ToString(),
                    "x4 Quan."
                  };
                  if (int32_2 == -6)
                  {
                    this.bom_contents.Add(strArray20);
                    this.bom_contents.Add(strArray21);
                    this.bom_contents.Add(strArray22);
                    this.bom_contents.Add(strArray23);
                    return;
                  }
                  if (int32_2 == -7)
                  {
                    this.bom_contents.Add(strArray20);
                    this.bom_contents.Add(strArray21);
                    this.bom_contents.Add(strArray22);
                    return;
                  }
                  if (int32_2 != -8)
                    return;
                  this.bom_contents.Add(strArray20);
                  this.bom_contents.Add(strArray21);
                  this.bom_contents.Add(strArray23);
                  return;
                case -5:
                case -4:
                case -3:
                  double num17 = Math.Ceiling(a);
                  double num18 = Math.Ceiling(Convert.ToDouble(this.SupportParams["S2"]));
                  double num19 = Math.Ceiling(Convert.ToDouble(this.SupportParams["G2"]));
                  string[] strArray24 = new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num17.ToString(),
                    ""
                  };
                  string[] strArray25 = new string[6]
                  {
                    "S1",
                    "STOPPER",
                    str5,
                    this.frame_material,
                    num18.ToString(),
                    "x2 Quan."
                  };
                  string[] strArray26 = new string[6]
                  {
                    "G1",
                    "GUIDE",
                    str5,
                    this.frame_material,
                    num19.ToString(),
                    "x2 Quan."
                  };
                  if (int32_2 == -3)
                  {
                    this.bom_contents.Add(strArray24);
                    this.bom_contents.Add(strArray25);
                    this.bom_contents.Add(strArray26);
                    return;
                  }
                  if (int32_2 == -4)
                  {
                    this.bom_contents.Add(strArray24);
                    this.bom_contents.Add(strArray25);
                    return;
                  }
                  if (int32_2 != -5)
                    return;
                  this.bom_contents.Add(strArray24);
                  this.bom_contents.Add(strArray26);
                  return;
                case -2:
                case -1:
                  string str8 = StandardSupport.PlateProfile(this.StandardName, BI);
                  double num20 = Math.Ceiling(a + PSUtil.PipeSize(int32_3) * 1.5);
                  if (int32_2 == -2)
                    num20 = Math.Ceiling(a);
                  string[] strArray27 = new string[6]
                  {
                    "Q1",
                    "FRAMEWORK",
                    BI + "A",
                    this.frame_material,
                    num20.ToString(),
                    ""
                  };
                  string[] strArray28 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str8,
                    this.frame_material,
                    "1",
                    ""
                  };
                  string[] strArray29 = new string[6]
                  {
                    "J1",
                    "SET ANCHOR",
                    StandardSupport.AnchorBoltDetail("TR", BI),
                    this.frame_material,
                    "4ea",
                    ""
                  };
                  this.bom_contents.Add(strArray27);
                  this.bom_contents.Add(strArray28);
                  this.bom_contents.Add(strArray29);
                  return;
                default:
                  return;
              }
            default:
              return;
          }
        case 3:
          switch (standardName1[2])
          {
            case '1':
              switch (standardName1)
              {
                case "GD1":
                case "RS1":
                  goto label_56;
                case "RC1":
                  break;
                default:
                  return;
              }
              break;
            case '2':
              switch (standardName1)
              {
                case "GD2":
                  double num21 = Convert.ToDouble(this.SupportParams["B"]);
                  double num22 = Convert.ToDouble(this.SupportParams["D"]);
                  double num23 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F1"]));
                  double num24 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F1"]));
                  double num25 = Math.Ceiling(num21 + num22);
                  double num26 = Math.Ceiling(num21 + num22 - 20.0);
                  int int32_4 = Convert.ToInt32(this.SupportParams["TY"]);
                  string str9 = StandardSupport.BeamProfile("210");
                  string str10 = StandardSupport.BeamProfile("15");
                  switch (int32_4)
                  {
                    case -2:
                      str9 = StandardSupport.BeamProfile("215");
                      str10 = StandardSupport.BeamProfile("16");
                      break;
                    case -1:
                      str9 = StandardSupport.BeamProfile("210");
                      str10 = StandardSupport.BeamProfile("15");
                      break;
                  }
                  string[] strArray30 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str9,
                    this.frame_material,
                    num23.ToString(),
                    ""
                  };
                  string[] strArray31 = new string[6]
                  {
                    "F2",
                    "FRAMEWORK",
                    str9,
                    this.frame_material,
                    num24.ToString(),
                    ""
                  };
                  string[] strArray32 = new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    str10,
                    this.frame_material,
                    num25.ToString(),
                    ""
                  };
                  string[] strArray33 = new string[6]
                  {
                    "F4",
                    "FRAMEWORK",
                    str10,
                    this.frame_material,
                    num26.ToString(),
                    ""
                  };
                  this.bom_contents.Add(strArray30);
                  this.bom_contents.Add(strArray31);
                  this.bom_contents.Add(strArray32);
                  this.bom_contents.Add(strArray33);
                  return;
                case "RS2":
                  goto label_66;
                case "RC2":
                  break;
                default:
                  return;
              }
              break;
            case '3':
              switch (standardName1)
              {
                case "GD3":
                  Dictionary<int, double> dictionary1 = new Dictionary<int, double>();
                  dictionary1.Add(150, 87.0);
                  dictionary1.Add(200, 113.0);
                  dictionary1.Add(250, 150.0);
                  dictionary1.Add(300, 165.0);
                  dictionary1.Add(350, 181.0);
                  dictionary1.Add(400, 206.0);
                  dictionary1.Add(450, 232.0);
                  dictionary1.Add(500, 257.0);
                  dictionary1.Add(550, 283.0);
                  dictionary1.Add(600, 308.0);
                  dictionary1.Add(650, 333.0);
                  dictionary1.Add(700, 359.0);
                  dictionary1.Add(850, 435.0);
                  double num27 = Convert.ToDouble(StandardSupport.DetailProfile(this.SupportParams["BI"]).Split('x')[1]);
                  int int32_5 = Convert.ToInt32(this.SupportParams["Dn"]);
                  double num28 = dictionary1[int32_5];
                  double num29 = Convert.ToDouble(this.SupportParams["F1"]) + num28 + 85.0;
                  double num30 = Convert.ToDouble(this.SupportParams["F1"]) + num28 + 85.0;
                  double num31 = 2.0 * num28 + 2.0 * num27;
                  double num32 = 2.0 * num28 + 2.0 * num27;
                  string str11 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
                  string str12 = StandardSupport.BeamProfile("17");
                  string[] strArray34 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str11,
                    this.frame_material,
                    num29.ToString(),
                    ""
                  };
                  string[] strArray35 = new string[6]
                  {
                    "F2",
                    "FRAMEWORK",
                    str11,
                    this.frame_material,
                    num30.ToString(),
                    ""
                  };
                  string[] strArray36 = new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    str12,
                    this.frame_material,
                    num31.ToString(),
                    ""
                  };
                  string[] strArray37 = new string[6]
                  {
                    "F4",
                    "FRAMEWORK",
                    str12,
                    this.frame_material,
                    num32.ToString(),
                    ""
                  };
                  this.bom_contents.Add(strArray34);
                  this.bom_contents.Add(strArray35);
                  this.bom_contents.Add(strArray36);
                  this.bom_contents.Add(strArray37);
                  return;
                case "RS3":
                  goto label_67;
                case "RC3":
                  break;
                default:
                  return;
              }
              break;
            case '4':
              switch (standardName1)
              {
                case "RS4":
                  goto label_67;
                case "RC4":
                  string str13 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
                  double num33 = Convert.ToDouble(this.SupportParams["A"]);
                  int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
                  double num34 = Convert.ToDouble(this.SupportParams["A1"]);
                  double num35 = 50.0; // B1 Overhang 고정
                  int int32_6 = Convert.ToInt32(this.SupportParams["TY"]);
                  double num36 = Math.Ceiling(num33 + num34);
                  double num37 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                  string str14 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
                  string str15 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
                  string[] strArray38 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str13,
                    this.frame_material,
                    num36.ToString(),
                    ""
                  };
                  string[] strArray39 = new string[6]
                  {
                    "F2",
                    "FRAMEWORK",
                    str13,
                    this.frame_material,
                    num37.ToString(),
                    ""
                  };
                  string[] strArray40 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str14,
                    this.frame_material,
                    "1",
                    ""
                  };
                  string[] strArray41 = new string[6]
                  {
                    "J1",
                    "SET ANCHOR",
                    str15,
                    this.frame_material,
                    "4ea",
                    ""
                  };
                  this.bom_contents.Add(strArray38);
                  this.bom_contents.Add(strArray39);
                  if (int32_6 == -2)
                  {
                    double num38 = Convert.ToDouble(StandardSupport.DetailProfile(this.SupportParams["BI"]).Split('x')[0]);
                    double num39 = Math.Ceiling((num36 - 10.0 - num38 - num35) / (Math.Sqrt(2.0) / 2.0));
                    this.bom_contents.Add(new string[6]
                    {
                      "F3",
                      "FRAMEWORK",
                      StandardSupport.BeamProfile("110"),
                      this.frame_material,
                      num39.ToString(),
                      ""
                    });
                  }
                  this.bom_contents.Add(strArray40);
                  this.bom_contents.Add(strArray41);
                  return;
                default:
                  return;
              }
            case '5':
              switch (standardName1)
              {
                case "RS5":
                  this.TryAddRs5OrRs6FrameBom();
                  return;
                case "RC5":
                  break;
                default:
                  return;
              }
              break;
            case '6':
              switch (standardName1)
              {
                case "RS6":
                  this.TryAddRs5OrRs6FrameBom();
                  return;
                case "RC6":
                  break;
                default:
                  return;
              }
              break;
            case '7':
              switch (standardName1)
              {
                case "RS7":
                  goto label_67;
                case "RC7":
                  string str16 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
                  double num40 = Convert.ToDouble(this.SupportParams["A"]);
                  int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
                  double num41 = Convert.ToDouble(this.SupportParams["A1"]);
                  double num42 = 150.0; // RC7 B1 Overhang 하드코딩
                  double num43 = Math.Ceiling(num40 + num41);
                  double num44 = Math.Ceiling((num43 - num42) / (Math.Sqrt(2.0) / 2.0));
                  string str17 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
                  string str18 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
                  string[] strArray42 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str16,
                    this.frame_material,
                    num43.ToString(),
                    ""
                  };
                  string[] strArray43 = new string[6]
                  {
                    "F2",
                    "FRAMEWORK",
                    str16,
                    this.frame_material,
                    num44.ToString(),
                    ""
                  };
                  string[] strArray44 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str17,
                    this.frame_material,
                    "2",
                    ""
                  };
                  string[] strArray45 = new string[6]
                  {
                    "J1",
                    "SET ANCHOR",
                    str18,
                    this.frame_material,
                    "8ea",
                    ""
                  };
                  this.bom_contents.Add(strArray42);
                  this.bom_contents.Add(strArray43);
                  this.bom_contents.Add(strArray44);
                  this.bom_contents.Add(strArray45);
                  return;
                default:
                  return;
              }
            case '8':
              switch (standardName1)
              {
                case "RS8":
                  goto label_67;
                case "RC8":
                  string str19 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
                  double num45 = Convert.ToDouble(this.SupportParams["A"]);
                  int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
                  double num46 = Convert.ToDouble(this.SupportParams["A1"]);
                  double num47 = Math.Ceiling(num45 + num46);
                  string str20 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
                  string str21 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
                  string[] strArray46 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str19,
                    this.frame_material,
                    num47.ToString(),
                    ""
                  };
                  string[] strArray47 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str20,
                    this.frame_material,
                    "1",
                    ""
                  };
                  string[] strArray48 = new string[6]
                  {
                    "J1",
                    "SET ANCHOR",
                    str21,
                    this.frame_material,
                    "4ea",
                    ""
                  };
                  this.bom_contents.Add(strArray46);
                  this.bom_contents.Add(strArray47);
                  this.bom_contents.Add(strArray48);
                  return;
                default:
                  return;
              }
            case '9':
              switch (standardName1)
              {
                case "RS9":
                  goto label_67;
                case "RC9":
                  string str22 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
                  double num48 = Convert.ToDouble(this.SupportParams["A"]);
                  int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
                  double num49 = Convert.ToDouble(this.SupportParams["A1"]);
                  double num50 = Math.Ceiling(num48 + num49);
                  double num51 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
                  double num52 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F3"]));
                  string str23 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
                  string str24 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
                  string[] strArray49 = new string[6]
                  {
                    "F1",
                    "FRAMEWORK",
                    str22,
                    this.frame_material,
                    num50.ToString(),
                    ""
                  };
                  string[] strArray50 = new string[6]
                  {
                    "F2",
                    "FRAMEWORK",
                    str22,
                    this.frame_material,
                    num51.ToString(),
                    ""
                  };
                  string[] strArray51 = new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    str22,
                    this.frame_material,
                    num52.ToString(),
                    ""
                  };
                  string[] strArray52 = new string[6]
                  {
                    "P1",
                    "BASE PLATE",
                    str23,
                    this.frame_material,
                    "2",
                    ""
                  };
                  string[] strArray53 = new string[6]
                  {
                    "J1",
                    "SET ANCHOR",
                    str24,
                    this.frame_material,
                    "8ea",
                    ""
                  };
                  this.bom_contents.Add(strArray49);
                  this.bom_contents.Add(strArray50);
                  this.bom_contents.Add(strArray51);
                  this.bom_contents.Add(strArray52);
                  this.bom_contents.Add(strArray53);
                  return;
                default:
                  return;
              }
            case 'S':
              if (!(standardName1 == "TRS"))
                return;
              Dictionary<int, double[]> dictionary2 = new Dictionary<int, double[]>();
              dictionary2.Add(50, new double[2]
              {
                65.0,
                175.0
              });
              dictionary2.Add(80, new double[2]
              {
                80.0,
                190.0
              });
              dictionary2.Add(100, new double[2]
              {
                90.0,
                200.0
              });
              dictionary2.Add(125, new double[2]
              {
                100.0,
                210.0
              });
              dictionary2.Add(150, new double[2]
              {
                110.0,
                220.0
              });
              dictionary2.Add(200, new double[2]
              {
                135.0,
                245.0
              });
              dictionary2.Add(250, new double[2]
              {
                165.0,
                275.0
              });
              dictionary2.Add(300, new double[2]
              {
                190.0,
                300.0
              });
              string str25 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
              StandardSupport.BeamProfile("16");
              double num53 = Convert.ToDouble(this.SupportParams["A"]);
              double num54 = Convert.ToDouble(this.SupportParams["T"]);
              int int32_7 = Convert.ToInt32(this.SupportParams["Dn"]);
              double num55 = num54 + dictionary2[int32_7][0];
              double num56 = num54 + dictionary2[int32_7][1];
              string str26 = StandardSupport.TrunnionProfile(int32_7).ToString() + "A";
              double num57 = num53 + num55 + 75.0;
              double num58 = num53 + num55 + 75.0;
              double num59 = num55 * 2.0 + 150.0;
              double num60 = num56;
              double num61 = num56;
              string[] strArray54 = new string[6]
              {
                "F1",
                "FRAMEWORK",
                str25,
                this.frame_material,
                num57.ToString(),
                ""
              };
              string[] strArray55 = new string[6]
              {
                "F2",
                "FRAMEWORK",
                str25,
                this.frame_material,
                num58.ToString(),
                ""
              };
              string[] strArray56 = new string[6]
              {
                "F3",
                "FRAMEWORK",
                str25,
                this.frame_material,
                num59.ToString(),
                ""
              };
              string[] strArray57 = new string[6]
              {
                "Q1",
                "FRAMEWORK",
                str26,
                this.frame_material,
                num60.ToString(),
                ""
              };
              string[] strArray58 = new string[6]
              {
                "Q2",
                "FRAMEWORK",
                str26,
                this.frame_material,
                num61.ToString(),
                ""
              };
              string[] strArray59 = new string[6]
              {
                "P1",
                "PLATE",
                "PIPE PAD",
                this.frame_material,
                "2",
                ""
              };
              this.bom_contents.Add(strArray54);
              this.bom_contents.Add(strArray55);
              this.bom_contents.Add(strArray56);
              this.bom_contents.Add(strArray57);
              this.bom_contents.Add(strArray58);
              this.bom_contents.Add(strArray59);
              return;
            default:
              return;
          }
          string str27 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
          double num62 = Convert.ToDouble(this.SupportParams["A"]);
          int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
          double num63 = Convert.ToDouble(this.SupportParams["A1"]);
          double num64 = Math.Ceiling(num62 + num63);
          double num65 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
          if (this.StandardName == "RC1")
          {
            double num66 = Convert.ToDouble(StandardSupport.DetailProfile(this.SupportParams["BI"]).Split('x')[0]);
            if (this.SupportParams["BI"] != "210")
              num65 += num66;
          }
          string str28 = StandardSupport.PlateProfile(this.StandardName, this.SupportParams["BI"]);
          string str29 = StandardSupport.AnchorBoltDetail(this.StandardName, this.SupportParams["BI"]);
          string[] strArray60 = new string[6]
          {
            "F1",
            "FRAMEWORK",
            str27,
            this.frame_material,
            num64.ToString(),
            ""
          };
          string[] strArray61 = new string[6]
          {
            "F2",
            "FRAMEWORK",
            str27,
            this.frame_material,
            num65.ToString(),
            ""
          };
          string[] strArray62 = new string[6]
          {
            "P1",
            "BASE PLATE",
            str28,
            this.frame_material,
            "1",
            ""
          };
          string[] strArray63 = new string[6]
          {
            "J1",
            "SET ANCHOR",
            str29,
            this.frame_material,
            "4ea",
            ""
          };
          string standardName2 = this.StandardName;
          if (standardName2 == "RC5" || standardName2 == "RC6")
          {
            strArray62[4] = "2";
            strArray63[4] = "8ea";
          }
          this.bom_contents.Add(strArray60);
          this.bom_contents.Add(strArray61);
          this.bom_contents.Add(strArray62);
          this.bom_contents.Add(strArray63);
          return;
        case 4:
          switch (standardName1[3])
          {
            case '0':
              if (!(standardName1 == "RS10"))
                return;
              goto label_67;
            case '1':
              if (!(standardName1 == "RS11"))
                return;
              break;
            case '2':
              return;
            case '3':
              if (!(standardName1 == "RS13"))
                return;
              string str30 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
              double num67 = Convert.ToDouble(this.SupportParams["A"]);
              double num68 = Convert.ToDouble(this.SupportParams["A1"]);
              double num69 = Convert.ToDouble(this.SupportParams["P1"]);
              double num70 = Math.Ceiling(num67 + num68);
              double num71 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
              double num72 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F3"]));
              double num73 = num71 + num69 - 100.0;
              double num74 = num72 + num69 - 100.0;
              double num75 = Math.Sqrt(3.0) / 2.0;
              double num76 = num73 / num75;
              double num77 = num74 / (Math.Sqrt(3.0) / 2.0);
              string[] strArray64 = new string[6]
              {
                "F1",
                "FRAMEWORK",
                str30,
                this.frame_material,
                num70.ToString(),
                ""
              };
              string[] strArray65 = new string[6]
              {
                "F2",
                "FRAMEWORK",
                str30,
                this.frame_material,
                num71.ToString(),
                ""
              };
              string[] strArray66 = new string[6]
              {
                "F3",
                "FRAMEWORK",
                str30,
                this.frame_material,
                num72.ToString(),
                ""
              };
              string[] strArray67 = new string[6]
              {
                "F4",
                "FRAMEWORK",
                str30,
                this.frame_material,
                num76.ToString(),
                ""
              };
              string[] strArray68 = new string[6]
              {
                "F5",
                "FRAMEWORK",
                str30,
                this.frame_material,
                num77.ToString(),
                ""
              };
              this.bom_contents.Add(strArray64);
              this.bom_contents.Add(strArray65);
              this.bom_contents.Add(strArray66);
              this.bom_contents.Add(strArray67);
              this.bom_contents.Add(strArray68);
              return;
            case '4':
              if (!(standardName1 == "RS14"))
                return;
              string str31 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
              double num78 = Convert.ToDouble(this.SupportParams["A1"]);
              double num79 = Convert.ToDouble(this.SupportParams["A2"]);
              int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
              double num80 = Math.Ceiling(num79 + num78);
              this.bom_contents.Add(new string[6]
              {
                "F1",
                "FRAMEWORK",
                str31,
                this.frame_material,
                num80.ToString(),
                ""
              });
              return;
            case '5':
              if (!(standardName1 == "RS15"))
                return;
              string supportParam3 = this.SupportParams["BI"];
              string str32 = StandardSupport.BeamProfile(supportParam3);
              int int32_8 = Convert.ToInt32(this.SupportParams["TY"]);
              double num81 = Convert.ToDouble(this.SupportParams["A1"]);
              double num82 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A2"]) + num81);
              double num83 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
              string[] strArray69 = new string[6]
              {
                "F1",
                "FRAMEWORK",
                str32,
                this.frame_material,
                num82.ToString(),
                ""
              };
              string[] strArray70 = new string[6]
              {
                "F2",
                "FRAMEWORK",
                str32,
                this.frame_material,
                num83.ToString(),
                ""
              };
              this.bom_contents.Add(strArray69);
              this.bom_contents.Add(strArray70);
              if (int32_8 != -2)
                return;
              double num84 = Convert.ToDouble(this.SupportParams["L1"]);
              double num85 = Convert.ToDouble(StandardSupport.DetailProfile(supportParam3).Split('x')[0]);
              switch (supportParam3)
              {
                case "110":
                  this.bom_contents.Add(new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    "ANGLE A7",
                    this.frame_material,
                    Math.Ceiling((num82 - num84 - 10.0) / (Math.Sqrt(2.0) / 2.0)).ToString(),
                    ""
                  });
                  return;
                case "215":
                case "220":
                  this.bom_contents.Add(new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    "ANGLE A7",
                    this.frame_material,
                    Math.Ceiling((num82 - num84 - num85 - 10.0) / (Math.Sqrt(2.0) / 2.0)).ToString(),
                    ""
                  });
                  return;
                case "315":
                  string[] strArray71 = new string[6]
                  {
                    "F3",
                    "FRAMEWORK",
                    "ANGLE A7",
                    this.frame_material,
                    Math.Ceiling((num82 - num84 - num85 - 10.0) / (Math.Sqrt(2.0) / 2.0)).ToString(),
                    ""
                  };
                  string[] strArray72 = new string[6]
                  {
                    "P1",
                    "STIFFENER",
                    "130x62x10",
                    this.frame_material,
                    "1",
                    "x4 Quan."
                  };
                  this.bom_contents.Add(strArray71);
                  this.bom_contents.Add(strArray72);
                  return;
                default:
                  return;
              }
            default:
              return;
          }
          break;
        case 5:
          switch (standardName1[4])
          {
            case 'A':
              if (!(standardName1 == "RS12A"))
                return;
              goto label_71;
            case 'B':
              if (!(standardName1 == "RS12B"))
                return;
              goto label_67;
            case 'C':
              if (!(standardName1 == "RS12C"))
                return;
              break;
            case 'D':
              if (!(standardName1 == "RS12D"))
                return;
              goto label_66;
            default:
              return;
          }
          break;
        case 6:
          if (!(standardName1 == "TYPE02"))
            return;
          string[] strArray73 = new string[6]
          {
            "F1",
            "FRAMEWORK",
            "CHANEL MS 41x41",
            "CS",
            "1",
            ""
          };
          string[] strArray74 = new string[6]
          {
            "F2",
            "FRAMEWORK",
            "CHANEL MS 41x41",
            "CS",
            "1",
            ""
          };
          string[] strArray75 = new string[6]
          {
            "F3",
            "FRAMEWORK",
            "CHANEL MS 41x41",
            "CS",
            "1",
            ""
          };
          string[] strArray76 = new string[6]
          {
            "P1",
            "BASE PLATE",
            "110x110x6mm",
            "CS",
            "2",
            ""
          };
          this.bom_contents.Add(strArray73);
          this.bom_contents.Add(strArray74);
          this.bom_contents.Add(strArray75);
          this.bom_contents.Add(strArray76);
          return;
        default:
          return;
      }
label_56:
      string str33 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
      double num86 = Convert.ToDouble(this.SupportParams["A"]);
      double num87 = 0.0;
      if (string.Equals(this.StandardName, "RS11", StringComparison.OrdinalIgnoreCase))
        num87 = Convert.ToDouble(this.SupportParams["A1"]);
      else if (!string.Equals(this.StandardName, "RS1", StringComparison.OrdinalIgnoreCase))
        num87 = PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0;
      double num88 = Math.Ceiling(num86 + num87);
      this.bom_contents.Add(new string[6]
      {
        "F1",
        "FRAMEWORK",
        str33,
        this.frame_material,
        num88.ToString(),
        ""
      });
      int int32_9 = Convert.ToInt32(this.SupportParams["TY"]);
      if (!(this.StandardName == "GD1") || int32_9 != -2)
        return;
      string str34 = StandardSupport.PlateProfile("GD1", this.SupportParams["BI"]);
      string str35 = StandardSupport.AnchorBoltDetail("GD1", this.SupportParams["BI"]);
      string[] strArray77 = new string[6]
      {
        "P1",
        "BASE PLATE",
        str34,
        this.frame_material,
        "1",
        ""
      };
      string[] strArray78 = new string[6]
      {
        "J1",
        "SET ANCHOR",
        str35,
        this.frame_material,
        "4ea",
        ""
      };
      this.bom_contents.Add(strArray77);
      this.bom_contents.Add(strArray78);
      return;
label_66:
      string str36 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
      double num89 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A"]) + (PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0));
      double num90 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
      string[] strArray79 = new string[6]
      {
        "F1",
        "FRAMEWORK",
        str36,
        this.frame_material,
        num89.ToString(),
        ""
      };
      string[] strArray80 = new string[6]
      {
        "F2",
        "FRAMEWORK",
        str36,
        this.frame_material,
        num90.ToString(),
        ""
      };
      this.bom_contents.Add(strArray79);
      this.bom_contents.Add(strArray80);
      return;
label_67:
      string str37 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
      double num91 = Convert.ToDouble(this.SupportParams["A"]);
      int32_1 = Convert.ToInt32(this.SupportParams["Dn"]);
      double num92 = Convert.ToDouble(this.SupportParams["A1"]);
      double num93 = Math.Ceiling(num91 + num92);
      double num94 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
      if (this.StandardName == "RS10")
      {
        double num95 = Convert.ToDouble(StandardSupport.DetailProfile(this.SupportParams["BI"]).Split('x')[0]);
        if (this.SupportParams["BI"] != "210")
          num94 += num95;
      }
      string[] strArray81 = new string[6]
      {
        "F1",
        "FRAMEWORK",
        str37,
        this.frame_material,
        num93.ToString(),
        ""
      };
      string[] strArray82 = new string[6]
      {
        "F2",
        "FRAMEWORK",
        str37,
        this.frame_material,
        num94.ToString(),
        ""
      };
      this.bom_contents.Add(strArray81);
      this.bom_contents.Add(strArray82);
      return;
label_71:
      string str38 = StandardSupport.BeamProfile(this.SupportParams["BI"]);
      double num96 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A"]) + Convert.ToDouble(this.SupportParams["A1"]));
      double num97 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"]));
      double num98 = Math.Ceiling(Convert.ToDouble(this.SupportParams["F3"]));
      string[] strArray83 = new string[6]
      {
        "F1",
        "FRAMEWORK",
        str38,
        this.frame_material,
        num96.ToString(),
        ""
      };
      string[] strArray84 = new string[6]
      {
        "F2",
        "FRAMEWORK",
        str38,
        this.frame_material,
        num97.ToString(),
        ""
      };
      string[] strArray85 = new string[6]
      {
        "F3",
        "FRAMEWORK",
        str38,
        this.frame_material,
        num98.ToString(),
        ""
      };
      this.bom_contents.Add(strArray83);
      this.bom_contents.Add(strArray84);
      this.bom_contents.Add(strArray85);
    }

    // RS5/RS6은 F2/F3가 아니라 Ha/Hb가 실제 프레임 높이다. 하나라도 없으면 부분 BOM을 만들지 않는다.
    private void TryAddRs5OrRs6FrameBom()
    {
      string[] requiredKeys = new string[] { "A", "A1", "Ha", "Hb", "BI" };
      List<string> missing = new List<string>();
      List<string> rawValues = new List<string>();
      for (int i = 0; i < requiredKeys.Length; i++)
      {
        string key = requiredKeys[i];
        string value = null;
        bool found = this.SupportParams != null && this.SupportParams.TryGetValue(key, out value);
        rawValues.Add(key + "=" + (found ? (value ?? "(null)") : "(missing)"));
        if (!found || string.IsNullOrWhiteSpace(value))
          missing.Add(key);
      }

      if (missing.Count > 0)
      {
        PlantOrthoView.FileDiag("BOM RS5/6 frame skip std=" + (this.StandardName ?? "unknown")
          + " missing=" + string.Join(",", missing) + " raw={" + string.Join(" | ", rawValues) + "}");
        return;
      }

      string profile = StandardSupport.BeamProfile(this.SupportParams["BI"]);
      double f1 = Math.Ceiling(Convert.ToDouble(this.SupportParams["A"]) + Convert.ToDouble(this.SupportParams["A1"]));
      double f2 = Math.Ceiling(Convert.ToDouble(this.SupportParams["Ha"]));
      double f3 = Math.Ceiling(Convert.ToDouble(this.SupportParams["Hb"]));
      this.bom_contents.Add(new string[6] { "F1", "FRAMEWORK", profile, this.frame_material, f1.ToString(), "" });
      this.bom_contents.Add(new string[6] { "F2", "FRAMEWORK", profile, this.frame_material, f2.ToString(), "" });
      this.bom_contents.Add(new string[6] { "F3", "FRAMEWORK", profile, this.frame_material, f3.ToString(), "" });
    }

    public void ExportMTO(string directory)
    {
      this.support = this.pnp_db.Tables["Support"];
      this.Supports = new Dictionary<string, Dictionary<string, List<ObjectId>>>();
      this.Frameworks = new Dictionary<string, List<ObjectId>>();
      this.Frameworks.Add("FRAMEWORK", new List<ObjectId>());
      this.FrameworkCurrentDrawingData();
      List<StringCollection> to_excel_values = new List<StringCollection>();
      foreach (ObjectId id in this.Frameworks["FRAMEWORK"])
      {
        StringCollection stringCollection1 = new StringCollection()
        {
          "ShortDescription",
          "SupportName",
          "MemberName"
        };
        StringCollection properties = this.dl_manager.GetProperties(id, stringCollection1, true);
        this.SupportParams = this.GetSupportDimension(id);
        this.StandardName = properties[0];
        string str1 = properties[1];
        string str2 = properties[2];
        this.bom_contents.Clear();
        this.FrameBOM();
        foreach (string[] bomContent in this.bom_contents)
        {
          StringCollection stringCollection2 = new StringCollection();
          stringCollection2.Add(str1);
          stringCollection2.Add(str2);
          for (int index = 0; index < bomContent.Length; ++index)
          {
            if (index != 5)
              stringCollection2.Add(bomContent[index]);
          }
          stringCollection2.Add(this.StandardName);
          to_excel_values.Add(stringCollection2);
        }
      }
      this.ExcelFormat(to_excel_values, directory);
    }

    private void FrameworkCurrentDrawingData()
    {
      Document document = this.GetDocument();
      string str1 = ((IEnumerable<string>) document.Name.Split('\\')).Last<string>();
      PnPTable table = this.pnp_db.Tables["PnPDrawings"];
      int num1 = 0;
      foreach (PnPRow pnProw in table.Select())
      {
        if (((PnPAbstractRow) pnProw)["Dwg Name"].ToString() == str1)
        {
          num1 = Convert.ToInt32(((PnPAbstractRow) pnProw)["PnPID"]);
          break;
        }
      }
      List<int> intList = new List<int>();
      foreach (PnPRow pnProw in this.pnp_db.Tables["PnPDataLinks"].Select())
      {
        int int32 = Convert.ToInt32(((PnPAbstractRow) pnProw)["DwgId"]);
        string str2 = ((PnPAbstractRow) pnProw)["RowClassName"].ToString();
        int num2 = num1;
        if (int32 == num2 && str2 == "Support")
          intList.Add(Convert.ToInt32(((PnPAbstractRow) pnProw)["RowId"]));
      }
      foreach (int num3 in intList)
      {
        ObjectId objectId = this.dl_manager.MakeAcDbObjectId(((IEnumerable<PpObjectId>) this.dl_manager.FindAcPpObjectIds(num3)).ToList<PpObjectId>()[0], document.Database);
        StringCollection stringCollection = new StringCollection()
        {
          "SupportType"
        };
        switch (this.dl_manager.GetProperties(objectId, stringCollection, true)[0])
        {
          case "FRAMEWORK":
            this.Frameworks["FRAMEWORK"].Add(objectId);
            continue;
          case "BASEPLATE":
          case "STIFFENER":
            this.Frameworks["PLATE"].Add(objectId);
            continue;
          default:
            continue;
        }
      }
    }

    private StringCollection AddRowValues(
      string value1,
      string value2,
      string value3,
      string value4,
      string value5,
      string value6,
      string value7)
    {
      return new StringCollection()
      {
        value1,
        value2,
        value3,
        value4,
        value5,
        value6,
        value7
      };
    }

    // MTO 시트 헤더. 열 순서가 곧 to_excel_values의 열 순서다.
    private static readonly string[] MtoHeaders = new string[]
    { "Name", "Member", "Sub Member", "Member Type", "Profile", "Material", "Length (mm)", "Standard Type" };

    // OpenXML(ClosedXML)로 직접 .xlsx를 만든다. 과거에는 Excel COM(CLSID 00024500)을 띄웠기 때문에
    // 고객 PC에 데스크톱 Excel이 설치돼 있어야 했고, 예외 시 COM 객체가 해제되지 않아 Excel
    // 프로세스가 유령으로 남을 수 있었다. PFO(BomExcelExporter)와 같은 규약으로 맞춘다.
    private void ExcelFormat(List<StringCollection> to_excel_values, string directory)
    {
      string fileName = null;
      try
      {
        string drawing = ((IEnumerable<string>) this.doc.Name.Split('\\')).Last<string>().Replace(".dwg", "");
        string stamp = DateTime.Now.ToString("yyMMdd-HH-mm-ss");
        fileName = Path.Combine(directory ?? string.Empty, "Support_MTO_for_" + drawing + "_drawing_" + stamp + ".xlsx");

        using (XLWorkbook workbook = new XLWorkbook())
        {
          IXLWorksheet sheet = workbook.Worksheets.Add("MTO");
          for (int c = 0; c < MtoHeaders.Length; c++)
          {
            IXLCell headerCell = sheet.Cell(1, c + 1);
            headerCell.Value = MtoHeaders[c];
            headerCell.Style.Font.Bold = true;
          }

          for (int r = 0; r < to_excel_values.Count; r++)
          {
            StringCollection row = to_excel_values[r];
            if (row == null) continue;
            for (int c = 0; c < row.Count; c++)
            {
              // 규격·프로파일에 "1-2" 같은 값이 있어 날짜로 자동 변환되면 안 된다.
              // 과거에는 선행 작은따옴표로 강제했으나, 셀 서식을 텍스트로 지정하는 편이 정확하다.
              IXLCell cell = sheet.Cell(r + 2, c + 1);
              cell.Style.NumberFormat.Format = "@";
              cell.Value = row[c] ?? string.Empty;
            }
          }

          sheet.Columns().AdjustToContents();
          workbook.SaveAs(fileName);
        }

        PlantOrthoView.FileDiag("MTO 내보내기 완료 rows=" + to_excel_values.Count + " path=" + fileName);
        Application.ShowAlertDialog("MTO를 저장했습니다.\n" + fileName);
      }
      catch (System.Exception ex)
      {
        PlantOrthoView.FileDiag("MTO 내보내기 실패 path=" + (fileName ?? "(미정)") + ": " + ex.GetType().Name + ": " + ex.Message);
        Application.ShowAlertDialog("MTO 저장에 실패했습니다.\n" + ex.Message);
      }
    }
  }
}
