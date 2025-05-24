document.addEventListener('DOMContentLoaded', () => {
    // Buttons
    const themeToggleBtnXs = document.getElementById('themeToggleBtnXs');
    const themeToggleBtnSmUp = document.getElementById('themeToggleBtnSmUp');

    // Icons for XS button
    const themeIconSunXs = document.getElementById('themeIconSunXs');
    const themeIconMoonXs = document.getElementById('themeIconMoonXs');

    // Icons for SM+ button
    const themeIconSunSmUp = document.getElementById('themeIconSunSmUp');
    const themeIconMoonSmUp = document.getElementById('themeIconMoonSmUp');

    const currentTheme = localStorage.getItem('theme') || 'light';

    const applyTheme = (theme) => {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (theme === 'dark') {
            // XS button icons
            if (themeIconSunXs) themeIconSunXs.style.display = 'none';
            if (themeIconMoonXs) themeIconMoonXs.style.display = 'inline-block';
            // SM+ button icons
            if (themeIconSunSmUp) themeIconSunSmUp.style.display = 'none';
            if (themeIconMoonSmUp) themeIconMoonSmUp.style.display = 'inline-block';
        } else {
            // XS button icons
            if (themeIconSunXs) themeIconSunXs.style.display = 'inline-block';
            if (themeIconMoonXs) themeIconMoonXs.style.display = 'none';
            // SM+ button icons
            if (themeIconSunSmUp) themeIconSunSmUp.style.display = 'inline-block';
            if (themeIconMoonSmUp) themeIconMoonSmUp.style.display = 'none';
        }
    };

    // Apply initial theme
    applyTheme(currentTheme);

    const handleToggleClick = () => {
        let newTheme = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        localStorage.setItem('theme', newTheme);
        applyTheme(newTheme);
    };

    if (themeToggleBtnXs) {
        themeToggleBtnXs.addEventListener('click', handleToggleClick);
    }

    if (themeToggleBtnSmUp) {
        themeToggleBtnSmUp.addEventListener('click', handleToggleClick);
    }

    // Optional: Add a listener for system theme changes if you want to be more reactive
    // window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
    //     const newColorScheme = event.matches ? "dark" : "light";
    //     if (!localStorage.getItem('theme')) { // Only if user hasn't set a preference
    //         applyTheme(newColorScheme);
    //     }
    // });
}); 