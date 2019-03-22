namespace Elastic.Apm.Api
{
	/// <summary>
	/// An object containing contextual data of the related http request.
	/// It can be attached to an <see cref="ISpan"/> through <see cref="ISpan.Context"/>
	/// </summary>
	public class Http
	{
		public string Method { get; set; }
		public int StatusCode { get; set; }
		public string Url { get; set; }
	}
}
