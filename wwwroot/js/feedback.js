// =====================================================
// KeepTrack — Feedback Script
// Handles: live character counter, form submission
// feedback, and auto-dismiss for the success alert.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. LIVE CHARACTER COUNTER
    // Tracks how many characters the user has typed in
    // the feedback textarea (max is set by maxlength).
    // =====================================================

    const messageInput = document.getElementById("feedback-message");
    const characterLimit = document.getElementById("feedback-character-limit");

    const MAX_CHARS = 2000;

    function updateCharacterCount() {
        if (!messageInput || !characterLimit) {
            return;
        }

        const length = messageInput.value.length;

        characterLimit.textContent = `${length} / ${MAX_CHARS}`;

        // Visual warning as the user approaches the limit
        if (length >= MAX_CHARS) {
            characterLimit.classList.add("is-at-limit");
        } else if (length >= MAX_CHARS * 0.9) {
            characterLimit.classList.remove("is-at-limit");
            characterLimit.classList.add("is-near-limit");
        } else {
            characterLimit.classList.remove("is-near-limit", "is-at-limit");
        }
    }

    if (messageInput) {
        messageInput.addEventListener("input", updateCharacterCount);
        updateCharacterCount(); // Initialize on page load
    }


    // =====================================================
    // 2. FORM SUBMISSION FEEDBACK
    // Locks the submit button while the form is posting
    // so it cannot be double-clicked.
    // =====================================================

    const form = document.getElementById("feedback-form");
    const submitButton = document.getElementById("feedback-submit-button");

    if (form && submitButton) {
        form.addEventListener("submit", function () {
            const message = submitButton.dataset.submitting;

            if (message) {
                submitButton.textContent = message;
            }

            submitButton.disabled = true;
        });
    }


    // =====================================================
    // 3. AUTO-DISMISS SUCCESS ALERT
    // After a successful submission, the success message
    // fades out on its own so the page stays clean.
    // =====================================================

    const successAlert = document.getElementById("feedback-success-alert");

    if (successAlert) {
        // Wait a few seconds before starting the fade-out
        setTimeout(function () {
            successAlert.classList.add("is-hiding");

            // Remove the element from the DOM after the fade completes
            setTimeout(function () {
                successAlert.remove();
            }, 400);
        }, 4500);
    }

});