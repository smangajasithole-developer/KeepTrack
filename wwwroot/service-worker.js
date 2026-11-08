self.addEventListener("push", event => {
    if (!event.data) return;

    let data = {};

    try {
        data = event.data.json();
    } catch {
        data = { title: "KeepTrack", body: event.data.text() };
    }

    event.waitUntil(
        self.registration.showNotification(data.title || "KeepTrack", {
            body: data.body || data.message || "You have a new notification.",
            icon: "/favicon.ico",
            badge: "/favicon.ico",
            data: {
                url: data.url || "/Notifications/Notifications"
            }
        })
    );
});

self.addEventListener("notificationclick", event => {
    event.notification.close();

    const targetUrl =
        event.notification.data?.url || "/Notifications/Notifications";

    event.waitUntil(
        clients.matchAll({
            type: "window",
            includeUncontrolled: true
        }).then(windowClients => {
            for (const client of windowClients) {
                if ("focus" in client) {
                    client.navigate(targetUrl);
                    return client.focus();
                }
            }

            return clients.openWindow(targetUrl);
        })
    );
});
