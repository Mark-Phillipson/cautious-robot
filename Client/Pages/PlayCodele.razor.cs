using System.Net.Http.Json;
using CodeleLogic;
using Microsoft.AspNetCore.Components;

namespace BlazorApp.Client.Pages;

public partial class PlayCodele : ComponentBase
{

    private ElementReference guess;
    private string message = "";
    private int counter = 0;
    // list of guesses the player has made so far
    private List<Guess> guesses = new();

    // the player's current guess
    private string? newGuess;

    // list of potential game answers (pulled from a json file)
    [Parameter]
    public string[]? answers { get; set; }

    // the answer for the current game
    [Parameter]
    public string? answer { get; set; }

    // # of tries the player has to guess the word
    public int attempts;

    // message to display in the modal popup depending on the game outcome
    private string? gameStatusMessage;

    // modal popup to display the game outcome
    private bool showModal { get; set; }

    // display message if guess is not 5 characters long
    private bool displayGuessSizeMsg { get; set; }

    // On load, set up the game
    protected override async Task OnInitializedAsync()
    {
        showModal = false;
        attempts = 1;
        answers = await Http.GetFromJsonAsync<string[]>("sample-data/codele-word-library.json");
        Random randomGenerator = new Random();
        if (answers != null)
        {
            answer = answers[randomGenerator.Next(0, answers.Length)];
        }
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await guess.FocusAsync();
        }
    }

    // use as Copilot example
    // Reset and start a new game
    private async Task StartNewGame()
    {
        showModal = false;
        guesses.Clear();
        newGuess = string.Empty;
        attempts = 1;
        Random randomGenerator = new Random();
        if (answers != null && answers.Length > 0)
        {
            answer = answers[randomGenerator.Next(0, answers.Length)];
        }
        await guess.FocusAsync();
    }

    // Close the game status modal
    private void ModalClose()
    {
        showModal = false;
    }

    // Submit and evaluate the player's guess
    private async Task SubmitGuess()
    {
        if (newGuess == null || answers == null)
        {
            return;
        }
        // check if guess is 5 characters long, display message if not
        if (newGuess?.Length != 5)
        {
            displayGuessSizeMsg = true;
            return;
        }
        else
        {
            newGuess = newGuess.ToLower();
            if (!answers.Any(x => x == newGuess))
            {
                message = "Invalid guess: not one of the words in the list.";
                displayGuessSizeMsg = true;
                return;
            }
            displayGuessSizeMsg = false;
            if (!string.IsNullOrEmpty(newGuess))
            {
                Guess guess = new Guess(newGuess);

                // if the player still has attempts left, evaluate their guess, otherwise display loss message
                if (attempts < 5 && answer != null)
                {
                    guess.GetGuessStatuses(answer);

                    // check if the player guessed correctly, display win message if true
                    if (guess.IsWinningGuess(answer))
                    {
                        showModal = true;
                        gameStatusMessage = "You Won!";
                    }
                    // if the player did not guess correctly, increment the attempt #
                    else
                    {
                        attempts++;
                    }
                }
                else
                {
                    if (answer != null)
                    {
                        guess.GetGuessStatuses(answer);
                    }

                    showModal = true;
                    gameStatusMessage = "You Lost!";
                }

                // add current guess to the player's list of guesses
                guesses.Add(guess);

                // clear the input box
                newGuess = string.Empty;
            }
        }
        await guess.FocusAsync();
    }

    // Method to determine CSS class for letter tiles based on their status
    private string GetLetterTileClass(LetterStatus status)
    {
        return status switch
        {
            LetterStatus.Correct => "correct",
            LetterStatus.IncorrectPosition => "incorrect-position",
            _ => "incorrect"
        };
    }
}