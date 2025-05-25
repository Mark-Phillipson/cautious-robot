using Newtonsoft.Json;
using BlazorApp.Client.Models;

namespace BlazorApp.Client.Helper;

public class WordsHelper
{
    private readonly string _apiKey;

    public WordsHelper(string apiKey)
    {
        _apiKey = apiKey;
    }

    public async static Task<LoadWordResults> GetRandomWord(string apiKey, int maximumWordsLength, string? beginsWith = null, string? wordType = null)
    {
        // https://www.wordsapi.com/ ( Documentation ) 500 requests per day free on basic
        var client = new HttpClient();
        LoadWordResults loadWordResults = new();
        string uri = $"https://wordsapiv1.p.rapidapi.com/words/?random=true&hasDetails=definitions&lettersMax={maximumWordsLength}&letterPattern=^{beginsWith}.";
        if (wordType != null && (wordType == "verb" || wordType == "noun"))
        {
            uri = $"{uri}&partOfSpeech={wordType}";
        }
        var request = new HttpRequestMessage
        {

            Method = HttpMethod.Get,
            RequestUri = new Uri(
                uri
            ),
            Headers =
            {
                { "x-rapidapi-key", apiKey },
                { "x-rapidapi-host", "wordsapiv1.p.rapidapi.com" },
            },
        };

        using (var response = await client.SendAsync(request))
            try
            {
                response.EnsureSuccessStatusCode();
                loadWordResults.Result = await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                loadWordResults.Message = exception.Message;
                throw new Exception($"Problem loading word: {exception.Message}");
            }
        return loadWordResults;
    }

    public async Task<LoadWordResults> LoadWord(int wordsToLoad, int maximumWordsLength, string? BeginsWith, string? wordType)
    {
        var loadWordResults = new LoadWordResults() { LettersToShow = 1 };
        for (int i = 0; i < wordsToLoad; i++)
        {
            var loadWordResultsSingle = await GetRandomWord(_apiKey, maximumWordsLength, BeginsWith, wordType);
            try
            {
                if (loadWordResultsSingle.Result != null)
                {
                    var temporaryResult =
                        $"{loadWordResultsSingle.Result.Replace("\'", String.Empty)}";
                    temporaryResult = temporaryResult.Replace("�", String.Empty);
                    WordResult? wordResult = JsonConvert.DeserializeObject<WordResult>(
                        value: temporaryResult
                    );
                    if (wordResult != null)
                    {
                        loadWordResults?.WordResults?.Add(item: wordResult);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                loadWordResultsSingle.Message = exception.Message;
                throw new Exception($"Problem loading word: {exception.Message}");
            }
        }
        if (loadWordResults != null)
        {
            return loadWordResults;
        }
        else
        {
            throw new Exception("Load word results variable is unexpectedly empty!");
        }
    }

    public async static Task<bool> IsValidWord(string apiKey, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return false;

        var client = new HttpClient();
        string uri = $"https://wordsapiv1.p.rapidapi.com/words/{word.ToLower()}";
        
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(uri),
            Headers =
            {
                { "x-rapidapi-key", apiKey },
                { "x-rapidapi-host", "wordsapiv1.p.rapidapi.com" },
            },
        };

        try
        {
            using (var response = await client.SendAsync(request))
            {
                // If the word exists, the API returns 200 OK
                // If the word doesn't exist, it returns 404 Not Found
                return response.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating word '{word}': {ex.Message}");
            return false;
        }
    }
}
