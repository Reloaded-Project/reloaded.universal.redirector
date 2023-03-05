// ReSharper disable CheckNamespace
#pragma warning disable CS1591
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0169

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

/// <summary/>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FILE_ALL_INFORMATION
{
    public FILE_BASIC_INFORMATION     BasicInformation;
    public FILE_STANDARD_INFORMATION  StandardInformation;
    public FILE_INTERNAL_INFORMATION  InternalInformation;
    public FILE_EA_INFORMATION        EaInformation;
    public FILE_ACCESS_INFORMATION    AccessInformation;
    public FILE_POSITION_INFORMATION  PositionInformation;
    public FILE_MODE_INFORMATION      ModeInformation;
    public FILE_ALIGNMENT_INFORMATION AlignmentInformation;
    public FILE_NAME_INFORMATION      NameInformation;
}