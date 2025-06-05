// Password toggle functionality - Standalone file
(function() {
    'use strict';
    
    console.log('Password toggle script loaded');
    
    function setupPasswordToggle(toggleId, inputId) {
        console.log(`Setting up toggle for: ${toggleId} -> ${inputId}`);
        
        const toggleButton = document.getElementById(toggleId);
        const passwordInput = document.getElementById(inputId);
        
        if (!toggleButton) {
            console.error(`Toggle button not found: ${toggleId}`);
            return false;
        }
        
        if (!passwordInput) {
            console.error(`Password input not found: ${inputId}`);
            return false;
        }
        
        console.log(`Successfully found elements for ${toggleId}`);
        
        // Remove any existing event listeners
        const newToggleButton = toggleButton.cloneNode(true);
        toggleButton.parentNode.replaceChild(newToggleButton, toggleButton);
        
        // Add event listeners to the new button
        newToggleButton.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log(`Toggle clicked for ${inputId}`);
            
            const icon = this.querySelector('i');
            if (!icon) {
                console.error('Icon not found in toggle button');
                return;
            }
            
            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                icon.className = 'bi bi-eye';
                console.log(`Password shown for ${inputId}`);
            } else {
                passwordInput.type = 'password';
                icon.className = 'bi bi-eye-slash';
                console.log(`Password hidden for ${inputId}`);
            }
        });
        
        // Prevent form submission on button click
        newToggleButton.addEventListener('mousedown', function(e) {
            e.preventDefault();
        });
        
        // Add touch support for mobile
        newToggleButton.addEventListener('touchstart', function(e) {
            e.preventDefault();
            this.click();
        });
        
        console.log(`Password toggle setup completed for ${toggleId}`);
        return true;
    }
    
    function initializePasswordToggles() {
        console.log('Initializing password toggles...');
        
        // Wait a bit to ensure DOM is fully ready
        setTimeout(function() {
            // Setup toggles for login page
            setupPasswordToggle('toggleLoginPassword', 'loginPasswordInput');
            
            // Setup toggles for register page
            setupPasswordToggle('toggleRegisterPassword', 'registerPasswordInput');
            setupPasswordToggle('toggleConfirmPassword', 'confirmPasswordInput');
            
            console.log('Password toggles initialization completed');
        }, 200);
    }
    
    // Multiple initialization strategies
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePasswordToggles);
    } else {
        initializePasswordToggles();
    }
    
    // Backup initialization after a delay
    setTimeout(initializePasswordToggles, 500);
    
    // Also initialize on window load as final backup
    window.addEventListener('load', initializePasswordToggles);
    
})(); 