/**
 * VitalCare Scroll Performance Testing Utility
 * Herramienta para monitorear y verificar las mejoras de rendimiento
 */

class ScrollPerformanceMonitor {
    constructor() {
        this.frameCount = 0;
        this.dropCount = 0;
        this.isMonitoring = false;
        this.lastFrameTime = performance.now();
        this.frameTimeHistory = [];
        this.maxHistoryLength = 60; // 1 segundo a 60fps
        
        this.createMonitorUI();
    }

    /**
     * Crear interfaz de monitoreo
     */
    createMonitorUI() {
        // Solo crear si estamos en desarrollo (no en producción)
        if (window.location.hostname === 'localhost' || window.location.hostname.includes('127.0.0.1')) {
            const monitorDiv = document.createElement('div');
            monitorDiv.id = 'scroll-performance-monitor';
            monitorDiv.style.cssText = `
                position: fixed;
                top: 10px;
                right: 10px;
                background: rgba(0, 0, 0, 0.8);
                color: white;
                padding: 10px;
                border-radius: 5px;
                font-family: monospace;
                font-size: 12px;
                z-index: 10000;
                min-width: 200px;
                display: none;
            `;
            
            monitorDiv.innerHTML = `
                <div><strong>Performance Monitor</strong></div>
                <div>FPS: <span id="fps-display">--</span></div>
                <div>Drops: <span id="drops-display">0</span></div>
                <div>Status: <span id="status-display">Stopped</span></div>
                <div style="margin-top: 8px;">
                    <button id="start-monitor" style="margin-right: 5px;">Start</button>
                    <button id="stop-monitor" style="margin-right: 5px;">Stop</button>
                    <button id="force-perf">Force Perf</button>
                </div>
                <div style="margin-top: 8px;">
                    <button id="test-scroll" style="margin-right: 5px;">Test Scroll</button>
                    <button id="reset-stats">Reset</button>
                </div>
            `;
            
            document.body.appendChild(monitorDiv);
            this.setupUIEventListeners();
        }
    }

    /**
     * Configurar event listeners para la UI
     */
    setupUIEventListeners() {
        const startBtn = document.getElementById('start-monitor');
        const stopBtn = document.getElementById('stop-monitor');
        const forcePerfBtn = document.getElementById('force-perf');
        const testScrollBtn = document.getElementById('test-scroll');
        const resetBtn = document.getElementById('reset-stats');

        if (startBtn) startBtn.addEventListener('click', () => this.startMonitoring());
        if (stopBtn) stopBtn.addEventListener('click', () => this.stopMonitoring());
        if (forcePerfBtn) forcePerfBtn.addEventListener('click', () => this.toggleForcePerformance());
        if (testScrollBtn) testScrollBtn.addEventListener('click', () => this.testScrollPerformance());
        if (resetBtn) resetBtn.addEventListener('click', () => this.resetStats());
    }

    /**
     * Iniciar monitoreo
     */
    startMonitoring() {
        this.isMonitoring = true;
        this.frameCount = 0;
        this.dropCount = 0;
        this.lastFrameTime = performance.now();
        
        const monitor = document.getElementById('scroll-performance-monitor');
        if (monitor) {
            monitor.style.display = 'block';
            document.getElementById('status-display').textContent = 'Monitoring';
        }
        
        this.monitorLoop();
        console.log('🔍 Scroll Performance Monitoring started');
    }

    /**
     * Detener monitoreo
     */
    stopMonitoring() {
        this.isMonitoring = false;
        const statusDisplay = document.getElementById('status-display');
        if (statusDisplay) statusDisplay.textContent = 'Stopped';
        
        console.log('⏹️ Scroll Performance Monitoring stopped');
        this.showStats();
    }

    /**
     * Loop de monitoreo
     */
    monitorLoop() {
        if (!this.isMonitoring) return;

        const now = performance.now();
        const deltaTime = now - this.lastFrameTime;
        
        this.frameCount++;
        this.frameTimeHistory.push(deltaTime);
        
        // Mantener historial limitado
        if (this.frameTimeHistory.length > this.maxHistoryLength) {
            this.frameTimeHistory.shift();
        }
        
        // Detectar frame drops (más de 16.67ms = menos de 60fps)
        if (deltaTime > 16.67) {
            this.dropCount++;
        }
        
        // Actualizar UI cada 30 frames
        if (this.frameCount % 30 === 0) {
            this.updateUI();
        }
        
        this.lastFrameTime = now;
        requestAnimationFrame(() => this.monitorLoop());
    }

    /**
     * Actualizar UI del monitor
     */
    updateUI() {
        const fpsDisplay = document.getElementById('fps-display');
        const dropsDisplay = document.getElementById('drops-display');
        
        if (fpsDisplay && dropsDisplay) {
            const avgFrameTime = this.frameTimeHistory.reduce((a, b) => a + b, 0) / this.frameTimeHistory.length;
            const fps = Math.round(1000 / avgFrameTime);
            
            fpsDisplay.textContent = fps;
            fpsDisplay.style.color = fps >= 55 ? '#28a745' : fps >= 30 ? '#ffc107' : '#dc3545';
            
            dropsDisplay.textContent = this.dropCount;
            dropsDisplay.style.color = this.dropCount <= 5 ? '#28a745' : this.dropCount <= 15 ? '#ffc107' : '#dc3545';
        }
    }

    /**
     * Alternar modo de performance forzado
     */
    toggleForcePerformance() {
        if (window.VitalCareScrollUtils) {
            const isPerformanceMode = document.body.classList.contains('performance-mode');
            
            if (isPerformanceMode) {
                window.VitalCareScrollUtils.disableOptimization();
                console.log('✅ Performance mode disabled');
            } else {
                window.VitalCareScrollUtils.forceOptimization();
                console.log('⚡ Performance mode enabled');
            }
        }
    }

    /**
     * Probar rendimiento del scroll
     */
    testScrollPerformance() {
        console.log('🧪 Testing scroll performance...');
        
        // Reset stats
        this.resetStats();
        this.startMonitoring();
        
        // Simular scroll
        const startPosition = window.pageYOffset;
        const targetPosition = Math.min(startPosition + 2000, document.body.scrollHeight - window.innerHeight);
        
        let currentPosition = startPosition;
        const scrollStep = 25;
        const testInterval = setInterval(() => {
            currentPosition += scrollStep;
            window.scrollTo(0, currentPosition);
            
            if (currentPosition >= targetPosition) {
                clearInterval(testInterval);
                
                // Volver a la posición original
                setTimeout(() => {
                    window.scrollTo({ top: startPosition, behavior: 'smooth' });
                    
                    // Mostrar resultados después de un tiempo
                    setTimeout(() => {
                        this.stopMonitoring();
                        this.showTestResults();
                    }, 2000);
                }, 1000);
            }
        }, 16); // ~60fps
    }

    /**
     * Mostrar resultados del test
     */
    showTestResults() {
        const avgFrameTime = this.frameTimeHistory.reduce((a, b) => a + b, 0) / this.frameTimeHistory.length;
        const fps = Math.round(1000 / avgFrameTime);
        const dropPercentage = Math.round((this.dropCount / this.frameCount) * 100);
        
        const results = {
            totalFrames: this.frameCount,
            avgFPS: fps,
            frameDrops: this.dropCount,
            dropPercentage: dropPercentage,
            performance: fps >= 55 ? 'Excelente' : fps >= 30 ? 'Bueno' : 'Necesita optimización'
        };
        
        console.log('📊 Test Results:', results);
        
        // Mostrar en UI si existe
        if (document.getElementById('scroll-performance-monitor')) {
            alert(`Test Results:
FPS Promedio: ${fps}
Frame Drops: ${this.dropCount} (${dropPercentage}%)
Performance: ${results.performance}`);
        }
    }

    /**
     * Resetear estadísticas
     */
    resetStats() {
        this.frameCount = 0;
        this.dropCount = 0;
        this.frameTimeHistory = [];
        this.lastFrameTime = performance.now();
        
        const dropsDisplay = document.getElementById('drops-display');
        if (dropsDisplay) {
            dropsDisplay.textContent = '0';
            dropsDisplay.style.color = '#28a745';
        }
    }

    /**
     * Mostrar estadísticas en consola
     */
    showStats() {
        if (this.frameCount > 0) {
            const avgFrameTime = this.frameTimeHistory.reduce((a, b) => a + b, 0) / this.frameTimeHistory.length;
            const fps = Math.round(1000 / avgFrameTime);
            const dropPercentage = Math.round((this.dropCount / this.frameCount) * 100);
            
            console.log(`📈 Performance Stats:
- Total Frames: ${this.frameCount}
- Average FPS: ${fps}
- Frame Drops: ${this.dropCount} (${dropPercentage}%)
- Performance Status: ${fps >= 55 ? '✅ Excellent' : fps >= 30 ? '⚠️ Good' : '❌ Needs optimization'}`);
        }
    }

    /**
     * API pública
     */
    getStats() {
        if (this.frameCount === 0) return null;
        
        const avgFrameTime = this.frameTimeHistory.reduce((a, b) => a + b, 0) / this.frameTimeHistory.length;
        return {
            frameCount: this.frameCount,
            avgFPS: Math.round(1000 / avgFrameTime),
            frameDrops: this.dropCount,
            dropPercentage: Math.round((this.dropCount / this.frameCount) * 100)
        };
    }
}

// Auto-inicializar solo en desarrollo
document.addEventListener('DOMContentLoaded', () => {
    if (window.location.hostname === 'localhost' || window.location.hostname.includes('127.0.0.1')) {
        window.VitalCarePerformanceMonitor = new ScrollPerformanceMonitor();
        
        // Atajos de teclado para testing
        document.addEventListener('keydown', (e) => {
            // Ctrl + Shift + M = Toggle monitor
            if (e.ctrlKey && e.shiftKey && e.key === 'M') {
                const monitor = document.getElementById('scroll-performance-monitor');
                if (monitor) {
                    monitor.style.display = monitor.style.display === 'none' ? 'block' : 'none';
                }
            }
            
            // Ctrl + Shift + T = Test scroll
            if (e.ctrlKey && e.shiftKey && e.key === 'T') {
                window.VitalCarePerformanceMonitor.testScrollPerformance();
            }
            
            // Ctrl + Shift + P = Toggle performance mode
            if (e.ctrlKey && e.shiftKey && e.key === 'P') {
                window.VitalCarePerformanceMonitor.toggleForcePerformance();
            }
        });
        
        console.log('🔧 Performance Monitor available. Shortcuts:\n- Ctrl+Shift+M: Toggle monitor\n- Ctrl+Shift+T: Test scroll\n- Ctrl+Shift+P: Toggle performance mode');
    }
}); 