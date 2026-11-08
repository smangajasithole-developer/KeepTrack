// =====================================================
// KeepTrack — Sign In Script
// Handles: autofocus on the first input and a loading
// state on the submit button while the form is posting.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. AUTOFOCUS
    // Focus the email input on page load so users can
    // immediately start typing.
    // =====================================================

    const emailInput = document.getElementById("sign-in-email");

    if (emailInput) {
        emailInput.focus();
    }


    // =====================================================
    // 2. SUBMIT BUTTON LOADING STATE
    // Locks the "Sign In" button while the form is
    // processing so the user cannot double-click.
    // =====================================================

    const form = document.getElementById("sign-in-form");
    const submitButton = document.getElementById("sign-in-submit");

    if (form && submitButton) {
        form.addEventListener("submit", function () {
            const message = submitButton.dataset.submitting;

            if (message) {
                submitButton.textContent = message;
            }

            submitButton.disabled = true;
        });
    }

});