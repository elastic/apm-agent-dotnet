<%@ Page Language="C#" CodeBehind="Webforms.aspx.cs" Inherits="AspNetFullFrameworkSampleApp.Webforms" %>
<!DOCTYPE html>
<html>
<head runat="server">
	<meta charset="utf-8"/>
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>@ViewBag.Title - My ASP.NET Application</title>
	<%: System.Web.Optimization.Styles.Render("~/Content/css") %>
	<%: System.Web.Optimization.Scripts.Render("~/bundles/modernizr") %>
</head>
<body>
<form id="HtmlForm" runat="server">
<div class="navbar navbar-inverse navbar-fixed-top">
	<div class="container">
		<div class="navbar-header">
			<button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-collapse">
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
				<span class="icon-bar"></span>
			</button>
			<a href="/" class="navbar-brand">Application name</a>
		</div>
		<div class="navbar-collapse collapse">
			<ul class="nav navbar-nav">
				<li><a href="/">Home</a></li>
				<li><a href="/Home/About">About</a></li>
				<li><a href="/Home/Contact">Contact</a></li>
			</ul>
		</div>
	</div>
</div>
<div class="container body-content">
	<div class="row">
    	<div class="col-md-12">
    		<h2>A webforms page</h2>
    		<p>
    			This is an example of a ASP.NET Webforms page
    		</p>
    	</div>
    </div>
	<hr/>
	<footer>
		<p>&copy; <%: DateTime.Now.Year %> - My ASP.NET Application</p>
	</footer>
</div>

<%: System.Web.Optimization.Scripts.Render("~/bundles/jquery") %>
<%: System.Web.Optimization.Scripts.Render("~/bundles/bootstrap") %>
</form>
</body>
</html>