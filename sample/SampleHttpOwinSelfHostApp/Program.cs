﻿using System;
using Microsoft.Owin.Hosting;

namespace SampleHttpOwinSelfHostApp
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var baseAddress = "http://localhost:54321/";

			// Start OWIN host
			using (WebApp.Start<Startup>(url: baseAddress))
			{
				Console.WriteLine("Api Service started with listening `54321` port");
				Console.ReadLine();
			}
		}
	}
}
