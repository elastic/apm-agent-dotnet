using System.Globalization;
using Microsoft.FSharp.Collections;

var germanCulture = new CultureInfo("de");
CultureInfo.CurrentCulture = germanCulture;
CultureInfo.CurrentUICulture = germanCulture;

// Throw a localized ArgumentException
try
{
	SeqModule.Head(SeqModule.Empty<int>());
}
catch (ArgumentException ex)
{
	Console.WriteLine($"Handled: {ex.Message}");
}
