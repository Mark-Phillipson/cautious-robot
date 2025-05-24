using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Pages
{
    public partial class ScrabbleGame : ComponentBase
    {
        private List<char> tileRack = new();
        private List<int> selectedTileIndices = new();
        private char[] board = new char[15];
        private bool centerLetterSelected;
        private int? centerLetterPosition;
        private readonly Random rng = new();
        private int currentScore;
        private int lastWordScore;
        private bool isProcessingClick = false;

        // Constants
        private static readonly string ScrabbleLetters = "EEEEEEEEEEEEAAAAAAAAAIIIIIIIIONNNNNNRRRRRRTTTTTTLLLLSSSSUUUUDDDDGGGBBCCMMPPFFHHVVWWYYKJXQZ";
        private readonly Dictionary<char, int> letterScores = new()
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
            StartNewGame();
        }

        private void SelectTile(int index)
        {
            Console.WriteLine($"SelectTile called with index: {index}");
            Console.WriteLine($"Current tile rack: [{string.Join(", ", tileRack)}]");
            Console.WriteLine($"Current selected indices: [{string.Join(", ", selectedTileIndices)}]");
            Console.WriteLine($"Center letter selected: {centerLetterSelected}, position: {centerLetterPosition}");

            try
            {
                if (index < 0 || index >= tileRack.Count)
                {
                    Console.WriteLine($"Invalid index: {index}");
                    return;
                }

                char selectedTile = tileRack[index];
                Console.WriteLine($"Attempting to select tile '{selectedTile}' at index {index}");

                if (!selectedTileIndices.Contains(index))
                {
                    var newIndices = new List<int>(selectedTileIndices);
                    newIndices.Add(index);
                    selectedTileIndices = newIndices;
                    
                    // If center letter hasn't been selected yet, increment its future position
                    if (!centerLetterSelected)
                    {
                        centerLetterPosition = selectedTileIndices.Count;
                    }
                    
                    Console.WriteLine($"Added index {index} to selection. Selected indices now: [{string.Join(", ", selectedTileIndices)}]");
                    Console.WriteLine($"Center letter position is now: {centerLetterPosition}");
                }
                else
                {
                    Console.WriteLine($"Index {index} was already selected");
                }
                
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SelectTile: {ex.Message}");
            }
        }

        private void OnCellClick(int col)
        {
            Console.WriteLine($"OnCellClick: col={col}, centerLetterSelected={centerLetterSelected}");
            if (col == 7 && !centerLetterSelected)
            {
                centerLetterSelected = true;
                // If no tiles selected yet, position will be 0
                centerLetterPosition = 0;
                Console.WriteLine("Center letter selected, position set to 0");
                StateHasChanged();
            }
        }

        private void RemoveLetter(char letter, bool isCenter)
        {
            if (isCenter)
            {
                centerLetterSelected = false;
                centerLetterPosition = null;
            }
            else
            {
                var index = selectedTileIndices.FindIndex(i => tileRack[i] == letter);
                if (index != -1)
                {
                    selectedTileIndices.RemoveAt(index);
                }
            }
            StateHasChanged();
        }

        private void PlayWord()
        {
            var word = GetSelectedWord();
            if (!word.Any() || !word.Any(w => w.isCenter)) return;

            var wordStr = string.Join("", word.Select(w => w.letter));
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
            foreach (var index in selectedTileIndices.OrderByDescending(i => i))
            {
                tileRack[index] = ScrabbleLetters[rng.Next(ScrabbleLetters.Length)];
            }

            ClearSelection();
        }

        private void ClearSelection()
        {
            selectedTileIndices.Clear();
            centerLetterSelected = false;
            centerLetterPosition = null;
            StateHasChanged();
        }

        private void StartNewGame()
        {
            try
            {
                Console.WriteLine("Starting new game");
                ClearSelection();
                currentScore = 0;
                lastWordScore = 0;
                
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

        private void ShuffleTiles()
        {
            ClearSelection();
            tileRack = Enumerable.Range(0, 7)
                .Select(_ => ScrabbleLetters[rng.Next(ScrabbleLetters.Length)])
                .ToList();
            StateHasChanged();
        }

        private string GetBoardCell(int col) => board[col] == '.' ? "" : board[col].ToString();

        private List<(char letter, bool isCenter)> GetSelectedWord()
        {
            var word = new List<(char letter, bool isCenter)>();
            
            // If center letter is selected and position is valid
            if (centerLetterSelected && centerLetterPosition.HasValue)
            {
                // Add tiles before center letter
                for (int i = 0; i < centerLetterPosition.Value && i < selectedTileIndices.Count; i++)
                {
                    word.Add((tileRack[selectedTileIndices[i]], false));
                }
                
                // Add center letter
                word.Add((board[7], true));
                
                // Add remaining tiles
                for (int i = centerLetterPosition.Value; i < selectedTileIndices.Count; i++)
                {
                    word.Add((tileRack[selectedTileIndices[i]], false));
                }
            }
            else
            {
                // If no center letter selected, just add all selected tiles
                foreach (var index in selectedTileIndices)
                {
                    word.Add((tileRack[index], false));
                }
            }
            
            Console.WriteLine($"Built word: {string.Join("", word.Select(w => w.letter))}");
            return word;
        }

        private bool HasSelectedLetters => selectedTileIndices.Any() || centerLetterSelected;
    }
}
