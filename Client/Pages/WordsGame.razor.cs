using Microsoft.AspNetCore.Components;
using BlazorApp.Client.Models;
using BlazorApp.Client.Helper;

namespace BlazorApp.Client.Pages
{	public partial class WordsGame
	{		[Inject] public required BlazorApp.Client.Shared.IWordsApiKeyService ApiKeyService { get; set; }
		private string? apiKey = null;
		public bool ApiKeyAvailable => !string.IsNullOrWhiteSpace(apiKey);		public async Task OnApiKeySaved()
		{
			apiKey = await ApiKeyService.GetApiKeyAsync();
			GameOptions.APIKey = apiKey;
			HideKey = true;
			await LoadWordAsync();
			StateHasChanged();
		}

		public async Task OnChangeApiKey()
		{
			// Clear the stored API key and reset state
			await ApiKeyService.ClearApiKeyAsync();
			apiKey = null;
			GameOptions.APIKey = null;
			HideKey = false;
			Message = null;
			LoadWordResults = null;
			WordResult = null;
			isLoading = false;
			StateHasChanged();
		}
		private int currentQuestionNumber = 0;
		ElementReference LoadWordsButton;
		private string? response;
		string? result = "";
		WordsHelper? wordsHelper;		private int questionsAnswered = 0;
		private int questionsCorrect = 0;
		private int wordCounter = 0;
		private bool isLoading = true; // Start with loading = true until we know the API key status
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

		}		private async Task LoadWordAsync()
		{
			isLoading = true;
			StateHasChanged();
			
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
			ReloadButtonClass();			// Use API key from service if available
			if (apiKey == null)
			{
				apiKey = await ApiKeyService.GetApiKeyAsync();
			}
			if (!string.IsNullOrWhiteSpace(apiKey))
			{
				GameOptions.APIKey = apiKey;
				HideKey = true;
				wordsHelper = new WordsHelper(apiKey);
			}			if (wordsHelper != null)
			{
				try
				{
					LoadWordResults =
						await wordsHelper.LoadWord(wordsToLoad, GameOptions?.MaximumWordLength ?? 20, GameOptions?.BeginsWith?.ToLower(), GameOptions?.WordType);
				}				catch (Exception ex)
				{
					var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" || 
					                   Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
					
					if (isDevelopment && ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
					{
						Message = $"Development Mode: API blocked due to CORS. This works in production. Error: {ex.Message}";
					}
					else
					{
						Message = $"API Error: {ex.Message}. Please check your API key or try again later.";
					}
					LoadWordResults = null;
					Console.WriteLine($"WordsGame API Error: {ex.Message}");
				}
				
				// Process successful API response
				if (LoadWordResults?.WordResults != null && LoadWordResults.WordResults.Count > 0)
				{
					WordResult = LoadWordResults.WordResults[0];
				}
				
				// Update message and result from API response
				if (string.IsNullOrEmpty(Message))
				{
					Message = LoadWordResults?.Message;
				}
				result = LoadWordResults?.Result;
			}
			else
			{
				// No API key available - set proper state
				Message = "Please enter your Words API key to load words.";
			}
			
			// Always ensure loading state is properly ended
			isLoading = false;
			StateHasChanged();
			StateHasChanged();
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
		}		protected override async Task OnInitializedAsync()
		{
			apiKey = await ApiKeyService.GetApiKeyAsync();
			if (ApiKeyAvailable)
			{
				GameOptions.APIKey = apiKey;
				HideKey = true;
				await LoadWordAsync();
			}
			else
			{
				// Ensure loading state is properly handled when no API key exists
				isLoading = false;
				StateHasChanged();
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
