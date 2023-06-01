<%@ Page Language="C#" CodeBehind="WebformsException.aspx.cs" Inherits="AspNetFullFrameworkSampleApp.WebformsException" %>
<!DOCTYPE html>
<html>
<head runat="server">
	<meta charset="utf-8"/>
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title><%: Title %> - My ASP.NET Application</title>
</head>
<body>
<form id="HtmlForm" runat="server">
	<%
		// throw divide by zero exception and let the framework wrap in a HttpUnhandledException
		var zero = 0;
		var result = 1 / zero;
		Response.Write(result);
	%>
</form>
</body>
</html>