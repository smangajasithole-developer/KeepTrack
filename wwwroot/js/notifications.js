// =====================================================
// KeepTrack — Notifications Script
// Handles: button loading states for marking notifications
// as read. Since the backend uses standard form submissions,
// this simply prevents double-clicks while the page loads.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    // =====================================================
    // 1. FORM SUBMISSION LOADING STATES
    // Any button with data-submitting will show that text
    // and disable itself while the form posts.
    // =====================================================

    const forms = document.querySelectorAll(
        ".notifications-mark-all-form, .notification-read-form"
    );

    forms.forEach(function (form) {
        form.addEventListener("submit", function () {
            const button = form.querySelector("button[data-submitting]");

            if (!button) {
                return;
            }

            // Change the label to the loading text
            const message = button.dataset.submitting;

            if (message) {
                button.textContent = message;
            }

            // Prevent double-clicks
            button.style.pointerEvents = "none";
            button.style.opacity = "0.7";
        });
    });


    // =====================================================
    // 2. AUTO-FOCUS (OPTIONAL UX BOOST)
    // If the user just landed here from the header bell
    // (via the URL hash or back button), we don't need to
    // do anything else — the backend handles the redirect.
    // This block is intentionally a no-op so the file is
    // ready to expand later without breaking anything.
    // =====================================================

    // (Reserved for future enhancements)
});