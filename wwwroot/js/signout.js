// =====================================================
// KeepTrack — Sign Out Script
// Handles: locks the submit button while the form is
// posting so the user cannot double-click.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    const form = document.getElementById("sign-out-form");
    const submitButton = document.getElementById("sign-out-submit");

    if (!form || !submitButton) {
        return;
    }

    form.addEventListener("submit", function () {
        const message = submitButton.dataset.submitting;

        if (message) {
            submitButton.textContent = message;
        }

        submitButton.disabled = true;
    });

});