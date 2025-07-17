namespace BlazorApp.Client.Models
{
    /// <summary>
    /// Represents a word with its text and grammatical type for the Word Type Snap game
    /// </summary>
    public class Word
    {
        public string Text { get; set; } = string.Empty;
        public WordType Type { get; set; }

        public Word() { }

        public Word(string text, WordType type)
        {
            Text = text;
            Type = type;
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
    }
}
