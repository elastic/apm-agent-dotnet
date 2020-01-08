namespace Elastic.Apm.Helpers
{
	internal class CascadePropertyFetcher : PropertyFetcher
	{
		private readonly PropertyFetcher _innerFetcher;

		public CascadePropertyFetcher(PropertyFetcher innerFetcher, string propertyName) : base(propertyName) => _innerFetcher = innerFetcher;

		public override object Fetch(object obj)
		{
			var fetchedObject = _innerFetcher.Fetch(obj);

			return base.Fetch(fetchedObject);
		}
	}
}
