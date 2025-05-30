document.addEventListener('DOMContentLoaded', function () {
    function setupPasswordToggle(inputId, toggleButtonId) {
        const passwordInput = document.getElementById(inputId);
        const togglePasswordButton = document.getElementById(toggleButtonId);

        if (passwordInput && togglePasswordButton) {
            togglePasswordButton.addEventListener('click', function () {
                // Toggle the type attribute
                const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
                passwordInput.setAttribute('type', type);

                // Toggle the icon
                const icon = this.querySelector('i');
                if (type === 'password') {
                    icon.classList.remove('bi-eye');
                    icon.classList.add('bi-eye-slash');
                } else {
                    icon.classList.remove('bi-eye-slash');
                    icon.classList.add('bi-eye');
                }
            });
        }
    }

    // Setup for Login page
    setupPasswordToggle('loginPasswordInput', 'toggleLoginPassword');

    // Setup for Register page
    setupPasswordToggle('registerPasswordInput', 'toggleRegisterPassword');
    setupPasswordToggle('confirmPasswordInput', 'toggleConfirmPassword');
}); 