using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartSql;
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
			return View();
		}
	}
}
