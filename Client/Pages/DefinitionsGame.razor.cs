using Microsoft.AspNetCore.Components;
using BlazorApp.Client.Models;
using BlazorApp.Client.Helper;
using BlazorApp.Client.Shared;
using BlazorApp.Client.Pages;

namespace BlazorApp.Client.Pages;

public partial class DefinitionsGame : ComponentBase
{
	[Inject] public required BlazorApp.Client.Shared.IWordsApiKeyService ApiKeyService { get; set; }
	private string? apiKey = null;
	public List<string> ButtonClass { get; set; } = new List<string>() { "btn-info", "btn-info", "btn-info", "btn-info", "btn-info", };
	public bool HideKey { get; set; } = false;
	public string? Message { get; set; }
	public bool ShowWord { get; set; } = true;

	public bool ApiKeyAvailable => !string.IsNullOrWhiteSpace(apiKey); public async Task OnApiKeySaved()
	{
		apiKey = await ApiKeyService.GetApiKeyAsync();
		GameOptions.APIKey = apiKey;
		HideKey = true;
		await LoadWordAsync();
		// StateHasChanged();
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
	public string? response;
	public string? result = "";
	public WordsHelper? wordsHelper;
	public int questionsAnswered = 0;
	public int questionsCorrect = 0;
	public int counter = 0;	private bool isLoading = true; // Start with loading = true until we know the API key status
	
	public async Task CheckAnswerAsync(string? guessedWord, int indexPosition)
	{
		PlayAudio = true;
		// Debug logging
		Console.WriteLine($"CheckAnswerAsync: indexPosition={indexPosition}, ButtonClass.Count={ButtonClass.Count}");
		Console.WriteLine($"guessedWord='{guessedWord}', currentQuestionNumber={currentQuestionNumber}");
		
		if (LoadWordResults?.WordResults != null && currentQuestionNumber < LoadWordResults.WordResults.Count)
		{
			var correctWord = LoadWordResults.WordResults[currentQuestionNumber].word;
			Console.WriteLine($"Correct word='{correctWord}', Match={guessedWord == correctWord}");
			
			// Additional debug - show all available words and their RandomOrder
			for (int j = 0; j < LoadWordResults.WordResults.Count; j++)
			{
				var wr = LoadWordResults.WordResults[j];
				Console.WriteLine($"Word[{j}]: '{wr.word}', RandomOrder: {wr.RandomOrder}");
			}
			
			// Show ordered words (how they appear in AnswerOptions)
			var orderedWords = LoadWordResults.WordResults.OrderBy(x => x.RandomOrder).ToList();
			for (int k = 0; k < orderedWords.Count; k++)
			{
				Console.WriteLine($"OrderedWord[{k}]: '{orderedWords[k].word}', OriginalIndex: {LoadWordResults.WordResults.IndexOf(orderedWords[k])}");
			}
		}
				// Defensive check to ensure ButtonClass is properly sized and ordered
		if (LoadWordResults?.WordResults != null && ButtonClass.Count != LoadWordResults.WordResults.Count)
		{
			Console.WriteLine($"ButtonClass count mismatch. Resizing from {ButtonClass.Count} to {LoadWordResults.WordResults.Count}");
			var orderedResults = LoadWordResults.WordResults.OrderBy(x => x.RandomOrder).ToList();
			ButtonClass = Enumerable.Repeat("btn-info", orderedResults.Count).ToList();
		}
				if (guessedWord == LoadWordResults?.WordResults?[currentQuestionNumber].word)
		{
			Console.WriteLine("CORRECT ANSWER - Changing button to green");
			// Correct answer - show green button
			if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			{
				Console.WriteLine($"Setting ButtonClass[{indexPosition}] to btn-success");
				ButtonClass[indexPosition] = "btn-success";
				StateHasChanged(); // Force UI update to show green
			}
			else
			{
				Console.WriteLine($"Index out of bounds: {indexPosition} not in range 0-{ButtonClass.Count-1}");
			}
			questionsAnswered++;
			questionsCorrect++;
			dynamicClass = "";
			AnswerState = true;
			
			// Wait 1 second to show the green color
			await Task.Delay(1000);
			
			if (currentQuestionNumber < (wordsToLoad - 1))
			{
				// More questions - move to next
				currentQuestionNumber++;
				response = "";
				WordResult = LoadWordResults?.WordResults?[currentQuestionNumber];
				dynamicClass = "animate__animated animate__backInUp";
						// Reset button color back to info
				if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
				{
					ButtonClass[indexPosition] = "btn-info";
					StateHasChanged(); // Force UI update
				}
			}
			else
			{				// Last question completed - reset and load new words
				dynamicClass = "animate__animated animate__backInUp";
						// Reset button color back to info
				if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
				{
					ButtonClass[indexPosition] = "btn-info";
					StateHasChanged(); // Force UI update
				}
						ShowWord = false;
				PlayAudio = false;
				await LoadWordAsync();
			}
		}		else
		{
			Console.WriteLine("WRONG ANSWER - Changing button to red");
			// Wrong answer - show red button
			if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			{
				Console.WriteLine($"Setting ButtonClass[{indexPosition}] to btn-danger");
				ButtonClass[indexPosition] = "btn-danger";
				StateHasChanged(); // Force UI update to show red
			}
			else
			{
				Console.WriteLine($"Index out of bounds: {indexPosition} not in range 0-{ButtonClass.Count-1}");
			}
			questionsCorrect--;
			AnswerState = false;
			
			// Wait 1 second to show the red color
			await Task.Delay(1000);
			
			// Reset button color back to info
			if (indexPosition >= 0 && indexPosition < ButtonClass.Count)
			{
				ButtonClass[indexPosition] = "btn-info";
				StateHasChanged(); // Force UI update
			}
		}
		
		await LoadWordsButton.FocusAsync();
		PlayAudio = false;
	}
	private async Task LoadWordAsync()
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
		ShowWord = true;            // Use API key from service if available
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
			try
			{
				LoadWordResults =
					await wordsHelper.LoadWord(wordsToLoad, GameOptions?.MaximumWordLength ?? 20, GameOptions?.BeginsWith?.ToLower(), null);
			}
			catch (Exception ex)
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
				Console.WriteLine($"DefinitionsGame API Error: {ex.Message}");
			}			// Process successful API response
			if (LoadWordResults?.WordResults != null)
			{
				// Ensure ButtonClass matches the number of answer options and follows the same RandomOrder
				var orderedResults = LoadWordResults.WordResults.OrderBy(x => x.RandomOrder).ToList();
				ButtonClass = Enumerable.Repeat("btn-info", orderedResults.Count).ToList();

				if (LoadWordResults.WordResults.Count > 0)
				{
					WordResult = LoadWordResults.WordResults[0];
				}
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
	public int Index { get; set; } = 0;	// Expose ordered answer options for rendering
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
							PartOfSpeech = partOfSpeech
							// Removed ButtonClass assignment - binding directly to ButtonClass[i] in Razor
						});
					}
				}
			}
			return options;
		}
	}
	public LoadWordResults? LoadWordResults { get; set; }
	public WordResult? WordResult { get; set; }
	protected override async Task OnInitializedAsync()
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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			await LoadWordsButton.FocusAsync();
		}
	}
	private void ShowOptions()
	{
		if (GameOptions != null)
		{
			GameOptions.ShowOptions = !GameOptions.ShowOptions;
		}
	}
}
