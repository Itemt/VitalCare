document.addEventListener('DOMContentLoaded', () => {
    // Buttons
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeToggleBtnMobile = document.getElementById('themeToggleBtnMobile');
    const themeToggleBtnGuest = document.getElementById('themeToggleBtnGuest');

    // Icons for desktop button (logged users)
    const themeIconSun = document.getElementById('themeIconSun');
    const themeIconMoon = document.getElementById('themeIconMoon');

    // Icons for mobile button
    const themeIconSunMobile = document.getElementById('themeIconSunMobile');
    const themeIconMoonMobile = document.getElementById('themeIconMoonMobile');
    
    // Icons for guest button
    const themeIconSunGuest = document.getElementById('themeIconSunGuest');
    const themeIconMoonGuest = document.getElementById('themeIconMoonGuest');

    const updateIcons = (theme) => {
        if (theme === 'dark') {
            // Desktop button icons (logged users)
            if (themeIconSun) themeIconSun.style.display = 'none';
            if (themeIconMoon) themeIconMoon.style.display = 'inline-block';
            // Mobile button icons
            if (themeIconSunMobile) themeIconSunMobile.style.display = 'none';
            if (themeIconMoonMobile) themeIconMoonMobile.style.display = 'inline-block';
            // Guest button icons
            if (themeIconSunGuest) themeIconSunGuest.style.display = 'none';
            if (themeIconMoonGuest) themeIconMoonGuest.style.display = 'inline-block';
        } else {
            // Desktop button icons (logged users)
            if (themeIconSun) themeIconSun.style.display = 'inline-block';
            if (themeIconMoon) themeIconMoon.style.display = 'none';
            // Mobile button icons
            if (themeIconSunMobile) themeIconSunMobile.style.display = 'inline-block';
            if (themeIconMoonMobile) themeIconMoonMobile.style.display = 'none';
            // Guest button icons
            if (themeIconSunGuest) themeIconSunGuest.style.display = 'inline-block';
            if (themeIconMoonGuest) themeIconMoonGuest.style.display = 'none';
        }
    };

    // Get current theme (already applied in head) and update icons
    const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
    updateIcons(currentTheme);

    const handleToggleClick = () => {
        const currentTheme = document.documentElement.getAttribute('data-bs-theme');
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        
        // Apply theme with smooth transition
        document.documentElement.setAttribute('data-bs-theme', newTheme);
        localStorage.setItem('theme', newTheme);
        updateIcons(newTheme);
    };

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', handleToggleClick);
    }

    if (themeToggleBtnMobile) {
        themeToggleBtnMobile.addEventListener('click', handleToggleClick);
    }
    
    if (themeToggleBtnGuest) {
        themeToggleBtnGuest.addEventListener('click', handleToggleClick);
    }

    // Optional: Add a listener for system theme changes if you want to be more reactive
    // window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
    //     const newColorScheme = event.matches ? "dark" : "light";
    //     if (!localStorage.getItem('theme')) { // Only if user hasn't set a preference
    //         applyTheme(newColorScheme);
    //     }
    // });
}); 