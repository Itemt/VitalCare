// Doctor Dashboard Module
class DoctorDashboard {
    constructor() {
        this.init();
    }

    init() {
        // Only initialize if we're on the dashboard page with the required elements
        if (this.isDashboardPage()) {
            this.setupEventListeners();
            // Commented out API call since statistics are now loaded server-side
            // this.loadDashboardData();
            this.initializeCalendar();
            this.setupNotifications();
        }
    }

    isDashboardPage() {
        // Check if the dashboard-specific elements exist
        return document.getElementById('doctorPanelDashboard') !== null;
    }

    setupEventListeners() {
        // Quick action buttons
        document.querySelectorAll('.quick-action-btn').forEach(btn => {
            btn.addEventListener('click', this.handleQuickAction.bind(this));
        });

        // Appointment confirmation buttons
        document.querySelectorAll('.confirm-appointment').forEach(btn => {
            btn.addEventListener('click', this.confirmAppointment.bind(this));
        });

        // Appointment reschedule buttons
        document.querySelectorAll('.reschedule-appointment').forEach(btn => {
            btn.addEventListener('click', this.rescheduleAppointment.bind(this));
        });

        // Rating interactions
        this.setupRatingSystem();
    }

    async loadDashboardData() {
        try {
            const response = await fetch('/api/doctor/dashboard-stats');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            this.updateDashboardStats(data);
        } catch (error) {
            console.error('Error loading dashboard data:', error);
            this.showNotification('Error al cargar los datos del dashboard', 'error');
        }
    }

    updateDashboardStats(data) {
        // Update today's appointments
        const todayAppointments = document.getElementById('doctorCitasHoyStat');
        if (todayAppointments) {
            todayAppointments.textContent = data.todayAppointments || 0;
        }

        // Update pending confirmations
        const pendingConfirmations = document.getElementById('doctorCitasConfirmadasStat');
        if (pendingConfirmations) {
            pendingConfirmations.textContent = data.confirmedAppointments || 0;
        }

        // Update rating
        const avgRating = document.getElementById('doctorSatisfaccionStat');
        if (avgRating) {
            avgRating.textContent = `${data.satisfactionPercentage || 0}%`;
        }

        // Update total patients
        const totalPatients = document.getElementById('total-patients');
        if (totalPatients) {
            totalPatients.textContent = data.totalPatients || 0;
        }
    }

    updateRatingStars(rating) {
        const starsContainer = document.querySelector('.rating-stars');
        if (starsContainer) {
            const fullStars = Math.floor(rating);
            const hasHalfStar = rating % 1 >= 0.5;
            
            let starsHtml = '';
            for (let i = 0; i < 5; i++) {
                if (i < fullStars) {
                    starsHtml += '<i class="fas fa-star"></i>';
                } else if (i === fullStars && hasHalfStar) {
                    starsHtml += '<i class="fas fa-star-half-alt"></i>';
                } else {
                    starsHtml += '<i class="far fa-star"></i>';
                }
            }
            starsContainer.innerHTML = starsHtml;
        }
    }

    handleQuickAction(event) {
        const action = event.target.dataset.action;
        
        switch (action) {
            case 'view-schedule':
                this.redirectTo('/Doctor/Schedule');
                break;
            case 'manage-patients':
                this.redirectTo('/Doctor/Patients');
                break;
            case 'update-profile':
                this.redirectTo('/Doctor/Profile');
                break;
            case 'view-ratings':
                this.redirectTo('/Doctor/Ratings');
                break;
            default:
                console.warn('Unknown action:', action);
        }
    }

    async confirmAppointment(event) {
        const appointmentId = event.target.dataset.appointmentId;
        
        try {
            const response = await fetch(`/api/appointments/${appointmentId}/confirm`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                this.showNotification('Cita confirmada exitosamente', 'success');
                event.target.closest('.appointment-card').classList.add('confirmed');
                event.target.remove();
                this.loadDashboardData(); // Refresh data
            } else {
                throw new Error('Error al confirmar la cita');
            }
        } catch (error) {
            console.error('Error confirming appointment:', error);
            this.showNotification('Error al confirmar la cita', 'error');
        }
    }

    async rescheduleAppointment(event) {
        const appointmentId = event.target.dataset.appointmentId;
        
        // Show reschedule modal or redirect to reschedule page
        this.redirectTo(`/Doctor/RescheduleAppointment/${appointmentId}`);
    }

    initializeCalendar() {
        // Initialize a simple calendar for appointments overview
        const calendarContainer = document.getElementById('appointments-calendar');
        if (calendarContainer) {
            // This would integrate with a calendar library like FullCalendar
            console.log('Initializing calendar...');
        }
    }

    setupRatingSystem() {
        document.querySelectorAll('.rating-item').forEach(item => {
            item.addEventListener('click', () => {
                const patientId = item.dataset.patientId;
                this.redirectTo(`/Doctor/PatientDetails/${patientId}`);
            });
        });
    }

    setupNotifications() {
        // Setup real-time notifications
        if (typeof signalR !== 'undefined') {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .build();

            connection.start().then(() => {
                console.log('SignalR connected for doctor notifications');
            }).catch(err => console.error('SignalR connection error:', err));

            connection.on("ReceiveNotification", (message) => {
                this.showNotification(message, 'info');
                this.updateNotificationBadge();
            });
        }
    }

    updateNotificationBadge() {
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            const currentCount = parseInt(badge.textContent) || 0;
            badge.textContent = currentCount + 1;
            badge.style.display = 'block';
        }
    }

    showNotification(message, type = 'info') {
        // Create and show notification
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show notification-toast`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
        `;
        
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    redirectTo(url) {
        window.location.href = url;
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new DoctorDashboard();
}); 