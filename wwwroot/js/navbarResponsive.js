// NAVBAR RESPONSIVO INTELIGENTE - MEJORADO PARA INCLUIR ELEMENTOS DEL LADO DERECHO
document.addEventListener('DOMContentLoaded', function() {
    function adjustNavbarResponsively() {
        const navbarNav = document.querySelector('.navbar-nav.me-auto');
        const moreDropdown = document.getElementById('moreOptionsDropdown');
        const rightSide = document.querySelector('.d-flex.align-items-center');
        const navbar = document.querySelector('.navbar .container-fluid');
        
        if (!navbarNav || !navbar || !rightSide) return;
        
        // Solo aplicar en pantallas grandes (donde el navbar está expandido)
        if (window.innerWidth < 992) {
            // En móviles, esconder el menú "Más" porque ya existe el menú hamburguesa
            if (moreDropdown) {
                moreDropdown.style.display = 'none';
            }
            // Mostrar todos los elementos principales (el menú hamburguesa los manejará)
            const allNavItems = navbarNav.querySelectorAll('.nav-item:not(.dropdown.d-lg-none)');
            allNavItems.forEach(item => {
                if (item.id !== 'moreOptionsDropdown') {
                    item.style.display = '';
                }
            });
            // Mostrar elementos del lado derecho en móviles
            showRightSideElements();
            return;
        }
        
        // En pantallas grandes, manejar responsividad inteligente
        const navbarWidth = navbar.offsetWidth;
        const brandWidth = document.querySelector('.navbar-brand')?.offsetWidth || 200;
        const moreDropdownWidth = 120; // Ancho estimado del botón "Más"
        const marginSafety = 50; // Margen de seguridad
        
        // Calcular ancho real del lado derecho
        const rightSideWidth = rightSide?.offsetWidth || 250;
        
        const availableWidth = navbarWidth - brandWidth - rightSideWidth - marginSafety;
        
        // Obtener todos los items principales (excluyendo dropdowns móviles y el menú "Más")
        const mainNavItems = Array.from(navbarNav.querySelectorAll('.nav-item:not(.dropdown.d-lg-none)'))
            .filter(item => item.id !== 'moreOptionsDropdown');
        
        // Mostrar todos inicialmente para medir
        mainNavItems.forEach(item => {
            item.style.display = '';
        });
        
        // Calcular ancho total necesario
        let totalWidth = 0;
        const itemWidths = [];
        
        mainNavItems.forEach(item => {
            const itemWidth = item.offsetWidth + 15; // 15px margen entre items
            itemWidths.push({ element: item, width: itemWidth });
            totalWidth += itemWidth;
        });
        
        // Verificar si todo cabe (incluyendo verificación de overflow del lado derecho)
        const isRightSideOverflowing = checkRightSideOverflow();
        
        if (totalWidth <= availableWidth && !isRightSideOverflowing) {
            // Todo cabe, no necesitamos el menú "Más"
            if (moreDropdown) {
                moreDropdown.style.display = 'none';
            }
            mainNavItems.forEach(item => {
                item.style.display = '';
                item.classList.remove('moved-to-more');
            });
            showRightSideElements();
        } else {
            // No todo cabe o el lado derecho se está cortando
            if (moreDropdown) {
                moreDropdown.style.display = '';
            }
            
            // Si el lado derecho se está cortando, reducir espacio disponible
            let adjustedAvailableWidth = availableWidth;
            if (isRightSideOverflowing) {
                adjustedAvailableWidth = availableWidth - 100; // Reducir espacio para el lado derecho
                handleRightSideOverflow();
            } else {
                showRightSideElements();
            }
            
            // Calcular cuántos items caben sin el menú "Más"
            const availableWidthWithMore = adjustedAvailableWidth - moreDropdownWidth;
            let currentWidth = 0;
            let itemsToShow = [];
            let itemsToHide = [];
            
            for (let i = 0; i < itemWidths.length; i++) {
                const { element, width } = itemWidths[i];
                
                if (currentWidth + width <= availableWidthWithMore) {
                    currentWidth += width;
                    itemsToShow.push(element);
                } else {
                    itemsToHide.push(element);
                }
            }
            
            // Aplicar visibilidad
            itemsToShow.forEach(item => {
                item.style.display = '';
                item.classList.remove('moved-to-more');
            });
            
            itemsToHide.forEach(item => {
                item.style.display = 'none';
                item.classList.add('moved-to-more');
            });
            
            // Actualizar contenido del menú "Más" dinámicamente
            updateMoreDropdownContent(itemsToHide);
        }
    }
    
    /**
     * Verificar si el lado derecho se está desbordando
     */
    function checkRightSideOverflow() {
        const navbar = document.querySelector('.navbar .container-fluid');
        const rightSide = document.querySelector('.d-flex.align-items-center');
        
        if (!navbar || !rightSide) return false;
        
        const navbarRect = navbar.getBoundingClientRect();
        const rightSideRect = rightSide.getBoundingClientRect();
        
        // Verificar si el lado derecho se extiende más allá del navbar
        return rightSideRect.right > navbarRect.right - 10; // 10px de margen
    }
    
    /**
     * Manejar overflow del lado derecho
     */
    function handleRightSideOverflow() {
        // Para usuarios no logueados, simplificar los botones
        const registerLink = document.querySelector('a[href*="Register"]');
        const loginBtn = document.querySelector('.btn-login');
        const themeBtn = document.getElementById('themeToggleBtn');
        
        if (registerLink && loginBtn) {
            // Ocultar texto "Registrarse" y solo mostrar ícono
            const registerText = registerLink.querySelector('.d-none.d-md-inline');
            if (registerText) {
                registerText.style.display = 'none';
            }
            
            // Simplificar botón de login
            const loginText = loginBtn.querySelector('span');
            if (loginText) {
                loginText.style.display = 'none';
            }
            
            // Hacer el botón de tema más pequeño
            if (themeBtn) {
                themeBtn.classList.add('btn-sm');
            }
        }
    }
    
    /**
     * Mostrar elementos del lado derecho normalmente
     */
    function showRightSideElements() {
        const registerLink = document.querySelector('a[href*="Register"]');
        const loginBtn = document.querySelector('.btn-login');
        const themeBtn = document.getElementById('themeToggleBtn');
        
        if (registerLink) {
            const registerText = registerLink.querySelector('span');
            if (registerText) {
                registerText.style.display = '';
            }
        }
        
        if (loginBtn) {
            const loginText = loginBtn.querySelector('span');
            if (loginText) {
                loginText.style.display = '';
            }
        }
        
        if (themeBtn) {
            themeBtn.classList.remove('btn-sm');
        }
    }
    
    /**
     * Actualizar el contenido del menú "Más" con los items que no caben
     */
    function updateMoreDropdownContent(hiddenItems) {
        const moreDropdownMenu = document.querySelector('#moreOptionsDropdown .dropdown-menu');
        if (!moreDropdownMenu) return;
        
        // Limpiar contenido dinámico previo
        const dynamicItems = moreDropdownMenu.querySelectorAll('.dynamic-nav-item');
        dynamicItems.forEach(item => item.remove());
        
        if (hiddenItems.length > 0) {
            // Agregar separator si hay contenido estático
            const staticItems = moreDropdownMenu.querySelectorAll('li:not(.dynamic-nav-item)');
            if (staticItems.length > 0) {
                const separator = document.createElement('li');
                separator.className = 'dynamic-nav-item';
                separator.innerHTML = '<hr class="dropdown-divider">';
                moreDropdownMenu.insertBefore(separator, staticItems[0]);
                
                const header = document.createElement('li');
                header.className = 'dynamic-nav-item';
                header.innerHTML = '<h6 class="dropdown-header">Navegación Principal</h6>';
                moreDropdownMenu.insertBefore(header, staticItems[0]);
            }
            
            // Agregar items ocultos al dropdown
            hiddenItems.forEach(item => {
                const navLink = item.querySelector('.nav-link');
                if (navLink && !navLink.classList.contains('dropdown-toggle')) {
                    const dropdownItem = document.createElement('li');
                    dropdownItem.className = 'dynamic-nav-item';
                    
                    const link = document.createElement('a');
                    link.className = 'dropdown-item d-flex align-items-center';
                    link.href = navLink.href;
                    link.innerHTML = navLink.innerHTML;
                    
                    // Copiar atributos asp-page si existen
                    if (navLink.hasAttribute('asp-page')) {
                        link.setAttribute('asp-page', navLink.getAttribute('asp-page'));
                    }
                    if (navLink.hasAttribute('asp-area')) {
                        link.setAttribute('asp-area', navLink.getAttribute('asp-area'));
                    }
                    
                    dropdownItem.appendChild(link);
                    
                    // Insertar antes del contenido estático
                    const staticItems = moreDropdownMenu.querySelectorAll('li:not(.dynamic-nav-item)');
                    if (staticItems.length > 0) {
                        moreDropdownMenu.insertBefore(dropdownItem, staticItems[0]);
                    } else {
                        moreDropdownMenu.appendChild(dropdownItem);
                    }
                }
            });
        }
    }
    
    // Función de debounce para optimizar rendimiento
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
    
    // Ejecutar al cargar con delay para asegurar que todo esté renderizado
    setTimeout(adjustNavbarResponsively, 200);
    
    // Ejecutar al redimensionar (con debounce para rendimiento)
    window.addEventListener('resize', debounce(adjustNavbarResponsively, 100));
    
    // Ejecutar cuando cambie el estado del navbar collapse
    const navbarCollapse = document.getElementById('navbarSupportedContent');
    if (navbarCollapse) {
        navbarCollapse.addEventListener('shown.bs.collapse', () => {
            setTimeout(adjustNavbarResponsively, 100);
        });
        navbarCollapse.addEventListener('hidden.bs.collapse', () => {
            setTimeout(adjustNavbarResponsively, 100);
        });
    }
    
    // Ejecutar cuando se complete la carga de fuentes (puede afectar el ancho)
    if (document.fonts) {
        document.fonts.ready.then(() => {
            setTimeout(adjustNavbarResponsively, 100);
        });
    }
    
    // Observar cambios en el DOM que puedan afectar el navbar
    const observer = new MutationObserver(debounce(adjustNavbarResponsively, 200));
    const navbarElement = document.querySelector('.navbar');
    if (navbarElement) {
        observer.observe(navbarElement, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'style']
        });
    }
    
    // Log para debugging en desarrollo
    if (window.location.hostname === 'localhost' || window.location.hostname.includes('127.0.0.1')) {
        console.log('🧭 Navbar responsivo inteligente activado (con detección de overflow del lado derecho)');
        
        // Comando de debug mejorado
        window.debugNavbar = () => {
            const rightSide = document.querySelector('.d-flex.align-items-center');
            console.log('Navbar Debug Info:', {
                navbarWidth: document.querySelector('.navbar .container-fluid')?.offsetWidth,
                brandWidth: document.querySelector('.navbar-brand')?.offsetWidth,
                rightSideWidth: rightSide?.offsetWidth,
                rightSideOverflowing: checkRightSideOverflow(),
                mainItems: document.querySelectorAll('.navbar-nav.me-auto .nav-item:not(.dropdown.d-lg-none)').length,
                moreDropdownVisible: document.getElementById('moreOptionsDropdown')?.style.display !== 'none',
                navbarRect: document.querySelector('.navbar .container-fluid')?.getBoundingClientRect(),
                rightSideRect: rightSide?.getBoundingClientRect()
            });
        };
    }
}); 