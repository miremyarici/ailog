document.addEventListener('DOMContentLoaded', function () {
    /* ==========================================
       🔔 Notification System
       ========================================== */
    const notifBtn = document.getElementById('notificationsBtn');
    const notifDropdown = document.getElementById('notificationDropdown');
    const unreadBadge = document.getElementById('unreadBadge');
    const notifList = document.getElementById('notificationList');
    const notifLoading = document.getElementById('notificationLoading');

    let notifSkip = 0;
    const notifTake = 5;
    let notifsHasMore = true;
    let notifsLoading = false;

    // Fetch Unread Count
    const fetchUnreadCount = () => {
        fetch('/Notification/GetUnreadCount')
            .then(res => res.json())
            .then(data => {
                if (data.count > 0) {
                    unreadBadge.textContent = data.count > 9 ? '9+' : data.count;
                    unreadBadge.style.display = 'flex';
                } else {
                    unreadBadge.style.display = 'none';
                }
            })
            .catch(err => console.error('Error fetching unread count:', err));
    };

    // Initial fetch
    if (notifBtn) {
        fetchUnreadCount();
        // Optional: Poll every 60 seconds
        setInterval(fetchUnreadCount, 60000);
    }

    // Toggle Dropdown
    if (notifBtn) {
        notifBtn.addEventListener('click', (e) => {
            // Ignore if clicking inside dropdown
            if (e.target.closest('#notificationDropdown')) return;

            const isOpen = notifBtn.classList.contains('open');

            // Close profile dropdown if open
            const userProfileBtn = document.getElementById('userProfileBtn');
            if (userProfileBtn) userProfileBtn.classList.remove('open');

            if (isOpen) {
                notifBtn.classList.remove('open');
            } else {
                notifBtn.classList.add('open');

                // Fetch first page of notifications
                if (notifList.children.length === 0) {
                    loadNotifications(true);
                } else {
                    // Just hide badge as we are opening it
                    unreadBadge.style.display = 'none';
                }
            }
        });
    }

    // Close when clicking outside
    document.addEventListener('click', (e) => {
        if (notifBtn && !notifBtn.contains(e.target)) {
            notifBtn.classList.remove('open');
        }
    });

    const loadNotifications = (reset = false) => {
        if (notifsLoading || (!notifsHasMore && !reset)) return;

        if (reset) {
            notifSkip = 0;
            notifsHasMore = true;
            notifList.innerHTML = '';
        }

        notifsLoading = true;
        notifLoading.style.display = 'block';

        fetch(`/Notification/GetNotifications?skip=${notifSkip}&take=${notifTake}`)
            .then(res => res.json())
            .then(data => {
                notifLoading.style.display = 'none';
                notifsLoading = false;

                const notifications = data.notifications;

                if (notifications.length < notifTake) {
                    notifsHasMore = false;
                }

                if (reset && notifications.length === 0) {
                    notifList.innerHTML = `
                        <div style="padding: 20px; text-align: center; color: var(--text-muted); font-size: 0.9rem;">
                            No notifications yet.
                        </div>
                    `;
                } else {
                    notifications.forEach(n => {
                        const item = document.createElement('a');
                        item.href = n.referenceLink || '#';
                        item.className = `notification-item ${n.isRead ? '' : 'unread'}`;
                        item.innerHTML = `
                            <div class="notification-message">${n.message}</div>
                            <div class="notification-time">${n.createdAt}</div>
                        `;
                        notifList.appendChild(item);
                    });
                }

                notifSkip += notifTake;
                // Since user opened dropdown and saw them, hide badge
                if (reset) {
                    unreadBadge.style.display = 'none';
                    // Optional: reset count endpoint side effect handles read status
                }
            })
            .catch(err => {
                console.error(err);
                notifLoading.style.display = 'none';
                notifsLoading = false;
            });
    };

    // Infinite Scroll for Notifications
    if (notifList) {
        notifList.addEventListener('scroll', () => {
            if (notifList.scrollTop + notifList.clientHeight >= notifList.scrollHeight - 20) {
                loadNotifications();
            }
        });
    }
});
