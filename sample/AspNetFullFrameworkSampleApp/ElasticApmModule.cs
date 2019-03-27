using System;
using System.Web;

namespace AspNetFullFrameworkSampleApp
{
	public class ElasticApmModule : IHttpModule
	{
		public void OnLogRequest(object source, EventArgs e)
		{
			//custom logging logic can go here
		}

		/// <summary>
		/// You will need to configure this module in the Web.config file of your
		/// web and register it with IIS before being able to use it. For more information
		/// see the following link: https://go.microsoft.com/?linkid=8101007
		/// </summary>

		#region IHttpModule Members

		public void Dispose() { }

		public void Init(HttpApplication context)
		{
			// Below is an example of how you can handle LogRequest event and provide 
			// custom logging implementation for it
			context.LogRequest += OnLogRequest;
			context.BeginRequest += Context_BeginRequest;
			context.EndRequest += Context_EndRequest;
		}

		private void Context_BeginRequest(object sender, EventArgs e)
		{
			//Start transaction here
		}

		private void Context_EndRequest(object sender, EventArgs e)
		{
			//End Transaction here
		}

		#endregion
	}
}
