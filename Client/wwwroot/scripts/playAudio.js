window.playAudioElement = (audioElement) => {
    if (audioElement) {
        audioElement.currentTime = 0;
        audioElement.play();
    }
};
