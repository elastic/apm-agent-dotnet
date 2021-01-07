using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.WebHost;

namespace AspNetFullFrameworkSampleApp
{
	public class CustomWebHostBufferPolicySelector : WebHostBufferPolicySelector
	{
		public override bool UseBufferedInputStream(object hostContext)
		{
			System.Web.HttpContextBase contextBase = hostContext as System.Web.HttpContextBase;
			if (contextBase != null && contextBase.Request.ContentType != null && contextBase.Request.ContentType.Contains("multipart")) return false;
			else return base.UseBufferedInputStream(hostContext);
		}

		public override bool UseBufferedOutputStream(System.Net.Http.HttpResponseMessage response)
		{
			return base.UseBufferedOutputStream(response);
		}
	}
}
