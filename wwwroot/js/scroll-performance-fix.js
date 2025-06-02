/**
 * VitalCare Scroll Performance Fix
 * Soluciona problemas de lag en elementos hero durante el scrolling
 */

class ScrollPerformanceFix {
    constructor() {
        this.isScrolling = false;
        this.scrollTimer = null;
        this.ticking = false;
        this.lastScrollTop = 0;
        this.scrollDirection = 'down';
        
        this.init();
    }

    init() {
        // Configurar passive scroll listeners para mejor rendimiento
        this.setupPassiveScrollListeners();
        
        // Desactivar animaciones durante scroll
        this.setupScrollAnimationControl();
        
        // Optimizar hero elements específicamente
        this.optimizeHeroElements();
        
        // Throttle resize events
        this.setupResizeOptimization();
        
        console.log('🏃‍♂️ Scroll Performance Fix initialized');
    }

    /**
     * Configura listeners de scroll pasivos
     */
    setupPassiveScrollListeners() {
        let scrollTimeout;
        
        // Usar passive: true para mejor rendimiento
        window.addEventListener('scroll', () => {
            if (!this.ticking) {
                requestAnimationFrame(() => {
                    this.handleScroll();
                    this.ticking = false;
                });
                this.ticking = true;
            }
        }, { passive: true });

        // Detectar fin de scroll
        window.addEventListener('scroll', () => {
            this.isScrolling = true;
            document.body.classList.add('scrolling');
            
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                this.isScrolling = false;
                document.body.classList.remove('scrolling');
                this.reactivateAnimations();
            }, 100);
        }, { passive: true });
    }

    /**
     * Maneja el scroll de forma optimizada
     */
    handleScroll() {
        const currentScrollTop = window.pageYOffset || document.documentElement.scrollTop;
        
        // Determinar dirección del scroll
        this.scrollDirection = currentScrollTop > this.lastScrollTop ? 'down' : 'up';
        this.lastScrollTop = currentScrollTop;

        // Optimizar hero section basado en scroll position
        this.optimizeHeroBasedOnScroll(currentScrollTop);
    }

    /**
     * Optimiza hero section basado en posición del scroll
     */
    optimizeHeroBasedOnScroll(scrollTop) {
        const heroSection = document.querySelector('.hero-section-enhanced');
        if (!heroSection) return;

        const heroHeight = heroSection.offsetHeight;
        const scrollProgress = Math.min(scrollTop / heroHeight, 1);

        // Si el hero está fuera de vista, desactivar efectos costosos
        if (scrollTop > heroHeight) {
            this.disableHeroEffects();
        } else {
            this.enableHeroEffects();
        }

        // Ajustar opacidad de elementos flotantes basado en scroll
        this.adjustFloatingElementsOpacity(1 - scrollProgress);
    }

    /**
     * Desactiva efectos del hero durante scroll
     */
    disableHeroEffects() {
        const heroElements = document.querySelectorAll(
            '.floating-icon, .hero-glow-border, .sparkle, .particle'
        );
        
        heroElements.forEach(element => {
            element.style.animationPlayState = 'paused';
            element.style.willChange = 'auto';
        });
    }

    /**
     * Reactiva efectos del hero
     */
    enableHeroEffects() {
        const heroElements = document.querySelectorAll(
            '.floating-icon, .hero-glow-border, .sparkle, .particle'
        );
        
        heroElements.forEach(element => {
            element.style.animationPlayState = 'running';
        });
    }

    /**
     * Ajusta opacidad de elementos flotantes
     */
    adjustFloatingElementsOpacity(opacity) {
        const floatingElements = document.querySelectorAll('.floating-icon');
        floatingElements.forEach(element => {
            element.style.opacity = Math.max(opacity, 0.1);
        });
    }

    /**
     * Control de animaciones durante scroll
     */
    setupScrollAnimationControl() {
        // Crear CSS para desactivar animaciones durante scroll
        const scrollOptimizationCSS = `
            .scrolling .hero-glow-border {
                animation-play-state: paused !important;
            }
            
            .scrolling .floating-icon {
                animation-play-state: paused !important;
                transition: none !important;
            }
            
            .scrolling .sparkle,
            .scrolling .particle {
                animation-play-state: paused !important;
            }
            
            .scrolling .hero-btn::before {
                transition: none !important;
            }
            
            .scrolling .hero-stat-card {
                transition: none !important;
            }
            
            /* Optimizaciones durante scroll */
            .scrolling * {
                pointer-events: none !important;
            }
            
            .scrolling a,
            .scrolling button,
            .scrolling .btn {
                pointer-events: auto !important;
            }
            
            /* Deshabilitar will-change durante scroll */
            .scrolling .hero-content-container,
            .scrolling .hero-glow-border,
            .scrolling .floating-icon {
                will-change: auto !important;
            }
        `;

        const styleSheet = document.createElement('style');
        styleSheet.textContent = scrollOptimizationCSS;
        document.head.appendChild(styleSheet);
    }

    /**
     * Optimiza elementos específicos del hero
     */
    optimizeHeroElements() {
        // Optimizar contenedor principal del hero
        const heroContainer = document.querySelector('.hero-content-container');
        if (heroContainer) {
            // Usar contain para optimizar rendering
            heroContainer.style.contain = 'layout style paint';
            
            // Optimizar backdrop-filter en móviles
            if (window.innerWidth <= 768) {
                heroContainer.style.backdropFilter = 'blur(5px)';
            }
        }

        // Optimizar iconos flotantes
        const floatingIcons = document.querySelectorAll('.floating-icon');
        floatingIcons.forEach(icon => {
            icon.style.contain = 'layout style';
            icon.style.backfaceVisibility = 'hidden';
            
            // Reducir complejidad en móviles
            if (window.innerWidth <= 768) {
                icon.style.display = 'none';
            }
        });

        // Optimizar tarjetas de estadísticas
        const statCards = document.querySelectorAll('.hero-stat-card');
        statCards.forEach(card => {
            card.style.contain = 'layout style';
            card.style.willChange = 'auto';
            
            // Solo activar will-change en hover
            card.addEventListener('mouseenter', () => {
                if (!this.isScrolling) {
                    card.style.willChange = 'transform, box-shadow';
                }
            });
            
            card.addEventListener('mouseleave', () => {
                card.style.willChange = 'auto';
            });
        });
    }

    /**
     * Reactiva animaciones cuando termina el scroll
     */
    reactivateAnimations() {
        if (!this.isScrolling) {
            const animatedElements = document.querySelectorAll(
                '.hero-glow-border, .floating-icon, .sparkle, .particle'
            );
            
            animatedElements.forEach(element => {
                element.style.animationPlayState = 'running';
            });
        }
    }

    /**
     * Optimiza eventos de resize
     */
    setupResizeOptimization() {
        let resizeTimeout;
        
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            
            // Desactivar animaciones durante resize
            document.body.classList.add('resizing');
            
            resizeTimeout = setTimeout(() => {
                document.body.classList.remove('resizing');
                this.optimizeHeroElements(); // Re-optimizar para nuevo tamaño
            }, 250);
        }, { passive: true });

        // CSS para optimizaciones durante resize
        const resizeCSS = `
            .resizing * {
                animation-play-state: paused !important;
                transition: none !important;
            }
        `;
        
        const resizeStyleSheet = document.createElement('style');
        resizeStyleSheet.textContent = resizeCSS;
        document.head.appendChild(resizeStyleSheet);
    }

    /**
     * API pública para control manual
     */
    forceOptimization() {
        this.disableHeroEffects();
        document.body.classList.add('performance-mode');
        
        const performanceCSS = `
            .performance-mode .floating-icon {
                display: none !important;
            }
            
            .performance-mode .sparkle,
            .performance-mode .particle {
                display: none !important;
            }
            
            .performance-mode .hero-glow-border {
                display: none !important;
            }
            
            .performance-mode .hero-content-container {
                backdrop-filter: none !important;
            }
        `;
        
        const perfStyleSheet = document.createElement('style');
        perfStyleSheet.id = 'force-performance-mode';
        perfStyleSheet.textContent = performanceCSS;
        document.head.appendChild(perfStyleSheet);
    }

    /**
     * Deshabilitar modo performance forzado
     */
    disableForceOptimization() {
        document.body.classList.remove('performance-mode');
        const perfStyle = document.getElementById('force-performance-mode');
        if (perfStyle) {
            perfStyle.remove();
        }
        this.enableHeroEffects();
    }
}

// Auto-inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    window.VitalCareScrollFix = new ScrollPerformanceFix();
});

// Exponer utilidades globales
window.VitalCareScrollUtils = {
    forceOptimization: () => window.VitalCareScrollFix?.forceOptimization(),
    disableOptimization: () => window.VitalCareScrollFix?.disableForceOptimization(),
    getCurrentScrollData: () => ({
        isScrolling: window.VitalCareScrollFix?.isScrolling,
        direction: window.VitalCareScrollFix?.scrollDirection,
        position: window.pageYOffset
    })
}; 