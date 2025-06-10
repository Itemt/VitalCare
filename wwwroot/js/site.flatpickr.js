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

    // Initialize Flatpickr for Date of Birth on Manage Profile page
    flatpickr(".datepicker-manage", {
        enableTime: false,
        dateFormat: "Y-m-d", // Format for display and parsing
        altInput: true,       // Show a user-friendly format, but submit the standard one
        altFormat: "d/m/Y",   // User-friendly display format
        locale: "es",         // Spanish locale
        maxDate: "today"      // Don't allow future dates
    });

    // Initialize Flatpickr for Date of Birth on Registration page
    flatpickr(".datepicker-register", {
        enableTime: false,
        dateFormat: "Y-m-d", // Format for display and parsing
        altInput: true,       // Show a user-friendly format, but submit the standard one
        altFormat: "d/m/Y",   // User-friendly display format
        locale: "es",         // Spanish locale
        maxDate: "today"      // Don't allow future dates
    });
}); 