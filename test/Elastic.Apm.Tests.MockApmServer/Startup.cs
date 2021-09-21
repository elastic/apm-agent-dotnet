﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.Apm.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class Startup
	{
		// ReSharper disable once NotAccessedField.Local
		private readonly IConfiguration _configuration;

		public Startup(IConfiguration configuration) => _configuration = configuration;

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc().AddApplicationPart(typeof(MockApmServer).Assembly);
			services.AddControllers();


			var contentRootDir = _configuration.GetValue<string>(WebHostDefaults.ContentRootKey);
			var specBranch = "7.x";
			var validator = new Validator(specBranch, Path.Combine(contentRootDir, "specs"));
			// not ideal to wait on async inside sync startup configuration, but do for now.
			// Force the specs to work with to be downloaded now, to avoid race conditions with
			// downloading for the first time.
			validator.DownloadAsync(specBranch, true).Wait();
			services.AddSingleton(validator);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseDeveloperExceptionPage();
			app.UseRouting();
			app.UseEndpoints(endpoints => endpoints.MapControllers());
		}
	}
}
