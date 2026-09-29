// AssemblyInfo.cs
// Gives the package's EditMode tests access to internal types (native wrapper, stream refcount,
// binder test hook).
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AudioReactive.Editor.Tests")]
