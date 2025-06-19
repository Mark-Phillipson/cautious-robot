using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text;
using BlazorApp.Client.Shared;

namespace BlazorApp.Client.Pages
{
    public partial class AIWordTutor : ComponentBase
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
        private AILearningSession? currentSession;        private string? currentContent = "";
        private List<string> conversationHistory = new();
        private string userInput = "";
        private List<WordChallenge> currentChallenges = new();
        private int currentChallengeIndex = 0;
        private bool PlayAudio = false;
        private bool hasApiKey = false;
        
        // Feedback system
        private bool showFeedback = false;
        private string feedbackMessage = "";
        private bool lastAnswerCorrect = false;
        private string correctAnswer = "";

        // Conversation practice scoring
        private List<string> conversationTargetWords = new();
        private HashSet<string> usedTargetWords = new();
        private int wordsUsedCorrectly = 0;

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
            currentGameMode = mode;
            gameStarted = true;
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

            StateHasChanged();

            try
            {
                currentSession = new AILearningSession
                {
                    Mode = mode,
                    Difficulty = difficulty,
                    StartTime = DateTime.Now
                };

                // Select words based on difficulty level
                var wordsToUse = GetRandomWords(5);

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

        private List<string> GetRandomWords(int count)
        {
            var availableWords = wordLibrary[difficulty];
            var random = new Random();
            return availableWords.OrderBy(x => random.Next()).Take(count).ToList();
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
            
            // Parse the response to extract story and questions
            var sections = aiResponse.Split(new[] { "QUESTIONS:" }, StringSplitOptions.RemoveEmptyEntries);
            
            if (sections.Length >= 2)
            {
                currentContent = sections[0].Replace("STORY:", "").Trim();
                
                // Parse questions and create challenges
                var questionLines = sections[1].Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in questionLines.Where(l => l.Trim().Length > 0))
                {
                    var cleanLine = line.Trim();
                    if (cleanLine.StartsWith("1.") || cleanLine.StartsWith("2.") || 
                        cleanLine.StartsWith("3.") || cleanLine.StartsWith("4.") || 
                        cleanLine.StartsWith("5."))
                    {
                        // Extract the word this question is about
                        var word = ExtractWordFromQuestion(cleanLine, words);
                        if (!string.IsNullOrEmpty(word))
                        {
                            currentChallenges.Add(new WordChallenge
                            {
                                Type = ChallengeType.Context,
                                TargetWord = word,
                                Question = cleanLine.Substring(2).Trim(),
                                IsOpenEnded = true,
                                Context = currentContent
                            });
                        }
                    }
                }
            }
            else
            {
                currentContent = aiResponse;
                // Fallback: create simple challenges
                 await GenerateSimpleChallenges(words);
            }
        }

        private string ExtractWordFromQuestion(string question, List<string> words)
        {
            var lowerQuestion = question.ToLower();
            return words.FirstOrDefault(word => lowerQuestion.Contains(word.ToLower())) ?? "";
        }

        private Task GenerateSimpleChallenges(List<string> words)
        {
            foreach (var word in words)
            {
                var random = new Random();
                var challengeType = random.Next(3) switch
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
                    Options = challengeType == ChallengeType.Comprehension ? GenerateMultipleChoiceOptions(word) : new List<string>(),
                    CorrectAnswer = challengeType == ChallengeType.Comprehension ? GetSimpleDefinition(word) : ""
                });
            }

            return Task.CompletedTask;
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
                var questionTypes = new[] { "definition", "synonym", "usage", "context" };
                var random = new Random();
                var questionType = questionTypes[random.Next(questionTypes.Length)];

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
                    Type = GetChallengeTypeFromQuestion(question),
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
        }        private string GetSimpleDefinition(string word)
        {
            var definitions = new Dictionary<string, string>
            {
                // Beginner words
                {"adventure", "An exciting and unusual experience or activity"},
                {"beautiful", "Pleasing to look at; attractive"},
                {"celebrate", "To acknowledge a significant or happy day or event"},
                {"discover", "To find something for the first time"},
                {"enormous", "Very large in size or quantity"},
                {"friendship", "A close relationship between friends"},
                {"grateful", "Feeling thankful for something"},
                {"harmony", "A pleasant combination of different things"},
                {"important", "Having great significance or value"},
                {"journey", "A trip from one place to another"},
                {"kindness", "The quality of being friendly and caring"},
                {"laughter", "The sound made when someone finds something funny"},
                {"mystery", "Something that is difficult to understand"},
                {"nature", "The physical world including plants and animals"},
                {"opportunity", "A chance to do something"},
                {"peaceful", "Calm and quiet; without conflict"},
                {"question", "A sentence that asks for information"},
                {"respect", "Admiration for someone or something"},
                {"sunshine", "Direct light from the sun"},
                {"treasure", "Something very valuable"},
                {"umbrella", "A device used for protection from rain"},
                {"victory", "Success in a struggle or contest"},
                {"wonderful", "Extremely good or pleasant"},
                {"explore", "To travel through an area to learn about it"},
                {"youthful", "Having the characteristics of a young person"},
                
                // Intermediate words
                {"ambitious", "Having a strong desire for success or achievement"},
                {"beneficial", "Having a good or helpful result"},
                {"comprehensive", "Complete and including everything"},
                {"demonstrate", "To show clearly by giving proof or evidence"},
                {"elaborate", "Involving many carefully arranged parts; detailed"},
                {"fundamental", "Forming a necessary base or core"},
                {"genuine", "Truly what it is said to be; authentic"},
                {"hypothesis", "A suggested explanation for something"},
                {"inevitable", "Certain to happen; unavoidable"},
                {"jurisdiction", "The official power to make legal decisions"},
                {"magnificent", "Extremely beautiful, elaborate, or impressive"},
                {"negligent", "Failing to take proper care"},
                {"optimistic", "Hopeful and confident about the future"},
                {"persistent", "Continuing firmly despite difficulties"},
                {"reluctant", "Unwilling or hesitant"},
                {"sophisticated", "Having a refined knowledge of culture and fashion"},
                {"temporary", "Lasting for only a limited period"},
                {"unprecedented", "Never done or known before"},
                {"versatile", "Able to adapt to many different functions"},
                {"wisdom", "The quality of having experience, knowledge, and good judgment"},
                {"analyze", "To examine something in detail"},
                {"bureaucracy", "A system of government with many departments"},
                {"catastrophe", "A sudden event causing great damage"},
                {"diligent", "Having or showing care in one's work"},
                {"empathy", "The ability to understand others' feelings"},
                
                // Advanced words
                {"ubiquitous", "Present, appearing, or found everywhere"},
                {"perspicacious", "Having a ready insight into things"},
                {"serendipitous", "Occurring by happy chance"},
                {"magnanimous", "Very generous or forgiving"},
                {"eloquent", "Fluent and persuasive in speaking"},
                {"ephemeral", "Lasting for a very short time"},
                {"indigenous", "Originating naturally in a particular place"},
                {"meticulous", "Showing great attention to detail"},
                {"ostentatious", "Characterized by showy display"},
                {"pragmatic", "Dealing with things in a practical way"},
                {"quintessential", "Representing the most perfect example"},
                {"resilient", "Able to recover quickly from difficulties"},
                {"scrupulous", "Diligent, thorough, and extremely attentive to details"},
                {"tenacious", "Holding fast; persistent"},
                {"vicarious", "Experienced through someone else"},
                {"whimsical", "Playfully quaint or fanciful"},
                {"xenophobic", "Having dislike of people from other countries"},
                {"zealous", "Having great energy or passion for something"},
                {"acquiesce", "To accept something reluctantly but without protest"},
                {"belligerent", "Hostile and aggressive"},
                {"cacophony", "A harsh, discordant mixture of sounds"},
                {"deleterious", "Causing harm or damage"},
                {"effervescent", "Vivacious and enthusiastic"},
                {"facetious", "Treating serious issues with inappropriate humor"},
                {"gregarious", "Fond of the company of others; sociable"}
            };

            return definitions.GetValueOrDefault(word, $"Definition for '{word}' (word not in dictionary)");
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
        }        private string GetDifficultyName(DifficultyLevel level)
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
            currentContent = "";
            conversationHistory.Clear();
            currentChallenges.Clear();
            currentChallengeIndex = 0;
            score = 0;
            streak = 0;
            showFeedback = false;
            feedbackMessage = "";
            lastAnswerCorrect = false;
            userInput = "";
            conversationTargetWords.Clear();
            usedTargetWords.Clear();
            wordsUsedCorrectly = 0;
            StateHasChanged();
            return Task.CompletedTask;
        }private async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(userInput))
            {
                await SendMessage();
            }
        }private async Task HandleKeyPressForTextarea(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && e.CtrlKey && !string.IsNullOrWhiteSpace(userInput))
            {
                await ProcessAnswer(userInput);
            }
        }private async Task ContinueLearning()
        {
            showFeedback = false;
            feedbackMessage = "";
            
            currentChallengeIndex++;
            
            if (currentChallengeIndex >= currentChallenges.Count)
            {
                // Generate new challenges or end session
                await GeneratePersonalizedQuiz(new List<string> { wordLibrary[difficulty][new Random().Next(wordLibrary[difficulty].Count)] });
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
            
            // Check for vocabulary usage in conversation practice mode
            if (currentGameMode == GameMode.ConversationPractice)
            {
                await EvaluateVocabularyUsage(userInput);
            }
            
            // Generate AI response using OpenAI
            var aiResponse = await GenerateAIResponse(userInput);
            conversationHistory.Add(aiResponse);
            
            userInput = "";
            StateHasChanged();
            
            // Scroll to bottom after message is added
            await Task.Delay(100);
            await ScrollChatToBottom();
        }        private async Task<string> GenerateAIResponse(string userMessage)
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
                
                var random = new Random();
                return fallbackResponses[random.Next(fallbackResponses.Length)];
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
            }            else
            {
                streak = 0;
                
                // Provide better feedback based on challenge type
                if (challenge.Type == ChallengeType.Comprehension)
                {
                    feedbackMessage = $"Not quite right. '{challenge.TargetWord}' means: {correctAnswer}";
                }
                else if (challenge.IsOpenEnded)
                {
                    feedbackMessage = $"Good effort! For '{challenge.TargetWord}', try incorporating its meaning more clearly in your response.";
                }
                else
                {
                    feedbackMessage = $"Not quite right. The correct answer is: {correctAnswer}";
                }
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
            var feedbacks = new[]
            {
                $"Excellent! You really understand '{word}' well.",
                $"Perfect! Great job with '{word}'.",
                $"Outstanding! You've mastered '{word}'.",
                $"Wonderful! Your understanding of '{word}' is impressive.",
                $"Brilliant! You've got '{word}' down perfectly."
            };
            
            var random = new Random();
            return feedbacks[random.Next(feedbacks.Length)];
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
        }        private async Task EvaluateVocabularyUsage(string userMessage)
        {
            Console.WriteLine($"Evaluating message: {userMessage}");
            Console.WriteLine($"Target words: {string.Join(", ", conversationTargetWords)}");
            Console.WriteLine($"Already used: {string.Join(", ", usedTargetWords)}");
            
            foreach (var targetWord in conversationTargetWords)
            {
                // Check if word is present in the message and not already used
                var messageWords = userMessage.ToLower().Split(new char[] { ' ', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '-' }, StringSplitOptions.RemoveEmptyEntries);
                var targetWordLower = targetWord.ToLower();
                  // Check for exact word match or word with common suffixes/prefixes
                bool wordFound = messageWords.Any(word => 
                    word == targetWordLower || 
                    word == targetWordLower + "s" || 
                    word == targetWordLower + "ed" || 
                    word == targetWordLower + "ing" || 
                    word == targetWordLower + "ly" ||
                    word == targetWordLower + "d" ||  // for words like "analyze" -> "analyzed"
                    word == targetWordLower.TrimEnd('e') + "ing" || // "analyze" -> "analyzing"
                    word.StartsWith(targetWordLower) && (word.EndsWith("ing") || word.EndsWith("ed") || word.EndsWith("s") || word.EndsWith("ly")) ||
                    (targetWordLower.EndsWith("e") && word == targetWordLower.TrimEnd('e') + "ing") // handle -e words
                );
                
                if (wordFound && !usedTargetWords.Contains(targetWord))
                {
                    Console.WriteLine($"Found word: {targetWord}");
                    
                    // Use AI to verify correct usage
                    var prompt = $@"Does this sentence use the word '{targetWord}' correctly in context?

Sentence: ""{userMessage}""

Respond with: YES if the word is used correctly in meaning and context, NO if it's incorrect or just mentioned without proper usage.

Be strict about correct usage - the word should be used meaningfully, not just mentioned.";

                    var systemMessage = "You are an English language expert evaluating vocabulary usage.";
                      try
                    {                        var aiResponse = await OpenAIService.GenerateContentAsync(prompt, systemMessage);                        Console.WriteLine($"AI response for '{targetWord}': '{aiResponse}'");
                        Console.WriteLine($"Response length: {aiResponse.Length}");
                        Console.WriteLine($"Response lower: '{aiResponse.ToLower()}'");
                        
                        // Check if the response indicates missing API key (case-insensitive)
                        var responseLower = aiResponse.ToLower();
                        bool hasApiKeyMessage = responseLower.Contains("please set your openai api key") || 
                            responseLower.Contains("api key first") ||
                            responseLower.Contains("openai api key first") ||
                            responseLower.Contains("set your openai api key");
                        
                        Console.WriteLine($"Has API key message: {hasApiKeyMessage}");
                        
                        if (hasApiKeyMessage)
                        {
                            Console.WriteLine($"API key not available, using fallback logic for '{targetWord}'");
                            // Fallback: Accept the word if it appears meaningfully in context
                            if (userMessage.ToLower().Contains(targetWord.ToLower()) && userMessage.Length > targetWord.Length + 10)
                            {
                                usedTargetWords.Add(targetWord);
                                wordsUsedCorrectly++;
                                score += 5;
                                Console.WriteLine($"Word '{targetWord}' accepted via API key fallback logic");
                            }
                        }
                        else if (aiResponse.Trim().ToUpper().StartsWith("YES"))
                        {
                            usedTargetWords.Add(targetWord);
                            wordsUsedCorrectly++;
                            score += 5; // Bonus points for conversation vocabulary usage
                            Console.WriteLine($"Word '{targetWord}' marked as correctly used!");
                        }
                        else
                        {
                            Console.WriteLine($"AI determined '{targetWord}' was not used correctly");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error evaluating vocabulary usage for '{targetWord}': {ex.Message}");
                        // Fallback: Accept the word if it appears meaningfully in context
                        if (userMessage.ToLower().Contains(targetWord.ToLower()) && userMessage.Length > targetWord.Length + 10)
                        {
                            usedTargetWords.Add(targetWord);
                            wordsUsedCorrectly++;
                            score += 5;
                            Console.WriteLine($"Word '{targetWord}' accepted via fallback logic");
                        }
                    }
                }
            }
            
            Console.WriteLine($"Final state - Words used correctly: {wordsUsedCorrectly}/{conversationTargetWords.Count}");
            StateHasChanged();
        }
        
        private async Task TestWordEvaluation()
        {
            var testSentence = "I think analyzing that my sentence would be beneficial and not to do so would be negligent I will persist in creating this sentence otherwise it would be a catastrophe.";
            Console.WriteLine($"Testing word evaluation with: {testSentence}");
            await EvaluateVocabularyUsage(testSentence);
        }
    }
}
