using Microsoft.AspNetCore.Components;
using BlazorApp.Client.Models;
using Newtonsoft.Json;
using BlazorApp.Client.Helper;
using Microsoft.Extensions.Configuration;
using BlazorApp.Client;

namespace BlazorApp.Client.Pages
{
	public partial class WordsTest
	{
		private int currentQuestionNumber = 0;
		ElementReference LoadWordsButton;
		private string? response;
		string? result = "";
		private int score;
		WordsHelper? wordsHelper;
		private int wordsToLoad = 5;

		private async Task CheckAnswerAsync(string? guessedWord, int indexPosition)
		{
			Play = true;
			if (guessedWord == LoadWordResults?.WordResults?[currentQuestionNumber].word)
			{
				ButtonClass[indexPosition] = "btn-success";
				score++;
				dynamicClass = "";
				if (currentQuestionNumber < (WordsToLoad - 1))
				{
					currentQuestionNumber++;
					response = "";
					AnswerState = true;
					await Task.Delay(2000);
					wordResult = LoadWordResults?.WordResults?[currentQuestionNumber];
					dynamicClass = "animate__animated animate__backInUp";
					ButtonClass[indexPosition] = "btn-info";
				}
				else
				{
					AnswerState = true;
					await Task.Delay(2000);
					response = $"✅ ({guessedWord}) ";
					response = $"{response}. Click the Load Word button to continue...";
					ShowWord = false;
					dynamicClass = "animate__animated animate__backInUp";
					wordResult = null;
					ButtonClass[indexPosition] = "btn-info";
					await LoadWordsButton.FocusAsync();
				}
			}
			else
			{
				ButtonClass[indexPosition] = "btn-danger";
				AnswerState = false;
				await Task.Delay(2000);
				//response = $"✖{guessedWord}";
				ButtonClass[indexPosition] = "btn-info";
			}
			await LoadWordsButton.FocusAsync();
			Play = false;
		}
		private async Task LoadWordAsync()
		{
			LoadWordResults = null;
			response = "";
			score = 0;
			dynamicClass = "";
			currentQuestionNumber = 0;
			ShowWord = true;
			// Try to get the API key from the text box in the user interface
			if (APIKey != null && APIKey != "TBC")
			{
				HideKey = true;
				wordsHelper = new WordsHelper(APIKey);
			}
			if (wordsHelper != null)
			{
				LoadWordResults =
					await wordsHelper.LoadWord(WordsToLoad, MaximumWordLength ?? 20, BeginsWith?.ToLower());
			}
			Message = LoadWordResults?.Message;
			result = LoadWordResults?.Result;
			if (LoadWordResults?.WordResults?.Count > 0)
			{
				wordResult = LoadWordResults.WordResults[0];
			}
		}
		private async Task Reload()
		{
			ButtonClass = new List<string>();
			for (int i = 0; i < WordsToLoad; i++)
			{
				ButtonClass.Add("btn-info");
			}
			await LoadWordAsync();
		}
		public bool AnswerState { get; set; } = false;
		public bool Play { get; set; } = false;
		[Inject] HttpClient? client { get; set; }

		[Inject]
		IConfiguration? Configuration
		{
			get;
			set;
		}
		private string dynamicClass
		{
			get;
			set;
		}
		= "";
		int index
		{
			get;
			set;
		} = 0;
		LoadWordResults? LoadWordResults
		{
			get;
			set;
		}
		WordResult? wordResult
		{
			get;
			set;
		}

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
				}
				else
				{
					Message = "API key not found";
				}
			}
		}

		public string? APIKey { get; set; } = null;
		public string? BeginsWith
		{
			get;
			set;
		}
		= null;
		public List<string> ButtonClass
		{
			get;
			set;
		}
		= new List<string>(){
			"btn-info", "btn-info", "btn-info", "btn-info", "btn-info",
		};
		public bool HideKey
		{
			get;
			set;
		}
		= false;
		public int? MaximumWordLength
		{
			get;
			set;
		}
		= null;
		public string? Message
		{
			get;
			set;
		}
		public bool ShowWord
		{
			get;
			set;
		}
		= true;
		public int WordsToLoad
		{
			get => wordsToLoad;
			private set
			{
				if (wordsToLoad == value)
				{
					return;
				}

				wordsToLoad = value;
			}
		}
	}
}
