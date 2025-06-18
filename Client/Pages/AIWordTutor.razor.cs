using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text;
using BlazorApp.Client.Shared;

namespace BlazorApp.Client.Pages
{    public partial class AIWordTutor : ComponentBase
    {
        [Inject] private HttpClient HttpClient { get; set; } = default!;
        [Inject] public required IOpenAIApiKeyService OpenAIApiKeyService { get; set; }
        [Inject] public required IOpenAIService OpenAIService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        // UI references
        private ElementReference chatHistoryContainer;

        // Game state
        private bool gameStarted = false;
        private GameMode currentGameMode = GameMode.StoryAdventure;
        private DifficultyLevel difficulty = DifficultyLevel.Intermediate;
        private int score = 0;
        private int streak = 0;
        private bool isLoading = false;
        private string errorMessage = "";

        // Learning session data
        private AILearningSession? currentSession;
        private string? currentContent = "";
        private List<string> conversationHistory = new();
        private string userInput = "";
        private List<WordChallenge> currentChallenges = new();        private int currentChallengeIndex = 0;
        private bool PlayAudio = false;
        private string apiKeyStatus = "";
        private bool hasApiKey = false;

        // Feedback system
        private bool showFeedback = false;
        private string feedbackMessage = "";
        private bool lastAnswerCorrect = false;
        private string correctAnswer = "";

        // Sample words organized by difficulty
        private readonly Dictionary<DifficultyLevel, List<string>> wordLibrary = new()
        {
            {
                DifficultyLevel.Beginner, new List<string>
                {
                    "adventure", "beautiful", "celebrate", "discover", "enormous", "friendship",
                    "grateful", "harmony", "important", "journey", "kindness", "laughter",
                    "mystery", "nature", "opportunity", "peaceful", "question", "respect",
                    "sunshine", "treasure", "umbrella", "victory", "wonderful", "explore", "youthful"
                }
            },
            {
                DifficultyLevel.Intermediate, new List<string>
                {
                    "ambitious", "beneficial", "comprehensive", "demonstrate", "elaborate",
                    "fundamental", "genuine", "hypothesis", "inevitable", "jurisdiction",
                    "magnificent", "negligent", "optimistic", "persistent", "reluctant",
                    "sophisticated", "temporary", "unprecedented", "versatile", "wisdom",
                    "analyze", "bureaucracy", "catastrophe", "diligent", "empathy"
                }
            },
            {
                DifficultyLevel.Advanced, new List<string>
                {
                    "ubiquitous", "perspicacious", "serendipitous", "magnanimous", "eloquent",
                    "ephemeral", "indigenous", "meticulous", "ostentatious", "pragmatic",
                    "quintessential", "resilient", "scrupulous", "tenacious", "vicarious",
                    "whimsical", "xenophobic", "zealous", "acquiesce", "belligerent",
                    "cacophony", "deleterious", "effervescent", "facetious", "gregarious"
                }
            }
        };        protected override async Task OnInitializedAsync()
        {
            // Check if API key already exists
            var apiKey = await OpenAIApiKeyService.GetApiKeyAsync();
            hasApiKey = !string.IsNullOrEmpty(apiKey);
        }

        private void SetDifficulty(DifficultyLevel newDifficulty)
        {
            difficulty = newDifficulty;
            StateHasChanged();
        }        private async Task OnApiKeySaved()
        {
            apiKeyStatus = "saved";
            hasApiKey = true;
            StateHasChanged();
            
            // Clear the status after a few seconds
            await Task.Delay(3000);
            apiKeyStatus = "";
            StateHasChanged();
        }        private async Task StartGame(GameMode mode)
        {
            // Check if API key exists before starting
            var apiKey = await OpenAIApiKeyService.GetApiKeyAsync();
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKeyStatus = "missing";
                StateHasChanged();
                return;
            }

            currentGameMode = mode;
            gameStarted = true;
            await InitializeGameSession();
        }

        private async Task InitializeGameSession()
        {
            isLoading = true;
            try
            {
                currentSession = new AILearningSession
                {
                    GameMode = currentGameMode,
                    Difficulty = difficulty,
                    StartTime = DateTime.Now,
                    WordsLearned = new List<string>(),
                    PlayerPreferences = new PlayerPreferences()
                };

                await GenerateInitialContent();
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

        private async Task GenerateInitialContent()
        {
            var selectedWords = GetRandomWords(3); // Start with 3 words
            currentChallenges.Clear();
            currentChallengeIndex = 0;

            switch (currentGameMode)
            {
                case GameMode.StoryAdventure:
                    await GenerateStoryContent(selectedWords);
                    break;
                case GameMode.ConversationPractice:
                    await GenerateConversationStarter(selectedWords);
                    break;
                case GameMode.ContextualLearning:
                    await GenerateContextualChallenges(selectedWords);
                    break;
                case GameMode.PersonalizedQuiz:
                    await GenerateQuizQuestions(selectedWords);
                    break;
            }
        }

        private List<string> GetRandomWords(int count)
        {
            var words = wordLibrary[difficulty];
            var random = new Random();
            return words.OrderBy(x => random.Next()).Take(count).ToList();
        }        private async Task GenerateStoryContent(List<string> words)
        {
            var wordsText = string.Join(", ", words);
            var difficultyText = difficulty.ToString().ToLower();              var prompt = $@"Create an engaging, {difficultyText}-level English learning story that naturally incorporates these words: {wordsText}. 

IMPORTANT: Format your response EXACTLY as shown below. Do not add any extra text or formatting:

STORY:
Write a 2-3 sentence story here that uses the vocabulary words naturally. Make it interesting and educational.

QUESTIONS:
1. What does '{words[0]}' mean in this story context?
A) First option here
B) Second option here  
C) Third option here
D) Fourth option here
Correct: A

2. Use the word '{words[1]}' in your own sentence:

3. How does '{words[2]}' contribute to the story's meaning?

CRITICAL: 
- Put ONLY the story after 'STORY:'
- Put questions starting with '1.' after 'QUESTIONS:'  
- For question 1, provide exactly 4 options (A, B, C, D) on separate lines
- End with 'Correct: [letter]' on its own line
- Questions 2 and 3 are open-ended (no options needed)";var systemMessage = "You are an expert English language tutor creating engaging educational content for vocabulary learning.";
            
            var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            await ParseStoryResponse(aiResponse, words);
        }        private async Task ParseStoryResponse(string aiResponse, List<string> words)
        {
            try
            {
                // Clean up the response
                var cleanResponse = aiResponse.Trim();
                
                // First, try to find the STORY: and QUESTIONS: markers
                var storyStartIndex = cleanResponse.IndexOf("STORY:", StringComparison.OrdinalIgnoreCase);
                var questionsStartIndex = cleanResponse.IndexOf("QUESTIONS:", StringComparison.OrdinalIgnoreCase);
                
                if (storyStartIndex >= 0 && questionsStartIndex > storyStartIndex)
                {
                    // Extract story content between markers
                    var storyStart = storyStartIndex + "STORY:".Length;
                    var storyLength = questionsStartIndex - storyStart;
                    var storyContent = cleanResponse.Substring(storyStart, storyLength).Trim();
                    
                    // Clean the story content - remove any remaining markers or formatting
                    var storyLines = storyContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !string.IsNullOrWhiteSpace(line) && 
                                     !line.Trim().StartsWith("QUESTIONS") &&
                                     !line.Trim().StartsWith("1.") &&
                                     !line.Trim().StartsWith("2.") &&
                                     !line.Trim().StartsWith("3."))
                        .Select(line => line.Trim())
                        .Where(line => !string.IsNullOrWhiteSpace(line));
                    
                    currentContent = string.Join(" ", storyLines);
                    
                    // Extract questions section
                    var questionsContent = cleanResponse.Substring(questionsStartIndex + "QUESTIONS:".Length).Trim();
                    await ParseQuestionsFromResponse(questionsContent, words);
                }
                else
                {
                    // Fallback parsing - try to separate story from questions
                    var lines = cleanResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var storyLines = new List<string>();
                    var questionLines = new List<string>();
                    bool inQuestions = false;
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // Skip empty lines and headers
                        if (string.IsNullOrWhiteSpace(trimmedLine) || 
                            trimmedLine.Equals("STORY:", StringComparison.OrdinalIgnoreCase) ||
                            trimmedLine.Equals("QUESTIONS:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        
                        // Detect start of questions
                        if (trimmedLine.StartsWith("1.") || trimmedLine.StartsWith("What") || 
                            trimmedLine.StartsWith("How") || trimmedLine.StartsWith("Why") ||
                            trimmedLine.StartsWith("Which") || trimmedLine.StartsWith("Use the word") ||
                            (trimmedLine.Contains("?") && (trimmedLine.StartsWith("A)") || trimmedLine.StartsWith("B)") || trimmedLine.Contains("Correct:"))))
                        {
                            inQuestions = true;
                        }
                        
                        if (inQuestions)
                        {
                            questionLines.Add(trimmedLine);
                        }
                        else
                        {
                            storyLines.Add(trimmedLine);
                        }
                    }
                    
                    // Join story lines properly
                    currentContent = string.Join(" ", storyLines.Where(line => !string.IsNullOrWhiteSpace(line)));
                    
                    if (questionLines.Count > 0)
                    {
                        var questionsText = string.Join("\n", questionLines);
                        await ParseQuestionsFromResponse(questionsText, words);
                    }
                    else
                    {
                        await CreateFallbackQuestions(words);
                    }
                }
                
                // Ensure we have some content
                if (string.IsNullOrWhiteSpace(currentContent))
                {
                    currentContent = "Let's start your English learning adventure with these words!";
                    await CreateFallbackQuestions(words);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing story response: {ex.Message}");
                // Create a simple story from the first few sentences
                var sentences = aiResponse.Split('.', StringSplitOptions.RemoveEmptyEntries);
                currentContent = sentences.Length > 0 ? string.Join(". ", sentences.Take(3)) + "." : "A learning story with vocabulary words.";
                await CreateFallbackQuestions(words);
            }
        }        private async Task ParseQuestionsFromResponse(string questionsText, List<string> words)
        {
            currentChallenges = new List<WordChallenge>();
            var lines = questionsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            WordChallenge? currentQuestion = null;
            
            // Debug logging
            Console.WriteLine($"Parsing questions from: {questionsText}");
            Console.WriteLine($"Split into {lines.Length} lines");
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                Console.WriteLine($"Processing line: '{trimmedLine}'");
                
                if (trimmedLine.StartsWith("1."))
                {
                    // Save previous question if it exists
                    if (currentQuestion != null) 
                    {
                        currentChallenges.Add(currentQuestion);
                        Console.WriteLine($"Added previous question with {currentQuestion.Options?.Count ?? 0} options");
                    }
                    
                    currentQuestion = new WordChallenge
                    {
                        Type = ChallengeType.Comprehension,
                        TargetWord = words.ElementAtOrDefault(0) ?? "",
                        Question = trimmedLine.Substring(2).Trim(),
                        Options = new List<string>(),
                        IsOpenEnded = true // Start as open-ended, change if we find options
                    };
                    Console.WriteLine($"Created new question 1: {currentQuestion.Question}");
                }
                else if (trimmedLine.StartsWith("2."))
                {
                    // Save previous question if it exists
                    if (currentQuestion != null) 
                    {
                        currentChallenges.Add(currentQuestion);
                        Console.WriteLine($"Added question 1 with {currentQuestion.Options?.Count ?? 0} options, IsOpenEnded: {currentQuestion.IsOpenEnded}");
                    }
                    
                    currentQuestion = new WordChallenge
                    {
                        Type = ChallengeType.Usage,
                        TargetWord = words.ElementAtOrDefault(1) ?? "",
                        Question = trimmedLine.Substring(2).Trim(),
                        IsOpenEnded = true
                    };
                    Console.WriteLine($"Created new question 2: {currentQuestion.Question}");
                }
                else if (trimmedLine.StartsWith("3."))
                {
                    // Save previous question if it exists
                    if (currentQuestion != null) 
                    {
                        currentChallenges.Add(currentQuestion);
                        Console.WriteLine($"Added question 2 with {currentQuestion.Options?.Count ?? 0} options, IsOpenEnded: {currentQuestion.IsOpenEnded}");
                    }
                    
                    currentQuestion = new WordChallenge
                    {
                        Type = ChallengeType.Context,
                        TargetWord = words.ElementAtOrDefault(2) ?? "",
                        Question = trimmedLine.Substring(2).Trim(),
                        IsOpenEnded = true
                    };
                    Console.WriteLine($"Created new question 3: {currentQuestion.Question}");
                }
                else if (trimmedLine.StartsWith("A)") || trimmedLine.StartsWith("B)") || 
                         trimmedLine.StartsWith("C)") || trimmedLine.StartsWith("D)"))
                {
                    if (currentQuestion?.Options != null)
                    {
                        var option = trimmedLine.Substring(2).Trim();
                        currentQuestion.Options.Add(option);
                        Console.WriteLine($"Added option: {option}");
                        
                        // Mark as multiple choice since we found options
                        currentQuestion.IsOpenEnded = false;
                        Console.WriteLine($"Set question as multiple choice (IsOpenEnded = false)");
                    }
                }
                else if (trimmedLine.StartsWith("Correct:"))
                {
                    if (currentQuestion?.Options != null && currentQuestion.Options.Count > 0)
                    {
                        var correctLetter = trimmedLine.Substring(8).Trim().ToUpper();
                        Console.WriteLine($"Correct answer letter: {correctLetter}");
                        
                        if (correctLetter.Length > 0)
                        {
                            var index = correctLetter[0] - 'A';
                            if (index >= 0 && index < currentQuestion.Options.Count)
                            {
                                currentQuestion.CorrectAnswer = currentQuestion.Options[index];
                                Console.WriteLine($"Set correct answer to: {currentQuestion.CorrectAnswer}");
                            }
                        }
                    }
                }
            }
            
            // Don't forget to add the last question
            if (currentQuestion != null) 
            {
                currentChallenges.Add(currentQuestion);
                Console.WriteLine($"Added final question with {currentQuestion.Options?.Count ?? 0} options, IsOpenEnded: {currentQuestion.IsOpenEnded}");
            }
            
            Console.WriteLine($"Total challenges created: {currentChallenges.Count}");
            foreach (var (challenge, index) in currentChallenges.Select((c, i) => (c, i)))
            {
                Console.WriteLine($"Challenge {index + 1}: {challenge.Question}, Options: {challenge.Options?.Count ?? 0}, IsOpenEnded: {challenge.IsOpenEnded}");
            }
            
            // If we didn't get enough questions, create fallbacks
            if (currentChallenges.Count < 3)
            {
                await CreateFallbackQuestions(words);
            }
        }

        private Task CreateFallbackQuestions(List<string> words)
        {
            currentChallenges = new List<WordChallenge>();
            
            for (int i = 0; i < Math.Min(words.Count, 3); i++)
            {
                var word = words[i];
                var challengeType = i switch
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
                    Options = challengeType == ChallengeType.Comprehension ? GenerateMultipleChoiceOptions(word) : new List<string>(),                    CorrectAnswer = challengeType == ChallengeType.Comprehension ? GetSimpleDefinition(word) : ""
                });
            }
            
            return Task.CompletedTask;
        }        private async Task GenerateConversationStarter(List<string> words)
        {
            var wordsText = string.Join(", ", words);
            var difficultyText = difficulty.ToString().ToLower();
            
            var prompt = $@"Start a friendly, engaging conversation for {difficultyText}-level English learners. 
Naturally incorporate these vocabulary words: {wordsText}
Ask thoughtful questions that encourage the learner to use these words in their responses.
Keep it conversational, warm, and encouraging. Limit to 2-3 sentences.";

            var systemMessage = "You are a friendly, encouraging English conversation partner helping students practice vocabulary through natural dialogue.";
            
            var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            currentContent = aiResponse;
            conversationHistory.Add(currentContent);
            
            // Ensure the initial conversation is visible
            StateHasChanged();
            await ScrollChatToBottom();
        }private async Task GenerateContextualChallenges(List<string> words)
        {
            currentChallenges = new List<WordChallenge>();
            
            foreach (var word in words)
            {
                var prompt = $@"Create a realistic, practical scenario for using the word '{word}' in everyday English conversation. 
Provide:
1. A specific, relatable situation (1-2 sentences)
2. A guiding question that helps the learner think about how to use '{word}' appropriately in that context

Make it relevant to modern life and appropriate for {difficulty.ToString().ToLower()}-level learners.";

                var systemMessage = "You are an English language teacher creating practical, real-world vocabulary exercises.";
                
                var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                
                currentChallenges.Add(new WordChallenge
                {
                    Type = ChallengeType.RealWorld,
                    TargetWord = word,
                    Question = aiResponse,
                    IsOpenEnded = true,
                    Context = $"Real-world usage of '{word}'"
                });
            }

            currentContent = "Let's practice using words in real-world situations!";
        }        private async Task GenerateQuizQuestions(List<string> words)
        {
            currentChallenges = new List<WordChallenge>();

            foreach (var word in words)
            {
                var questionTypes = new[] { "definition", "synonym", "usage", "context" };
                var random = new Random();
                var questionType = questionTypes[random.Next(questionTypes.Length)];

                var prompt = $@"Create a {questionType} question for the word '{word}' appropriate for {difficulty.ToString().ToLower()}-level English learners.

For definition questions: Provide the question and 4 multiple choice options (A, B, C, D) with one correct answer.
For synonym questions: Ask for the closest meaning and provide 4 options.
For usage questions: Create a sentence with a blank and 4 word choices.
For context questions: Provide a short scenario and ask how the word fits.

Format as:
QUESTION: [Your question]
A) [Option 1]
B) [Option 2] 
C) [Option 3]
D) [Option 4]
CORRECT: [Letter of correct answer]";

                var systemMessage = "You are an expert language assessment creator making engaging vocabulary questions.";
                
                var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                var challenge = await ParseQuizResponse(aiResponse, word);
                currentChallenges.Add(challenge);
            }

            currentContent = "Let's test your knowledge with some smart questions!";
        }

        private Task<WordChallenge> ParseQuizResponse(string aiResponse, string word)
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
                }                return Task.FromResult(new WordChallenge
                {
                    Type = ChallengeType.Definition, // Default type
                    TargetWord = word,
                    Question = !string.IsNullOrEmpty(question) ? question : $"What does '{word}' mean?",
                    Options = options.Count > 0 ? options : GenerateMultipleChoiceOptions(word),
                    CorrectAnswer = !string.IsNullOrEmpty(correctAnswer) ? correctAnswer : GetSimpleDefinition(word)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing quiz response: {ex.Message}");                // Return fallback question
                return Task.FromResult(new WordChallenge
                {
                    Type = ChallengeType.Definition,
                    TargetWord = word,
                    Question = $"What does '{word}' mean?",
                    Options = GenerateMultipleChoiceOptions(word),
                    CorrectAnswer = GetSimpleDefinition(word)
                });
            }
        }

        // Utility methods for generating content
        private List<string> GenerateMultipleChoiceOptions(string word)
        {
            var options = new List<string>
            {
                GetSimpleDefinition(word),
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
        }

        private string GetSimpleDefinition(string word)
        {
            var definitions = new Dictionary<string, string>
            {
                {"adventure", "An exciting and unusual experience or activity"},
                {"beautiful", "Pleasing to look at; attractive"},
                {"celebrate", "To acknowledge a significant or happy day or event"},
                {"ambitious", "Having a strong desire for success or achievement"},
                {"ubiquitous", "Present, appearing, or found everywhere"},
                {"friendship", "A close relationship between friends"},
                {"wisdom", "The quality of having experience, knowledge, and good judgment"},
                {"magnificent", "Extremely beautiful, elaborate, or impressive"}
            };

            return definitions.GetValueOrDefault(word, "A meaningful word in English language");
        }

        private string GetSynonym(string word)
        {
            var synonyms = new Dictionary<string, string>
            {
                {"beautiful", "attractive"},
                {"adventure", "journey"},
                {"ambitious", "determined"},
                {"wisdom", "knowledge"},
                {"magnificent", "splendid"}
            };

            return synonyms.GetValueOrDefault(word, "similar");
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
            var random = new Random();
            return definitions[random.Next(definitions.Count)];
        }

        private string GetRandomWord()
        {
            var words = new List<string>
            {
                "triangle", "thermometer", "biology", "lake", "mountain", "telephone", "computer", "elephant"
            };
            var random = new Random();
            return words[random.Next(words.Count)];
        }

        // Missing UI helper methods
        private string GetGameModeTitle(GameMode mode)
        {
            return mode switch
            {
                GameMode.StoryAdventure => "📚 Story Adventure",
                GameMode.ConversationPractice => "💬 Conversation Practice",
                GameMode.ContextualLearning => "🎯 Contextual Learning",
                GameMode.PersonalizedQuiz => "🧠 Smart Quiz",
                _ => "Learning Mode"
            };
        }

        private string GetDifficultyName(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Beginner => "Beginner",
                DifficultyLevel.Intermediate => "Intermediate", 
                DifficultyLevel.Advanced => "Advanced",
                _ => "Unknown"
            };
        }        private Task ExitGame()
        {
            gameStarted = false;
            currentSession = null;
            currentChallenges.Clear();
            conversationHistory.Clear();
            userInput = "";
            feedbackMessage = "";
            errorMessage = "";
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userInput))
            {
                await SendMessage();
            }
        }        private async Task ContinueLearning()
        {
            if (currentChallengeIndex < currentChallenges.Count - 1)
            {
                currentChallengeIndex++;
                userInput = ""; // Reset user input instead of userAnswer
                lastAnswerCorrect = false; // Reset to false instead of null
                feedbackMessage = "";
                StateHasChanged();
            }
            else
            {
                // Generate new content or end session
                await GenerateInitialContent();
                currentChallengeIndex = 0;
                userInput = ""; // Reset user input instead of userAnswer
                lastAnswerCorrect = false; // Reset to false instead of null
                feedbackMessage = "";
                StateHasChanged();
            }
        }        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(userInput)) return;

            conversationHistory.Add(userInput);
            
            // Generate AI response using OpenAI
            var aiResponse = await GenerateAIResponse(userInput);
            conversationHistory.Add(aiResponse);
            
            userInput = "";
            StateHasChanged();
            
            // Scroll chat to bottom to show latest messages
            await ScrollChatToBottom();
        }

        private async Task<string> GenerateAIResponse(string userMessage)
        {
            var conversationContext = string.Join("\n", conversationHistory.TakeLast(6));
            var targetWords = currentSession?.WordsLearned ?? new List<string>();
            var wordsText = targetWords.Count > 0 ? string.Join(", ", targetWords) : "";
            
            var prompt = $@"Continue this English learning conversation. The student just said: '{userMessage}'

Previous conversation context:
{conversationContext}

Please provide an encouraging, natural response that:
1. Responds thoughtfully to what they said
2. Gently encourages use of vocabulary words{(wordsText.Length > 0 ? $" like: {wordsText}" : "")}
3. Asks a follow-up question to keep the conversation flowing
4. Stays appropriate for {difficulty.ToString().ToLower()}-level learners

Keep it conversational and supportive (1-2 sentences).";

            var systemMessage = "You are a patient, encouraging English conversation partner helping students practice vocabulary naturally.";
            
            try
            {
                return await OpenAIService.GenerateContentAsync(prompt, systemMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating AI response: {ex.Message}");
                return "That's interesting! Can you tell me more about that? I'd love to hear your thoughts.";
            }
        }

        private async Task ProcessAnswer(string answer)
        {
            if (currentChallengeIndex >= currentChallenges.Count) return;

            var challenge = currentChallenges[currentChallengeIndex];
            
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
                feedbackMessage = $"Not quite right. The correct answer is: {correctAnswer}";
            }

            showFeedback = true;
            PlayAudio = true;
            userInput = ""; // Clear input after processing
            StateHasChanged();

            await Task.Delay(100); // Brief pause for sound effect
            PlayAudio = false;
        }

        private async Task<bool> CheckAnswerWithAI(WordChallenge challenge, string answer)
        {
            if (challenge.IsOpenEnded)
            {
                // Use AI to evaluate open-ended responses
                var prompt = $@"Evaluate this English learning response:

Question: {challenge.Question}
Target word: '{challenge.TargetWord}'
Student answer: '{answer}'

Please assess:
1. Does the response demonstrate understanding of the word '{challenge.TargetWord}'?
2. Is the usage appropriate and contextually correct?
3. Is it a meaningful attempt (not just random text)?

Respond with: CORRECT if it shows good understanding, or INCORRECT if it doesn't.
Then provide a brief, encouraging feedback comment (1 sentence).

Format:
RESULT: [CORRECT/INCORRECT]
FEEDBACK: [Your encouraging comment]";

                var systemMessage = "You are a patient English language teacher evaluating student responses with encouragement and constructive guidance.";
                
                try
                {
                    var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);
                    return ParseAIEvaluation(aiResponse);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in AI evaluation: {ex.Message}");
                    // Fallback to simple check
                    return !string.IsNullOrWhiteSpace(answer) && answer.Length > 10;
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
        }

        private bool CheckAnswer(WordChallenge challenge, string answer)
        {
            if (challenge.IsOpenEnded)
            {
                // For open-ended questions, check if the target word is used meaningfully
                return !string.IsNullOrWhiteSpace(answer) && answer.Length > 10;
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
            var feedback = new List<string>
            {
                $"Excellent! You really understand how to use '{word}' correctly.",
                $"Perfect! Your understanding of '{word}' is spot on.",
                $"Great job! You've mastered the word '{word}'.",
                $"Wonderful! '{word}' is now part of your vocabulary.",
                $"Outstanding! You used '{word}' like a native speaker would."            };

            var random = new Random();            return feedback[random.Next(feedback.Count)];
        }        // Helper method to scroll chat to bottom
        private async Task ScrollChatToBottom()
        {
            try
            {
                // Small delay to ensure DOM has been updated
                await Task.Delay(50);
                await JSRuntime.InvokeVoidAsync("scrollToBottom", chatHistoryContainer);
            }
            catch (Exception ex)
            {
                // Silently handle JS interop errors - not critical functionality
                Console.WriteLine($"Chat scroll error: {ex.Message}");
            }
        }
    }

    // Enums and Data Models
    public enum GameMode
    {
        StoryAdventure,
        ConversationPractice,
        ContextualLearning,
        PersonalizedQuiz
    }

    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public enum ChallengeType
    {
        Definition,
        Synonym,
        Usage,
        Comprehension,
        Context,
        RealWorld
    }

    public class AILearningSession
    {
        public GameMode GameMode { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public DateTime StartTime { get; set; }
        public List<string> WordsLearned { get; set; } = new();
        public PlayerPreferences PlayerPreferences { get; set; } = new();
    }

    public class PlayerPreferences
    {
        public bool PrefersVisualLearning { get; set; }
        public bool PrefersAudioFeedback { get; set; } = true;
        public List<string> InterestTopics { get; set; } = new();
    }    public class WordChallenge
    {
        public ChallengeType Type { get; set; }
        public string TargetWord { get; set; } = "";        public string Question { get; set; } = "";
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }        public bool IsOpenEnded { get; set; }
        public string? Context { get; set; }
    }
}
