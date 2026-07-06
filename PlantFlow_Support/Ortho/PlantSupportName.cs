using Autodesk.ProcessPower.DataObjects;
using System;
using System.Collections.Generic;

namespace PlantFlow_Support
{
  public class PlantSupportName : PSUtil
  {
    public PnPRow[] support_rows;
    public string std_name;
    public List<PnPRow> supports_in_same_family = new List<PnPRow>();
    public Dictionary<string, string> support_params;

    public string TurnTypeFromCodeToStr(string TY)
    {
      string str = "";
      if (TY != null && TY.Length == 2)
      {
        switch (TY[1])
        {
          case '1':
            if (TY == "-1")
            {
              str = "A";
              break;
            }
            break;
          case '2':
            if (TY == "-2")
            {
              str = "B";
              break;
            }
            break;
          case '3':
            if (TY == "-3")
            {
              str = "C";
              break;
            }
            break;
          case '4':
            if (TY == "-4")
            {
              str = "D";
              break;
            }
            break;
          case '5':
            if (TY == "-5")
            {
              str = "E";
              break;
            }
            break;
          case '6':
            if (TY == "-6")
            {
              str = "F";
              break;
            }
            break;
          case '7':
            if (TY == "-7")
            {
              str = "G";
              break;
            }
            break;
        }
      }
      if (this.std_name == "TR")
        str = this.TurnTypeForTrunnion(TY);
      else if (this.std_name == "FS")
        str = this.TurnTypeForFieldSupport(TY);
      return str;
    }

    public string TurnTypeForFieldSupport(string TY)
    {
      string str1 = "";
      string supportParam1 = this.support_params["SM"];
      string supportParam2 = this.support_params["SD"];
      switch (TY)
      {
        case "-1":
          str1 = "I";
          break;
        case "-2":
          str1 = "L";
          break;
        case "-3":
          str1 = "M";
          break;
        case "-4":
          str1 = "T";
          break;
        case "-5":
          str1 = "V";
          break;
      }
      string str2 = "S";
      string str3 = "U";
      if (supportParam1 == "-2")
        str2 = "C";
      switch (supportParam2)
      {
        case "-2":
          str3 = "D";
          break;
        case "-3":
          str3 = "S";
          break;
      }
      return str1 + str2 + str3;
    }

    public string TurnTypeForTrunnion(string TY)
    {
        string str = "";
        if (TY != null)
        {
            switch (TY.Length)
            {
                case 2:
                    if (TY == "-1") str = "P1";
                    else if (TY == "-2") str = "T1";
                    else if (TY == "-3") str = "A";
                    else if (TY == "-4") str = "ST";
                    else if (TY == "-5") str = "G";
                    else if (TY == "-6") str = "DA";
                    else if (TY == "-7") str = "DST";
                    else if (TY == "-8") str = "DG";
                    else if (TY == "-9") str = "U";
                    break;
                case 3:
                     if (TY == "-10") str = "DU";
                     else if (TY == "-11") str = "R";
                     else if (TY == "-12") str = "DR";
                     else if (TY == "-13") str = "P2";
                     else if (TY == "-14") str = "T2";
                     break;
            }
        }
        return str;
    }

    public List<int> CurrentDrawingData(string drawing_name)
    {
      PnPTable table = this.pnp_db.Tables["PnPDrawings"];
      int num1 = 0;
      foreach (PnPRow pnProw in table.Select())
      {
        if (pnProw["Dwg Name"].ToString() == drawing_name)
        {
          num1 = Convert.ToInt32(pnProw["PnPID"]);
          break;
        }
      }
      List<int> intList = new List<int>();
      foreach (PnPRow pnProw in this.pnp_db.Tables["PnPDataLinks"].Select())
      {
        int int32 = Convert.ToInt32(pnProw["DwgId"]);
        string str = pnProw["RowClassName"].ToString();
        int num2 = num1;
        if (int32 == num2 && str == "Support")
          intList.Add(Convert.ToInt32(pnProw["RowId"]));
      }
      return intList;
    }
  }
}
