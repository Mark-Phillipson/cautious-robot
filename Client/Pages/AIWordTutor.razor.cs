using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text;
using BlazorApp.Client.Shared;

namespace BlazorApp.Client.Pages
{
    public partial class AIWordTutor : ComponentBase, IDisposable
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] public required IOpenAIApiKeyService OpenAIApiKeyService { get; set; }
        [Inject] public required IOpenAIService OpenAIService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;        // UI references
        private ElementReference chatHistoryContainer;
        private ElementReference chatInputElement;
        private ElementReference answerTextAreaRef;
        private ElementReference feedbackSectionRef;

        // Game state
        private bool gameStarted = false;
        private GameMode currentGameMode = GameMode.StoryAdventure;
        private DifficultyLevel difficulty = DifficultyLevel.Intermediate;
        private int score = 0;
        private int streak = 0;
        private bool isLoading = false;
        private string errorMessage = "";

        // Learning session data
        private AILearningSession? currentSession;        private string? currentContent = "";
        private List<string> conversationHistory = new();
        private string userInput = "";
        private List<WordChallenge> currentChallenges = new();
        private int currentChallengeIndex = 0;
        private bool PlayAudio = false;
        private bool hasApiKey = false;        // Feedback system
        private bool showFeedback = false;
        private string feedbackMessage = "";
        private bool lastAnswerCorrect = false;
        private string correctAnswer = "";
        private Timer? feedbackTimer;
        private bool showProgress = true;
        
        // Countdown timer for feedback popup
        private int countdownSeconds = 10;
        private int totalCountdownSeconds = 10;
        private Timer? countdownTimer;

        // Progress percentage for countdown visualization
        private double ProgressPercentage => totalCountdownSeconds > 0 ? 
            ((double)(totalCountdownSeconds - countdownSeconds) / totalCountdownSeconds) * 100 : 0;

        // Conversation practice scoring
        private List<string> conversationTargetWords = new();
        private HashSet<string> usedTargetWords = new();
        private int wordsUsedCorrectly = 0;

        // Text-to-speech state
        private bool isReading = false;
        private bool speechSupported = true;

        // Chat message text-to-speech state
        private bool isReadingChat = false;
        private string currentReadingMessage = "";

        // Hangman hint text-to-speech state
        private bool isReadingHangmanHint = false;

        // Browser detection
        private bool isEdgeBrowser = false;
        private bool isMobileDevice = false;

        private string? themeInput = string.Empty;

        // Picture guess mode state
        private string? pictureGuessImageDataUrl;
        private string pictureGuessTargetWord = string.Empty;
        private string pictureGuessUserGuess = string.Empty;
        private string pictureGuessStatusMessage = string.Empty;
        private readonly HashSet<string> pictureGuessUsedWords = new(StringComparer.OrdinalIgnoreCase);
        private string pictureGuessHint = string.Empty;
        private int pictureGuessWrongGuesses = 0;
        private ElementReference pictureGuessInputElement;

        private bool showPictureGuessToast = false;
        private string pictureGuessToastMessage = string.Empty;
        private Timer? pictureGuessToastTimer;

        private static readonly HashSet<string> PictureGuessBlockedWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "sex", "sexual", "sexy", "nude", "nudity", "porn", "pornography", "erotic",
            "fetish", "lingerie", "strip", "stripping", "stripper",
            "penis", "vagina", "breast", "breasts", "boob", "boobs", "nipple", "nipples",
            "condom", "orgasm", "brothel"
        };

        private static bool IsPictureGuessAllowedWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return false;
            return !PictureGuessBlockedWords.Contains(word);
        }
        private static readonly string[] DefaultThemes = new[]
        {
            "Nature", "Travel", "Food", "Technology", "Sports", "Music", "Friendship", "Adventure", "School", "Weather",
            "Animals", "Science", "Art", "History", "Health", "Family", "Hobbies", "Careers", "Transportation", "Clothing",
            "Emotions", "Business", "Environment", "Culture", "Entertainment", "Cooking", "Gardening", "Literature",
            "Photography", "Fitness", "Shopping", "Holidays", "Communication", "Medicine", "Architecture", "Geography",
            "Movies", "Books", "Dance", "Theater", "Politics", "Economics", "Philosophy", "Psychology", "Astronomy",
            "Ocean", "Mountains", "Cities", "Countries", "Languages", "Celebrations", "Inventions", "Discoveries","Military","Navy","Army","Airforce","Drones","Cyber","Code","Encryption"
        };
        private static readonly Random _random = new(Environment.TickCount);

        private HashSet<char> HangmanGuessedLettersUpper => new HashSet<char>(hangmanGuesses.Select(c => char.ToUpperInvariant(c)));

        protected override async Task OnInitializedAsync()
        {
            // Always start with a random theme to ensure variety
            if (string.IsNullOrWhiteSpace(themeInput))
            {
                themeInput = GetRandomTheme();
                Console.WriteLine($"Initial random theme selected: {themeInput}");
            }
            
            // Check if API key already exists
            var apiKey = await OpenAIApiKeyService.GetApiKeyAsync();
            hasApiKey = !string.IsNullOrEmpty(apiKey);
            
            // Detect browser type
            await DetectBrowser();
        }

        private string GetRandomTheme()
        {
            var themes = DefaultThemes;
            return themes[_random.Next(themes.Length)];
        }

        private async Task DetectBrowser()
        {
            try
            {
                var userAgent = await JSRuntime.InvokeAsync<string>("eval", "navigator.userAgent");
                // Check for both the new Edge (Edg/) and legacy Edge (Edge/)
                isEdgeBrowser = userAgent.Contains("Edg/") || userAgent.Contains("Edge/");
                
                // Detect mobile devices - improved detection
                isMobileDevice = userAgent.Contains("Mobile") || 
                                userAgent.Contains("Android") || 
                                userAgent.Contains("iPhone") || 
                                userAgent.Contains("iPad") || 
                                userAgent.Contains("iPod") ||
                                userAgent.Contains("Windows Phone") ||
                                userAgent.Contains("BlackBerry");
                
                // Also check screen width as a fallback
                try
                {
                    var screenWidth = await JSRuntime.InvokeAsync<int>("eval", "window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth");
                    if (screenWidth <= 768)
                    {
                        isMobileDevice = true;
                    }
                }
                catch
                {
                    // Fallback if screen width detection fails
                }
                
                Console.WriteLine($"Browser detected - User Agent: {userAgent}, Is Edge: {isEdgeBrowser}, Is Mobile: {isMobileDevice}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting browser: {ex.Message}");
                isEdgeBrowser = false; // Default to false if detection fails
                isMobileDevice = false; // Default to false if detection fails
            }
        }

        private async Task EnsureMobileDetection()
        {
            try
            {
                // Quick mobile detection check using screen width
                var screenWidth = await JSRuntime.InvokeAsync<int>("eval", "window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth");
                if (screenWidth <= 768)
                {
                    isMobileDevice = true;
                    Console.WriteLine($"Mobile device detected via screen width: {screenWidth}px");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in mobile detection: {ex.Message}");
            }
        }

        private void PickRandomTheme()
        {
            themeInput = GetRandomTheme();
            StateHasChanged();
        }        private void SetDifficulty(DifficultyLevel newDifficulty)
        {
            difficulty = newDifficulty;
            StateHasChanged();
        }
          private void OnApiKeySaved()
          {
              hasApiKey = true;
              StateHasChanged();
          }
        
        private async Task StartGame(GameMode mode)
        {
            // Enforce mandatory theme
            if (string.IsNullOrWhiteSpace(themeInput))
            {
                errorMessage = "Please enter a theme to start the game.";
                StateHasChanged();
                return;
            }

            currentGameMode = mode;
            gameStarted = true;

                pictureGuessUsedWords.Clear();
            isLoading = true;
            errorMessage = "";
            score = 0;
            streak = 0;
            currentChallenges.Clear();
            currentChallengeIndex = 0;
            conversationHistory.Clear();
            conversationTargetWords.Clear();
            usedTargetWords.Clear();
            wordsUsedCorrectly = 0;
            // Preserve previous word so we can avoid repeats
            var previousHangmanWord = hangmanWord;

            hangmanGuesses.Clear();
            hangmanWrongGuesses = 0;
            hangmanGameOver = false;
            hangmanWin = false;
            hangmanWord = string.Empty;
            hangmanDefinition = null;
            StateHasChanged();

            try
            {
                currentSession = new AILearningSession
                {
                    Mode = mode,
                    Difficulty = difficulty,
                    StartTime = DateTime.Now
                };

                if (mode == GameMode.Hangman)
                {
                    // Ensure new hangman word differs from previous to avoid repeats
                    string newWord;
                    do
                    {
                        var words = await GetWordsFromAI(1);
                        newWord = words.FirstOrDefault() ?? "example";
                        Console.WriteLine($"Generated hangman word candidate: '{newWord}' (length: {newWord.Length})");
                    }
                    while (!string.IsNullOrEmpty(previousHangmanWord) && newWord.Equals(previousHangmanWord, StringComparison.OrdinalIgnoreCase));
                    
                    hangmanWord = newWord;
                    Console.WriteLine($"Final hangman word set: '{hangmanWord}' (length: {hangmanWord.Length})");
                    Console.WriteLine($"Hangman word characters: {string.Join(", ", hangmanWord.Select(c => $"'{c}' ({(int)c})"))}");

                    hangmanGuesses.Clear();
                    hangmanWrongGuesses = 0;
                    hangmanGameOver = false;
                    hangmanWin = false;
                    hangmanDefinition = await GetHangmanHintAsync(hangmanWord); // Use safer hint instead of definition
                    StateHasChanged();
                    return;
                }

                // Select words using OpenAI (theme-aware)
                var wordsToUse = await GetWordsFromAI(5);

                switch (mode)
                {
                    case GameMode.StoryAdventure:
                        await GenerateStoryAdventure(wordsToUse);
                        break;
                    case GameMode.ConversationPractice:
                        await GenerateConversationStarter(wordsToUse);
                        break;
                    case GameMode.ContextualLearning:
                        await GenerateContextualChallenges(wordsToUse);
                        break;
                    case GameMode.PersonalizedQuiz:
                        await GeneratePersonalizedQuiz(wordsToUse);
                        break;
                    case GameMode.PictureGuess:
                        await StartNewPictureGuessRoundAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to start game: {ex.Message}";
                gameStarted = false;
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task StartNewPictureGuessRoundAsync()
        {
            pictureGuessStatusMessage = string.Empty;
            pictureGuessUserGuess = string.Empty;
            pictureGuessImageDataUrl = null;
            pictureGuessTargetWord = string.Empty;
            pictureGuessHint = string.Empty;
            pictureGuessWrongGuesses = 0;

            // Best-effort focus immediately so the user can type while the image loads.
            await FocusPictureGuessInputAsync();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var word = await GetPictureGuessWordFromAIAsync();
                pictureGuessTargetWord = word;
                if (!string.IsNullOrWhiteSpace(pictureGuessTargetWord))
                {
                    pictureGuessUsedWords.Add(pictureGuessTargetWord);
                }

                pictureGuessHint = await GetPictureGuessHintAsync(pictureGuessTargetWord);

                var imagePrompt = BuildPictureGuessImagePrompt(word);
                var imageResult = await OpenAIService.GenerateImageAsync(imagePrompt, size: "1024x1024");
                if (imageResult.Success)
                {
                    pictureGuessImageDataUrl = imageResult.DataUrl;
                    await FocusPictureGuessInputAsync();
                    return;
                }

                var err = imageResult.Error ?? "Failed to generate image.";
                var isSafetyRejection = err.Contains("rejected by the safety system", StringComparison.OrdinalIgnoreCase)
                    || err.Contains("safety_violations", StringComparison.OrdinalIgnoreCase);

                if (!isSafetyRejection)
                {
                    pictureGuessStatusMessage = err;
                    return;
                }

                pictureGuessStatusMessage = "Image blocked by safety filters. Trying a different word...";
                StateHasChanged();
            }

            pictureGuessStatusMessage = "Image generation failed after multiple attempts. Please try again.";
        }

        private async Task SubmitPictureGuessAsync()
        {
            if (isLoading) return;
            if (string.IsNullOrWhiteSpace(pictureGuessUserGuess)) return;
            if (string.IsNullOrWhiteSpace(pictureGuessTargetWord)) return;

            var guess = NormalizeGuess(pictureGuessUserGuess);
            var target = NormalizeGuess(pictureGuessTargetWord);

            if (string.Equals(guess, target, StringComparison.OrdinalIgnoreCase))
            {
                lastAnswerCorrect = true;
                streak++;
                score += 10;

                ShowPictureGuessToast($"✅ Correct! The word was '{pictureGuessTargetWord}'.");

                pictureGuessStatusMessage = "✅ Correct! Generating a new picture...";
                isLoading = true;
                StateHasChanged();
                try
                {
                    await StartNewPictureGuessRoundAsync();
                }
                finally
                {
                    isLoading = false;
                    StateHasChanged();
                }
            }
            else
            {
                lastAnswerCorrect = false;
                streak = 0;
                pictureGuessWrongGuesses++;
                pictureGuessStatusMessage = pictureGuessWrongGuesses > 0 && !string.IsNullOrWhiteSpace(pictureGuessHint)
                    ? "❌ Wrong guess. Try again (hint below)."
                    : "❌ Wrong guess. Try again.";
                await FocusPictureGuessInputAsync();
            }
        }

        private void ShowPictureGuessToast(string message)
        {
            pictureGuessToastMessage = message;
            showPictureGuessToast = true;
            pictureGuessToastTimer?.Dispose();
            pictureGuessToastTimer = new Timer(async _ =>
            {
                try
                {
                    await InvokeAsync(() =>
                    {
                        showPictureGuessToast = false;
                        StateHasChanged();
                    });
                }
                catch
                {
                    // Ignore; component may be disposed.
                }
            }, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        }

        private async Task HandlePictureGuessKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await SubmitPictureGuessAsync();
            }
        }

        private async Task FocusPictureGuessInputAsync()
        {
            try
            {
                // Delay to ensure the element is rendered.
                await Task.Delay(50);
                await pictureGuessInputElement.FocusAsync();
            }
            catch
            {
                // Fallback for cases where ElementReference is not available yet.
                try
                {
                    await JSRuntime.InvokeVoidAsync("eval", "document.querySelector('.picture-guess-input')?.focus()");
                }
                catch
                {
                    // No-op; focus is best-effort.
                }
            }
        }

        private string NormalizeGuess(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var lettersOnly = new string(value.Trim().Where(char.IsLetter).ToArray());
            return lettersOnly.ToLowerInvariant();
        }

        private string BuildPictureGuessImagePrompt(string targetWord)
        {
            var theme = themeInput!.Trim();
            var difficultyLabel = GetDifficultyName(difficulty);

            // Intentionally low-quality image request; also forbid text to avoid giving away the answer.
            return $@"Create a deliberately low-quality, low-resolution, blurry, grainy image that represents the single word '{targetWord}'.\n\nConstraints:\n- No text, letters, captions, watermarks, or logos\n- One main subject only; centered and easy to see\n- Plain background\n- Looks like a cheap camera / low quality screenshot\n- If the word is abstract, depict a simple everyday scene that clearly represents it\n\nSafety:\n- Safe for all ages\n- No nudity or sexual content\n- No violence, weapons, gore, or self-harm\n\nTheme context: {theme}\nDifficulty: {difficultyLabel}";
        }

        private async Task<string> GetPictureGuessWordFromAIAsync()
        {
            var theme = themeInput!.Trim();

            // Add a time-based seed to encourage randomness across rounds.
            var randomSeed = DateTime.UtcNow.Ticks % 100000;

            // Use the UI-facing labels (Easy/Medium/Difficult) for clarity.
            var difficultyLabel = GetDifficultyName(difficulty);
            var lengthRule = difficulty switch
            {
                DifficultyLevel.Beginner => "3-6 letters",
                DifficultyLevel.Intermediate => "6-9 letters",
                DifficultyLevel.Advanced => "8-12 letters",
                _ => "3-9 letters"
            };

            var usedWordsText = pictureGuessUsedWords.Count == 0
                ? "(none)"
                : string.Join(", ", pictureGuessUsedWords.TakeLast(50));

            const int candidateCount = 12;
            var prompt = $@"Generate exactly {candidateCount} single English words the student can guess from a picture.\n\nWord types:\n- Include a mix of nouns, verbs, and adjectives (varied across rounds)\n- Each word must be visually depictable (object/action/state); avoid purely abstract concepts\n\nSafety requirements:\n- Safe for all ages\n- Avoid sexual/adult terms, violence, weapons, gore, self-harm\n\nOther requirements:\n- Relevant to theme: '{theme}'\n- Exactly one word each (letters only; no spaces/hyphens/punctuation/numbers)\n- Common enough to guess\n- Length guideline: {lengthRule}\n- Must NOT include any of these words (already used this session): {usedWordsText}\n\nRandom seed: {randomSeed}\n\nReturn ONLY a comma-separated list like: word1, word2, word3";

            var systemMessage = "Return ONLY a comma-separated list of single English words. No extra text.";

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                var candidates = ParsePictureGuessCandidates(aiResponse)
                    .Where(w => !pictureGuessUsedWords.Contains(w))
                    .Where(IsPictureGuessAllowedWord)
                    .ToList();

                if (candidates.Count > 0)
                {
                    return candidates[_random.Next(candidates.Count)];
                }

                randomSeed = (randomSeed + _random.Next(1, 9999)) % 100000;
                usedWordsText = pictureGuessUsedWords.Count == 0
                    ? "(none)"
                    : string.Join(", ", pictureGuessUsedWords.TakeLast(75));

                prompt = $@"Generate exactly {candidateCount} single English words the student can guess from a picture.\n\nWord types:\n- Include a mix of nouns, verbs, and adjectives (varied across rounds)\n- Each word must be visually depictable (object/action/state); avoid purely abstract concepts\n\nSafety requirements:\n- Safe for all ages\n- Avoid sexual/adult terms, violence, weapons, gore, self-harm\n\nOther requirements:\n- Relevant to theme: '{theme}'\n- Exactly one word each (letters only; no spaces/hyphens/punctuation/numbers)\n- Common enough to guess\n- Length guideline: {lengthRule}\n- Must NOT include any of these words (already used this session): {usedWordsText}\n\nRandom seed: {randomSeed}\n\nReturn ONLY a comma-separated list like: word1, word2, word3";
            }

            var fallbacks = GetFallbackWords(theme, 15)
                .Select(CleanWord)
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 3 && !pictureGuessUsedWords.Contains(w))
                .Where(IsPictureGuessAllowedWord)
                .ToList();

            return fallbacks.FirstOrDefault() ?? "Example";
        }

        private List<string> ParsePictureGuessCandidates(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse)) return new List<string>();

            // Prefer comma-separated lists.
            var commaSplit = aiResponse.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var words = commaSplit
                .Select(w => CleanWord(w.Trim()))
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.All(char.IsLetter) && w.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (words.Count > 0) return words;

            // Fallback to whitespace/newline splitting.
            var whitespaceSplit = aiResponse.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            return whitespaceSplit
                .Select(w => CleanWord(w.Trim()))
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.All(char.IsLetter) && w.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<string> GetPictureGuessHintAsync(string targetWord)
        {
            if (string.IsNullOrWhiteSpace(targetWord)) return string.Empty;

            var prompt = $@"Write a short hint (max 12 words) describing the meaning of the word, without using the word itself.\n\nWord: {targetWord}\n\nRules:\n- Do NOT include the word or any close spelling of it\n- Avoid obvious synonyms that give it away\n- If the word is abstract, describe a simple everyday scenario that represents it\n\nReturn ONLY the hint sentence.";

            var systemMessage = "Return ONLY the hint sentence. No extra text.";
            var hint = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            hint = (hint ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(hint)) return string.Empty;
            if (hint.Contains(targetWord, StringComparison.OrdinalIgnoreCase))
            {
                // If the model leaks the target, don't show the hint.
                return string.Empty;
            }

            return hint.Length > 140 ? hint.Substring(0, 140).Trim() : hint;
        }

        private async Task<List<string>> GetWordsFromAI(int count)
        {
            var theme = themeInput!.Trim(); // Always use the current theme, which is mandatory
            var difficultyText = difficulty.ToString().ToLower();
            
            // Add randomization elements to ensure varied word selection
            var randomSeed = DateTime.Now.Ticks % 10000; // Use current time as seed for variety
            var varietyInstructions = new[]
            {
                "Focus on varied vocabulary including verbs, nouns, and adjectives.",
                "Include both common and less common words to provide variety.",
                "Mix different aspects and subtopics within the theme.",
                "Choose diverse words that cover different facets of the topic.",
                "Select words from various categories and contexts within this theme."
            };
            var varietyInstruction = varietyInstructions[_random.Next(varietyInstructions.Length)];
            
            var prompt = $@"Generate exactly {count} English vocabulary words about '{theme}' appropriate for {difficultyText}-level learners. 

IMPORTANT: 
- Generate DIFFERENT words each time - avoid repeating the same common words
- {varietyInstruction}
- Each word must be separated by a comma and space
- Use only single words (no phrases)
- Return ONLY the words separated by commas

Example format: word1, word2, word3, word4, word5

For fitness theme examples: exercise, strength, cardio, flexibility, endurance
For travel theme examples: journey, adventure, explore, destination, culture

Random seed: {randomSeed}";
            
            var systemMessage = "You are an expert English language teacher. Return ONLY a comma-separated list of vocabulary words. Format: word1, word2, word3, word4, word5. No other text or explanations.";
            var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            
            Console.WriteLine($"AI Response for words: '{aiResponse}'");
            
            // Try multiple splitting methods to handle different response formats
            var words = new List<string>();
            
            // First try comma separation
            var commaSplit = aiResponse.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (commaSplit.Length >= count)
            {
                words = commaSplit.Select(w => CleanWord(w.Trim()))
                                 .Where(w => !string.IsNullOrWhiteSpace(w) && w.All(char.IsLetter) && w.Length > 1)
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Take(count)
                                 .ToList();
            }
            
            // If comma splitting didn't work, try whitespace/newline splitting
            if (words.Count < count)
            {
                var whitespaceSplit = aiResponse.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                words = whitespaceSplit.Select(w => CleanWord(w.Trim()))
                                      .Where(w => !string.IsNullOrWhiteSpace(w) && w.All(char.IsLetter) && w.Length > 1)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .Take(count)
                                      .ToList();
            }
            
            // Fallback to predefined words if AI response parsing fails
            if (words.Count < count)
            {
                Console.WriteLine($"AI response parsing failed, using fallback words. AI Response was: '{aiResponse}'");
                words = GetFallbackWords(theme, count);
            }
            
            Console.WriteLine($"Final words generated: {string.Join(", ", words)}");
            return words;
        }

        private List<string> GetFallbackWords(string theme, int count)
        {
            var fallbackWordsByTheme = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fitness"] = new() { "Exercise", "Strength", "Cardio", "Flexibility", "Endurance", "Stamina", "Workout", "Training", "Muscle", "Health", "Nutrition", "Diet", "Running", "Jogging", "Weights" },
                ["travel"] = new() { "Journey", "Adventure", "Destination", "Explore", "Culture", "Tourist", "Vacation", "Trip", "Flight", "Hotel", "Passport", "Luggage", "Guide", "Map", "Tour" },
                ["food"] = new() { "Recipe", "Ingredient", "Cooking", "Delicious", "Flavor", "Taste", "Meal", "Breakfast", "Lunch", "Dinner", "Restaurant", "Kitchen", "Chef", "Spice", "Fresh" },
                ["technology"] = new() { "Computer", "Software", "Internet", "Digital", "Innovation", "Programming", "Data", "Network", "Device", "Application", "System", "Algorithm", "Artificial", "Intelligence", "Machine" },
                ["nature"] = new() { "Forest", "Mountain", "River", "Ocean", "Wildlife", "Environment", "Ecosystem", "Plants", "Animals", "Trees", "Flowers", "Birds", "Conservation", "Natural", "Landscape" },
                ["education"] = new() { "Learning", "Student", "Teacher", "Knowledge", "Study", "School", "University", "Research", "Book", "Library", "Classroom", "Lesson", "Exam", "Homework", "Graduation" },
                ["business"] = new() { "Company", "Market", "Economy", "Profit", "Investment", "Strategy", "Management", "Leadership", "Customer", "Service", "Product", "Sales", "Marketing", "Finance", "Budget" },
                ["health"] = new() { "Medicine", "Doctor", "Hospital", "Treatment", "Wellness", "Prevention", "Therapy", "Healing", "Recovery", "Diagnosis", "Symptoms", "Patient", "Nurse", "Surgery", "Medication" }
            };

            // Try to find matching theme or use general words
            var themeWords = fallbackWordsByTheme.GetValueOrDefault(theme.ToLower()) ??
                           fallbackWordsByTheme.Values.SelectMany(x => x).Take(15).ToList();

            // Shuffle and take requested count
            var shuffled = themeWords.OrderBy(x => _random.Next()).Take(count).ToList();

            // If we still don't have enough, pad with generic words
            while (shuffled.Count < count)
            {
                var genericWords = new[] { "Example", "Learning", "Practice", "Study", "Knowledge", "Skill", "Progress", "Success", "Challenge", "Goal" };
                shuffled.Add(genericWords[_random.Next(genericWords.Length)]);
            }

            return shuffled.Take(count).ToList();
        }

        private string CleanWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return "";

            // Remove any non-letter characters and convert to proper case
            var cleanedWord = new string(word.Where(char.IsLetter).ToArray());

            // Convert to proper case (first letter uppercase, rest lowercase)
            if (cleanedWord.Length > 0)
            {
                cleanedWord = char.ToUpperInvariant(cleanedWord[0]) + cleanedWord.Substring(1).ToLowerInvariant();
            }

            return cleanedWord;
        }

        private async Task GenerateStoryAdventure(List<string> words)
        {
            var wordsText = string.Join(", ", words);
            var difficultyText = difficulty.ToString().ToLower();
            
            var prompt = $@"Create an engaging short story (2-3 paragraphs) for {difficultyText}-level English learners that naturally incorporates these vocabulary words: {wordsText}.

The story should:
1. Be appropriate for {difficultyText} level learners
2. Use each word naturally in context
3. Be interesting and engaging
4. Help learners understand word meanings through context
5. Be exactly 2-3 paragraphs

After the story, create 3-5 comprehension questions about the vocabulary words used.

Format:
STORY:
[Your story here]

QUESTIONS:
1. [Question about first word]
2. [Question about second word]
etc.";

            var systemMessage = "You are an expert English language teacher creating engaging educational content.";
            
            var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            
            Console.WriteLine($"AI Response for story: {aiResponse}");
            Console.WriteLine($"AI Response length: {aiResponse.Length}");
            
            // Parse the response to extract story and questions
            var sections = aiResponse.Split(new[] { "QUESTIONS:" }, StringSplitOptions.RemoveEmptyEntries);
            
            Console.WriteLine($"Sections found: {sections.Length}");
            
            if (sections.Length >= 2)
            {
                // Clean up the story content more thoroughly
                var storyContent = sections[0].Replace("STORY:", "").Trim();
                
                // Remove any unwanted headers like "Scenario:", "Story:", etc.
                var linesToRemove = new[] { "SCENARIO:", "STORY:", "scenario:", "story:" };
                var lines = storyContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var cleanedLines = lines.Where(line => 
                    !linesToRemove.Any(header => line.Trim().StartsWith(header, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                
                currentContent = string.Join("\n", cleanedLines).Trim();
                
                Console.WriteLine($"Story content set: {currentContent}");
                
                // Parse questions and create challenges
                var questionLines = sections[1].Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"Question lines found: {questionLines.Length}");
                
                foreach (var line in questionLines.Where(l => l.Trim().Length > 0))
                {
                    Console.WriteLine($"Processing line: {line}");
                    var cleanLine = line.Trim();
                    if (cleanLine.StartsWith("1.") || cleanLine.StartsWith("2.") || 
                        cleanLine.StartsWith("3.") || cleanLine.StartsWith("4.") || 
                        cleanLine.StartsWith("5."))
                    {
                        // Extract the word this question is about
                        var word = ExtractWordFromQuestion(cleanLine, words);
                        Console.WriteLine($"Extracted word: {word} from line: {cleanLine}");
                        if (!string.IsNullOrEmpty(word))
                        {
                            currentChallenges.Add(new WordChallenge
                            {
                                Type = ChallengeType.Context,
                                TargetWord = word,
                                Question = cleanLine.Substring(2).Trim(),
                                IsOpenEnded = true
                                // Don't set Context for story challenges - the story is already displayed above
                            });
                            Console.WriteLine($"Added challenge for word: {word}");
                        }
                    }
                }
                Console.WriteLine($"Total challenges created: {currentChallenges.Count}");
                
                // Fallback: if no challenges were created from parsing, create some basic ones
                if (currentChallenges.Count == 0)
                {
                    Console.WriteLine("No challenges parsed from AI response, creating fallback challenges");
                    await GenerateSimpleChallenges(words);
                }
            }
            else
            {
                Console.WriteLine("Could not split AI response into story and questions sections");
                currentContent = aiResponse;
                // Fallback: create simple challenges
                await GenerateSimpleChallenges(words);
            }
            
            // Final safety check: ensure we have at least one challenge
            if (currentChallenges.Count == 0)
            {
                Console.WriteLine("CRITICAL: No challenges created, adding emergency challenge");
                var firstWord = words.FirstOrDefault() ?? "learning";
                currentChallenges.Add(new WordChallenge
                {
                    Type = ChallengeType.Context,
                    TargetWord = firstWord,
                    Question = $"How would you use the word '{firstWord}' in your own sentence?",
                    IsOpenEnded = true
                });
            }
            
            Console.WriteLine($"Final challenge count: {currentChallenges.Count}");
        }

        private string ExtractWordFromQuestion(string question, List<string> words)
        {
            var lowerQuestion = question.ToLower();
            return words.FirstOrDefault(word => lowerQuestion.Contains(word.ToLower())) ?? "";
        }

        private async Task GenerateSimpleChallenges(List<string> words)
        {
            foreach (var word in words)
            {
                var challengeType = _random.Next(3) switch
                {
                    0 => ChallengeType.Comprehension,
                    1 => ChallengeType.Usage,
                    _ => ChallengeType.Context
                };

                currentChallenges.Add(new WordChallenge
                {
                    Type = challengeType,
                    TargetWord = word,
                    Question = challengeType switch
                    {
                        ChallengeType.Comprehension => $"What does '{word}' mean in this context?",
                        ChallengeType.Usage => $"Use the word '{word}' in your own sentence:",
                        _ => $"How does '{word}' contribute to the meaning of this story?"
                    },
                    IsOpenEnded = challengeType != ChallengeType.Comprehension,
                    Options = challengeType == ChallengeType.Comprehension ? await GenerateMultipleChoiceOptionsAsync(word) : new List<string>(),
                    CorrectAnswer = challengeType == ChallengeType.Comprehension ? await GetSimpleDefinitionAsync(word) : ""
                });
            }
            // No return needed for async Task
        }
        private async Task GenerateConversationStarter(List<string> words)
        {
            conversationTargetWords = words;
            var wordsText = string.Join(", ", words);
            var difficultyText = difficulty.ToString().ToLower();

            var prompt = $@"You are starting a friendly English conversation practice session. The student should practice using these words naturally: {wordsText}.

Create an engaging conversation starter (1-2 sentences) that:
1. Is appropriate for {difficultyText}-level learners
2. Naturally introduces a topic where these words might be used
3. Asks an open-ended question to get the conversation going
4. Is warm and encouraging

Example topics: daily activities, hobbies, travel experiences, personal goals, etc.

Just provide the conversation starter, nothing else.";

            var systemMessage = "You are a friendly English conversation teacher helping students practice vocabulary.";
            
            var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            
            currentContent = "Ready to practice conversation! Try to use the target words naturally.";
            conversationHistory.Add(aiResponse.Trim());
        }

        private async Task GenerateContextualChallenges(List<string> words)
        {
            currentContent = "Let's practice understanding words in different contexts!";
            
            foreach (var word in words)
            {
                var prompt = $@"Create a contextual English learning exercise for the word '{word}' at {difficulty.ToString().ToLower()} level.

Create a short scenario (1-2 sentences) that uses the word '{word}' in context, then ask a comprehension question about it.

Format:
SCENARIO: [1-2 sentences using the word naturally]
QUESTION: [Question about the word's meaning or usage in this context]";

                var systemMessage = "You are creating contextual vocabulary exercises for English learners.";
                
                try
                {
                    var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                    var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    var scenario = "";
                    var question = "";
                    
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("SCENARIO:"))
                            scenario = line.Substring(9).Trim();
                        else if (line.StartsWith("QUESTION:"))
                            question = line.Substring(9).Trim();
                    }
                    
                    currentChallenges.Add(new WordChallenge
                    {
                        Type = ChallengeType.Context,
                        TargetWord = word,
                        Question = !string.IsNullOrEmpty(question) ? question : $"What does '{word}' mean in this context?",
                        Context = !string.IsNullOrEmpty(scenario) ? scenario : $"Context for '{word}'",
                        IsOpenEnded = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating contextual challenge for {word}: {ex.Message}");
                    // Fallback challenge
                    currentChallenges.Add(new WordChallenge
                    {
                        Type = ChallengeType.Context,
                        TargetWord = word,
                        Question = $"How would you use '{word}' in a sentence?",
                        IsOpenEnded = true
                    });
                }
            }
        }

        private async Task GeneratePersonalizedQuiz(List<string> words)
        {
            currentContent = "Let's test your knowledge with some smart questions!";
            
            foreach (var word in words)
            {
                try
                {
                    var questionTypes = new[] { "definition", "synonym", "usage", "context" };
                    var questionType = questionTypes[_random.Next(questionTypes.Length)];

                    var prompt = $@"Create a {questionType} question for the word '{word}' appropriate for {difficulty.ToString().ToLower()}-level English learners.

For definition questions: Ask 'What does [word] mean?' and provide 4 definition options (A, B, C, D) with one correct definition.
For synonym questions: Ask 'Which word means the same as [word]?' and provide 4 word options with one correct synonym.
For usage questions: Ask 'Which sentence uses [word] correctly?' and provide 4 complete sentence options.
For context questions: Provide a short scenario and ask how the word fits, with 4 definition options.

Make sure the question type is clear from the wording. 

Format as:
QUESTION: [Your clear question indicating the type]
A) [Option 1]
B) [Option 2] 
C) [Option 3]
D) [Option 4]
CORRECT: [Letter of correct answer]";

                    var systemMessage = "You are an expert language assessment creator making engaging vocabulary questions.";
                    
                    var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                    var challenge = await ParseQuizResponse(aiResponse, word);
                    currentChallenges.Add(challenge);
                    Console.WriteLine($"Generated challenge for word: {word}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating challenge for word '{word}': {ex.Message}");
                    // Add fallback challenge for this word
                    currentChallenges.Add(new WordChallenge
                    {
                        Type = ChallengeType.Definition,
                        TargetWord = word,
                        Question = $"What does '{word}' mean?",
                        Options = new List<string> { "A basic meaning", "Something else", "Another option", "Not this one" },
                        CorrectAnswer = "A basic meaning"
                    });
                }
            }
        }

        private async Task<WordChallenge> ParseQuizResponse(string aiResponse, string word)
        {
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var question = "";
                var options = new List<string>();
                var correctAnswer = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("QUESTION:"))
                    {
                        question = trimmedLine.Substring(9).Trim();
                    }
                    else if (trimmedLine.StartsWith("A)") || trimmedLine.StartsWith("B)") || 
                             trimmedLine.StartsWith("C)") || trimmedLine.StartsWith("D)"))
                    {
                        options.Add(trimmedLine.Substring(2).Trim());
                    }
                    else if (trimmedLine.StartsWith("CORRECT:"))
                    {
                        var correctLetter = trimmedLine.Substring(8).Trim().ToUpper();
                        var index = correctLetter[0] - 'A';
                        if (index >= 0 && index < options.Count)
                        {
                            correctAnswer = options[index];
                        }
                    }
                }
                return new WordChallenge
                {
                    Type = GetChallengeTypeFromQuestion(question),
                    TargetWord = word,
                    Question = !string.IsNullOrEmpty(question) ? question : $"What does '{word}' mean?",
                    Options = options.Count > 0 ? options : await GenerateMultipleChoiceOptionsAsync(word),
                    CorrectAnswer = !string.IsNullOrEmpty(correctAnswer) ? correctAnswer : await GetSimpleDefinitionAsync(word)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing quiz response: {ex.Message}");
                // Return fallback question
                return new WordChallenge
                {
                    Type = ChallengeType.Definition,
                    TargetWord = word,
                    Question = $"What does '{word}' mean?",
                    Options = await GenerateMultipleChoiceOptionsAsync(word),
                    CorrectAnswer = await GetSimpleDefinitionAsync(word)
                };
            }
        }

        private ChallengeType GetChallengeTypeFromQuestion(string question)
        {
            if (string.IsNullOrEmpty(question))
                return ChallengeType.Comprehension;
                
            var lowerQuestion = question.ToLower();
            
            if (lowerQuestion.Contains("what does") && lowerQuestion.Contains("mean"))
                return ChallengeType.Comprehension;
            else if (lowerQuestion.Contains("which word means the same") || lowerQuestion.Contains("synonym"))
                return ChallengeType.Synonym;
            else if (lowerQuestion.Contains("which sentence uses") || lowerQuestion.Contains("correctly"))
                return ChallengeType.Usage;
            else if (lowerQuestion.Contains("context") || lowerQuestion.Contains("scenario"))
                return ChallengeType.Context;
            else
                return ChallengeType.Comprehension; // Default
        }

        // Utility methods for generating content
        private async Task<List<string>> GenerateMultipleChoiceOptionsAsync(string word)
        {
            var options = new List<string>
            {
                await GetSimpleDefinitionAsync(word),
                GetRandomDefinition(),
                GetRandomDefinition(),
                GetRandomDefinition()
            };
            return options.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private List<string> GenerateSynonymOptions(string word)
        {
            var options = new List<string>
            {
                GetSynonym(word),
                GetRandomWord(),
                GetRandomWord(),
                GetRandomWord()
            };
            return options.OrderBy(x => Guid.NewGuid()).ToList();
        }        private readonly Dictionary<string, string> _definitionCache = new();
private async Task<string> GetSimpleDefinitionAsync(string word)
{
    if (string.IsNullOrWhiteSpace(word)) return "";
    word = word.Trim();
    var wordLower = word.ToLowerInvariant();
    if (_definitionCache.TryGetValue(wordLower, out var cachedDef))
        return cachedDef;

    // Use OpenAI to fetch a definition
    var prompt = $"Provide a simple, clear English definition for the word '{wordLower}'. Limit to one sentence. Do not use the word itself or any obvious forms of it in the definition.";
    var systemMessage = "You are an expert English dictionary.";
    try
    {
        var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
        var definition = aiResponse.Trim();
        if (string.IsNullOrWhiteSpace(definition))
            definition = $"No definition found for '{wordLower}'.";

        // Post-process: mask the word and simple variants in the definition
        string MaskWord(string def, string w)
        {
            var forms = new List<string> { w };
            if (w.Length > 0)
                forms.Add(char.ToUpper(w[0]) + w.Substring(1));
            var suffixes = new[] { "s", "es", "ed", "ing" };
            foreach (var suffix in suffixes)
            {
                forms.Add(w + suffix);
                if (w.Length > 0)
                    forms.Add(char.ToUpper(w[0]) + w.Substring(1) + suffix);
            }
            string root = w;
            if (w.EndsWith("ing") && w.Length > 3) root = w.Substring(0, w.Length - 3);
            else if (w.EndsWith("ed") && w.Length > 2) root = w.Substring(0, w.Length - 2);
            else if (w.EndsWith("es") && w.Length > 2) root = w.Substring(0, w.Length - 2);
            else if (w.EndsWith("s") && w.Length > 1) root = w.Substring(0, w.Length - 1);
            if (root != w && root.Length > 2) forms.Add(root);
            foreach (var form in forms.Distinct())
            {
                // Mask at start of string
                def = System.Text.RegularExpressions.Regex.Replace(def, $@"^(?i){System.Text.RegularExpressions.Regex.Escape(form)}", "_____", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Mask as whole word anywhere
                def = System.Text.RegularExpressions.Regex.Replace(def, $@"\b{System.Text.RegularExpressions.Regex.Escape(form)}\b", "_____", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return def;
        }
        definition = MaskWord(definition, wordLower);
        _definitionCache[wordLower] = definition;
        return definition;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching definition for '{wordLower}': {ex.Message}");
        return $"No definition found for '{wordLower}'.";
    }
}        private string GetSynonym(string word)
        {
            var synonyms = new Dictionary<string, string>
            {
                // Beginner words
                {"adventure", "journey"},
                {"beautiful", "attractive"},
                {"celebrate", "commemorate"},
                {"discover", "find"},
                {"enormous", "huge"},
                {"friendship", "companionship"},
                {"grateful", "thankful"},
                {"harmony", "balance"},
                {"important", "significant"},
                {"journey", "trip"},
                {"kindness", "compassion"},
                {"laughter", "giggling"},
                {"mystery", "puzzle"},
                {"nature", "environment"},
                {"opportunity", "chance"},
                {"peaceful", "calm"},
                {"question", "inquiry"},
                {"respect", "honor"},
                {"sunshine", "sunlight"},
                {"treasure", "riches"},
                {"umbrella", "parasol"},
                {"victory", "triumph"},
                {"wonderful", "amazing"},
                {"explore", "investigate"},
                {"youthful", "young"},
                
                // Intermediate words
                {"ambitious", "determined"},
                {"beneficial", "helpful"},
                {"comprehensive", "complete"},
                {"demonstrate", "show"},
                {"elaborate", "detailed"},
                {"fundamental", "basic"},
                {"genuine", "authentic"},
                {"hypothesis", "theory"},
                {"inevitable", "unavoidable"},
                {"jurisdiction", "authority"},
                {"magnificent", "splendid"},
                {"negligent", "careless"},
                {"optimistic", "hopeful"},
                {"persistent", "determined"},
                {"reluctant", "hesitant"},
                {"sophisticated", "refined"},
                {"temporary", "brief"},
                {"unprecedented", "unparalleled"},
                {"versatile", "adaptable"},
                {"wisdom", "knowledge"},
                {"analyze", "examine"},
                {"bureaucracy", "administration"},
                {"catastrophe", "disaster"},
                {"diligent", "hardworking"},
                {"empathy", "compassion"},
                
                // Advanced words
                {"ubiquitous", "omnipresent"},
                {"perspicacious", "perceptive"},
                {"serendipitous", "fortuitous"},
                {"magnanimous", "generous"},
                {"eloquent", "articulate"},
                {"ephemeral", "fleeting"},
                {"indigenous", "native"},
                {"meticulous", "careful"},
                {"ostentatious", "showy"},
                {"pragmatic", "practical"},
                {"quintessential", "typical"},
                {"resilient", "tough"},
                {"scrupulous", "thorough"},
                {"tenacious", "persistent"},
                {"vicarious", "indirect"},
                {"whimsical", "playful"},
                {"xenophobic", "prejudiced"},
                {"zealous", "enthusiastic"},
                {"acquiesce", "agree"},
                {"belligerent", "aggressive"},
                {"cacophony", "noise"},
                {"deleterious", "harmful"},
                {"effervescent", "bubbly"},
                {"facetious", "joking"},
                {"gregarious", "sociable"}
            };

            return synonyms.GetValueOrDefault(word, $"synonym for '{word}'");
        }

        private string GetRandomDefinition()
        {
            var definitions = new List<string>
            {
                "Having three sides and three angles",
                "A device for measuring temperature",
                "The study of living organisms",
                "A large body of water surrounded by land"
            };
            return definitions[_random.Next(definitions.Count)];
        }

        private string GetRandomWord()
        {
            var words = new List<string>
            {
                "triangle", "thermometer", "biology", "lake", "mountain", "telephone", "computer", "elephant"
            };
            return words[_random.Next(words.Count)];
        }

        // Missing UI helper methods
        private string GetGameModeTitle(GameMode mode)
        {
            return mode switch
            {
                GameMode.StoryAdventure => "📚 Story Adventure",
                GameMode.ConversationPractice => "💬 Conversation Practice",
                GameMode.ContextualLearning => "🎯 Contextual Learning",
                GameMode.PersonalizedQuiz => "🧠 Personalized Quiz",
                GameMode.Hangman => "🎪 Hangman",
                GameMode.WordTypeSnap => "⚡ Word Type Snap",
                GameMode.PictureGuess => "🖼️ Picture Guess",
                _ => "Learning Mode"
            };
        }        private string GetDifficultyName(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Beginner => "Easy",
                DifficultyLevel.Intermediate => "Medium",
                DifficultyLevel.Advanced => "Difficult",
                _ => "Unknown"
            };        }        private Task ExitGame()
        {
            gameStarted = false;
            currentContent = "";
            conversationHistory.Clear();
            currentChallenges.Clear();
            currentChallengeIndex = 0;
            score = 0;
            streak = 0;
            StopFeedbackTimer();
            showFeedback = false;
            feedbackMessage = "";
            lastAnswerCorrect = false;
            userInput = "";
            conversationTargetWords.Clear();
            usedTargetWords.Clear();
            wordsUsedCorrectly = 0;
            hangmanGuesses.Clear();
            hangmanWrongGuesses = 0;
            hangmanGameOver = false;
            hangmanWin = false;
            hangmanWord = string.Empty;

            pictureGuessImageDataUrl = null;
            pictureGuessTargetWord = string.Empty;
            pictureGuessUserGuess = string.Empty;
            pictureGuessStatusMessage = string.Empty;

            StateHasChanged();
            return Task.CompletedTask;
        }        private async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userInput) && !isLoading)
            {
                await SendMessage();
            }
        }private async Task HandleKeyPressForTextarea(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && e.CtrlKey && !string.IsNullOrWhiteSpace(userInput))
            {
                await ProcessAnswer(userInput);
            }        }        private async Task ContinueLearning()
        {
            StopFeedbackTimer();
            showFeedback = false;
            feedbackMessage = "";
            PlayAudio = false; // Reset audio flag to prevent unwanted sounds when clicking continue

            // Only move to next challenge if the last answer was correct
            if (lastAnswerCorrect)
            {
                currentChallengeIndex++;
            }

            // If we've reached the end of challenges, generate new content
            if (currentChallengeIndex >= currentChallenges.Count)
            {
                isLoading = true;
                StateHasChanged();
                try
                {
                    switch (currentGameMode)
                    {
                        case GameMode.StoryAdventure:
                            await GenerateNewStoryAdventure();
                            break;                case GameMode.PersonalizedQuiz:
                    await GenerateNewPersonalizedQuiz();
                    // Double-check that we have challenges after generation
                    if (currentChallenges.Count == 0)
                    {
                        Console.WriteLine("Critical: No challenges after quiz generation, adding emergency fallback");
                        currentChallenges.Add(new WordChallenge {
                            Type = ChallengeType.Definition,
                            TargetWord = "knowledge",
                            Question = "What does 'knowledge' mean?",
                            Options = new List<string> { "Information and understanding", "A building", "A tool", "A color" },
                            CorrectAnswer = "Information and understanding"
                        });
                    }
                    break;
                        case GameMode.ContextualLearning:
                            await GenerateNewContextualChallenges();
                            break;
                        case GameMode.ConversationPractice:
                            if (wordsUsedCorrectly >= conversationTargetWords.Count)
                            {
                                await GenerateNewConversationTopic();
                            }
                            // else: continue current conversation - no action needed
                            break;
                        case GameMode.PictureGuess:
                            await StartNewPictureGuessRoundAsync();
                            break;
                    }
                    currentChallengeIndex = 0;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Failed to load next challenge: {ex.Message}";
                    Console.WriteLine(errorMessage);
                    // Fallback: reset state to avoid stuck UI
                    currentContent = "Sorry, something went wrong. Please try again.";
                    currentChallenges.Clear();
                    currentChallengeIndex = 0;
                }
                finally
                {
                    isLoading = false;
                    StateHasChanged();
                }
            }
            else
            {
                // If not at end, just reset input and state
                userInput = "";
                lastAnswerCorrect = false;
                feedbackMessage = "";
                StateHasChanged();
                await FocusAnswerTextAreaIfNeeded();
            }
        }

        private string GetFullFeedbackMessage()
        {
            var message = feedbackMessage;
            if (!lastAnswerCorrect && !string.IsNullOrEmpty(correctAnswer))
            {
                message += $"\n\nCorrect answer: {correctAnswer}";
            }
            return message;
        }

        private async Task GenerateNewStoryAdventure()
        {
            var newWords = await GetWordsFromAI(5);
            currentChallenges.Clear();
            await GenerateStoryAdventure(newWords);
        }

        private async Task GenerateNewPersonalizedQuiz()
        {
            try
            {
                var newWords = await GetWordsFromAI(5);
                currentChallenges.Clear();
                currentChallengeIndex = 0;
                await GeneratePersonalizedQuiz(newWords);
                
                Console.WriteLine($"Generated {currentChallenges.Count} new quiz challenges");
                
                // Ensure at least one challenge exists
                if (currentChallenges.Count == 0)
                {
                    Console.WriteLine("No challenges generated, adding fallback");
                    var fallbackWord = newWords.FirstOrDefault() ?? "example";
                    currentChallenges.Add(new WordChallenge
                    {
                        Type = ChallengeType.Definition,
                        TargetWord = fallbackWord,
                        Question = $"What does '{fallbackWord}' mean?",
                        Options = new List<string> { "A sample or instance", "A mistake", "A tool", "A color" },
                        CorrectAnswer = "A sample or instance"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating new quiz: {ex.Message}");
                // Fallback challenge
                currentChallenges.Clear();
                currentChallengeIndex = 0;
                currentChallenges.Add(new WordChallenge
                {
                    Type = ChallengeType.Definition,
                    TargetWord = "learning",
                    Question = "What does 'learning' mean?",
                    Options = new List<string> { "The process of gaining knowledge", "A type of building", "A measurement", "A color" },
                    CorrectAnswer = "The process of gaining knowledge"
                });
            }
        }

        private async Task GenerateNewContextualChallenges()
        {
            var newWords = await GetWordsFromAI(5);
            currentChallenges.Clear();
            await GenerateContextualChallenges(newWords);
        }        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(userInput) || isLoading) return;

            isLoading = true; // Prevent multiple submissions
            var currentMessage = userInput;
            userInput = ""; // Clear input immediately
            StateHasChanged(); // Update UI to show cleared input and loading state
            
            try
            {
                conversationHistory.Add(currentMessage);
                
                // Check for vocabulary usage in conversation practice mode
                if (currentGameMode == GameMode.ConversationPractice)
                {
                    EvaluateVocabularyUsage(currentMessage);
                    
                    // Force UI update after evaluation
                    await InvokeAsync(StateHasChanged);
                }
                
                // Generate AI response using OpenAI
                var aiResponse = await GenerateAIResponse(currentMessage);
                conversationHistory.Add(aiResponse);
            }
            finally
            {
                isLoading = false; // Reset loading state
                StateHasChanged();
                
                // Scroll to bottom after message is added
                await Task.Delay(100);
                await ScrollChatToBottom();
                
                // Focus back to input field
                await Task.Delay(50);
                await FocusChatInput();
            }
        }private async Task<string> GenerateAIResponse(string userMessage)
        {
            try
            {
                var wordsContext = string.Join(", ", conversationTargetWords);
                var prompt = $@"You are a friendly English conversation teacher. The user is practicing with these target words: {wordsContext}.

User said: ""{userMessage}""

Guidelines:
1. If they used any target words correctly, acknowledge it positively
2. Keep responses conversational and encouraging
3. Try to naturally use some target words in your response
4. Ask follow-up questions to continue the conversation
5. Gently guide them to use more target words if they haven't used many
6. Keep responses to 1-2 sentences maximum

Previous conversation context: {string.Join(" ", conversationHistory.TakeLast(6))}

Respond naturally as a conversation partner:";

                var systemMessage = "You are a supportive English conversation teacher helping students practice vocabulary in natural conversation.";
                
                return await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating AI response: {ex.Message}");
                var fallbackResponses = new[]
                {
                    "That's interesting! Can you tell me more about that?",
                    "I see! What do you think about that situation?",
                    "That sounds great! How did that make you feel?",
                    "Wonderful! Can you share another example?",
                    "That's a good point! What would you do differently?",
                    "I understand. Can you explain that in more detail?",
                    "That's fascinating! What else can you tell me about it?"
                };
                
                return fallbackResponses[_random.Next(fallbackResponses.Length)];
            }
        }

        private async Task ProcessAnswer(string answer)
        {
            if (currentChallengeIndex >= currentChallenges.Count) return;

            var challenge = currentChallenges[currentChallengeIndex];
            isLoading = true; // Show spinner while processing answer
            StateHasChanged();
            
            // Use AI-powered checking for open-ended questions, regular checking for multiple choice
            var isCorrect = challenge.IsOpenEnded 
                ? await CheckAnswerWithAI(challenge, answer)
                : CheckAnswer(challenge, answer);

            lastAnswerCorrect = isCorrect;
            correctAnswer = challenge.CorrectAnswer ?? "";

            if (isCorrect)
            {
                score += GetScoreForDifficulty();
                streak++;
                
                // Use AI to generate personalized feedback
                feedbackMessage = challenge.IsOpenEnded 
                    ? await GeneratePositiveFeedback(challenge.TargetWord, answer)
                    : GeneratePositiveFeedback(challenge.TargetWord);
                
                if (currentSession != null)
                {
                    currentSession.WordsLearned.Add(challenge.TargetWord);
                }
            }
            else
            {
                streak = 0;
                
                // Provide better feedback based on challenge type
                if (challenge.Type == ChallengeType.Comprehension)
                {
                    feedbackMessage = $"Not quite right. '{challenge.TargetWord}' means: {correctAnswer}";
                }
                else if (challenge.IsOpenEnded)
                {
                    // Only mention the target word if the user actually used it; otherwise give a generic prompt
                    if (answer.IndexOf(challenge.TargetWord, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        feedbackMessage = $"Good effort! For '{challenge.TargetWord}', try incorporating its meaning more clearly in your response.";
                    }
                    else
                    {
                        feedbackMessage = "Not quite right. Please try again focusing on the question.";
                    }
                }
                else
                {
                    feedbackMessage = $"Not quite right. The correct answer is: {correctAnswer}";
                }
            }            
            // Re-check mobile status before showing feedback (in case initial detection failed)
            await EnsureMobileDetection();
            
            Console.WriteLine($"Showing feedback - isMobileDevice: {isMobileDevice}");
            
            showFeedback = true;
            StartFeedbackTimer();
            PlayAudio = true;
            userInput = ""; // Clear input after processing
            isLoading = false; // Reset loading state after processing answer
            StateHasChanged();
            
            // Scroll to the feedback section after a brief delay to ensure it's rendered
            await ScrollToFeedback();

            // Debug logging
            Console.WriteLine($"AIWordTutor: PlayAudio set to true, lastAnswerCorrect: {lastAnswerCorrect}");

            await Task.Delay(2000); // Longer delay for sound effect to play
            PlayAudio = false;
            StateHasChanged();
        }
        private async Task<bool> CheckAnswerWithAI(WordChallenge challenge, string answer)
        {
            if (challenge.IsOpenEnded)
            {
                // First, check if this is obviously a non-answer before sending to AI
                if (!IsValidAnswer(answer))
                {
                    Console.WriteLine($"Pre-filtered non-answer: '{answer}'");
                    return false;
                }

                // Use AI to evaluate open-ended responses
                var prompt = $@"Evaluate this English learning response:

Question: {challenge.Question}
Target word: '{challenge.TargetWord}'
Student answer: '{answer}'

Please assess:
1. Does the response demonstrate understanding of the word '{challenge.TargetWord}'?
2. Is the usage appropriate and contextually correct?
3. Is it a meaningful attempt (not just random text)?

IMPORTANT: If the student says they don't know, are unsure, or gives non-answers like 'I don't know', 'not sure', 'no idea', etc., respond with INCORRECT.

Respond with: CORRECT if it shows good understanding, or INCORRECT if it doesn't.
Use emotes where applicable
Then provide a brief, encouraging feedback comment (1 sentence).

Format:
RESULT: [CORRECT/INCORRECT]
FEEDBACK: [Your encouraging comment]";

                var systemMessage = "You are a patient English language teacher evaluating student responses with encouragement and constructive guidance. Be strict about marking non-answers and 'I don't know' responses as incorrect.";

                try
                {
                    var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                    return ParseAIEvaluation(aiResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AI evaluation: {ex.Message}");
                    // Improved fallback logic that recognizes non-answers
                    return IsValidAnswer(answer);
                }
            }

            return string.Equals(answer.Trim(), challenge.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private bool ParseAIEvaluation(string aiResponse)
        {
            try
            {
                var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("RESULT:", StringComparison.OrdinalIgnoreCase))
                    {
                        var result = line.Substring(7).Trim().ToUpper();
                        return result.Contains("CORRECT");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing AI evaluation: {ex.Message}");
            }
            
            return false; // Default to incorrect if parsing fails
        }        private bool CheckAnswer(WordChallenge challenge, string answer)
        {
            if (challenge.IsOpenEnded)
            {
                // For open-ended questions, use the improved answer validation
                return IsValidAnswer(answer);
            }

            return string.Equals(answer.Trim(), challenge.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private int GetScoreForDifficulty()
        {
            return difficulty switch
            {
                DifficultyLevel.Beginner => 10,
                DifficultyLevel.Intermediate => 15,
                DifficultyLevel.Advanced => 25,
                _ => 10
            };
        }

        private async Task<string> GeneratePositiveFeedback(string word, string userAnswer = "")
        {
            var prompt = $@"Create a brief, encouraging piece of feedback for an English learner who just correctly used or understood the word '{word}'{(string.IsNullOrEmpty(userAnswer) ? "" : $" in their response: '{userAnswer}'")}. 

Make it:
- Positive and motivating
- Specific to their achievement
- Encouraging for continued learning
- 1-2 sentences maximum

Examples: 'Excellent! You really understand how to use '{word}' correctly.' or 'Perfect! Your grasp of '{word}' is impressive.'";

            var systemMessage = "You are an encouraging English teacher giving positive reinforcement to language learners.";
            
            try
            {
                return await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating feedback: {ex.Message}");
                // Fallback to simple positive feedback
                return $"Excellent! You really understand how to use '{word}' correctly.";
            }
        }

        private string GeneratePositiveFeedback(string word)
        {
            var feedbacks = new[]
            {
                $"Excellent! You really understand '{word}' well.",
                $"Perfect! Great job with '{word}'.",
                $"Outstanding! You've mastered '{word}'.",
                $"Wonderful! Your understanding of '{word}' is impressive.",
                $"Brilliant! You've got '{word}' down perfectly."
            };

            return feedbacks[_random.Next(feedbacks.Length)];
        }

        private async Task ScrollChatToBottom()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("scrollToBottom", chatHistoryContainer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scrolling chat: {ex.Message}");
            }
        }        private async Task FocusChatInput()
        {
            try
            {
                await chatInputElement.FocusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error focusing chat input: {ex.Message}");
                // Fallback to JavaScript method
                try
                {
                    await JSRuntime.InvokeVoidAsync("focusChatInput");
                }
                catch (Exception jsEx)
                {
                    Console.WriteLine($"Error with JS focus fallback: {jsEx.Message}");
                }
            }
        }        private void EvaluateVocabularyUsage(string userMessage)
        {
            Console.WriteLine($"Evaluating message: {userMessage}");
            Console.WriteLine($"Target words: {string.Join(", ", conversationTargetWords)}");
            
            if (conversationTargetWords.Count == 0) return;
            
            var messageLower = userMessage.ToLowerInvariant();
            var wordsFoundInMessage = new List<string>();
            
            foreach (var targetWord in conversationTargetWords)
            {
                if (messageLower.Contains(targetWord.ToLowerInvariant()) && !usedTargetWords.Contains(targetWord))
                {
                    usedTargetWords.Add(targetWord);
                    wordsFoundInMessage.Add(targetWord);
                    wordsUsedCorrectly++;
                    score += 10; // Award points for using target words
                    
                    Console.WriteLine($"Found target word: {targetWord}");
                }
            }
            
            if (wordsFoundInMessage.Count > 0)
            {
                Console.WriteLine($"Words found this message: {string.Join(", ", wordsFoundInMessage)}");
                Console.WriteLine($"Total words used: {wordsUsedCorrectly}/{conversationTargetWords.Count}");
                Console.WriteLine($"Current score: {score}");
            }
        }
          private async Task TestWordEvaluation()
        {
            var testSentence = "I think analyzing that my sentence would be beneficial and not to do so would be negligent I will persist in creating this sentence otherwise it would be a catastrophe.";
            Console.WriteLine($"Testing word evaluation with: {testSentence}");
            
            // Show initial state
            Console.WriteLine($"Before test - Score: {score}, Words used: {wordsUsedCorrectly}/{conversationTargetWords.Count}");
            
            EvaluateVocabularyUsage(testSentence);
            
            // Force UI update after test
            await InvokeAsync(StateHasChanged);
            
            // Show final state
            Console.WriteLine($"After test - Score: {score}, Words used: {wordsUsedCorrectly}/{conversationTargetWords.Count}");
        }        private void StartFeedbackTimer()
        {
            StopFeedbackTimer(); // Stop any existing timer
            
            // Reset countdown
            countdownSeconds = totalCountdownSeconds;
            Console.WriteLine($"Starting feedback timer - countdown reset to {countdownSeconds}, Mobile: {isMobileDevice}");
            
            // On mobile devices, don't start the auto-close timer to allow full-screen reading
            if (!isMobileDevice)
            {
                // Start countdown timer that updates every second
                countdownTimer = new Timer(UpdateCountdown, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                
                // Start the main feedback timer for auto-hide
                feedbackTimer = new Timer(AutoHideFeedback, null, TimeSpan.FromSeconds(totalCountdownSeconds), Timeout.InfiniteTimeSpan);
            }
            else
            {
                // On mobile, set countdown to 0 to hide the countdown display
                countdownSeconds = 0;
                Console.WriteLine("Mobile device detected - disabling auto-close timer for full-screen feedback");
            }
        }

        private void StopFeedbackTimer()
        {
            feedbackTimer?.Dispose();
            feedbackTimer = null;
            countdownTimer?.Dispose();
            countdownTimer = null;
            Console.WriteLine("Stopped feedback timers");
        }

        private async Task ScrollToFeedback()
        {
            try
            {
                // Wait a brief moment for the DOM to update
                await Task.Delay(100);
                
                // Scroll the feedback section into view
                await JSRuntime.InvokeVoidAsync("scrollToElement", feedbackSectionRef);
                Console.WriteLine("Scrolled to feedback section");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scrolling to feedback: {ex.Message}");
            }
        }        private async void UpdateCountdown(object? state)
        {
            await InvokeAsync(() =>
            {
                if (showFeedback && countdownSeconds > 0)
                {
                    countdownSeconds--;
                    Console.WriteLine($"Countdown updated: {countdownSeconds}");
                    StateHasChanged();
                }
            });
        }private async void AutoHideFeedback(object? state)
        {
            await InvokeAsync(async () =>
            {
                if (showFeedback)
                {
                    showFeedback = false;
                    feedbackMessage = "";
                    
                    // Automatically advance to the next challenge when feedback auto-hides
                    await ContinueLearning();
                }
            });
        }

        // Text-to-speech methods
        private async Task ToggleTextToSpeech()
        {
            try
            {
                if (isReading)
                {
                    await JSRuntime.InvokeVoidAsync("stopSpeech");
                    isReading = false;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentContent))
                    {
                        // Check if speech synthesis is supported
                        speechSupported = await JSRuntime.InvokeAsync<bool>("checkSpeechSupport");
                        
                        if (speechSupported)
                        {
                            await JSRuntime.InvokeVoidAsync("speakText", currentContent);
                            isReading = true;
                        }
                        else
                        {
                            errorMessage = "Text-to-speech is not supported in this browser.";
                        }
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with text-to-speech: {ex.Message}");
                errorMessage = "Error occurred while using text-to-speech.";
                isReading = false;
                StateHasChanged();
            }
        }

        private async Task ToggleChatMessageSpeech(string message)
        {
            try
            {
                if (isReadingChat && currentReadingMessage == message)
                {
                    // Stop reading if this message is currently being read
                    await JSRuntime.InvokeVoidAsync("stopSpeech");
                    isReadingChat = false;
                    currentReadingMessage = "";
                }
                else
                {
                    // Stop any currently playing speech first
                    if (isReading)
                    {
                        await JSRuntime.InvokeVoidAsync("stopSpeech");
                        isReading = false;
                    }
                    if (isReadingChat)
                    {
                        await JSRuntime.InvokeVoidAsync("stopSpeech");
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        // Check if speech synthesis is supported
                        speechSupported = await JSRuntime.InvokeAsync<bool>("checkSpeechSupport");
                        
                        if (speechSupported)
                        {
                            await JSRuntime.InvokeVoidAsync("speakText", message);
                            isReadingChat = true;
                            currentReadingMessage = message;
                        }
                        else
                        {
                            errorMessage = "Text-to-speech is not supported in this browser.";
                        }
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with chat text-to-speech: {ex.Message}");
                errorMessage = "Error occurred while using text-to-speech.";
                isReadingChat = false;
                currentReadingMessage = "";
                StateHasChanged();
            }
        }

        private async Task ToggleHangmanHintSpeech()
        {
            try
            {
                if (isReadingHangmanHint)
                {
                    await JSRuntime.InvokeVoidAsync("stopSpeech");
                    isReadingHangmanHint = false;
                }
                else
                {
                    // Stop any currently playing speech first
                    if (isReading)
                    {
                        await JSRuntime.InvokeVoidAsync("stopSpeech");
                        isReading = false;
                    }
                    if (isReadingChat)
                    {
                        await JSRuntime.InvokeVoidAsync("stopSpeech");
                        isReadingChat = false;
                        currentReadingMessage = "";
                    }

                    if (!string.IsNullOrEmpty(hangmanDefinition))
                    {
                        // Check if speech synthesis is supported
                        speechSupported = await JSRuntime.InvokeAsync<bool>("checkSpeechSupport");
                        
                        if (speechSupported)
                        {
                            await JSRuntime.InvokeVoidAsync("speakText", hangmanDefinition);
                            isReadingHangmanHint = true;
                        }
                        else
                        {
                            errorMessage = "Text-to-speech is not supported in this browser.";
                        }
                    }
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with hangman hint text-to-speech: {ex.Message}");
                errorMessage = "Error occurred while using text-to-speech.";
                isReadingHangmanHint = false;
                StateHasChanged();
            }
        }

        private void OnSpeechEnd()
        {
            isReading = false;
            isReadingChat = false;
            currentReadingMessage = "";
            isReadingHangmanHint = false;
            StateHasChanged();
        }
        public void Dispose()
        {
            StopFeedbackTimer();
            pictureGuessToastTimer?.Dispose();
            pictureGuessToastTimer = null;
            
            // Stop any ongoing speech synthesis
            try
            {
                JSRuntime.InvokeVoidAsync("stopSpeech");
                isReading = false;
                isReadingChat = false;
                currentReadingMessage = "";
                isReadingHangmanHint = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping speech on dispose: {ex.Message}");
            }
        }        private async Task GenerateNewConversationTopic()
        {
            // Reset conversation state for new topic
            var newWords = await GetWordsFromAI(5);
            conversationTargetWords = newWords;
            usedTargetWords.Clear();
            wordsUsedCorrectly = 0;
            
            // Generate new conversation starter
            await GenerateConversationStarter(newWords);
        }

        private bool IsValidAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;

            var trimmedAnswer = answer.Trim().ToLowerInvariant();
            
            // List of common non-answers that should not be marked as correct
            var nonAnswers = new[]
            {
                "don't know",
                "dont know", 
                "i don't know",
                "i dont know",
                "no idea",
                "not sure",
                "idk",
                "i'm not sure",
                "im not sure",
                "i have no idea",
                "no clue",
                "pass",
                "skip",
                "nothing",
                "none",
                "unsure"
            };

            // Check if the answer is a recognized non-answer
            if (nonAnswers.Contains(trimmedAnswer))
                return false;

            // Check if answer is too short to be meaningful
            if (trimmedAnswer.Length < 3)
                return false;

            // Check if answer is just repeated characters or nonsense
            if (IsNonsenseAnswer(trimmedAnswer))
                return false;

            // If it passes all the non-answer checks and has reasonable length, consider it valid
            return trimmedAnswer.Length >= 10;
        }

        private bool IsNonsenseAnswer(string answer)
        {
            // Check for repeated characters (like "aaaaaaa" or "xxxxxxx")
            if (answer.Length >= 5 && answer.Distinct().Count() <= 2)
                return true;

            // Check for keyboard mashing patterns
            var keyboardPatterns = new[] { "asdf", "qwer", "zxcv", "hjkl", "1234", "abcd" };
            if (keyboardPatterns.Any(pattern => answer.Contains(pattern)))
                return true;

            return false;
        }

        private string GetHangmanDisplay()
        {
            if (string.IsNullOrEmpty(hangmanWord)) return string.Empty;
            return string.Join(" ", hangmanWord.Select(c => hangmanGuesses.Contains(char.ToUpperInvariant(c)) ? c.ToString() : "_"));
        }

        public async Task ProcessHangmanGuess(char guess)
        {
            if (hangmanGameOver || string.IsNullOrEmpty(hangmanWord)) return;
            guess = char.ToUpperInvariant(guess);
            if (!char.IsLetter(guess) || hangmanGuesses.Contains(guess)) return;
            hangmanGuesses.Add(guess);
            var wordUpper = hangmanWord.ToUpperInvariant();
            
            Console.WriteLine($"Processing guess '{guess}' for word '{hangmanWord}' (length: {hangmanWord.Length})");
            
            if (!wordUpper.Contains(guess))
            {
                // Incorrect guess - play incorrect sound
                PlayAudio = true;
                lastAnswerCorrect = false;
                hangmanWrongGuesses++;
                Console.WriteLine($"Incorrect guess. Wrong guesses: {hangmanWrongGuesses}/{hangmanMaxWrong}");
                if (hangmanWrongGuesses >= hangmanMaxWrong)
                {
                    hangmanGameOver = true;
                    hangmanWin = false;
                }
            }
            else
            {
                // Correct guess - play correct sound
                PlayAudio = true;
                lastAnswerCorrect = true;
                Console.WriteLine($"Correct guess! Guessed letters so far: {string.Join(", ", hangmanGuesses.OrderBy(c => c))}");
                
                // Check if all letters have been guessed (fixed win condition)
                var lettersInWord = wordUpper.Where(char.IsLetter).ToList();
                var allLettersGuessed = lettersInWord.All(c => hangmanGuesses.Contains(c));
                
                Console.WriteLine($"Letters in word: {string.Join(", ", lettersInWord)}");
                Console.WriteLine($"All letters guessed: {allLettersGuessed}");
                
                if (allLettersGuessed)
                {
                    hangmanGameOver = true;
                    hangmanWin = true;
                    score += 20; // Award points for win
                    Console.WriteLine($"HANGMAN WIN! Word was '{hangmanWord}'");
                }
            }
            // Trigger UI update asynchronously to satisfy async signature
            await InvokeAsync(StateHasChanged);
            
            // Reset audio after delay to allow sound to play
            await Task.Delay(1000);
            PlayAudio = false;
            await InvokeAsync(StateHasChanged);
        }

        private void OnHangmanLayoutChanged(bool useKeyboard)
        {
            useKeyboardLayout = useKeyboard;
            StateHasChanged();
        }

        // Hangman state fields
        private string hangmanWord = string.Empty;
        private string? hangmanDefinition;
        private HashSet<char> hangmanGuesses = new();
        private int hangmanWrongGuesses = 0;
        private int hangmanMaxWrong = 6;
        private bool hangmanGameOver = false;
        private bool hangmanWin = false;
        private bool useKeyboardLayout = true; // Default to keyboard layout
        
        private async Task FocusAnswerTextAreaIfNeeded()
        {
            if (currentGameMode == GameMode.StoryAdventure && currentChallengeIndex < currentChallenges.Count)
            {
                try
                {
                    await InvokeAsync(async () => await answerTextAreaRef.FocusAsync());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error focusing answer textarea: {ex.Message}");
                }
            }
        }

        public RenderFragment RenderCurrentChallenge => builder =>
        {
            Console.WriteLine($"RenderCurrentChallenge: index={currentChallengeIndex}, count={currentChallenges.Count}, isLoading={isLoading}");
            Console.WriteLine($"Challenges: {string.Join(", ", currentChallenges.Select(c => c.Question))}");
            
            if (currentChallengeIndex >= currentChallenges.Count)
            {
                if (isLoading)
                {
                    Console.WriteLine("Showing loading message");
                    builder.AddMarkupContent(0, "<p>🤖 AI is generating your next challenge...</p>");
                }
                else
                {
                    Console.WriteLine("Showing no challenges message");
                    builder.AddMarkupContent(0, "<p>No more challenges available. Something went wrong.</p>");
                }
                return;
            }

            var challenge = currentChallenges[currentChallengeIndex];
            Console.WriteLine($"Rendering challenge: {challenge.Question}, IsOpenEnded: {challenge.IsOpenEnded}");
            
            if (challenge.IsOpenEnded)
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "challenge-section");

                // Show context/scenario for contextual challenges
                if (challenge.Type == ChallengeType.Context && !string.IsNullOrEmpty(challenge.Context))
                {
                    builder.OpenElement(1, "div");
                    builder.AddAttribute(2, "class", "scenario-content");
                    builder.AddAttribute(3, "style", "background: #f8f9fa; padding: 1rem; border-radius: 10px; margin-bottom: 1.5rem; border-left: 4px solid #667eea;");
                    builder.OpenElement(4, "h5");
                    builder.AddAttribute(5, "style", "color: #667eea; margin-bottom: 0.5rem;");
                    builder.AddContent(6, "📖 Scenario:");
                    builder.CloseElement();
                    builder.OpenElement(7, "p");
                    builder.AddAttribute(8, "style", "margin: 0; font-style: italic; color: #555;");
                    builder.AddContent(9, challenge.Context);
                    builder.CloseElement();
                    builder.CloseElement();
                }

                builder.OpenElement(2, "h4");
                builder.AddAttribute(3, "class", "challenge-question");
                builder.AddAttribute(4, "style", "color: #111;");
                builder.AddContent(5, challenge.Question);
                builder.CloseElement();

                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", "text-input-container");
                builder.OpenElement(6, "textarea");
                builder.AddAttribute(7, "class", "answer-textarea");
                builder.AddAttribute(8, "placeholder", "Write your answer here...");
                builder.AddAttribute(9, "value", userInput);
                builder.AddAttribute(10, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, (e) => userInput = e.Value?.ToString() ?? ""));
                builder.AddAttribute(11, "onkeydown", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.KeyboardEventArgs>(this, HandleKeyPressForTextarea));
                builder.AddElementReferenceCapture(12, r => answerTextAreaRef = r);
                builder.CloseElement();
                if (isLoading)
                {
                    builder.OpenElement(100, "div");
                    builder.AddAttribute(101, "class", "answer-spinner-overlay");
                    builder.OpenElement(102, "div");
                    builder.AddAttribute(103, "class", "loading-spinner");
                    builder.CloseElement();
                    builder.CloseElement();
                }
                builder.OpenElement(13, "button");
                builder.AddAttribute(14, "class", "submit-btn");
                builder.AddAttribute(15, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ProcessAnswer(userInput)));
                builder.AddAttribute(15, "disabled", isLoading);
                builder.AddContent(16, "Submit Answer");
                builder.CloseElement();
                builder.CloseElement(); // text-input-container
                builder.CloseElement(); // challenge-section
            }
            else
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "challenge-section");
                builder.OpenElement(2, "h4");
                builder.AddAttribute(3, "class", "challenge-question");
                builder.AddAttribute(4, "style", "color: #111;");
                builder.AddContent(5, challenge.Question);
                builder.CloseElement();
                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", "options-grid");
                builder.AddAttribute(6, "style",
                    "display: grid; " +
                    "grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); " +
                    "gap: 15px; " +
                    "margin-top: 1.5rem; " +
                    "margin-bottom: 1rem;");
                if (challenge.Options != null)
                {
                    var optionIndex = 7;
                    foreach (var option in challenge.Options)
                    {
                        builder.OpenElement(optionIndex++, "button");
                        builder.AddAttribute(optionIndex++, "class", "option-btn");
                        builder.AddAttribute(optionIndex++, "style",
                            "background: linear-gradient(135deg, #f8f9fa, #ffffff); " +
                            "border: 2px solid #dee2e6; " +
                            "border-radius: 15px; " +
                            "padding: 18px 20px; " +
                            "cursor: pointer; " +
                            "transition: all 0.3s ease; " +
                            "text-align: center; " +
                            "font-size: 1.1rem; " +
                            "font-weight: 500; " +
                            "color: #495057; " +
                            "box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05); " +
                            "font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; " +
                            "min-height: 60px; " +
                            "width: 100%; " +
                            "display: flex; " + "align-items: center; " + "justify-content: center;");
                        builder.AddAttribute(optionIndex++, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ProcessAnswer(option)));
                        builder.AddContent(optionIndex++, option);
                        builder.CloseElement();
                    }
                    builder.CloseElement(); // options-grid
                }
                builder.CloseElement(); // challenge-section
            }
        };

        private async Task<string> GetHangmanHintAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return "";
            
            var prompt = $@"Create a cryptic hint for the word '{word}' for a hangman game. The hint should:
1. NOT contain the word itself or any obvious forms of it
2. NOT be too specific or revealing
3. Be challenging but fair - give a general category or vague description
4. Be maximum 10 words
5. Use synonyms and indirect references

Examples:
- For 'rhythm' -> 'Musical beat or pattern in time'
- For 'elephant' -> 'Large gray mammal with trunk'
- For 'computer' -> 'Electronic device for processing data'

Hint for '{word}':";

            var systemMessage = "You are creating challenging but fair hints for hangman games. Never use the target word directly.";
            
            try
            {
                var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                var hint = aiResponse.Trim();
                
                // Double-check that the hint doesn't contain the word
                if (hint.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Fallback to a very generic hint based on theme
                    return $"A word related to {themeInput?.ToLower() ?? "this topic"}";
                }
                
                return hint;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating hangman hint: {ex.Message}");
                // Fallback to theme-based hint
                return $"A word related to {themeInput?.ToLower() ?? "this topic"}";
            }
        }

        // Word Type Snap callback method
        private Task OnWordTypeSnapScoreChanged(int newScore)
        {
            // Update the main game score with the Word Type Snap score
            score = newScore;
            StateHasChanged();
            return Task.CompletedTask;
        }

        // Word Type Snap streak callback method
        private Task OnWordTypeSnapStreakChanged(int newStreak)
        {
            // Update the main game streak with the Word Type Snap streak
            streak = newStreak;
            StateHasChanged();
            return Task.CompletedTask;
        }
    }
}
