using System;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using System.ComponentModel;


namespace PlantFlow_Support
{
  public partial class PaletteTab : UserControl
  {
    public PaletteTab()
    {
      Log("PaletteTab Constructor Started - RESTORE UI MODE");
      
      // Initialize Dictionaries
      this.SupportSelection = new Dictionary<string, SupportInfo>();
      this.InitializeViewDefinitions();

      try 
      {
          this.InitializeComponent();
          Log("InitializeComponent Success");
      } 
      catch (System.Exception ex)
      {
          Log("InitializeComponent Failed: " + ex.Message);
          MessageBox.Show("Error Loading PaletteTab UI: " + ex.Message);
      }
      
      try 
      {
          this.InitializePSDM(); // Logic Method
          Log("InitializePSDM Success");
      }
      catch (System.Exception ex)
      {
           Log("InitializePSDM Failed: " + ex.Message);
      }

      try 
      {
          this.LoadPreviousSetting(); // Logic Method
          Log("LoadPreviousSetting Success");
      }
      catch {}

      try
      {
          this.InitializeSupportCatalog();
          Log("InitializeSupportCatalog Success");
      }
      catch (System.Exception ex)
      {
          Log("InitializeSupportCatalog Failed: " + ex.Message);
      }
      
      Log("PaletteTab Constructor Finished");
    }

    private void Log(string msg)
    {
        try 
        {
            string path = @"C:\TEMP\PlantFlow_Log.txt";
            using (StreamWriter sw = File.AppendText(path))
            {
               sw.WriteLine(DateTime.Now.ToString() + ": " + msg);
            }
        }
        catch {}
    }
  }
}
