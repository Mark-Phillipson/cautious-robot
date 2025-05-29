using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BlazorApp.Client.Helper;

namespace BlazorApp.Client.Pages
{
    public partial class ScrabbleGame : ComponentBase
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;

        protected List<char> tileRack = new();
        protected List<(int index, bool isCenter)> selectedLetters = new();
        protected char[] board = new char[15];
        private readonly Random rng = new();        protected int currentScore;
        protected int lastWordScore;
        protected string? validationMessage;
        protected string? currentWordDefinition;
        protected bool isValidatingWord;
        protected string userApiKey = "";
        protected bool gameStarted = false;
        protected string apiKeyValidationMessage = "";// Constants
        protected static readonly string ScrabbleLetters = "EEEEEEEEEEEEAAAAAAAAAIIIIIIIIONNNNNNRRRRRRTTTTTTLLLLSSSSUUUUDDDDGGGBBCCMMPPFFHHVVWWYYKJXQZ";
        protected readonly Dictionary<char, int> letterScores = new()
        {
            {'A', 1}, {'B', 3}, {'C', 3}, {'D', 2}, {'E', 1},
            {'F', 4}, {'G', 2}, {'H', 4}, {'I', 1}, {'J', 8},
            {'K', 5}, {'L', 1}, {'M', 3}, {'N', 1}, {'O', 1},
            {'P', 3}, {'Q', 10}, {'R', 1}, {'S', 1}, {'T', 1},
            {'U', 1}, {'V', 4}, {'W', 4}, {'X', 8}, {'Y', 4},
            {'Z', 10}
        };

        protected override void OnInitialized()
        {
            Console.WriteLine("ScrabbleGame: OnInitialized called");
            StartNewGame();
        }

        protected void SelectTile(int index)
        {
            Console.WriteLine("=== SelectTile Debug Info ===");
            Console.WriteLine($"SelectTile called with index: {index}");
            Console.WriteLine($"Current tile rack: [{string.Join(", ", tileRack)}]");
            Console.WriteLine($"Current selected letters: [{string.Join(", ", selectedLetters.Select(l => l.isCenter ? "Center" : tileRack[l.index].ToString()))}]");
            Console.WriteLine($"Tile rack count: {tileRack.Count}");

            try
            {
                if (index < 0 || index >= tileRack.Count)
                {
                    Console.WriteLine($"Invalid index: {index}");
                    return;
                }

                if (!selectedLetters.Any(l => l.index == index && !l.isCenter))
                {
                    selectedLetters.Add((index, false));
                    Console.WriteLine($"Added tile '{tileRack[index]}' to selection");
                }

                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SelectTile: {ex.Message}");
            }
        }

        protected void OnCellClick(int col)
        {
            Console.WriteLine($"OnCellClick: col={col}");
            if (col == 7 && !selectedLetters.Any(l => l.isCenter))
            {
                selectedLetters.Add((-1, true));  // Use -1 to indicate center letter
                Console.WriteLine($"Added center letter '{board[7]}' to selection");
                StateHasChanged();
            }
        }

        protected void RemoveLetter(char letter, bool isCenter)
        {
            if (isCenter)
            {
                selectedLetters.RemoveAll(l => l.isCenter);
            }
            else
            {
                var letterEntry = selectedLetters.FindIndex(l => !l.isCenter && tileRack[l.index] == letter);
                if (letterEntry != -1)
                {
                    selectedLetters.RemoveAt(letterEntry);
                }
            }
            StateHasChanged();
        }
        protected async Task PlayWord()
        {
            var word = GetSelectedWord();
            if (!word.Any() || !word.Any(w => w.isCenter))
            {
                validationMessage = "You must include the center letter in your word!";
                StateHasChanged();
                return;
            }

            var wordStr = string.Join("", word.Select(w => w.letter));

            if (wordStr.Length < 2)
            {
                validationMessage = "Word must be at least 2 letters long!";
                StateHasChanged();
                return;
            }

            // Clear any previous validation message and show loading
            validationMessage = null;
            isValidatingWord = true;
            StateHasChanged();            try
            {
                // Validate the word using the user's Words API
                if (string.IsNullOrEmpty(userApiKey))
                {
                    validationMessage = "API key not provided. Please enter your Words API key to validate words.";
                    isValidatingWord = false;
                    StateHasChanged();
                    return;
                }

                var (isValid, definition) = await WordsHelper.IsValidWordWithDefinition(userApiKey, wordStr);

                if (!isValid)
                {
                    validationMessage = $"'{wordStr}' is not a valid English word. Try a different combination!";
                    currentWordDefinition = null;
                    isValidatingWord = false;
                    StateHasChanged();
                    return;
                }

                // Store the definition for display
                currentWordDefinition = definition;

                // Word is valid, proceed with placing it on the board
                var centerIndex = word.FindIndex(w => w.isCenter);
                var startCol = 7 - centerIndex;

                // Clear the board except center
                for (int i = 0; i < board.Length; i++)
                {
                    if (i != 7) board[i] = '.';
                }

                // Place the word
                for (int i = 0; i < wordStr.Length; i++)
                {
                    if ((startCol + i) >= 0 && (startCol + i) < board.Length)
                    {
                        board[startCol + i] = wordStr[i];
                    }
                }

                // Score and cleanup
                lastWordScore = wordStr.Sum(c => letterScores[c]);
                currentScore += lastWordScore;

                // Replace used tiles
                foreach (var letter in selectedLetters.Where(l => !l.isCenter))
                {
                    tileRack[letter.index] = ScrabbleLetters[rng.Next(ScrabbleLetters.Length)];
                }

                validationMessage = $"Great! '{wordStr}' is a valid word worth {lastWordScore} points!";
                ClearSelection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating word: {ex.Message}");
                validationMessage = "Unable to validate word. Please try again.";
            }
            finally
            {
                isValidatingWord = false;
                StateHasChanged();
            }
        }        protected void ClearSelection()
        {
            selectedLetters.Clear();
            validationMessage = null;
            //currentWordDefinition = null;
            StateHasChanged();
        }
        protected void StartNewGame()
        {            try
            {
                Console.WriteLine("Starting new game");
                ClearSelection();
                currentScore = 0;
                lastWordScore = 0;
                validationMessage = null;
                currentWordDefinition = null;

                // Reset the board
                for (int i = 0; i < board.Length; i++)
                {
                    board[i] = i == 7 ? ScrabbleLetters[rng.Next(ScrabbleLetters.Length)] : '.';
                }

                // Generate new tile rack
                var newTiles = new List<char>();
                for (int i = 0; i < 7; i++)
                {
                    newTiles.Add(ScrabbleLetters[rng.Next(ScrabbleLetters.Length)]);
                }
                tileRack = newTiles;

                Console.WriteLine($"New tile rack: [{string.Join(", ", tileRack)}]");
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartNewGame: {ex.Message}");
            }
        }
        protected void ShuffleTiles()
        {
            ClearSelection();
            tileRack = Enumerable.Range(0, 7)
                .Select(_ => ScrabbleLetters[rng.Next(ScrabbleLetters.Length)])
                .ToList();
            StateHasChanged();
        }

        protected string GetBoardCell(int col) => board[col] == '.' ? "" : board[col].ToString(); protected List<(char letter, bool isCenter)> GetSelectedWord()
        {
            var word = new List<(char letter, bool isCenter)>();

            foreach (var letter in selectedLetters)
            {
                if (letter.isCenter)
                {
                    word.Add((board[7], true));
                    Console.WriteLine($"Added center letter: {board[7]}");
                }
                else
                {
                    word.Add((tileRack[letter.index], false));
                    Console.WriteLine($"Added tile: {tileRack[letter.index]}");
                }
            }

            var finalWord = string.Join("", word.Select(w => w.letter));
            Console.WriteLine($"Final word built: {finalWord}");
            return word;
        }

        protected bool HasSelectedLetters => selectedLetters.Any();
        protected async Task ValidateApiKeyAndStartGame()
        {
            if (string.IsNullOrWhiteSpace(userApiKey))
            {
                apiKeyValidationMessage = "Please enter your Words API key.";
                StateHasChanged();
                return;
            }

            apiKeyValidationMessage = "Validating API key...";
            StateHasChanged();

            try
            {
                // Test the API key by validating a simple word
                bool isValid = await WordsHelper.IsValidWord(userApiKey, "test");

                if (isValid || userApiKey.Length > 10) // Accept if test word validates or if key looks valid
                {
                    gameStarted = true;
                    apiKeyValidationMessage = "";
                    StartNewGame();
                }
                else
                {
                    apiKeyValidationMessage = "Invalid API key. Please check your key and try again.";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating API key: {ex.Message}");
                apiKeyValidationMessage = "Error validating API key. Please check your key and internet connection.";
                StateHasChanged();
            }
        }

        protected void ResetGame()
        {
            gameStarted = false;
            userApiKey = "";
            apiKeyValidationMessage = "";            currentScore = 0;
            lastWordScore = 0;
            validationMessage = null;
            currentWordDefinition = null;
            ClearSelection();
            StateHasChanged();
        }
    }
}
