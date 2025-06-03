// NAVBAR RESPONSIVO CON ANIMACIONES PERSONALIZADAS
document.addEventListener('DOMContentLoaded', function() {
    console.log('Navbar responsivo cargado con animaciones personalizadas');
    
    // Crear icono de hamburguesa animado personalizado
    createAnimatedHamburgerIcon();
    
    // Configurar eventos del navbar
    setupNavbarEvents();
    
    // Manejar cambios de tamaño de ventana
    setupResizeHandler();
});

function createAnimatedHamburgerIcon() {
    const toggler = document.querySelector('.navbar-toggler');
    if (!toggler) return;
    
    // Reemplazar el icono de Bootstrap con uno personalizado
    const togglerIcon = toggler.querySelector('.navbar-toggler-icon');
    if (togglerIcon) {
        togglerIcon.innerHTML = `
            <div class="hamburger-lines">
                <span class="line line1"></span>
                <span class="line line2"></span>
                <span class="line line3"></span>
            </div>
        `;
    }
}

function setupNavbarEvents() {
    const toggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('#navbarSupportedContent');
    
    if (!toggler || !navbarCollapse) return;
    
    // Eventos de Bootstrap Collapse - CORREGIR LA LÓGICA
    navbarCollapse.addEventListener('show.bs.collapse', function () {
        // Cuando se ABRE el menú, mostrar X (active)
        toggler.classList.add('active');
        toggler.setAttribute('aria-expanded', 'true');
    });
    
    navbarCollapse.addEventListener('hide.bs.collapse', function () {
        // Cuando se CIERRA el menú, mostrar hamburguesa (remover active)
        toggler.classList.remove('active');
        toggler.setAttribute('aria-expanded', 'false');
    });
    
    // Cerrar menú al hacer click en enlaces (solo en móvil)
    const navLinks = navbarCollapse.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', function() {
            if (window.innerWidth < 1200) {
                const bsCollapse = new bootstrap.Collapse(navbarCollapse, {
                    toggle: false
                });
                bsCollapse.hide();
            }
        });
    });
}

function setupResizeHandler() {
    let resizeTimeout;
    
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimeout);
        
        resizeTimeout = setTimeout(() => {
            const toggler = document.querySelector('.navbar-toggler');
            const navbarCollapse = document.querySelector('#navbarSupportedContent');
            
            if (window.innerWidth >= 1200) {
                // En desktop, asegurar que el menú esté cerrado y hamburguesa sin animación
                if (toggler) {
                    toggler.classList.remove('active');
                    toggler.setAttribute('aria-expanded', 'false');
                }
                
                if (navbarCollapse && navbarCollapse.classList.contains('show')) {
                    const bsCollapse = new bootstrap.Collapse(navbarCollapse, {
                        toggle: false
                    });
                    bsCollapse.hide();
                }
            }
        }, 250);
    });
} 