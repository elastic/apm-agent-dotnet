using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm;
using Microsoft.AspNetCore.Mvc;
using SmartSql;
using SmartSqlAspNetCodeApp.Entity;
using SmartSqlAspNetCodeApp.Models;

namespace SmartSqlAspNetCodeApp.Controllers
{
	public class HomeController : Controller
	{
		private ISqlMapper _sqlMapper;
		public HomeController(ISqlMapper sqlMapper)
		{
			_sqlMapper = sqlMapper;
		}
		public IActionResult Index()
		{
			_sqlMapper.Execute(new RequestContext
			{
				Request = new {Id=1},
				Scope = "Member",
				SqlId = "GetEntity"
			});

			_sqlMapper.Query<Member>(new RequestContext
			{

			});
			return View();
		}

		public IActionResult TriggerError()
		{
			Agent.Tracer.CurrentTransaction.Tags["foo"] = "bar";
			throw new Exception("This is a test exception!");
		}
	}
}
