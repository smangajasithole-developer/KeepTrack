
(() => {
    "use strict";

    const settings = document.getElementById("browser-notifications-settings");

    if (!settings) return;

    const enableButton = document.getElementById("enable-browser-notifications-button");
    const disableButton = document.getElementById("disable-browser-notifications-button");
    const status = document.getElementById("browser-notification-status");

    const publicKey = settings.dataset.vapidPublicKey;

    const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    const saveUrl = "/Settings?handler=SaveBrowserPushSubscription";
    const disableUrl = "/Settings?handler=DisableBrowserPushSubscription";

    function setStatus(message) {
        status.textContent = message;
    }

    function updateButtons(isEnabled) {
        enableButton.hidden = isEnabled;
        disableButton.hidden = !isEnabled;
    }

    function urlBase64ToUint8Array(base64String) {
        const padding = "=".repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/-/g, "+")
            .replace(/_/g, "/");

        const rawData = window.atob(base64);
        const output = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; i++) {
            output[i] = rawData.charCodeAt(i);
        }

        return output;
    }

    function hasCurrentApplicationServerKey(subscription) {
        const actualBuffer = subscription?.options?.applicationServerKey;

        if (!actualBuffer || !publicKey) {
            return false;
        }

        const expected = urlBase64ToUint8Array(publicKey);
        const actual = new Uint8Array(actualBuffer);

        return actual.length === expected.length &&
            actual.every((byte, index) => byte === expected[index]);
    }

    async function sendRequest(url, data) {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            let message = "The server could not complete the request.";

            try {
                const result = await response.json();
                if (result.error) message = result.error;
            } catch {
                // Keep the default message.
            }

            throw new Error(message);
        }

        return response.json();
    }

    async function getRegistration() {
        return navigator.serviceWorker.register("/service-worker.js");
    }

    async function refreshStatus() {
        if (!("serviceWorker" in navigator) ||
            !("PushManager" in window) ||
            !("Notification" in window)) {
            setStatus("This browser does not support browser push notifications.");
            updateButtons(false);
            enableButton.disabled = true;
            return;
        }

        if (!window.isSecureContext) {
            setStatus("Browser notifications require a secure HTTPS connection.");
            updateButtons(false);
            enableButton.disabled = true;
            return;
        }

        if (!publicKey) {
            setStatus("Browser notifications are not configured yet. The VAPID public key is missing.");
            updateButtons(false);
            enableButton.disabled = true;
            return;
        }

        if (Notification.permission === "denied") {
            setStatus("Notifications are blocked in your browser. Allow them in your browser settings to enable them here.");
            updateButtons(false);
            return;
        }

        try {
            const registration = await getRegistration();
            const subscription = await registration.pushManager.getSubscription();

            if (subscription && Notification.permission === "granted") {
                if (hasCurrentApplicationServerKey(subscription)) {
                    setStatus("Browser notifications are enabled on this device.");
                    updateButtons(true);
                } else {
                    setStatus("Your browser notification subscription needs to be refreshed. Enable notifications again to update it.");
                    updateButtons(false);
                }
            } else {
                setStatus("Browser notifications are currently disabled on this device.");
                updateButtons(false);
            }
        } catch (error) {
            console.error("Could not check browser notification status:", error);
            setStatus("Could not check browser notification status. Please refresh the page.");
            updateButtons(false);
        }
    }

    async function enableNotifications() {
        enableButton.disabled = true;
        setStatus("Checking browser notification subscription...");

        try {
            const permission = await Notification.requestPermission();

            if (permission !== "granted") {
                setStatus(
                    permission === "denied"
                        ? "Notifications were blocked. You can allow them in your browser settings."
                        : "Notification permission was not granted."
                );
                return;
            }

            const registration = await getRegistration();

            let subscription = await registration.pushManager.getSubscription();

            if (subscription && !hasCurrentApplicationServerKey(subscription)) {
                setStatus("Updating your browser notification subscription...");

                // Deactivate the old subscription in the database first.
                await sendRequest(disableUrl, {
                    endpoint: subscription.endpoint
                });

                // Remove the old browser subscription.
                const unsubscribed = await subscription.unsubscribe();

                if (!unsubscribed) {
                    throw new Error("Could not remove the outdated browser subscription. Please try again.");
                }

                subscription = null;
            }

            if (!subscription) {
                setStatus("Creating a new browser notification subscription...");

                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicKey)
                });
            }

            const subscriptionData = subscription.toJSON();

            if (!subscriptionData.endpoint ||
                !subscriptionData.keys?.p256dh ||
                !subscriptionData.keys?.auth) {
                throw new Error("The browser returned incomplete subscription information.");
            }

            await sendRequest(saveUrl, {
                endpoint: subscriptionData.endpoint,
                p256dh: subscriptionData.keys.p256dh,
                auth: subscriptionData.keys.auth
            });

            setStatus("Browser notifications have been enabled on this device.");
            updateButtons(true);
        } catch (error) {
            console.error("Could not enable browser notifications:", error);
            setStatus(error.message || "Could not enable browser notifications.");
        } finally {
            enableButton.disabled = false;
        }
    }

    async function disableNotifications() {
        disableButton.disabled = true;
        setStatus("Disabling browser notifications...");

        try {
            const registration = await navigator.serviceWorker.getRegistration("/");

            const subscription = registration
                ? await registration.pushManager.getSubscription()
                : null;

            if (subscription) {
                await sendRequest(disableUrl, {
                    endpoint: subscription.endpoint
                });

                await subscription.unsubscribe();
            }

            setStatus("Browser notifications have been disabled on this device.");
            updateButtons(false);
        } catch (error) {
            console.error("Could not disable browser notifications:", error);
            setStatus(error.message || "Could not disable browser notifications.");
        } finally {
            disableButton.disabled = false;
        }
    }

    enableButton.addEventListener("click", enableNotifications);
    disableButton.addEventListener("click", disableNotifications);

    refreshStatus();
})();
