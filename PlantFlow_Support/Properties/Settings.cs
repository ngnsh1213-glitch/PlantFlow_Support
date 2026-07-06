using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#nullable disable
namespace PlantFlow_Support.Properties
{
  [CompilerGenerated]
  [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.9.0.0")]
  internal sealed class Settings : ApplicationSettingsBase
  {
    private static Settings defaultInstance = (Settings) SettingsBase.Synchronized((SettingsBase) new Settings());

    public static Settings Default => Settings.defaultInstance;

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbTemplate
    {
      get => (string) this[nameof (tbTemplate)];
      set => this[nameof (tbTemplate)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbSaveMTOAs
    {
      get => (string) this[nameof (tbSaveMTOAs)];
      set => this[nameof (tbSaveMTOAs)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbDwgTitle1
    {
      get => (string) this[nameof (tbDwgTitle1)];
      set => this[nameof (tbDwgTitle1)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbDwgRevision
    {
      get => (string) this[nameof (tbDwgRevision)];
      set => this[nameof (tbDwgRevision)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbXlabel
    {
      get => (string) this[nameof (tbXlabel)];
      set => this[nameof (tbXlabel)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbYlabel
    {
      get => (string) this[nameof (tbYlabel)];
      set => this[nameof (tbYlabel)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbDwgNo
    {
      get => (string) this[nameof (tbDwgNo)];
      set => this[nameof (tbDwgNo)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbProjectNo
    {
      get => (string) this[nameof (tbProjectNo)];
      set => this[nameof (tbProjectNo)] = (object) value;
    }

    [UserScopedSetting]
    [DebuggerNonUserCode]
    [DefaultSettingValue("")]
    public string tbRevisionTable
    {
      get => (string) this[nameof (tbRevisionTable)];
      set => this[nameof (tbRevisionTable)] = (object) value;
    }
  }
}
