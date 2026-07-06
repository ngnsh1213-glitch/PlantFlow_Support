// Decompiled with JetBrains decompiler
// Type: Microsoft.Office.Interop.Excel.Workbooks
// Assembly: AUTO2DPIPESUPPORT, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 503E85E1-3D1B-40F1-A567-5E4C05655DB9
// Assembly location: C:\Temp\CADLIB\AUTO2DPIPESUPPORT.dll

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace Microsoft.Office.Interop.Excel
{
  [CompilerGenerated]
  [Guid("000208DB-0000-0000-C000-000000000046")]
  [DefaultMember("_Default")]
  [TypeIdentifier]
  [ComImport]
  public interface Workbooks : IEnumerable
  {
    [SpecialName]
    [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
    sealed extern void _VtblGap1_3();

    [DispId(181)]
    [LCIDConversion(1)]
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    [return: MarshalAs(UnmanagedType.Interface)]
    Workbook Add([MarshalAs(UnmanagedType.Struct), In, Optional] object Template);
  }
}
