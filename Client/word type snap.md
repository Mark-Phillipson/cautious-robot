
I Want to add another mini-game to the AI word tutor called "Word Type Snap" in this project. 

The rules are:

- Two words are displayed on the screen at the same time.
- The user clicks a "Snap!" button to collect them **only if both words are of the same word type** (e.g. both nouns or both verbs).
- If the words are the same type, the user gains points. If they are not, they lose points or their streak resets.
- The game should then display two new random words from a predefined list after each click.

can we make this a separate component even though it will show in the AI word tutor  as the file is getting crowded

Please draft:

1. A simple C# class to represent a Word, including:
    - The word text
    - The word type (noun, verb, adjective, adverb, etc.)

2. A Razor component that:
    - Displays two Word objects with their text
    - Has a "Snap!" button
    - Shows the current score
    - Handles click events to check if the two words are the same type
    - Updates the score accordingly
    - Loads two new random words after each snap attempt

Use the same styling  as AI word tutor games and keep logic clear for later UI refinement. This is for an educational AI-based word learning game.

it should be fun to play and  words should be beautiful and distinct to look at.
