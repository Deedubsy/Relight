#if !NET5_0_OR_GREATER
// Enables C# 9 records / init-only setters on the netstandard2.1 profile when the runtime lacks the marker type.
namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }
#endif
