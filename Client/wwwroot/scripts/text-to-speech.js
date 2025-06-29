// Text-to-Speech functionality for AI Word Tutor
window.speechSynthesisUtterance = null;
window.speechSynthesis = window.speechSynthesis || null;

// Check if speech synthesis is supported
window.checkSpeechSupport = () => {
    return 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
};

// Speak the given text
window.speakText = (text) => {
    if (!window.checkSpeechSupport()) {
        console.warn('Speech synthesis not supported');
        return;
    }

    // Stop any ongoing speech
    window.stopSpeech();

    try {
        const utterance = new SpeechSynthesisUtterance(text);
        
        // Configure speech settings
        utterance.rate = 0.9; // Slightly slower for better comprehension
        utterance.pitch = 1.0;
        utterance.volume = 1.0;
        
        // Try to use a natural-sounding voice if available
        const voices = speechSynthesis.getVoices();
        const preferredVoices = voices.filter(voice => 
            voice.lang.startsWith('en') && 
            (voice.name.includes('Natural') || voice.name.includes('Enhanced') || voice.name.includes('Premium'))
        );
        
        if (preferredVoices.length > 0) {
            utterance.voice = preferredVoices[0];
        } else {
            // Fallback to any English voice
            const englishVoices = voices.filter(voice => voice.lang.startsWith('en'));
            if (englishVoices.length > 0) {
                utterance.voice = englishVoices[0];
            }
        }

        // Event handlers
        utterance.onend = () => {
            console.log('Speech finished');
            // Notify Blazor component that speech has ended
            if (window.DotNet && window.dotNetHelper) {
                window.dotNetHelper.invokeMethodAsync('OnSpeechEnd');
            }
        };

        utterance.onerror = (event) => {
            console.error('Speech synthesis error:', event.error);
            if (window.DotNet && window.dotNetHelper) {
                window.dotNetHelper.invokeMethodAsync('OnSpeechEnd');
            }
        };

        utterance.onstart = () => {
            console.log('Speech started');
        };

        // Store reference and start speaking
        window.speechSynthesisUtterance = utterance;
        speechSynthesis.speak(utterance);
        
    } catch (error) {
        console.error('Error creating speech utterance:', error);
    }
};

// Stop current speech
window.stopSpeech = () => {
    if (window.checkSpeechSupport() && speechSynthesis.speaking) {
        speechSynthesis.cancel();
    }
    window.speechSynthesisUtterance = null;
};

// Get available voices (useful for debugging)
window.getAvailableVoices = () => {
    if (!window.checkSpeechSupport()) {
        return [];
    }
    return speechSynthesis.getVoices().map(voice => ({
        name: voice.name,
        lang: voice.lang,
        isDefault: voice.default
    }));
};

// Initialize voices when they become available
if (window.checkSpeechSupport()) {
    speechSynthesis.onvoiceschanged = () => {
        console.log('Voices loaded:', speechSynthesis.getVoices().length);
    };
}

// Keyboard shortcut support for Microsoft Edge Immersive Reader
window.triggerImmersiveReader = () => {
    // This attempts to trigger Edge's Immersive Reader using F9 key
    // Note: This might not work due to browser security restrictions
    const event = new KeyboardEvent('keydown', {
        key: 'F9',
        code: 'F9',
        keyCode: 120,
        which: 120,
        bubbles: true,
        cancelable: true
    });
    document.dispatchEvent(event);
};

console.log('Text-to-speech module loaded successfully');
