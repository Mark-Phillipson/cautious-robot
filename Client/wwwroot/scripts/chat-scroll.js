// Chat scroll functionality for AI Word Tutor
window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

// Alternative method using smooth scrolling
window.smoothScrollToBottom = (element) => {
    if (element) {
        element.scrollTo({
            top: element.scrollHeight,
            behavior: 'smooth'
        });
    }
};

// Focus chat input functionality
window.focusChatInput = () => {
    const chatInput = document.querySelector('.chat-input');
    if (chatInput) {
        chatInput.focus();
    }
};

// Scroll to any element smoothly
window.scrollToElement = (element) => {
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start',
            inline: 'nearest'
        });
    }
};
