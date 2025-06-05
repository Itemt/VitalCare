// Real-time validation for registration form
(function() {
    'use strict';
    
    document.addEventListener('DOMContentLoaded', function() {
        const documentIdInput = document.getElementById('documentIdInput');
        const emailInput = document.getElementById('emailInput');
        const phoneInput = document.getElementById('phoneInput');
        
        // Validation functions (no real-time validation, only on submit)
        function validateDocumentId(value) {
            return /^[0-9]+$/.test(value);
        }
        
        function validatePhoneNumber(value) {
            return /^[0-9]+$/.test(value);
        }
        
        function validateEmailFormat(email) {
            const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
            return emailPattern.test(email);
        }
        
        // Function to show field errors for required fields
        function showFieldError(input, message) {
            // Look for existing validation span
            let validationSpan = input.parentElement.querySelector('.auth-validation-error');
            if (validationSpan) {
                validationSpan.textContent = message;
                validationSpan.style.display = 'flex';
                validationSpan.style.color = '#dc3545';
                validationSpan.style.fontSize = '0.875rem';
                validationSpan.style.marginTop = '0.25rem';
            }
        }
        
        // Form submission validation
        const registerForm = document.getElementById('registerForm');
        if (registerForm) {
            registerForm.addEventListener('submit', function(e) {
                let hasErrors = false;
                
                // Hide all previous alerts
                document.querySelectorAll('.alert').forEach(alert => {
                    alert.classList.add('d-none');
                    alert.classList.remove('d-block');
                });
                
                // Check required fields first
                const requiredFields = [
                    { input: document.getElementById('Input_FirstName'), name: 'nombre' },
                    { input: document.getElementById('Input_LastName'), name: 'apellido' },
                    { input: documentIdInput, name: 'documento de identidad' },
                    { input: document.getElementById('Input_Gender'), name: 'género' },
                    { input: emailInput, name: 'correo electrónico' },
                    { input: document.getElementById('registerPasswordInput'), name: 'contraseña' },
                    { input: document.getElementById('confirmPasswordInput'), name: 'confirmación de contraseña' },
                    { input: phoneInput, name: 'teléfono' },
                    { input: document.getElementById('Input_DateOfBirth'), name: 'fecha de nacimiento' }
                ];
                
                // Check for empty required fields
                requiredFields.forEach(field => {
                    if (field.input && field.input.value.trim() === '') {
                        // Create and show a temporary alert for empty fields
                        showFieldError(field.input, `El campo ${field.name} es obligatorio.`);
                        hasErrors = true;
                    }
                });
                
                // Check document ID format (only if not empty)
                if (documentIdInput) {
                    const value = documentIdInput.value.trim();
                    if (value.length > 0 && !validateDocumentId(value)) {
                        const documentIdAlert = document.getElementById('documentIdAlert');
                        documentIdAlert.classList.remove('d-none');
                        documentIdAlert.classList.add('d-block');
                        hasErrors = true;
                    }
                }
                
                // Check phone number format (only if not empty)
                if (phoneInput) {
                    const value = phoneInput.value.trim();
                    if (value.length > 0 && !validatePhoneNumber(value)) {
                        const phoneAlert = document.getElementById('phoneAlert');
                        phoneAlert.classList.remove('d-none');
                        phoneAlert.classList.add('d-block');
                        hasErrors = true;
                    }
                }
                
                // Check email format (only if not empty)
                if (emailInput) {
                    const value = emailInput.value.trim();
                    if (value.length > 0 && !validateEmailFormat(value)) {
                        const emailAlert = document.getElementById('emailAlert');
                        emailAlert.classList.remove('d-none');
                        emailAlert.classList.add('d-block');
                        hasErrors = true;
                    }
                }
                
                // Check password requirements (only if not empty)
                const passwordInput = document.getElementById('registerPasswordInput');
                const confirmPasswordInput = document.getElementById('confirmPasswordInput');
                
                if (passwordInput && passwordInput.value.trim().length > 0) {
                    if (passwordInput.value.length < 4) {
                        showFieldError(passwordInput, 'La contraseña debe tener al menos 4 caracteres.');
                        hasErrors = true;
                    }
                }
                
                // Check password confirmation (only if both passwords have values)
                if (passwordInput && confirmPasswordInput && 
                    passwordInput.value.trim().length > 0 && 
                    confirmPasswordInput.value.trim().length > 0) {
                    if (passwordInput.value !== confirmPasswordInput.value) {
                        showFieldError(confirmPasswordInput, 'Las contraseñas no coinciden.');
                        hasErrors = true;
                    }
                }
                
                // If there are errors, prevent submission and reset button
                if (hasErrors) {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    // Reset submit button if it was loading
                    const submitBtn = document.getElementById('registerSubmit');
                    if (submitBtn) {
                        submitBtn.classList.remove('auth-btn-loading');
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = '<i class="bi bi-person-plus me-2"></i>Registrarse';
                    }
                    
                    // Scroll to first error
                    const firstAlert = document.querySelector('.alert:not(.d-none)');
                    if (firstAlert) {
                        firstAlert.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
                    
                    return false;
                }
            });
        }
    });
})(); 