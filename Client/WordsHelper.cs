using Newtonsoft.Json;
using BlazorApp.Client.Models;
using System.Linq;

namespace BlazorApp.Client.Helper;

public class WordsHelper
{
    private readonly string _apiKey;

    public WordsHelper(string apiKey)
    {
        _apiKey = apiKey;
    }    public async static Task<LoadWordResults> GetRandomWord(string apiKey, int maximumWordsLength, string? beginsWith = null, string? wordType = null)
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
        };        using (var response = await client.SendAsync(request))
            try
            {                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error Response: {response.StatusCode} - {errorContent}");
                    loadWordResults.Message = $"API Error: {response.StatusCode} - {errorContent}";
                    
                    // Special handling for development environment CORS issues
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new Exception($"API Error: This might be a CORS issue in development. The API key works in production but may be blocked from localhost. Try testing on the production site instead. Status: {response.StatusCode} ({response.ReasonPhrase})");
                    }
                    else
                    {
                        throw new Exception($"Problem loading word. Response status code does not indicate success: {response.StatusCode} ({response.ReasonPhrase}). Please check your API key or try again later.");
                    }
                }
                response.EnsureSuccessStatusCode();
                loadWordResults.Result = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Exception: {httpEx.Message}");
                loadWordResults.Message = $"Network Error: {httpEx.Message}";
                throw new Exception($"Problem loading word: {httpEx.Message}");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"General Exception: {exception.Message}");
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
    }    public async static Task<bool> IsValidWord(string apiKey, string word)
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
    }    public async static Task<(bool isValid, string? definitionAndSynonyms)> IsValidWordWithDefinition(string apiKey, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return (false, null);

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
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var wordResult = JsonConvert.DeserializeObject<WordResult>(content);
                        var definition = wordResult?.results?.FirstOrDefault()?.definition;
                        var synonyms = wordResult?.results?.SelectMany(r => r.synonyms ?? Enumerable.Empty<string>()).Distinct().ToList();
                        
                        var result = "";
                        
                        // Add definition if available
                        if (!string.IsNullOrWhiteSpace(definition))
                        {
                            result = definition;
                        }
                        
                        // Add synonyms if available
                        if (synonyms != null && synonyms.Count > 0)
                        {
                            var synonymsText = $"Synonyms: {string.Join(", ", synonyms)}";
                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                result += $"\n\n{synonymsText}";
                            }
                            else
                            {
                                result = synonymsText;
                            }
                        }
                        
                        // If neither definition nor synonyms found
                        if (string.IsNullOrWhiteSpace(result))
                        {
                            result = "No definition or synonyms found in the dictionary.";
                        }
                        
                        return (true, result);
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error parsing word definition: {parseEx.Message}");
                        return (true, null); // Word is valid but couldn't parse definition
                    }
                }
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating word '{word}': {ex.Message}");
            return (false, null);
        }
    }
}
