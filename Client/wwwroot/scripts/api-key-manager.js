/**
 * API Key Manager for Blazor Words Game Application
 * Provides secure localStorage-based API key management with prevention of Edge password manager conflicts
 */
window.apiKeyManager = {
    /**
     * Save an API key for a specific service
     * @param {string} service - The service name (e.g., 'openai', 'wordsapi')
     * @param {string} key - The API key to save
     */
    saveApiKey: function(service, key) {
        if (!service || !key) {
            console.warn('ApiKeyManager: Service and key are required');
            return false;
        }
        
        try {
            const storageKey = `${service}_api_key`;
            localStorage.setItem(storageKey, key);
            console.log(`ApiKeyManager: Successfully saved API key for ${service}`);
            return true;
        } catch (error) {
            console.error('ApiKeyManager: Failed to save API key:', error);
            return false;
        }
    },
    
    /**
     * Retrieve an API key for a specific service
     * @param {string} service - The service name
     * @returns {string|null} The API key or null if not found
     */
    getApiKey: function(service) {
        if (!service) {
            console.warn('ApiKeyManager: Service is required');
            return null;
        }
        
        try {
            const storageKey = `${service}_api_key`;
            return localStorage.getItem(storageKey);
        } catch (error) {
            console.error('ApiKeyManager: Failed to retrieve API key:', error);
            return null;
        }
    },
    
    /**
     * Clear an API key for a specific service
     * @param {string} service - The service name
     */
    clearApiKey: function(service) {
        if (!service) {
            console.warn('ApiKeyManager: Service is required');
            return false;
        }
        
        try {
            const storageKey = `${service}_api_key`;
            localStorage.removeItem(storageKey);
            console.log(`ApiKeyManager: Successfully cleared API key for ${service}`);
            return true;
        } catch (error) {
            console.error('ApiKeyManager: Failed to clear API key:', error);
            return false;
        }
    },
    
    /**
     * Get all stored API keys with service names
     * @returns {Array} Array of objects with service and hasKey properties
     */
    getAllApiKeys: function() {
        const apiKeys = [];
        
        try {
            for (let i = 0; i < localStorage.length; i++) {
                const key = localStorage.key(i);
                if (key && key.endsWith('_api_key')) {
                    const service = key.replace('_api_key', '');
                    apiKeys.push({
                        service: service,
                        hasKey: true,
                        keyPreview: localStorage.getItem(key)?.substring(0, 10) + '...'
                    });
                }
            }
        } catch (error) {
            console.error('ApiKeyManager: Failed to get all API keys:', error);
        }
        
        return apiKeys;
    },
    
    /**
     * Validate an API key format for a specific service
     * @param {string} service - The service name
     * @param {string} key - The API key to validate
     * @returns {boolean} True if the key format is valid
     */
    validateApiKey: function(service, key) {
        if (!service || !key) {
            return false;
        }
        
        switch (service.toLowerCase()) {
            case 'openai':
                return key.startsWith('sk-') && key.length > 20;
            case 'wordsapi':
            case 'rapidapi':
                return key.length > 10; // Basic validation for RapidAPI keys
            default:
                return key.length > 5; // Minimum length for any API key
        }
    }
};

// Utility function to prevent Edge password manager interference
window.preventPasswordManagerInterference = function() {
    const apiKeyInputs = document.querySelectorAll('input[data-service]');
    
    apiKeyInputs.forEach(input => {
        // Add event listeners to prevent password manager from interfering
        input.addEventListener('focus', function() {
            this.setAttribute('autocomplete', 'off');
        });
        
        input.addEventListener('input', function() {
            // Clear any browser-suggested values
            if (this.value && this.value.length < 5) {
                setTimeout(() => {
                    if (this.value.length < 5) {
                        this.value = '';
                    }
                }, 100);
            }
        });
    });
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.preventPasswordManagerInterference();
});
