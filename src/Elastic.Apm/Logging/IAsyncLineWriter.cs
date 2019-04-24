using System.Threading.Tasks;

namespace Elastic.Apm.Logging
{
	internal interface IAsyncLineWriter
	{
		Task WriteLineAsync(string line);
	}
}
