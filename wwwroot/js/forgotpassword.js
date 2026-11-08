// =====================================================
// KeepTrack — Forgot Password Script
// Handles: code expiry countdown, 24-hour lockout
// countdown, and loading states on submit buttons.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. TIME FORMATTERS
    // =====================================================

    function formatMinutesSeconds(totalSeconds) {
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;

        return (
            `${String(minutes).padStart(2, "0")}:` +
            `${String(seconds).padStart(2, "0")}`
        );
    }

    function formatHoursMinutesSeconds(totalSeconds) {
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds % 3600) / 60);
        const seconds = totalSeconds % 60;

        return (
            `${String(hours).padStart(2, "0")}:` +
            `${String(minutes).padStart(2, "0")}:` +
            `${String(seconds).padStart(2, "0")}`
        );
    }


    // =====================================================
    // 2. COUNTDOWN STARTER
    // Ticks every 250ms until the server-provided expiry
    // (Unix milliseconds) has passed.
    // =====================================================

    function startCountdown({ containerId, timeId, finishedMessageId, formatTime }) {
        const container = document.getElementById(containerId);
        const timeElement = document.getElementById(timeId);
        const finishedMessage = document.getElementById(finishedMessageId);

        if (!container || !timeElement) {
            return;
        }

        // Both timestamps are Unix milliseconds.
        const expiryTimestamp = Number(container.dataset.expiryUnix);

        if (!Number.isFinite(expiryTimestamp) || expiryTimestamp <= 0) {
            timeElement.textContent = formatTime(0);
            return;
        }

        let intervalId = null;

        function updateCountdown() {
            const remainingMs = Math.max(0, expiryTimestamp - Date.now());
            const remainingSeconds = Math.ceil(remainingMs / 1000);

            timeElement.textContent = formatTime(remainingSeconds);

            if (remainingSeconds <= 0) {
                if (finishedMessage) {
                    finishedMessage.hidden = false;
                }

                if (intervalId !== null) {
                    window.clearInterval(intervalId);
                    intervalId = null;
                }
            }
        }

        updateCountdown();

        if (Date.now() < expiryTimestamp) {
            intervalId = window.setInterval(updateCountdown, 250);
        }
    }


    // =====================================================
    // 3. START BOTH COUNTDOWNS
    // =====================================================

    // Reset code expiry (MM:SS)
    startCountdown({
        containerId: "forgot-password-countdown",
        timeId: "forgot-password-countdown-time",
        finishedMessageId: "forgot-password-expired-message",
        formatTime: formatMinutesSeconds
    });

    // 24-hour lockout (HH:MM:SS)
    startCountdown({
        containerId: "forgot-password-lockout-countdown",
        timeId: "forgot-password-lockout-countdown-time",
        finishedMessageId: "forgot-password-lockout-finished-message",
        formatTime: formatHoursMinutesSeconds
    });


    // =====================================================
    // 4. SUBMIT BUTTON LOADING STATES
    // Locks the button while the form is posting so the
    // user cannot double-click.
    // =====================================================

    function attachSubmitLoading(formId, buttonId) {
        const form = document.getElementById(formId);
        const button = document.getElementById(buttonId);

        if (!form || !button) {
            return;
        }

        form.addEventListener("submit", function () {
            const message = button.dataset.submitting;

            if (message) {
                button.textContent = message;
            }

            button.disabled = true;
        });
    }

    attachSubmitLoading("send-code-form", "send-code-button");
    attachSubmitLoading("verify-code-form", "verify-code-button");
    attachLoading("resend-code-form", "resend-code-button");
    attachSubmitLoading("reset-password-form", "reset-password-button");

    function attachLoading(formId, buttonId) {
        attachSubmitLoading(formId, buttonId);
    }
});