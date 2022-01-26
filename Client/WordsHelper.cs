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

    public async static Task<LoadWordResults> GetRandomWord(string apiKey, int maximumWordsLength,string? beginsWith= null )
    {
        // https://www.wordsapi.com/ ( Documentation ) 500 requests per day free on basic
        var client = new HttpClient();
        LoadWordResults loadWordResults = new();
        var request = new HttpRequestMessage
        {

            Method = HttpMethod.Get,
            // RequestUri = new
            // Uri($"https://wordsapiv1.p.rapidapi.com/words/?random=true&partOfSpeech={partOfSpeech}"),
            RequestUri = new Uri(
                $"https://wordsapiv1.p.rapidapi.com/words/?random=true&hasDetails=definitions&lettersMax={maximumWordsLength}&letterPattern=^{beginsWith}."
            ),
            // RequestUri = new Uri($"https://wordsapiv1.p.rapidapi.com/words/{Word}"),
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

    public async Task<LoadWordResults> LoadWord(int wordsToLoad, int maximumWordsLength,string? BeginsWith)
    {
        var loadWordResults = new LoadWordResults() { LettersToShow = 1 };
        for (int i = 0; i < wordsToLoad; i++)
        {
            var loadWordResultsSingle = await GetRandomWord(_apiKey, maximumWordsLength,BeginsWith);
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
}
