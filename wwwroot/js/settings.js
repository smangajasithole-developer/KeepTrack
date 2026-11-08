// =====================================================
// KeepTrack — Settings Script
// Handles: accordion sections, edit profile, security
// verification modal, code expiry countdown, request
// lockout countdown, and loading states for form buttons.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. ELEMENT CACHE
    // Look up every element once so the rest of the file
    // can reference them by name without re-querying.
    // =====================================================

    // ---------- Accordion ----------
    const sectionToggles = document.querySelectorAll(".settings-section-toggle");

    // ---------- Profile editor ----------
    const editProfileButton = document.getElementById("edit-profile-button");
    const profileDisplay = document.getElementById("profile-display");
    const editProfileForm = document.getElementById("edit-profile-form");
    const cancelEditProfileButton = document.getElementById("cancel-edit-profile-button");

    // ---------- Security editor ----------
    const changeSecurityButton = document.getElementById("change-security-button");
    const securityAccountDisplay = document.getElementById("security-account-display");
    const securityChangesForm = document.getElementById("security-changes-form");
    const cancelSecurityChangesButton = document.getElementById("cancel-security-changes-button");

    // ---------- Security modal ----------
    const securityModal = document.getElementById("security-verification-modal");
    const closeSecurityModalButton = document.getElementById("close-security-modal-button");

    // ---------- Send / verify forms ----------
    const sendSecurityCodeForm = document.getElementById("send-security-code-form");
    const sendSecurityCodeButton = document.getElementById("send-security-code-button");
    const verifySecurityCodeForm = document.getElementById("verify-security-code-form");
    const verifySecurityCodeButton = document.getElementById("verify-security-code-button");
    const verificationCodeInput = document.getElementById("SecurityInput_VerificationCode");

    // ---------- Countdown / status displays ----------
    const securityCodeCountdown = document.getElementById("security-code-countdown");
    const securityCodeStatus = document.getElementById("security-code-status");
    const securityRequestCount = document.getElementById("security-request-count");

    // ---------- Timers ----------
    let countdownInterval = null;
    let lockoutInterval = null;
    let previousFocusedElement = null;


    // =====================================================
    // 2. ACCORDION
    // Only one section open at a time.
    // =====================================================

    function openSection(targetId) {
        sectionToggles.forEach(function (toggle) {
            const isTarget =
                toggle.getAttribute("aria-controls") === targetId;

            toggle.setAttribute("aria-expanded", isTarget ? "true" : "false");
        });
    }

    sectionToggles.forEach(function (toggle) {
        toggle.addEventListener("click", function () {
            const wasOpen = toggle.getAttribute("aria-expanded") === "true";

            // Close every section first
            sectionToggles.forEach(function (other) {
                other.setAttribute("aria-expanded", "false");
            });

            // Reopen the one that was clicked, unless it was already open
            if (!wasOpen) {
                toggle.setAttribute("aria-expanded", "true");
            }
        });
    });


    // =====================================================
    // 3. EDIT PROFILE
    // Toggles between the read-only profile view and the
    // editable form.
    // =====================================================

    function openProfileEditor() {
        if (!profileDisplay || !editProfileForm) {
            return;
        }

        profileDisplay.hidden = true;
        editProfileForm.hidden = false;

        openSection("settings-account-content");

        const firstNameInput = editProfileForm.querySelector(
            '[name="ProfileInput.FirstName"]'
        );

        if (firstNameInput) {
            firstNameInput.focus();
        }
    }

    function closeProfileEditor() {
        if (!profileDisplay || !editProfileForm) {
            return;
        }

        editProfileForm.reset();
        editProfileForm.hidden = true;
        profileDisplay.hidden = false;
    }

    if (editProfileButton) {
        editProfileButton.addEventListener("click", openProfileEditor);
    }

    if (cancelEditProfileButton) {
        cancelEditProfileButton.addEventListener("click", closeProfileEditor);
    }


    // =====================================================
    // 4. SECURITY VERIFICATION MODAL
    // Opens when the user clicks "Change" next to their
    // account credentials. Traps focus inside the modal.
    // =====================================================

    function openSecurityModal() {
        if (!securityModal) {
            return;
        }

        previousFocusedElement = document.activeElement;

        securityModal.hidden = false;
        document.body.classList.add("settings-modal-open");

        openSection("settings-security-content");

        // Focus the most useful control inside the modal
        if (verifySecurityCodeForm &&
            !verifySecurityCodeForm.hidden &&
            verificationCodeInput) {
            verificationCodeInput.focus();
        } else if (sendSecurityCodeButton && !sendSecurityCodeButton.disabled) {
            sendSecurityCodeButton.focus();
        } else if (closeSecurityModalButton) {
            closeSecurityModalButton.focus();
        }
    }

    function closeSecurityModal() {
        if (!securityModal) {
            return;
        }

        securityModal.hidden = true;
        document.body.classList.remove("settings-modal-open");

        // Restore focus to whatever opened the modal
        if (previousFocusedElement &&
            typeof previousFocusedElement.focus === "function") {
            previousFocusedElement.focus();
        }
    }

    if (changeSecurityButton) {
        changeSecurityButton.addEventListener("click", openSecurityModal);
    }

    if (closeSecurityModalButton) {
        closeSecurityModalButton.addEventListener("click", closeSecurityModal);
    }

    // Click the backdrop to close
    if (securityModal) {
        securityModal.addEventListener("click", function (event) {
            if (event.target === securityModal) {
                closeSecurityModal();
            }
        });
    }

    // Escape key closes the modal
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape" && securityModal && !securityModal.hidden) {
            closeSecurityModal();
        }
    });


    // =====================================================
    // 5. SECURITY FORM EDITOR
    // Shown after the verification step succeeds.
    // =====================================================

    function showSecurityEditor() {
        if (!securityChangesForm || !securityAccountDisplay) {
            return;
        }

        securityAccountDisplay.hidden = true;
        securityChangesForm.hidden = false;

        openSection("settings-security-content");

        const firstSecurityInput = securityChangesForm.querySelector(
            '[name="SecurityInput.NewUserName"]'
        );

        if (firstSecurityInput) {
            firstSecurityInput.focus();
        }
    }

    function hideSecurityEditor() {
        if (!securityChangesForm || !securityAccountDisplay) {
            return;
        }

        securityChangesForm.reset();
        securityChangesForm.hidden = true;
        securityAccountDisplay.hidden = false;
    }

    if (cancelSecurityChangesButton) {
        cancelSecurityChangesButton.addEventListener("click", hideSecurityEditor);
    }


    // =====================================================
    // 6. SIX-DIGIT CODE INPUT
    // Digits only, max 6 characters. Disables the verify
    // button until exactly six digits are entered.
    // =====================================================

    function updateVerifyButtonState() {
        if (!verificationCodeInput || !verifySecurityCodeButton) {
            return;
        }

        verifySecurityCodeButton.disabled =
            verificationCodeInput.value.length !== 6;
    }

    if (verificationCodeInput) {
        verificationCodeInput.addEventListener("input", function () {
            verificationCodeInput.value =
                verificationCodeInput.value.replace(/\D/g, "").slice(0, 6);

            updateVerifyButtonState();
        });

        updateVerifyButtonState();
    }


    // =====================================================
    // 7. CODE EXPIRY COUNTDOWN
    // Ticks once per second until the code expires.
    // =====================================================

    function stopCodeCountdown() {
        if (countdownInterval !== null) {
            clearInterval(countdownInterval);
            countdownInterval = null;
        }
    }

    function startCodeCountdown() {
        stopCodeCountdown();

        if (!securityCodeCountdown) {
            return;
        }

        const expiry = Number(securityCodeCountdown.dataset.expiresAt);

        if (!Number.isFinite(expiry) || expiry <= 0) {
            securityCodeCountdown.textContent =
                "Request a verification code to begin.";
            return;
        }

        function updateCountdown() {
            const secondsRemaining = Math.max(
                0,
                Math.ceil((expiry - Date.now()) / 1000)
            );

            securityCodeCountdown.textContent =
                secondsRemaining > 0
                    ? `Time remaining: ${secondsRemaining} second(s)`
                    : "Your code has expired. Request a new code if requests remain.";

            if (secondsRemaining <= 0) {
                stopCodeCountdown();

                if (verifySecurityCodeButton) {
                    verifySecurityCodeButton.disabled = true;
                }

                if (securityCodeStatus) {
                    securityCodeStatus.textContent =
                        "Your verification code has expired.";
                }
            }
        }

        updateCountdown();

        if (Date.now() < expiry) {
            countdownInterval = setInterval(updateCountdown, 1000);
        }
    }


    // =====================================================
    // 8. REQUEST LIMIT / LOCKOUT COUNTDOWN
    // Shows how many codes have been requested and
    // disables the send button during a lockout.
    // =====================================================

    function stopLockoutCountdown() {
        if (lockoutInterval !== null) {
            clearInterval(lockoutInterval);
            lockoutInterval = null;
        }
    }

    function startLockoutCountdown() {
        stopLockoutCountdown();

        if (!securityRequestCount) {
            return;
        }

        const lockoutUntil = Number(securityRequestCount.dataset.lockoutUntil);
        const requestsUsed = Number(securityRequestCount.dataset.requestsUsed || 0);

        function updateLockout() {
            securityRequestCount.textContent =
                `Requests used: ${requestsUsed} of 3`;

            if (!Number.isFinite(lockoutUntil) || lockoutUntil <= Date.now()) {
                stopLockoutCountdown();
                return;
            }

            const remainingSeconds = Math.ceil(
                (lockoutUntil - Date.now()) / 1000
            );

            const hours = Math.floor(remainingSeconds / 3600);
            const minutes = Math.floor((remainingSeconds % 3600) / 60);
            const seconds = remainingSeconds % 60;

            const countdownText = `${hours}h ${minutes}m ${seconds}s`;

            if (sendSecurityCodeButton) {
                sendSecurityCodeButton.disabled = true;
            }

            if (securityCodeStatus) {
                securityCodeStatus.textContent =
                    `New codes are temporarily unavailable. Try again in ${countdownText}.`;
            }

            if (remainingSeconds <= 0) {
                stopLockoutCountdown();
            }
        }

        updateLockout();

        if (Number.isFinite(lockoutUntil) && lockoutUntil > Date.now()) {
            lockoutInterval = setInterval(updateLockout, 1000);
        }
    }


    // =====================================================
    // 9. INITIAL PAGE STATE
    // Restores the state the server rendered so the page
    // looks exactly right on reload.
    // =====================================================

    // Case A — server rendered the security form visible
    // (successful verification on a previous request)
    if (securityChangesForm && !securityChangesForm.hidden) {
        showSecurityEditor();
    }

    // Case B — server rendered the verification code form visible
    // (a code is still pending)
    if (verifySecurityCodeForm && !verifySecurityCodeForm.hidden) {
        openSecurityModal();
        startCodeCountdown();
    }

    // Case C — server rendered the request lockout
    startLockoutCountdown();

    // Case D — server rendered the profile editor open
    // (validation error on a previous submission)
    if (editProfileForm && !editProfileForm.hidden && profileDisplay) {
        profileDisplay.hidden = true;
    }


    // =====================================================
    // 10. FORM SUBMISSION FEEDBACK
    // Locks the button so the user cannot double-submit
    // while the server processes the request.
    // =====================================================

    if (sendSecurityCodeForm) {
        sendSecurityCodeForm.addEventListener("submit", function () {
            if (sendSecurityCodeButton) {
                sendSecurityCodeButton.disabled = true;
                sendSecurityCodeButton.textContent = "Sending code...";
            }
        });
    }

    if (verifySecurityCodeForm) {
        verifySecurityCodeForm.addEventListener("submit", function () {
            if (verifySecurityCodeButton) {
                verifySecurityCodeButton.disabled = true;
                verifySecurityCodeButton.textContent = "Verifying...";
            }
        });
    }

});