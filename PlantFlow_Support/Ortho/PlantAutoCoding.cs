using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.PnP3dObjects;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace PlantFlow_Support
{
  public class PlantAutoCoding : PSUtil
  {
    public PortCollection PPorts;

    public Dictionary<string, string> SupportParams { get; set; }

    public Dictionary<string, string> Properties { get; set; }

    public string SupportCoding(string std_type)
    {
      string coding = "";
      if (std_type != null)
      {
         // System.Windows.Forms.MessageBox.Show("SupportCoding Input: '" + std_type + "'", "Debug");
         switch (std_type.Length)
        {
          case 1:
            if (std_type == "S")
            {
              this.S(ref coding);
              goto label_59;
            }
            else
              goto label_59;
          case 2:
            switch (std_type[0])
            {
              case 'F':
                if (std_type == "FS")
                {
                  this.FS(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case 'S':
                if (std_type == "SG")
                {
                  this.SG(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case 'T':
                if (std_type == "TR")
                {
                  this.TR(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case 'U':
                if (std_type == "UB")
                  break;
                goto label_59;
              default:
                goto label_59;
            }
            break;
          case 3:
            switch (std_type[2])
            {
              case '1':
                switch (std_type)
                {
                  case "GD1":
                  case "RS1":
                    this.GD1(ref coding);
                    goto label_59;
                  case "RC1":
                    this.RC1(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '2':
                switch (std_type)
                {
                  case "RS2":
                    this.RS2(ref coding);
                    goto label_59;
                  case "GD2":
                    this.GD2(ref coding);
                    goto label_59;
                  case "RC2":
                    this.RC2(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '3':
                switch (std_type)
                {
                  case "GD3":
                    this.GD3(ref coding);
                    goto label_59;
                  case "RS3":
                    this.RS3(ref coding);
                    goto label_59;
                  case "RC3":
                    this.RC3(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '4':
                switch (std_type)
                {
                  case "RS4":
                    this.RS4(ref coding);
                    goto label_59;
                  case "RC4":
                    this.RC4(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '5':
                switch (std_type)
                {
                  case "RS5":
                    this.RS5(ref coding);
                    goto label_59;
                  case "RC5":
                    this.RC5(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '6':
                switch (std_type)
                {
                  case "RS6":
                    this.RS6(ref coding);
                    goto label_59;
                  case "RC6":
                    this.RC6(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '7':
                switch (std_type)
                {
                  case "RS7":
                    this.RS7(ref coding);
                    goto label_59;
                  case "RC7":
                    this.RC7(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '8':
                switch (std_type)
                {
                  case "RS8":
                    this.RS8(ref coding);
                    goto label_59;
                  case "RC8":
                    this.RC8(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case '9':
                switch (std_type)
                {
                  case "RS9":
                    this.RS9(ref coding);
                    goto label_59;
                  case "RC9":
                    this.RC9(ref coding);
                    goto label_59;
                  default:
                    goto label_59;
                }
              case 'D':
                if (std_type == "UBD")
                  break;
                goto label_59;
              case 'S':
                if (std_type == "TRS")
                {
                  this.TRS(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              default:
                goto label_59;
            }
            break;
          case 4:
            switch (std_type[3])
            {
              case '0':
                if (std_type == "RS10")
                {
                  this.RS10(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case '1':
                if (std_type == "RS11")
                {
                  this.RS11(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case '2':
                if (std_type == "RS12")
                {
                  this.RS12(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case '3':
                if (std_type == "RS13")
                {
                  this.RS13(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case '4':
                if (std_type == "RS14")
                {
                  this.RS14(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              case 'A':
                if (std_type == "RC1A")
                {
                  System.Windows.Forms.MessageBox.Show("RC1A Logic Called properly!", "Debug");
                  this.RC1(ref coding);
                  goto label_59;
                }
                goto label_59;
              case '5':
                if (std_type == "RS15")
                {
                  this.RS15(ref coding);
                  goto label_59;
                }
                else
                  goto label_59;
              default:
                goto label_59;
            }
          default:
            goto label_59;
        }
        this.UB(ref coding);
      }
label_59:
      return coding;
    }

    private void S(ref string coding)
    {
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-H" + Math.Ceiling(Convert.ToDouble(this.SupportParams["H"])).ToString();
      coding = property + "-" + str1 + str2;
    }

    private void SG(ref string coding)
    {
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-H" + Math.Ceiling(Convert.ToDouble(this.SupportParams["H"])).ToString();
      coding = property + "-" + str1 + str2 + "-A2";
    }

    private void UB(ref string coding)
    {
      string property = this.Properties["Tag"].Replace("?", "");
      string str = PSUtil.MetricImperial(this.Properties["Size"]);
      coding = property + "-" + str;
    }

    private void GD1(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-E" + this.GetEParam(2);
      string str4 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void GD2(ref string coding)
    {
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + this.SupportParams["F1"];
      string str3 = "-L" + (Convert.ToDouble(this.SupportParams["B"]) + Convert.ToDouble(this.SupportParams["D"])).ToString();
      string str4 = "-B" + this.SupportParams["B"];
      string str5 = "-D" + this.SupportParams["D"];
      string str6 = "-E" + this.GetEParam(0);
      string str7 = "-C15-A6";
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6 + str7;
    }

    private void GD3(ref string coding)
    {
      Dictionary<string, double> dictionary = new Dictionary<string, double>()
      {
        {
          "150",
          87.0
        },
        {
          "200",
          113.0
        },
        {
          "250",
          150.0
        },
        {
          "300",
          165.0
        },
        {
          "350",
          181.0
        },
        {
          "400",
          206.0
        },
        {
          "450",
          232.0
        },
        {
          "500",
          257.0
        },
        {
          "550",
          283.0
        },
        {
          "600",
          308.0
        },
        {
          "650",
          333.0
        },
        {
          "700",
          359.0
        },
        {
          "850",
          435.0
        }
      };
      double num1 = Convert.ToDouble(StandardSupport.DetailProfile(this.SupportParams["BI"]).Split('x')[1]);
      double num2 = 75.0;
      double num3 = 10.0;
      double num4 = Convert.ToDouble(this.SupportParams["F1"]);
      string supportParam = this.SupportParams["Dn"];
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num4 + dictionary[supportParam] + num2 + num3).ToString();
      string str3 = "-B" + Math.Ceiling(dictionary[supportParam] * 2.0 + num1 * 2.0).ToString();
      string str4 = "-C" + dictionary[supportParam].ToString();
      string str5 = "-E" + this.GetEParam(0);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6;
    }

    private void RS2(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-E" + this.GetEParam(3);
      string str4 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void RS3(ref string coding)
    {
      string supportParam1 = this.SupportParams["BI"];
      string supportParam2 = this.SupportParams["TY"];
      double num1 = Convert.ToDouble(StandardSupport.DetailProfile(supportParam1).Split('x')[0]);
      double num2 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num3 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num3).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      if (supportParam2 == "-2" || supportParam2 == "-4")
        str2 = "-A" + Math.Ceiling(num2 + num3 - num1).ToString();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS4(ref string coding)
    {
      string supportParam1 = this.SupportParams["BI"];
      string supportParam2 = this.SupportParams["TY"];
      double num1 = Convert.ToDouble(StandardSupport.DetailProfile(supportParam1).Split('x')[0]);
      double num2 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num3 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num3).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      if (supportParam2 == "-2" || supportParam2 == "-4")
        str2 = "-A" + Math.Ceiling(num2 + num3 - num1).ToString();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS5(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a1 = Convert.ToDouble(this.SupportParams["F2"]);
      double a2 = Convert.ToDouble(this.SupportParams["F3"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H1" + Math.Ceiling(a1).ToString();
      string str4 = "-H2" + Math.Ceiling(a2).ToString();
      string str5 = "-E" + this.GetEParam(3);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6;
    }

    private void RS6(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a1 = Convert.ToDouble(this.SupportParams["F2"]);
      double a2 = Convert.ToDouble(this.SupportParams["F3"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H1" + Math.Ceiling(a1).ToString();
      string str4 = "-H2" + Math.Ceiling(a2).ToString();
      string str5 = "-E" + this.GetEParam(3);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6;
    }

    private void RS7(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string supportParam = this.SupportParams["TY"];
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      if (supportParam == "-2")
        str3 = "-H" + Math.Ceiling(a - 100.0).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS8(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToDouble(this.SupportParams["B1"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string supportParam = this.SupportParams["TY"];
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      if (supportParam == "-2")
        str3 = "-H" + Math.Ceiling(a - 100.0).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS9(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string supportParam = this.SupportParams["TY"];
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      if (supportParam == "-2")
        str3 = "-H" + Math.Ceiling(a - 100.0).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS10(ref string coding)
    {
      double a1 = Convert.ToDouble(this.SupportParams["A"]);
      double a2 = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToDouble(this.SupportParams["B1"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double a3 = Convert.ToDouble(this.SupportParams["A1"]);
      string supportParam = this.SupportParams["TY"];
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-B" + Math.Ceiling(a1).ToString();
      string str3 = "-C" + Math.Ceiling(a3).ToString();
      string str4 = "-H" + Math.Ceiling(a2).ToString();
      if (supportParam == "-2")
        str4 = "-H" + Math.Ceiling(a2 - 100.0).ToString();
      string str5 = "-E" + this.GetEParam(2);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6;
    }

    private void RS11(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-E" + this.GetEParam(1);
      string str4 = "-F" + this.Properties["Position Z"];
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str3 + str4 + str2 + str5;
    }

    private void RS12(ref string coding)
    {
      switch (this.SupportParams["TY"])
      {
        case "-1":
          this.RS12A(ref coding);
          break;
        case "-2":
          this.RS12B(ref coding);
          break;
        case "-3":
          this.RS12C(ref coding);
          break;
        case "-4":
          this.RS12D(ref coding);
          break;
      }
    }

    private void RS12A(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(a).ToString();
      string str3 = "-B" + Math.Ceiling(num1 + num2).ToString();
      string str4 = "-E" + this.GetEParam(0);
      string str5 = "-A10";
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS12B(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(a).ToString();
      string str3 = "-B" + Math.Ceiling(num1 + num2).ToString();
      string str4 = "-E" + this.GetEParam(0);
      string str5 = "-A10";
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS12C(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-E" + this.GetEParam(0);
      string str4 = "-A10";
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void RS12D(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      double num2 = PSUtil.PipeSize(Convert.ToInt32(this.SupportParams["Dn"])) / 2.0 + 100.0;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(a).ToString();
      string str3 = "-B" + Math.Ceiling(num1 + num2).ToString();
      string str4 = "-E" + this.GetEParam(0);
      string str5 = "-A10";
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS13(ref string coding)
    {
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string supportParam = this.SupportParams["BI"];
      double delta = 0.0;
      switch (supportParam)
      {
        case "210":
          delta = 50.0;
          break;
        case "215":
          delta = 75.0;
          break;
        case "220":
          delta = 100.0;
          break;
      }
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(a).ToString();
      string str3 = "-B" + Math.Ceiling(num1 + num2).ToString();
      string str4 = "-E" + this.GetEParam(0, delta);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RS14(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A1"]);
      double num2 = Convert.ToDouble(this.SupportParams["A2"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-E" + this.GetEParam(1);
      string str4 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void RS15(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A1"]);
      double num2 = Convert.ToDouble(this.SupportParams["A2"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      string supportParam = this.SupportParams["TY"];
      int no = 0;
      if (supportParam == "-2")
        no = 2;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      double num3 = Math.Ceiling(num1 + num2);
      string str2 = "-A" + num3.ToString();
      num3 = Math.Ceiling(a);
      string str3 = "-H" + num3.ToString();
      string str4 = "-E" + this.GetEParam(no);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void TR(ref string coding)
    {
      string supportParam = this.SupportParams["TY"];
      if (supportParam == "-1" || supportParam == "-2" || supportParam == "-13" || supportParam == "-14")
        this.TRPT(ref coding);
      else
        this.TR_default(ref coding);
    }

    private void TRPT(ref string coding)
    {
      int int32 = Convert.ToInt32(this.SupportParams["Dn"]);
      double a = Convert.ToDouble(this.SupportParams["A"]);
      string str1 = PSUtil.MetricImperial(StandardSupport.TrunnionProfile(int32).ToString());
      string property = this.Properties["Tag"].Replace("?", "");
      string str2 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(0);
      string str5 = "-" + str1;
      coding = property + "-" + str2 + str3 + str4 + str5;
    }

    private void TR_default(ref string coding)
    {
      int int32 = Convert.ToInt32(this.SupportParams["Dn"]);
      double a = Convert.ToDouble(this.SupportParams["A"]);
      string str1 = PSUtil.MetricImperial(StandardSupport.TrunnionProfile(int32).ToString());
      string property = this.Properties["Tag"].Replace("?", "");
      string str2 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str3 = "-A" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(0);
      string str5 = "-" + str1;
      coding = property + "-" + str2 + str3 + str4 + str5;
    }

    private void TRS(ref string coding)
    {
      Dictionary<int, double[]> dictionary = new Dictionary<int, double[]>();
      dictionary.Add(50, new double[2]{ 65.0, 175.0 });
      dictionary.Add(80, new double[2]{ 80.0, 190.0 });
      dictionary.Add(100, new double[2]{ 90.0, 200.0 });
      dictionary.Add(125, new double[2]{ 100.0, 210.0 });
      dictionary.Add(150, new double[2]{ 110.0, 220.0 });
      dictionary.Add(200, new double[2]{ 135.0, 245.0 });
      dictionary.Add(250, new double[2]{ 165.0, 275.0 });
      dictionary.Add(300, new double[2]{ 190.0, 300.0 });
      int int32 = Convert.ToInt32(this.SupportParams["Dn"]);
      double a1 = Convert.ToDouble(this.SupportParams["A"]);
      string str1 = PSUtil.MetricImperial(StandardSupport.TrunnionProfile(int32).ToString());
      double num = Convert.ToDouble(this.SupportParams["T"]);
      double a2 = num + dictionary[int32][0];
      double a3 = num + dictionary[int32][1];
      double a4 = a1 + a2 + 75.0;
      double a5 = a2 * 2.0 + 150.0;
      string property = this.Properties["Tag"].Replace("?", "");
      string str2 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str3 = "-A" + Math.Ceiling(a1).ToString();
      string str4 = "-B" + Math.Ceiling(a2).ToString();
      string str5 = "-E" + this.GetEParam(3);
      string str6 = "-L" + Math.Ceiling(a4).ToString();
      string str7 = "-C15";
      string str8 = "-W" + Math.Ceiling(a5).ToString();
      string str9 = "-A6";
      string str10 = "-D" + Math.Ceiling(a3).ToString();
      string str11 = "-" + str1;
      coding = property + "-" + str2 + str3 + str4 + str5 + str6 + str7 + str8 + str9 + str10 + str11;
    }

    private void RC1(ref string coding)
    {
      // Safe parsing to allow empty/missing values (defaults to 0)
      Convert.ToInt32(this.SupportParams["Dn"]); // Keep checks if Dn is guaranteed? Or should I safe this too? Usually Dn exists.
      string supportParam = this.SupportParams.ContainsKey("TY") ? this.SupportParams["TY"] : "";
      
      double num1 = this.ToDoubleSafe("A1");
      double num2 = this.ToDoubleSafe("A");
      this.ToDoubleSafe("B2");
      double a = this.ToDoubleSafe("F2");
      
      if (supportParam == "-1")
        a -= 50.0;
      string property = this.Properties.ContainsKey("Tag") ? this.Properties["Tag"].Replace("?", "") : "";
      string str1 = PSUtil.MetricImperial(this.Properties.ContainsKey("Size") ? this.Properties["Size"] : "");
      string str2 = "-A" + Math.Ceiling(num1 + num2).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "";
      if (this.SupportParams.ContainsKey("BI"))
      {
         var parts = StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ');
         if(parts.Length > 0) str5 = "-" + ((IEnumerable<string>)parts).Last<string>();
      }

      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private double ToDoubleSafe(string key)
    {
        if (this.SupportParams != null && this.SupportParams.ContainsKey(key))
        {
            if (double.TryParse(this.SupportParams[key], out double val)) return val;
        }
        return 0.0;
    }

    private void RC2(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RC3(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(2);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RC4(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      string supportParam = this.SupportParams["TY"];
      int no = 0;
      if (supportParam == "-2")
        no = 2;
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      double num3 = Math.Ceiling(num2 + num1);
      string str2 = "-A" + num3.ToString();
      num3 = Math.Ceiling(a);
      string str3 = "-H" + num3.ToString();
      string str4 = "-E" + this.GetEParam(no);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RC5(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a1 = Convert.ToDouble(this.SupportParams["F2"]);
      double a2 = Convert.ToDouble(this.SupportParams["B1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-H" + Math.Ceiling(a1).ToString();
      string str4 = "-B" + Math.Ceiling(a2).ToString();
      string str5 = "-E" + this.GetEParam(5);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str5 + str6;
    }

    private void RC6(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a = Convert.ToDouble(this.SupportParams["F2"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-H" + Math.Ceiling(a).ToString();
      string str4 = "-E" + this.GetEParam(5);
      string str5 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5;
    }

    private void RC7(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-E" + this.GetEParam(5);
      string str4 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void RC8(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-E" + this.GetEParam(3);
      string str4 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4;
    }

    private void RC9(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      double num1 = Convert.ToDouble(this.SupportParams["A"]);
      double num2 = Convert.ToDouble(this.SupportParams["A1"]);
      double a1 = Convert.ToDouble(this.SupportParams["F2"]);
      double a2 = Convert.ToDouble(this.SupportParams["F3"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(num2 + num1).ToString();
      string str3 = "-H1" + Math.Ceiling(a1).ToString();
      string str4 = "-H2" + Math.Ceiling(a2).ToString();
      string str5 = "-E" + this.GetEParam(6);
      string str6 = "-" + ((IEnumerable<string>) StandardSupport.BeamProfile(this.SupportParams["BI"]).Split(' ')).Last<string>();
      coding = property + "-" + str1 + str2 + str3 + str4 + str5 + str6;
    }

    private void FS(ref string coding)
    {
      Convert.ToInt32(this.SupportParams["Dn"]);
      string supportParam = this.SupportParams["TY"];
      double num = Convert.ToDouble(this.SupportParams["A"]);
      double a = Convert.ToDouble(this.SupportParams["A1"]);
      string property = this.Properties["Tag"].Replace("?", "");
      string str1 = PSUtil.MetricImperial(this.Properties["Size"]);
      string str2 = "-A" + Math.Ceiling(a + num).ToString();
      string str3 = "-B" + Math.Ceiling(a).ToString();
      switch (supportParam)
      {
        case "-1":
        case "-5":
          coding = property + "-" + str1 + str2 + str3;
          break;
        case "-2":
        case "-3":
        case "-4":
          string str4 = "-H" + Math.Ceiling(Convert.ToDouble(this.SupportParams["F2"])).ToString();
          coding = property + "-" + str1 + str2 + str4 + str3;
          break;
      }
    }

    private string GetEParam(int no)
    {
      Point3d position = this.PPorts[no].Position;
      return Math.Round(position.Z).ToString();
    }

    private string GetEParam(int no, double delta)
    {
      Point3d position = this.PPorts[no].Position;
      return Math.Round(position.Z + delta).ToString();
    }

    public List<int> CurrentDrawingData(string drawing_name)
    {
      PnPTable table = this.pnp_db.Tables["PnPDrawings"];
      int num1 = 0;
      foreach (PnPRow pnProw in table.Select())
      {
        if (((PnPAbstractRow) pnProw)["Dwg Name"].ToString() == drawing_name)
        {
          num1 = Convert.ToInt32(((PnPAbstractRow) pnProw)["PnPID"]);
          break;
        }
      }
      List<int> intList = new List<int>();
      foreach (PnPRow pnProw in this.pnp_db.Tables["PnPDataLinks"].Select())
      {
        int int32 = Convert.ToInt32(((PnPAbstractRow) pnProw)["DwgId"]);
        string str = ((PnPAbstractRow) pnProw)["RowClassName"].ToString();
        int num2 = num1;
        if (int32 == num2 && str == "Support")
          intList.Add(Convert.ToInt32(((PnPAbstractRow) pnProw)["RowId"]));
      }
      return intList;
    }
  }
}
