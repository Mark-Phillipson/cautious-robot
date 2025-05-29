namespace BlazorApp.Client.Pages
{
	public class AnswerOption
	{
		public string Word { get; set; } = string.Empty;
		public string Definition { get; set; } = string.Empty;
		public string? PartOfSpeech { get; set; }
		public string ButtonClass { get; set; } = "btn-info";
	}
}
