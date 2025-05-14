document.addEventListener('DOMContentLoaded', function () {
    const notificationBadge = document.getElementById('notificationBadge');
    const notificationItemsUl = document.getElementById('notificationItemsList');
    const noNotificationsMessageDiv = document.getElementById('noNotificationsMessageDisplay');
    const markAllAsReadLink = document.getElementById('markAllAsReadLink');
    const notificationDropdown = document.getElementById('notificationDropdown');

    const API_BASE_URL = '/api/notifications';
    let pollingIntervalId = null;

    async function fetchUnreadCount() {
        try {
            const response = await fetch(`${API_BASE_URL}/unreadcount`);
            if (!response.ok) {
                console.error('Error fetching unread count:', response.status);
                updateBadge(0);
                return;
            }
            const count = await response.json();
            updateBadge(count);
        } catch (error) {
            console.error('Error fetching unread count:', error);
            updateBadge(0);
        }
    }

    function updateBadge(count) {
        if (notificationBadge) {
            if (count > 0) {
                notificationBadge.textContent = count > 99 ? '99+' : count;
                notificationBadge.style.display = 'inline-block';
            } else {
                notificationBadge.style.display = 'none';
            }
        }
        if (markAllAsReadLink) {
            if (count > 0) {
                markAllAsReadLink.classList.remove('disabled');
                markAllAsReadLink.removeAttribute('aria-disabled');
                markAllAsReadLink.style.pointerEvents = 'auto';
            } else {
                markAllAsReadLink.classList.add('disabled');
                markAllAsReadLink.setAttribute('aria-disabled', 'true');
                markAllAsReadLink.style.pointerEvents = 'none';
            }
        }
    }

    async function fetchNotifications() {
        try {
            const response = await fetch(`${API_BASE_URL}/unread`);
            if (!response.ok) {
                console.error('Error fetching notifications:', response.status);
                renderNotificationItems([]);
                return;
            }
            const notifications = await response.json();
            renderNotificationItems(notifications);
        } catch (error) {
            console.error('Error fetching notifications:', error);
            renderNotificationItems([]);
        }
    }

    function renderNotificationItems(notifications) {
        if (!notificationItemsUl || !noNotificationsMessageDiv) return;

        notificationItemsUl.innerHTML = '';

        if (notifications && notifications.length > 0) {
            noNotificationsMessageDiv.style.display = 'none';
            notificationItemsUl.style.display = 'block';

            notifications.forEach(notification => {
                const listItem = document.createElement('li');
                const link = document.createElement('a');
                link.classList.add('dropdown-item', 'notification-item');
                link.href = '#';
                link.dataset.notificationId = notification.id;

                const messageDiv = document.createElement('div');
                messageDiv.classList.add('fw-normal', 'small');
                messageDiv.textContent = notification.message;

                const dateDiv = document.createElement('div');
                dateDiv.classList.add('text-muted', 'small');
                dateDiv.textContent = new Date(notification.createdAt).toLocaleString(); 
                
                link.appendChild(messageDiv);
                link.appendChild(dateDiv);
                listItem.appendChild(link);
                notificationItemsUl.appendChild(listItem);

                link.addEventListener('click', async function(e) {
                    e.preventDefault();
                    await markNotificationAsRead(notification.id);
                    this.classList.add('read');
                    this.style.backgroundColor = '#f8f9fa';
                    fetchUnreadCount();
                });
            });
        } else {
            notificationItemsUl.style.display = 'none';
            noNotificationsMessageDiv.style.display = 'block';
        }
    }

    async function markNotificationAsRead(notificationId) {
        try {
            const response = await fetch(`${API_BASE_URL}/${notificationId}/markasread`, {
                method: 'POST',
                headers: {
                    // CSRF token might be needed if not a GET request and using anti-forgery
                }
            });
            if (!response.ok) {
                console.error('Error marking notification as read:', response.status);
            }
        } catch (error) {
            console.error('Error marking notification as read:', error);
        }
    }

    async function markAllAsRead() {
        try {
            const response = await fetch(`${API_BASE_URL}/markallasread`, {
                method: 'POST',
                headers: {
                    // CSRF token might be needed
                }
            });
            if (response.ok) {
                fetchUnreadCount();
                fetchNotifications();
            } else {
                console.error('Error marking all notifications as read:', response.status);
            }
        } catch (error) {
            console.error('Error marking all notifications as read:', error);
        }
    }

    if (notificationDropdown) {
        notificationDropdown.addEventListener('show.bs.dropdown', function () {
            fetchNotifications();
        });
    }

    if (markAllAsReadLink) {
        markAllAsReadLink.addEventListener('click', function (e) {
            e.preventDefault();
            if (!this.classList.contains('disabled')) {
                markAllAsRead();
            }
        });
    }
    
    if (notificationDropdown) {
        fetchUnreadCount();
        pollingIntervalId = setInterval(fetchUnreadCount, 30000);
    }

    window.addEventListener('beforeunload', () => {
        if (pollingIntervalId) {
            clearInterval(pollingIntervalId);
        }
    });
}); 