// Decompiled with JetBrains decompiler
// Type: Microsoft.Office.Interop.Excel._Worksheet
// Assembly: AUTO2DPIPESUPPORT, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 503E85E1-3D1B-40F1-A567-5E4C05655DB9
// Assembly location: C:\Temp\CADLIB\AUTO2DPIPESUPPORT.dll

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace Microsoft.Office.Interop.Excel
{
  [CompilerGenerated]
  [Guid("000208D8-0000-0000-C000-000000000046")]
  [TypeIdentifier]
  [ComImport]
  public interface _Worksheet
  {
    [SpecialName]
    [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
    sealed extern void _VtblGap1_45();

    [DispId(238)]
    Range Cells { [DispId(238), MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)] [return: MarshalAs(UnmanagedType.Interface)] get; }
  }
}
