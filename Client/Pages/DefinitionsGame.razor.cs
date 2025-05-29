using Microsoft.AspNetCore.Components;
using BlazorApp.Client.Models;
using BlazorApp.Client.Helper;
using BlazorApp.Client.Shared;
using BlazorApp.Client.Pages;

namespace BlazorApp.Client.Pages;

	public partial class DefinitionsGame
	{
		[Inject] public required BlazorApp.Client.Shared.IApiKeyService ApiKeyService { get; set; }
		private string? apiKey = null;
		public bool ApiKeyAvailable => !string.IsNullOrWhiteSpace(apiKey);
		public async Task OnApiKeySaved()
		{
			apiKey = await ApiKeyService.GetApiKeyAsync();
			GameOptions.APIKey = apiKey;
			HideKey = true;
			await LoadWordAsync();
			// StateHasChanged();
		}
		private int currentQuestionNumber = 0;
		ElementReference LoadWordsButton;
		public string? response;
		public string? result = "";
		public WordsHelper? wordsHelper;
		public int questionsAnswered = 0;
		public int questionsCorrect = 0;
		public int counter = 0;
		public async Task CheckAnswerAsync(string? guessedWord, int indexPosition)
		{
			PlayAudio = true;
			if (guessedWord == LoadWordResults?.WordResults?[currentQuestionNumber].word)
			{
			   if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			   {
				   ButtonClass[indexPosition] = "btn-success";
			   }
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
				   if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
				   {
					   ButtonClass[indexPosition] = "btn-info";
				   }
				}
				else
				{
					AnswerState = true;
					//response = $"✅ ({guessedWord}) ";
					dynamicClass = "animate__animated animate__backInUp";
					await Task.Delay(1000);
					try
					{
					   if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
					   {
						   ButtonClass[indexPosition] = "btn-info";
					   }
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
			   if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			   {
				   ButtonClass[indexPosition] = "btn-danger";
			   }
				AnswerState = false;
				await Task.Delay(1000);
			   if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			   {
				   ButtonClass[indexPosition] = "btn-info";
			   }
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
			// Use API key from service if available
			if (apiKey == null)
			{
				apiKey = await ApiKeyService.GetApiKeyAsync();
			}
			if (!string.IsNullOrWhiteSpace(apiKey))
			{
				GameOptions.APIKey = apiKey;
				HideKey = true;
				wordsHelper = new WordsHelper(apiKey);
			}
			if (wordsHelper != null)
			{
				LoadWordResults =
					await wordsHelper.LoadWord(wordsToLoad, GameOptions?.MaximumWordLength ?? 20, GameOptions?.BeginsWith?.ToLower(), null);
			}
			// Ensure ButtonClass matches the number of answer options
			int optionCount = LoadWordResults?.WordResults?.Count ?? 0;
			ButtonClass = Enumerable.Repeat("btn-info", optionCount).ToList();
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
		public int wordsToLoad = 2;

		public GameOptions GameOptions { get; set; } = new GameOptions();
		public bool AnswerState { get; set; } = false;
		public bool PlayAudio { get; set; } = false;
		[Inject] IConfiguration? Configuration { get; set; }
		public string dynamicClass = "";
		public int Index { get; set; } = 0;
		// Expose ordered answer options for rendering
		public List<AnswerOption> AnswerOptions
		{
			get
			{
				var options = new List<AnswerOption>();
				if (LoadWordResults?.WordResults != null)
				{
					var ordered = LoadWordResults.WordResults.OrderBy(x => x.RandomOrder).ToList();
					for (int i = 0; i < ordered.Count; i++)
					{
						var wr = ordered[i];
						// wr.results is Result[]? (nullable array)
						var resultsArr = wr?.results;
						if (ShowWord && !string.IsNullOrWhiteSpace(wr?.word) && resultsArr != null && resultsArr.Length > 0)
						{
							var word = wr.word ?? string.Empty;
							var firstResult = resultsArr[0];
							var definition = firstResult?.definition ?? string.Empty;
							var partOfSpeech = firstResult?.partOfSpeech;
							options.Add(new AnswerOption
							{
								Word = word,
								Definition = definition,
								PartOfSpeech = partOfSpeech,
								ButtonClass = (ButtonClass.Count > i) ? ButtonClass[i] : "btn-info"
							});
						}
					}
				}
				return options;
			}
		}
		public LoadWordResults? LoadWordResults { get; set; }
		public WordResult? WordResult { get; set; }

		// Lifecycle methods removed for code-behind partial class
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
