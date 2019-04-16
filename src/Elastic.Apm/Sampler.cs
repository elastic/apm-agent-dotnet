namespace Elastic.Apm.Api
{
	internal class Sampler
	{
		private readonly double _rate;

		internal Sampler(double rate)
		{
			_rate = rate;
		}
	}
}
