namespace BlazorApp.Client.Models
{
    /// <summary>
    /// Represents a word with its text and grammatical type for the Word Type Snap game
    /// </summary>
    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public WordType Type { get; set; }
        public string ExampleSentence { get; set; } = string.Empty;

        public Word() { }

        public Word(string text, WordType type, string exampleSentence = "")
        {
            Text = text;
            Type = type;
            ExampleSentence = exampleSentence;
        }
    }

    /// <summary>
    /// Enumeration of word types for grammatical classification
    /// </summary>
    public enum WordType
    {
        Noun,
        Verb,
        Adjective,
        Adverb,
        Preposition,
        Pronoun,
        Conjunction,
        Interjection
    }

    /// <summary>
    /// Extension methods for WordType enum
    /// </summary>
    public static class WordTypeExtensions
    {
        public static string GetDisplayName(this WordType wordType)
        {
            return wordType switch
            {
                WordType.Noun => "Noun",
                WordType.Verb => "Verb",
                WordType.Adjective => "Adjective",
                WordType.Adverb => "Adverb",
                WordType.Preposition => "Preposition",
                WordType.Pronoun => "Pronoun",
                WordType.Conjunction => "Conjunction",
                WordType.Interjection => "Interjection",
                _ => wordType.ToString()
            };
        }

        public static string GetEmoji(this WordType wordType)
        {
            return wordType switch
            {
                WordType.Noun => "🏠",
                WordType.Verb => "🏃",
                WordType.Adjective => "🎨",
                WordType.Adverb => "⚡",
                WordType.Preposition => "🌉",
                WordType.Pronoun => "👤",
                WordType.Conjunction => "🔗",
                WordType.Interjection => "❗",
                _ => "📝"
            };
        }

        public static string GetDefinition(this WordType wordType)
        {
            return wordType switch
            {
                WordType.Noun => "a person, place, thing, or idea",
                WordType.Verb => "an action or state of being",
                WordType.Adjective => "describes a noun",
                WordType.Adverb => "describes a verb, adjective, or other adverb",
                WordType.Preposition => "shows relationship between words",
                WordType.Pronoun => "replaces a noun",
                WordType.Conjunction => "connects words or phrases",
                WordType.Interjection => "expresses emotion or exclamation",
                _ => "a word or part of speech"
            };
        }
    }

    /// <summary>
    /// Extension methods for Word class
    /// </summary>
    public static class WordExtensions
    {
        /// <summary>
        /// Gets the example sentence with the word highlighted in bold
        /// </summary>
        /// <param name="word">The word object</param>
        /// <returns>HTML string with the word highlighted in bold within the sentence</returns>
        public static string GetHighlightedSentence(this Word word)
        {
            if (string.IsNullOrEmpty(word.ExampleSentence))
            {
                return string.Empty;
            }

            // Find the word in the sentence (case-insensitive) and replace it with bold version
            var sentence = word.ExampleSentence;
            var wordText = word.Text;
            
            // Handle different forms of the word (e.g., "run" in "running", "runs", etc.)
            // First try exact match (case-insensitive)
            var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(wordText)}\b";
            var highlightedSentence = System.Text.RegularExpressions.Regex.Replace(
                sentence, 
                pattern, 
                $"<strong>{wordText}</strong>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // If no exact match found, try to find variations (root word)
            if (highlightedSentence == sentence)
            {
                // Try to find the word as part of another word (like "run" in "running")
                var rootPattern = $@"\b\w*{System.Text.RegularExpressions.Regex.Escape(wordText.ToLower())}\w*\b";
                var match = System.Text.RegularExpressions.Regex.Match(sentence, rootPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success)
                {
                    var foundWord = match.Value;
                    highlightedSentence = sentence.Replace(foundWord, $"<strong>{foundWord}</strong>", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // Fallback: just bold the original word at the beginning of the sentence
                    highlightedSentence = $"<strong>{wordText}</strong>: {sentence}";
                }
            }
            
            return highlightedSentence;
        }
    }
}
