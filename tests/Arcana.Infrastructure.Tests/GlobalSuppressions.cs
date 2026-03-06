// This file is used by Code Analysis to maintain SuppressMessage attributes
// that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// CA1873: Avoid using LoggerExtensions with expensive argument evaluation
[assembly: SuppressMessage("Performance", "CA1873", Justification = "Acceptable logging pattern")]

// CA1859: Use concrete types for performance
[assembly: SuppressMessage("Performance", "CA1859", Justification = "Interface abstraction required")]

// CA1862: Use StringComparison enum for case-insensitive comparison
[assembly: SuppressMessage("Globalization", "CA1862", Justification = "EF Core query translated to SQL")]

// CA1816: GC.SuppressFinalize should call base.Dispose(false)
[assembly: SuppressMessage("Usage", "CA1816", Justification = "Dispose pattern handled correctly")]

// CA1860: Prefer comparing Count to 0
[assembly: SuppressMessage("Performance", "CA1860", Justification = "Any() is more readable")]

// CA2254: Template is not a constant
[assembly: SuppressMessage("Logging", "CA2254", Justification = "Dynamic log templates used intentionally")]

// CA1854: Prefer TryGetValue over ContainsKey + indexer
[assembly: SuppressMessage("Performance", "CA1854", Justification = "Acceptable pattern")]

// CA1869: Cache JsonSerializerOptions
[assembly: SuppressMessage("Performance", "CA1869", Justification = "Options specific to call site")]

// CA1068: CancellationToken parameters must come last
[assembly: SuppressMessage("Design", "CA1068", Justification = "API compatibility")]

// CA1822: Member does not access instance data
[assembly: SuppressMessage("Performance", "CA1822", Justification = "May need instance context in future")]

// CA1861: Prefer static readonly fields over constant array arguments
[assembly: SuppressMessage("Performance", "CA1861", Justification = "Test data arrays are readable inline")]
