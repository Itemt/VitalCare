document.addEventListener('DOMContentLoaded', () => {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIconSun = document.getElementById('themeIconSun');
    const themeIconMoon = document.getElementById('themeIconMoon');
    const currentTheme = localStorage.getItem('theme') || 'light';

    const applyTheme = (theme) => {
        document.documentElement.setAttribute('data-bs-theme', theme);
        if (theme === 'dark') {
            if (themeIconSun) themeIconSun.style.display = 'none';
            if (themeIconMoon) themeIconMoon.style.display = 'inline-block';
        } else {
            if (themeIconSun) themeIconSun.style.display = 'inline-block';
            if (themeIconMoon) themeIconMoon.style.display = 'none';
        }
    };

    // Apply initial theme
    applyTheme(currentTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', () => {
            let newTheme = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            localStorage.setItem('theme', newTheme);
            applyTheme(newTheme);
        });
    }

    // Optional: Add a listener for system theme changes if you want to be more reactive
    // window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', event => {
    //     const newColorScheme = event.matches ? "dark" : "light";
    //     if (!localStorage.getItem('theme')) { // Only if user hasn't set a preference
    //         applyTheme(newColorScheme);
    //     }
    // });
}); 