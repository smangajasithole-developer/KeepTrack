// =====================================================
// KeepTrack — Sign Up Script
// Handles: autofocus on the first input and a loading
// state on the submit button while the form is posting.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. AUTOFOCUS
    // Focus the first name field on page load so users
    // can immediately start typing.
    // =====================================================

    const firstNameInput = document.getElementById("sign-up-firstname");

    if (firstNameInput) {
        firstNameInput.focus();
    }


    // =====================================================
    // 2. SUBMIT BUTTON LOADING STATE
    // Locks the "Sign Up" button while the form is
    // processing so the user cannot double-click.
    // =====================================================

    const form = document.getElementById("sign-up-form");
    const submitButton = document.getElementById("sign-up-submit");

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