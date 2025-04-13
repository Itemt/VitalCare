// wwwroot/js/site.flatpickr.js
document.addEventListener('DOMContentLoaded', function () {
    // Initialize Flatpickr on elements with the class 'datetimepicker'
    flatpickr(".datetimepicker", {
        enableTime: true,       // Enable time selection
        dateFormat: "Y-m-d H:i", // Format matching datetime-local and C# DateTime (adjust if needed)
        time_24hr: true,        // Use 24-hour time format
        minuteIncrement: 15,    // Optional: Set minute increments (e.g., every 15 minutes)
        // You can add more options like minDate, maxDate, disable specific dates/times, etc.
        // Example: Disable weekends
        // disable: [
        //     function(date) {
        //         // return true to disable
        //         return (date.getDay() === 0 || date.getDay() === 6);
        //     }
        // ],
        // Example: Set locale (requires importing locale file)
        // locale: "es" // requires locale script: https://cdn.jsdelivr.net/npm/flatpickr/dist/l10n/es.js
    });
}); 