// wwwroot/js/site.flatpickr.js
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Flatpickr on elements with the class 'datetimepicker'
    flatpickr(".datetimepicker", {
        enableTime: true,       // Enable time selection
        dateFormat: "Y-m-d h:i K", // Format for 24hr backend, 12hr frontend (K for AM/PM)
        altInput: true,         // Show a user-friendly format, but submit the standard one
        altFormat: "d/m/Y h:i K", // User-friendly display format (dd/mm/YYYY h:i AM/PM)
        time_24hr: false,       // Use 12-hour time format with AM/PM
        minuteIncrement: 15,    // Optional: Set minute increments (e.g., every 15 minutes)
        locale: "es",         // Spanish locale
        minDate: "today"      // Prevent selection of past dates
    });

    // Initialize Flatpickr for Date of Birth fields (Registration and Profile Management)
    flatpickr(".datepicker-register", {
        dateFormat: "Y-m-d",     // Format for backend (YYYY-MM-DD)
        altInput: true,          // Show a user-friendly format
        altFormat: "d/m/Y",      // User-friendly display format (DD/MM/YYYY)
        locale: "es",            // Spanish locale
        maxDate: new Date(new Date().setFullYear(new Date().getFullYear() - 18)), // Max 18 years ago
        yearRange: [1920, new Date().getFullYear() - 18], // Birth years from 1920 to 18 years ago
        defaultDate: new Date(new Date().setFullYear(new Date().getFullYear() - 25)) // Default to 25 years ago
    });

    // Initialize Flatpickr for Date of Birth in Profile Management
    flatpickr(".datepicker-manage", {
        dateFormat: "Y-m-d",     // Format for backend (YYYY-MM-DD)
        altInput: true,          // Show a user-friendly format
        altFormat: "d/m/Y",      // User-friendly display format (DD/MM/YYYY)
        locale: "es",            // Spanish locale
        maxDate: new Date(new Date().setFullYear(new Date().getFullYear() - 18)), // Max 18 years ago
        yearRange: [1920, new Date().getFullYear() - 18] // Birth years from 1920 to 18 years ago
    });
}); 