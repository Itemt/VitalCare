document.addEventListener('DOMContentLoaded', () => {
    // Buttons
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeToggleBtnMobile = document.getElementById('themeToggleBtnMobile');

    // Icons for desktop button
    const themeIconSun = document.getElementById('themeIconSun');
    const themeIconMoon = document.getElementById('themeIconMoon');

    // Icons for mobile button
    const themeIconSunMobile = document.getElementById('themeIconSunMobile');
    const themeIconMoonMobile = document.getElementById('themeIconMoonMobile');

    const currentTheme = localStorage.getItem('theme') || 'light';

    const applyTheme = (theme) => {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (theme === 'dark') {
            // Desktop button icons
            if (themeIconSun) themeIconSun.style.display = 'none';
            if (themeIconMoon) themeIconMoon.style.display = 'inline-block';
            // Mobile button icons
            if (themeIconSunMobile) themeIconSunMobile.style.display = 'none';
            if (themeIconMoonMobile) themeIconMoonMobile.style.display = 'inline-block';
        } else {
            // Desktop button icons
            if (themeIconSun) themeIconSun.style.display = 'inline-block';
            if (themeIconMoon) themeIconMoon.style.display = 'none';
            // Mobile button icons
            if (themeIconSunMobile) themeIconSunMobile.style.display = 'inline-block';
            if (themeIconMoonMobile) themeIconMoonMobile.style.display = 'none';
        }
    };

    // Apply initial theme
    applyTheme(currentTheme);

    const handleToggleClick = () => {
        let newTheme = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        localStorage.setItem('theme', newTheme);
        applyTheme(newTheme);
    };

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', handleToggleClick);
    }

    if (themeToggleBtnMobile) {
        themeToggleBtnMobile.addEventListener('click', handleToggleClick);
    }

    // Optional: Add a listener for system theme changes if you want to be more reactive
    // window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
    //     const newColorScheme = event.matches ? "dark" : "light";
    //     if (!localStorage.getItem('theme')) { // Only if user hasn't set a preference
    //         applyTheme(newColorScheme);
    //     }
    // });
}); 