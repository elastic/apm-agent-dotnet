using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	public class System
	{
		public Container Container { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(System)) { { "Container", Container } }.ToString();
	}
}
