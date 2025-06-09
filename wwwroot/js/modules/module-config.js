// Module Configuration and Registry
class ModuleConfig {
    constructor() {
        this.modules = {
            doctor: {
                css: ['/css/modules/doctor/doctor-dashboard.css'],
                js: ['/js/modules/doctor/doctor-dashboard.js'],
                dependencies: ['common']
            },
            patient: {
                css: ['/css/modules/patient/patient-dashboard.css'],
                js: ['/js/modules/patient/patient-dashboard.js'],
                dependencies: ['common']
            },
            admin: {
                css: ['/css/modules/admin/admin-dashboard.css'],
                js: ['/js/modules/admin/admin-dashboard.js'],
                dependencies: ['common']
            },
            public: {
                css: ['/css/modules/public/public-pages.css'],
                js: ['/js/modules/public/public-pages.js'],
                dependencies: []
            },
            common: {
                css: ['/css/modules/common/common-styles.css'],
                js: ['/js/modules/common/common-utils.js'],
                dependencies: []
            }
        };
        
        this.loadedModules = new Set();
    }

    async loadModule(moduleName) {
        if (this.loadedModules.has(moduleName)) {
            return;
        }

        const module = this.modules[moduleName];
        if (!module) {
            console.error(`Module ${moduleName} not found`);
            return;
        }

        // Load dependencies first
        for (const dependency of module.dependencies) {
            await this.loadModule(dependency);
        }

        // Load CSS files
        for (const cssFile of module.css) {
            this.loadCSS(cssFile);
        }

        // Load JS files
        for (const jsFile of module.js) {
            await this.loadJS(jsFile);
        }

        this.loadedModules.add(moduleName);
        console.log(`Module ${moduleName} loaded successfully`);
    }

    loadCSS(href) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        document.head.appendChild(link);
    }

    loadJS(src) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    getModuleInfo(moduleName) {
        return this.modules[moduleName] || null;
    }

    isModuleLoaded(moduleName) {
        return this.loadedModules.has(moduleName);
    }

    // Auto-detect current page module
    autoLoadModule() {
        const path = window.location.pathname.toLowerCase();
        
        // SOLO carga el módulo doctor en la página específica del dashboard
        if (path === '/userdashboards/doctor' || path === '/userdashboards/doctor/') {
            this.loadModule('doctor');
        } else if (path.includes('/userdashboards/patient') || path.includes('/patient/')) {
            this.loadModule('patient');
        } else if (path.includes('/userdashboards/admin') || path.includes('/admin/')) {
            this.loadModule('admin');
        } else if (path.includes('/public/')) {
            this.loadModule('public');
        } else {
            this.loadModule('common');
        }
    }
}

// Global instance
window.ModuleConfig = new ModuleConfig();

// Auto-load on page load
document.addEventListener('DOMContentLoaded', () => {
    window.ModuleConfig.autoLoadModule();
}); 