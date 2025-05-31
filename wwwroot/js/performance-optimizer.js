/**
 * VitalCare Performance Optimizer
 * Optimiza animaciones y efectos basado en capacidades del dispositivo
 */

class PerformanceOptimizer {
    constructor() {
        this.isLowEndDevice = this.detectLowEndDevice();
        this.reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        this.connectionSpeed = this.detectConnectionSpeed();
        
        this.init();
    }

    /**
     * Detecta dispositivos de bajos recursos
     */
    detectLowEndDevice() {
        // Detectar basado en memoria y cores del dispositivo
        const memory = navigator.deviceMemory || 4; // Default 4GB si no está disponible
        const cores = navigator.hardwareConcurrency || 4; // Default 4 cores
        const isMobile = window.innerWidth <= 768;
        
        // Considerar dispositivo de bajos recursos si:
        // - Menos de 4GB RAM
        // - Menos de 4 cores
        // - Dispositivo móvil con ancho pequeño
        return memory < 4 || cores < 4 || (isMobile && memory < 6);
    }

    /**
     * Detecta velocidad de conexión
     */
    detectConnectionSpeed() {
        const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
        if (connection) {
            // Slow 2G, 2G, 3G = slow, 4G = fast
            return connection.effectiveType === '4g' ? 'fast' : 'slow';
        }
        return 'unknown';
    }

    /**
     * Inicializa las optimizaciones
     */
    init() {
        // Si es dispositivo de bajos recursos, aplicar optimizaciones agresivas
        if (this.isLowEndDevice || this.reducedMotion) {
            this.applyLowEndOptimizations();
        }

        // Configurar Intersection Observer para lazy animations
        this.setupIntersectionObserver();

        // Optimizar will-change dinámicamente
        this.setupWillChangeOptimization();

        // Monitorear performance
        this.setupPerformanceMonitoring();

        console.log('🚀 VitalCare Performance Optimizer initialized', {
            lowEndDevice: this.isLowEndDevice,
            reducedMotion: this.reducedMotion,
            connectionSpeed: this.connectionSpeed
        });
    }

    /**
     * Aplica optimizaciones para dispositivos de bajos recursos
     */
    applyLowEndOptimizations() {
        // Crear y aplicar CSS para optimizaciones de bajo rendimiento
        const style = document.createElement('style');
        style.textContent = `
            /* Optimizaciones para dispositivos de bajos recursos */
            .feature-card-hover:hover,
            .stat-card:hover,
            .action-card-hover:hover {
                transform: none !important;
                animation: none !important;
            }
            
            .sparkle, .particle, .floating-icon {
                display: none !important;
            }
            
            .navbar {
                backdrop-filter: none !important;
            }
            
            .hero-particles,
            .icon-particles,
            .feature-sparkles {
                display: none !important;
            }
            
            /* Simplificar gradientes */
            .app-footer {
                background: #343a40 !important;
            }
            
            /* Reducir complejidad de sombras */
            .card {
                box-shadow: 0 1px 3px rgba(0,0,0,0.1) !important;
            }
        `;
        document.head.appendChild(style);

        // Agregar clase al body para identificación
        document.body.classList.add('low-end-device');
    }

    /**
     * Configura Intersection Observer para animaciones lazy
     */
    setupIntersectionObserver() {
        if (!window.IntersectionObserver) return;

        const options = {
            root: null,
            rootMargin: '50px',
            threshold: 0.1
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    // Elemento está visible, activar animaciones
                    entry.target.classList.add('in-view');
                    
                    // Para elementos con efectos especiales, activar solo si no es dispositivo de bajos recursos
                    if (!this.isLowEndDevice && !this.reducedMotion) {
                        entry.target.classList.add('effects-enabled');
                    }
                } else {
                    // Elemento fuera de vista, desactivar para ahorrar recursos
                    entry.target.classList.remove('in-view');
                }
            });
        }, options);

        // Observar elementos que tienen animaciones
        const animatedElements = document.querySelectorAll(
            '.feature-card-hover, .stat-card, .action-card-hover, .animate-on-scroll'
        );
        
        animatedElements.forEach(element => {
            observer.observe(element);
            
            // Inicialmente ocultos hasta que entren en viewport
            if (element.classList.contains('animate-on-scroll')) {
                element.style.opacity = '0';
                element.style.transform = 'translateY(20px)';
            }
        });
    }

    /**
     * Optimiza will-change dinámicamente
     */
    setupWillChangeOptimization() {
        const elementsWithHover = document.querySelectorAll(
            '.feature-card-hover, .stat-card, .action-card-hover, .btn'
        );

        elementsWithHover.forEach(element => {
            // Resetear will-change inicialmente
            element.style.willChange = 'auto';

            // Solo activar will-change en hover si no es dispositivo de bajos recursos
            if (!this.isLowEndDevice) {
                element.addEventListener('mouseenter', () => {
                    element.style.willChange = 'transform, box-shadow';
                });

                element.addEventListener('mouseleave', () => {
                    element.style.willChange = 'auto';
                });
            }
        });
    }

    /**
     * Monitorea el rendimiento y ajusta dinámicamente
     */
    setupPerformanceMonitoring() {
        if (!window.performance || !window.performance.observer) return;

        let frameDropCount = 0;
        let lastTime = performance.now();

        const checkFrameRate = () => {
            const now = performance.now();
            const delta = now - lastTime;
            
            // Si el frame toma más de 16.67ms (60fps), contar como drop
            if (delta > 16.67) {
                frameDropCount++;
                
                // Si hay muchos frame drops, aplicar optimizaciones adicionales
                if (frameDropCount > 10) {
                    this.applyEmergencyOptimizations();
                    frameDropCount = 0; // Reset counter
                }
            }
            
            lastTime = now;
            requestAnimationFrame(checkFrameRate);
        };

        // Solo monitorear si no es dispositivo de bajos recursos (ya optimizado)
        if (!this.isLowEndDevice) {
            requestAnimationFrame(checkFrameRate);
        }
    }

    /**
     * Optimizaciones de emergencia cuando el rendimiento es muy bajo
     */
    applyEmergencyOptimizations() {
        console.warn('⚠️ Applying emergency performance optimizations due to low frame rate');
        
        // Desactivar todas las animaciones temporalmente
        const style = document.createElement('style');
        style.id = 'emergency-optimizations';
        style.textContent = `
            * {
                animation: none !important;
                transition: none !important;
            }
            
            .feature-card-hover:hover,
            .stat-card:hover,
            .btn:hover {
                transform: none !important;
            }
        `;
        document.head.appendChild(style);

        // Remover después de 5 segundos para permitir recuperación
        setTimeout(() => {
            const emergencyStyle = document.getElementById('emergency-optimizations');
            if (emergencyStyle) {
                emergencyStyle.remove();
            }
        }, 5000);
    }

    /**
     * API pública para deshabilitar efectos específicos
     */
    disableEffect(effectName) {
        const effectsMap = {
            'particles': '.sparkle, .particle',
            'floating': '.floating-icon',
            'hover-transforms': '.feature-card-hover:hover, .stat-card:hover',
            'backdrop-blur': '.navbar'
        };

        const selector = effectsMap[effectName];
        if (selector) {
            const elements = document.querySelectorAll(selector);
            elements.forEach(element => {
                element.style.display = 'none';
            });
        }
    }

    /**
     * Obtener métricas de rendimiento
     */
    getPerformanceMetrics() {
        const navigation = performance.getEntriesByType('navigation')[0];
        return {
            deviceInfo: {
                memory: navigator.deviceMemory || 'unknown',
                cores: navigator.hardwareConcurrency || 'unknown',
                connection: this.connectionSpeed,
                isLowEnd: this.isLowEndDevice
            },
            timing: {
                domContentLoaded: navigation ? navigation.domContentLoadedEventEnd : 0,
                loadComplete: navigation ? navigation.loadEventEnd : 0
            }
        };
    }
}

// Auto-inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    window.VitalCareOptimizer = new PerformanceOptimizer();
});

// Exponer utilidades globales
window.VitalCareUtils = {
    disableAnimations: () => window.VitalCareOptimizer?.applyLowEndOptimizations(),
    getMetrics: () => window.VitalCareOptimizer?.getPerformanceMetrics(),
    disableEffect: (effect) => window.VitalCareOptimizer?.disableEffect(effect)
}; 