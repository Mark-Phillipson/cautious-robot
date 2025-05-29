
using BlazorApp.Client.Helper;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Client.Pages
{
    public partial class Hangman : ComponentBase
    {
        [Inject] public required IJSRuntime JSRuntime { get; set; }
        private DotNetObjectReference<Hangman>? objRef;
        private WordsHelper? wordsHelper;
        [Inject] public required BlazorApp.Client.Shared.IApiKeyService ApiKeyService { get; set; }
        private bool loading = false;
        private string? apiKey = null;
        private bool ApiKeyAvailable => !string.IsNullOrWhiteSpace(apiKey);

        [Inject] public required IConfiguration Configuration { get; set; }
        private Dictionary<string, string> Words = new Dictionary<string, string>
        {
        { "APPLE", "A fruit that is typically red or green and grows on trees." },
        { "BANANA", "A long curved fruit with a yellow skin." },
        { "CHERRY", "A small round fruit that is typically red or black." },
        { "GRAPE", "A small juicy fruit that grows in clusters." },
        { "ORANGE", "A round citrus fruit with a tough bright reddish-yellow rind." }
        };

        private string CurrentWord = "";
        private string WordDescription = "Please click Load New Word to begin.";
        private List<char> CorrectGuesses = new List<char>();
        private List<char> IncorrectGuesses = new List<char>();

        private string GameStatus
        {
            get
            {
                if (CurrentWord.Length > 0 && CurrentWord.Replace(" ", "").All(letter => CorrectGuesses.Contains(letter)))
                {
                    return "You win!";
                }
                else if (IncorrectGuesses.Count >= 6)
                {
                    return $"The word was {CurrentWord}, You lose!";
                }
                else if (CurrentWord.Length == 0)
                {
                    return "First word not yet loaded.";
                }
                else if (CorrectGuesses.Count > 0 || IncorrectGuesses.Count > 0)
                {
                    return "Keep guessing...";
                }
                else
                {
                    return "";
                }
            }
        }

        private List<char> Alphabet = new List<char>
                    {
                         'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P' ,
                         'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L' ,
                         'Z', 'X', 'C', 'V', 'B', 'N', 'M'
                    };
        private Dictionary<string, string> TalonAlphabet = new Dictionary<string, string>
        {
            { "Q", "Quench" }, { "W", "Whale" }, { "E", "Eve" }, { "R", "Red" }, { "T", "Trap" },
            { "Y", "Yank" }, { "U", "Urge" }, { "I", "sIt" }, { "O", "Odd" }, { "P", "Pit" },
            { "A", "Act" }, { "S", "Sun" }, { "D", "Drum" }, { "F", "Fox" }, { "G", "Golf" },
            { "H", "Hot" }, { "J", "Jury" }, { "K", "crunch" }, { "L", "Look" },
            { "Z", "Zip" }, { "X", "pleX" }, { "C", "Cat" }, { "V", "Vest" }, { "B", "Bat" },
            { "N", "Near" }, { "M", "Mike" }
        };
        protected override async Task OnInitializedAsync()
        {
            apiKey = await ApiKeyService.GetApiKeyAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                objRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("registerKeyPress", objRef);
            }
        }
        [JSInvokable]
        public void OnKeyPress(string key)
        {
            // Handle the key press event here
            Console.WriteLine($"Key pressed: {key}");
            if (char.TryParse(key, out char letter) && char.IsLetter(letter))
            {
                //Check that the guess has not already been made
                if (!CorrectGuesses.Contains(char.ToUpper(letter)) && !IncorrectGuesses.Contains(char.ToUpper(letter)))
                {
                    MakeGuess(char.ToUpper(letter));
                }
            }
        }

        private async Task StartNewGame()
        {
            loading = true;
            apiKey = await ApiKeyService.GetApiKeyAsync();
            if (!ApiKeyAvailable)
            {
                loading = false;
                return;
            }
            wordsHelper = new WordsHelper(apiKey!);
            LoadWordResults loadWordResults;
            do
            {
                loadWordResults = await wordsHelper.LoadWord(1, 30, null, null);
                CurrentWord = loadWordResults?.WordResults?[0].word ?? "Testing";
            } while (CurrentWord.Any(char.IsDigit) || CurrentWord.Contains("-") || CurrentWord.Contains("."));
            WordDescription = loadWordResults?.WordResults?[0].results?[0].definition ?? "The word is testing";
            CurrentWord = CurrentWord.ToUpper();
            CorrectGuesses.Clear();
            IncorrectGuesses.Clear();
            loading = false;
        }

        private void MakeGuess(char letter)
        {
            if (CurrentWord.Contains(letter.ToString()))
            {
                CorrectGuesses.Add(letter);
            }
            else
            {
                IncorrectGuesses.Add(letter);
            }
            StateHasChanged();
        }
        public void Dispose()
        {
            objRef?.Dispose();
        }

        private async Task OnApiKeySaved()
        {
            apiKey = await ApiKeyService.GetApiKeyAsync();
            StateHasChanged();
        }

    }
}