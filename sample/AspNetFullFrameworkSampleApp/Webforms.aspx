<%@ Page Language="C#" CodeBehind="Webforms.aspx.cs" Inherits="AspNetFullFrameworkSampleApp.Webforms" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="System.Web.Mvc.Html" %>
<%@ Import Namespace="Microsoft.AspNet.Identity" %>
<!DOCTYPE html>
<html>
<head runat="server">
	<meta charset="utf-8"/>
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title><%: Title %> - My ASP.NET Application</title>
	<%: System.Web.Optimization.Styles.Render("~/Content/css") %>
	<%: System.Web.Optimization.Scripts.Render("~/bundles/modernizr") %>
</head>
<body>
<form id="HtmlForm" runat="server">
<nav class="navbar navbar-expand-lg navbar-fixed-top navbar-light bg-light">
    <%: Html.ActionLink("Elastic APM", "Index", "Home", new { area = "" }, new { @class = "navbar-brand" }) %>
    <button class="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
    	<span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarSupportedContent">
    	<ul class="navbar-nav mr-auto mt-2 mt-lg-0">
    		<li class="nav-item">
    			<%: Html.ActionLink("Home", "Index", "Home", null, new { @class = "nav-link" }) %>
    		</li>
    		<li class="nav-item">
    			<%: Html.ActionLink("About", "About", "Home", null, new { @class = "nav-link" }) %>
    		</li>
    		<li class="nav-item">
    			<%: Html.ActionLink("Contact", "Contact", "Home", null, new { @class = "nav-link" }) %>
    		</li>
    		<li class="nav-item">
                <%: Html.ActionLink("Diagnostics", "Index", "Diagnostics", null, new { @class = "nav-link" }) %>
            </li>
	        <li class="nav-item">
                <a href="/Webforms.aspx" class="nav-link">Webforms</a>
            </li>
	        <li class="nav-item">
				<a href="/RoutedWebforms" class="nav-link">RoutedWebforms</a>
            </li>
    	</ul>
    	<ul class="navbar-nav my-2 my-lg-0">
	        <% if (Request.IsAuthenticated) { %>
		        <li>
        			<span class="navbar-text"><%: User.Identity.GetUserName() %></span>
        		</li>
        		<li>
        			<% using (Html.BeginForm("LogOff", "Account", FormMethod.Post, new { id = "logoutForm", @class = "form-inline" })) { %>
        				<%: Html.AntiForgeryToken() %>
        				<button type="submit" class="btn btn-link nav-link">Log off</button>
        			<% } %>
	            </li>
	        <% } else { %>
        		<li><%: Html.ActionLink("Register", "Register", "Account", null, new { @class = "nav-link", id = "registerLink" }) %></li>
        		<li><%: Html.ActionLink("Log in", "Login", "Account", null, new { @class = "nav-link", id = "loginLink" }) %></li>
	        <% } %>
        </ul>
    </div>
</nav>
<div class="container body-content mt-5">
	<div class="row">
    	<div class="col-md-12">
    		<h2>A <%: Title %> page</h2>
    		<p>
    			This is an example of a ASP.NET Webforms page
    		</p>
    	</div>
    </div>
	<hr/>
	<footer>
		<p>&copy; <%: DateTime.Now.Year %> - Elastic APM</p>
	</footer>
</div>

<%: System.Web.Optimization.Scripts.Render("~/bundles/jquery") %>
<%: System.Web.Optimization.Scripts.Render("~/bundles/bootstrap") %>
</form>
</body>
</html>