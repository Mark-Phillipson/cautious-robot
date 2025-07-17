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
                WordType.Noun => "A person, place, thing, or idea",
                WordType.Verb => "An action or state of being",
                WordType.Adjective => "Describes a noun",
                WordType.Adverb => "Describes a verb, adjective, or other adverb",
                WordType.Preposition => "Shows relationship between words",
                WordType.Pronoun => "Replaces a noun",
                WordType.Conjunction => "Connects words or phrases",
                WordType.Interjection => "Expresses emotion or exclamation",
                _ => "A word or part of speech"
            };
        }
    }

    /// <summary>
    /// Extension methods for Word class
    /// </summary>
    public static class WordExtensions
    {
        /// <summary>
        /// Gets the example sentence with the word highlighted in bold and all words wrapped as clickable elements
        /// </summary>
        /// <param name="word">The word object</param>
        /// <returns>HTML string with the word highlighted in bold and all words clickable</returns>
        public static string GetSentenceWithClickableWords(this Word word)
        {
            if (string.IsNullOrEmpty(word.ExampleSentence))
            {
                return string.Empty;
            }

            var sentence = word.ExampleSentence;
            var wordText = word.Text;
            
            // Split sentence into words while preserving punctuation and spaces
            var parts = System.Text.RegularExpressions.Regex.Split(sentence, @"(\W+)");
            var result = new System.Text.StringBuilder();
            var wordIndex = 0; // Track word position for unique IDs
            
            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part) || System.Text.RegularExpressions.Regex.IsMatch(part, @"^\W+$"))
                {
                    // Keep whitespace and punctuation as is
                    result.Append(part);
                }
                else
                {
                    // This is a word - determine its type and make it clickable
                    var wordType = DetermineWordType(part);
                    var isTargetWord = IsTargetWord(part, wordText);
                    
                    var cssClass = isTargetWord ? "target-word clickable-word" : "clickable-word";
                    
                    result.Append($"<span class=\"{cssClass}\" " +
                                 $"data-word=\"{part}\" " +
                                 $"data-type=\"{wordType.GetDisplayName()}\" " +
                                 $"data-definition=\"{wordType.GetDefinition()}\" " +
                                 $"data-emoji=\"{wordType.GetEmoji()}\" " +
                                 $"data-word-id=\"word-{wordIndex}\">");
                    
                    if (isTargetWord)
                    {
                        result.Append($"<strong>{part}</strong>");
                    }
                    else
                    {
                        result.Append(part);
                    }
                    result.Append("</span>");
                    wordIndex++;
                }
            }
            
            return result.ToString();
        }

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

        /// <summary>
        /// Determines if a word part matches the target word (handles variations like "running" for "run")
        /// </summary>
        /// <param name="wordPart">The word part from the sentence</param>
        /// <param name="targetWord">The target word to match against</param>
        /// <returns>True if the word part matches the target word</returns>
        private static bool IsTargetWord(string wordPart, string targetWord)
        {
            // Exact match (case-insensitive)
            if (string.Equals(wordPart, targetWord, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Check if the word part contains the target word (for variations like "running" contains "run")
            if (wordPart.Length > targetWord.Length && 
                wordPart.ToLower().Contains(targetWord.ToLower()))
                return true;
            
            return false;
        }

        /// <summary>
        /// Determines the word type of a given word using basic heuristics
        /// Note: This is a simplified classification and may not be 100% accurate for all words
        /// </summary>
        /// <param name="wordPart">The word to classify</param>
        /// <returns>The determined WordType</returns>
        private static WordType DetermineWordType(string wordPart)
        {
            var word = wordPart.ToLower().Trim();
            
            // Common pronouns
            if (IsInList(word, "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them", 
                        "my", "your", "his", "her", "its", "our", "their", "mine", "yours", "hers", "ours", "theirs",
                        "this", "that", "these", "those", "who", "whom", "whose", "which", "what"))
                return WordType.Pronoun;
            
            // Common prepositions
            if (IsInList(word, "in", "on", "at", "by", "for", "with", "to", "from", "of", "about", "under", "over",
                        "through", "between", "among", "during", "before", "after", "above", "below", "beside",
                        "behind", "across", "around", "against", "within", "without", "upon", "beneath"))
                return WordType.Preposition;
            
            // Common conjunctions
            if (IsInList(word, "and", "but", "or", "nor", "for", "so", "yet", "because", "since", "although",
                        "though", "while", "whereas", "if", "unless", "until", "when", "where", "why", "how"))
                return WordType.Conjunction;
            
            // Common interjections
            if (IsInList(word, "oh", "ah", "wow", "hey", "hi", "hello", "goodbye", "yes", "no", "well", "hmm",
                        "ouch", "yay", "hooray", "alas", "oops", "phew", "shh", "psst"))
                return WordType.Interjection;
            
            // Common adverbs (often end in -ly, but not always)
            if (word.EndsWith("ly") && word.Length > 3)
                return WordType.Adverb;
            
            if (IsInList(word, "very", "quite", "rather", "too", "so", "really", "always", "never", "often",
                        "sometimes", "here", "there", "now", "then", "today", "yesterday", "tomorrow"))
                return WordType.Adverb;
            
            // Common adjectives
            if (word.EndsWith("ful") || word.EndsWith("less") || word.EndsWith("ous") || word.EndsWith("ive") ||
                word.EndsWith("able") || word.EndsWith("ible") || word.EndsWith("ant") || word.EndsWith("ent"))
                return WordType.Adjective;
            
            if (IsInList(word, "good", "bad", "big", "small", "hot", "cold", "fast", "slow", "happy", "sad",
                        "beautiful", "ugly", "smart", "stupid", "easy", "hard", "clean", "dirty", "new", "old",
                        "young", "high", "low", "long", "short", "wide", "narrow", "thick", "thin", "heavy", "light"))
                return WordType.Adjective;
            
            // Common verbs
            if (word.EndsWith("ing") || word.EndsWith("ed"))
                return WordType.Verb;
            
            // Check for third-person singular verbs ending in 's' (but exclude common plural nouns)
            if (word.EndsWith("s") && !word.EndsWith("ss") && !IsCommonPluralNoun(word))
                return WordType.Verb;
            
            if (IsInList(word, "be", "is", "are", "was", "were", "been", "being", "have", "has", "had", "do", "does",
                        "did", "will", "would", "could", "should", "may", "might", "can", "must", "go", "come",
                        "see", "look", "hear", "feel", "think", "know", "say", "tell", "get", "take", "give",
                        "make", "put", "run", "walk", "eat", "drink", "sleep", "work", "play", "read", "write"))
                return WordType.Verb;
            
            // Default to noun if no other pattern matches
            return WordType.Noun;
        }
        
        /// <summary>
        /// Helper method to check if a word is in a list of words
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <param name="wordList">The list of words to check against</param>
        /// <returns>True if the word is in the list</returns>
        private static bool IsInList(string word, params string[] wordList)
        {
            return wordList.Contains(word, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Helper method to check if a word is a common plural noun that ends in 's'
        /// This helps distinguish between plural nouns and third-person singular verbs
        /// </summary>
        /// <param name="word">The word to check</param>
        /// <returns>True if the word is a common plural noun</returns>
        private static bool IsCommonPluralNoun(string word)
        {
            // Common plural nouns that might be mistaken for verbs
            return IsInList(word, "flowers", "books", "cars", "houses", "trees", "animals", "people", "children",
                          "dogs", "cats", "birds", "shoes", "clothes", "games", "toys", "colors", "words", "letters",
                          "numbers", "pictures", "stories", "friends", "family", "parents", "students", "teachers",
                          "computers", "phones", "tables", "chairs", "windows", "doors", "keys", "glasses", "bags",
                          "papers", "pencils", "pens", "minutes", "hours", "days", "weeks", "months", "years",
                          "places", "countries", "cities", "streets", "roads", "buildings", "rooms", "bathrooms",
                          "kitchens", "bedrooms", "gardens", "parks", "stores", "restaurants", "schools", "hospitals",
                          "libraries", "museums", "theaters", "movies", "songs", "videos", "photos", "emails");
        }
    }
}
