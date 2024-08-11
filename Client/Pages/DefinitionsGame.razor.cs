using Microsoft.AspNetCore.Components;
using BlazorApp.Client.Models;
using BlazorApp.Client.Helper;

namespace BlazorApp.Client.Pages
{
	public partial class DefinitionsGame
	{
		private int currentQuestionNumber = 0;
		ElementReference LoadWordsButton;
		private string? response;
		string? result = "";
		WordsHelper? wordsHelper;
		private int questionsAnswered = 0;
		private int questionsCorrect = 0;
		private int counter = 0;
		private async Task CheckAnswerAsync(string? guessedWord, int indexPosition)
		{
			PlayAudio = true;
			if (guessedWord == LoadWordResults?.WordResults?[currentQuestionNumber].word)
			{
				ButtonClass[indexPosition] = "btn-success";
				questionsAnswered++;
				questionsCorrect++;
				dynamicClass = "";
				if (currentQuestionNumber < (wordsToLoad - 1))
				{
					currentQuestionNumber++;
					response = "";
					AnswerState = true;
					await Task.Delay(1000);
					WordResult = LoadWordResults?.WordResults?[currentQuestionNumber];
					dynamicClass = "animate__animated animate__backInUp";
					ButtonClass[indexPosition] = "btn-info";
				}
				else
				{
					AnswerState = true;
					//response = $"✅ ({guessedWord}) ";
					dynamicClass = "animate__animated animate__backInUp";
					await Task.Delay(1000);
					try
					{
						ButtonClass[indexPosition] = "btn-info";
					}
					catch (Exception exception)
					{

						throw new Exception($" error changing button class {exception.Message}");
					}
					ShowWord = false;
					PlayAudio = false;
					await LoadWordAsync();
				}
			}
			else
			{
				questionsCorrect--;
				ButtonClass[indexPosition] = "btn-danger";
				AnswerState = false;
				await Task.Delay(1000);
				ButtonClass[indexPosition] = "btn-info";
			}
			await LoadWordsButton.FocusAsync();
			PlayAudio = false;

		}
		private async Task LoadWordAsync()
		{
			Message = null;
			LoadWordResults = null;
			WordResult = null;
			if (questionsCorrect == wordsToLoad)
			{
				wordsToLoad++;
			}
			else if (questionsCorrect < wordsToLoad)
			{
				wordsToLoad = 2;
			}
			response = "";
			questionsAnswered = 0;
			questionsCorrect = 0;
			Index = 0;
			dynamicClass = "";
			currentQuestionNumber = 0;
			ShowWord = true;
			ReloadButtonClass();
			// Try to get the API key from the text box in the user interface
			if (GameOptions.APIKey != null && GameOptions.APIKey != "TBC")
			{
				HideKey = true;
				wordsHelper = new WordsHelper(GameOptions.APIKey);
			}
			if (wordsHelper != null)
			{
				LoadWordResults =
					await wordsHelper.LoadWord(wordsToLoad, GameOptions?.MaximumWordLength ?? 20, GameOptions?.BeginsWith?.ToLower(), null);
			}
			Message = LoadWordResults?.Message;
			result = LoadWordResults?.Result;
			if (LoadWordResults?.WordResults?.Count > 0)
			{
				WordResult = LoadWordResults.WordResults[0];
			}
		}
		private void ReloadButtonClass()
		{
			ButtonClass = new List<string>();
			for (int i = 0; i < wordsToLoad; i++)
			{
				ButtonClass.Add("btn-info");
			}
		}
		private int wordsToLoad = 2;

		public GameOptions GameOptions { get; set; } = new GameOptions();
		public bool AnswerState { get; set; } = false;
		public bool PlayAudio { get; set; } = false;
		[Inject] IConfiguration? Configuration { get; set; }
		private string dynamicClass = "";
		private int Index { get; set; } = 0;
		LoadWordResults? LoadWordResults { get; set; }
		WordResult? WordResult { get; set; }

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await LoadWordsButton.FocusAsync();
			}
		}
		protected override async Task OnInitializedAsync()
		{
			if (Configuration != null)
			{
				var apiKey = "TBC";
				if (apiKey == null || apiKey == "TBC")
				{
					apiKey = Environment.GetEnvironmentVariable("WORDSAPIKEY");
				}
				if (apiKey == null || apiKey == "TBC")
				{
					apiKey = Configuration["WordsApiKey"];
				}
				if (apiKey != null && apiKey != "" && apiKey != "TBC")
				{
					wordsHelper = new WordsHelper(apiKey);
					await LoadWordAsync();
					GameOptions = new GameOptions();
				}
				else
				{
					Message = "API key not found";
				}
			}
		}
		private void ShowOptions()
		{
			if (GameOptions != null)
			{
				GameOptions.ShowOptions = !GameOptions.ShowOptions;
			}
		}
		public List<string> ButtonClass { get; set; } = new List<string>() { "btn-info", "btn-info", "btn-info", "btn-info", "btn-info", };
		public bool HideKey { get; set; } = false;
		public string? Message { get; set; }
		public bool ShowWord { get; set; } = true;
	}
}
