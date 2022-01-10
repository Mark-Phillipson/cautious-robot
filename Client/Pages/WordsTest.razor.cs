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
        [Inject]
        HttpClient? client { get; set; }

        [Inject]
        IConfiguration? Configuration { get; set; }
        WordResult? wordResult { get; set; }
        LoadWordResults? LoadWordResults { get; set; }
        ElementReference LoadWordsButton;
        WordsHelper? wordsHelper;
        public int MaximumWordLength { get; set; } = 20;
        public string? Message { get; set; }
        string? result = "";
        private string? response;
        private string dynamicClass { get; set; } = "";
        public bool ShowWord { get; set; } = true;
        public int WordsToLoad { get; private set; } = 5;
        private int currentQuestionNumber = 0;
        private int score;

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
                var apiKey = Configuration["WordsApiKey"];
				if (apiKey== null || apiKey=="TBC")
				{
                	apiKey = Environment.GetEnvironmentVariable("WordsApiKey");
				}
                if (apiKey != null)
                {
                    wordsHelper = new WordsHelper(apiKey);
                    await LoadWordAsync();
                }
            }
        }

        private async Task CheckAnswerAsync(string? guessedWord)
        {
            if (guessedWord == LoadWordResults?.WordResults?[currentQuestionNumber].word)
            {
                score++;
                response = $"✅ ({guessedWord}) ";
                dynamicClass = "";
                if (currentQuestionNumber < (WordsToLoad - 1))
                {
                    currentQuestionNumber++;
                    await Task.Delay(2000);
                    response = "";
                    wordResult = LoadWordResults?.WordResults?[currentQuestionNumber];
                    dynamicClass = "animate__animated animate__backInUp";
                }
                else
                {
                    response = $"{response}. Click the Load Word button to continue...";
                    ShowWord = false;
                    wordResult = null;
                    await LoadWordsButton.FocusAsync();
                }
            }
            else
            {
                response = $"✖{guessedWord}";
            }
        }

        private async Task LoadWordAsync()
        {
            LoadWordResults = null;
            response = "";
            score = 0;
            dynamicClass = "";
            currentQuestionNumber = 0;
            ShowWord = true;
            if (wordsHelper != null)
            {
                LoadWordResults = await wordsHelper.LoadWord(WordsToLoad, MaximumWordLength);
            }
            Message = LoadWordResults?.Message;
            result = LoadWordResults?.Result;
            if (LoadWordResults?.WordResults?.Count > 0)
            {
                wordResult = LoadWordResults.WordResults[0];
            }
        }
    }
}
