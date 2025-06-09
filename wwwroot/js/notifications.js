document.addEventListener('DOMContentLoaded', function () {
    const notificationBadge = document.getElementById('notificationBadge');
    const notificationPanel = document.getElementById('notificationPanel');
    const noNotificationsMessageDisplay = document.getElementById('noNotificationsMessageDisplay');
    const markAllAsReadLink = document.getElementById('markAllAsReadLink');
    const notificationDropdown = document.getElementById('notificationDropdown');
    const notificationDivider = document.getElementById('notificationItemsDivider');
    const notificationItemsList = document.getElementById('notificationItemsList');
    const notificationFooter = document.getElementById('notificationFooter');

    console.log('notifications.js loaded'); // Log inicial

    let areNotificationsFetched = false;

    async function fetchUnreadCount() {
        try {
            const response = await fetch('/api/notifications/unreadcount');
            if (!response.ok) {
                console.error('Error fetching unread count:', response.statusText);
                if (notificationBadge) notificationBadge.textContent = '0';
                return 0;
            }
            const count = await response.json();
            if (notificationBadge) {
                notificationBadge.textContent = count;
                notificationBadge.style.display = count > 0 ? 'inline-block' : 'none';
            }
            return count;
        } catch (error) {
            console.error('Error fetching unread count:', error);
            if (notificationBadge) notificationBadge.textContent = '0';
            return 0;
        }
    }

    async function fetchAllNotifications() {
        console.log('Fetching all notifications from /api/notifications/all...');
        try {
            const response = await fetch('/api/notifications/all');
            if (!response.ok) {
                console.error('Error fetching all notifications API:', response.status, response.statusText);
                const errorText = await response.text();
                console.error('API Error Response Text:', errorText);
                return [];
            }
            const data = await response.json();
            console.log('Received data from /api/notifications/all:', data);
            return data;
        } catch (error) {
            console.error('Error in fetchAllNotifications function:', error);
            return [];
        }
    }

    function renderNotifications(notifications) {
        console.log('renderNotifications called with:', notifications);
        if (!notificationItemsList || !noNotificationsMessageDisplay || !markAllAsReadLink || !notificationDivider || !notificationFooter) {
            console.error('renderNotifications: One or more critical panel elements not found. ItemsList:', notificationItemsList, 'NoMsg:', noNotificationsMessageDisplay, 'MarkAllLink:', markAllAsReadLink, 'Divider:', notificationDivider, 'Footer:', notificationFooter);
            return;
        }

        while (notificationItemsList.firstChild) {
            notificationItemsList.removeChild(notificationItemsList.firstChild);
        }
        console.log('Cleared previous notification items from notificationItemsList.');
        
        let unreadNotificationsExist = false;
        if (notifications && notifications.length > 0) {
            console.log(`Rendering ${notifications.length} notifications.`);
            notifications.forEach((notification, index) => {
                console.log(`Rendering notification ${index + 1}:`, notification);
                const listItem = document.createElement('li');
                listItem.classList.add('notification-item');
                listItem.dataset.notificationId = notification.id;
                
                if (notification.navigationPath) {
                    listItem.dataset.navigationPath = notification.navigationPath;
                }
                if (notification.appointmentId) {
                    listItem.dataset.appointmentId = notification.appointmentId; 
                }

                const link = document.createElement('a');
                link.classList.add('dropdown-item', 'text-wrap');
                link.href = notification.navigationPath ? notification.navigationPath : '#';
                link.style.whiteSpace = 'normal';
                link.style.wordWrap = 'break-word';
                link.style.overflowWrap = 'break-word';

                const messageP = document.createElement('p');
                messageP.classList.add('mb-0');
                messageP.textContent = notification.message;

                link.appendChild(messageP);
                listItem.appendChild(link);

                if (notification.isRead) {
                    listItem.classList.add('notification-read');
                } else {
                    listItem.classList.add('notification-unread');
                    unreadNotificationsExist = true;
                }
                notificationItemsList.appendChild(listItem); 
            });

            noNotificationsMessageDisplay.style.display = 'none';
            notificationItemsList.style.display = 'block'; 
            notificationDivider.style.display = 'block';
            notificationFooter.style.display = unreadNotificationsExist ? 'block' : 'none';
            console.log('Finished rendering notifications. All are displayed as active.');
        } else {
            console.log('No notifications to render, or notifications array is empty.');
            noNotificationsMessageDisplay.style.display = 'block';
            notificationItemsList.style.display = 'none';
            notificationDivider.style.display = 'none';
            notificationFooter.style.display = 'none';
        }
        areNotificationsFetched = true;
    }

    if (notificationDropdown) {
        notificationDropdown.addEventListener('show.bs.dropdown', async function () {
            console.log('Notification dropdown opening...');
            const notifications = await fetchAllNotifications();
            renderNotifications(notifications);
            await fetchUnreadCount(); // Ensure badge is also up-to-date
        });
    }
    
    // Ensure notification footer is hidden from the start if it exists
    if (notificationFooter) {
        notificationFooter.style.display = 'none';
    }

    if (notificationItemsList) {
        notificationItemsList.addEventListener('click', async function (event) {
            const listItem = event.target.closest('li.notification-item');
            if (!listItem) return;

            event.preventDefault(); // Prevent default link behavior, JS will handle navigation

            const notificationId = listItem.dataset.notificationId;
            const navigationPath = listItem.dataset.navigationPath; // Use navigationPath
            const isAlreadyRead = listItem.classList.contains('notification-read');

            const performNavigation = () => {
                if (navigationPath && navigationPath !== '#') {
                    window.location.href = navigationPath;
                } else {
                    // Fallback to appointmentId if navigationPath is somehow missing but appointmentId is there
                    const appointmentIdFallback = listItem.dataset.appointmentId;
                    if(appointmentIdFallback){
                        console.warn('NavigationPath missing, falling back to appointmentId for navigation.');
                        window.location.href = `/Appointments/Details/${appointmentIdFallback}`;
                    } else {
                        console.warn('No valid navigation path or appointmentId for this notification.');
                    }
                }
            };

            if (!isAlreadyRead && notificationId) {
                try {
                    const response = await fetch(`/api/notifications/${notificationId}/markasread`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' }
                    });

                    if (response.ok) {
                        listItem.classList.remove('notification-unread');
                        listItem.classList.add('notification-read');
                        await fetchUnreadCount(); 

                        const unreadInPanel = notificationItemsList.querySelectorAll('.notification-unread').length;
                        if (notificationFooter) {
                            notificationFooter.style.display = unreadInPanel > 0 ? 'block' : 'none';
                        }
                        performNavigation(); // Navigate after successful mark as read
                    } else {
                        console.error('Failed to mark notification as read', await response.text());
                        performNavigation(); // Navigate even if mark as read fails
                    }
                } catch (error) {
                    console.error('Error marking notification as read:', error);
                    performNavigation(); // Navigate on error too
                }
            } else {
                // If already read or no notificationId (should not happen for clickable items), just navigate
                performNavigation();
            }
        });
    }

    if (markAllAsReadLink) {
        markAllAsReadLink.addEventListener('click', async function (event) {
            event.preventDefault();
            try {
                const response = await fetch('/api/notifications/markallasread', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });

                if (response.ok) {
                    const notifications = await fetchAllNotifications();
                    renderNotifications(notifications); // Re-render panel, styles and "Mark all" link will update
                    await fetchUnreadCount(); // Update badge (should be 0)
                } else {
                    console.error('Failed to mark all notifications as read', await response.text());
                }
            } catch (error) {
                console.error('Error marking all notifications as read:', error);
            }
        });
    }

    // Initial fetch for badge count and periodic refresh
    fetchUnreadCount();
    setInterval(fetchUnreadCount, 30000); // Refresh badge count every 30 seconds
}); 