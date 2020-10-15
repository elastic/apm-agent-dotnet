using System.Text;
using System.Web;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.Extensions;

namespace AspNetFullFrameworkSampleApp.Bootstrap
{
	public static class AlertExtensions
	{
		public static IHtmlString Alert(this HtmlHelper helper) =>
			helper.ViewContext.TempData.Get<Alert>("alert") ?? (IHtmlString)MvcHtmlString.Empty;
	}

	public class SuccessAlert : Alert
	{
		public SuccessAlert(string message) : base(message) => Status = AlertStatus.Success;

		public SuccessAlert(string title, string message) : base(title, message) => Status = AlertStatus.Success;
	}

	public class Alert : IHtmlString
	{
		public Alert(string message) => Message = message;

		public Alert(string title, string message)
		{
			Title = title;
			Message = message;
		}

		public string Title { get; }

		public string Message { get; }

		public AlertStatus Status { get; set; }

		public bool Dismissable { get; set; } = true;

		public string ToHtmlString()
		{
			var tag = new TagBuilder("div");
			tag.AddCssClass("alert");

			switch (Status)
			{
				case AlertStatus.Success:
					tag.AddCssClass("alert-success");
					break;
				case AlertStatus.Warning:
					tag.AddCssClass("alert-warning");
					break;
				case AlertStatus.Danger:
					tag.AddCssClass("alert-danger");
					break;
				case AlertStatus.Info:
					tag.AddCssClass("alert-info");
					break;
			}

			var html = new StringBuilder();

			if (Dismissable)
			{
				var button = new TagBuilder("button");
				button.AddCssClass("close");
				button.MergeAttribute("data-dismiss", "alert");
				button.MergeAttribute("aria-label", "close");
				button.InnerHtml = "<span aria-hidden=\"true\">&times;</span>";
				tag.AddCssClass("alert-dismissable");
				html.Append(button);
			}

			if (!string.IsNullOrEmpty(Title))
			{
				var title = new TagBuilder("h4");
				title.AddCssClass("alert-heading");
				title.SetInnerText(Title);
				html.AppendLine(title.ToString());
			}

			html.AppendLine(Message);
			tag.InnerHtml = html.ToString();

			return tag.ToString();
		}
	}
}
