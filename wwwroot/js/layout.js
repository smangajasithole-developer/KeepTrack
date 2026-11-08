// =====================================================
// KeepTrack — Layout Script
// Handles: theme toggle, search shortcuts, notifications.
// The theme is persisted per-session and cleared on sign-in/out.
// =====================================================

(function () {
    "use strict";

    // =====================================================
    // CONSTANTS
    // =====================================================

    const THEME_KEY = "kt-temporary-theme";
    const AUTH_STATE_KEY = "kt-theme-auth-state";


    // =====================================================
    // AUTH / APPEARANCE HELPERS
    // =====================================================

    function isAuthenticated() {
        return document.body.dataset.isAuthenticated === "true";
    }

    // Reads the "Appearance" preference from the Settings page (Light/Dark).
    // Guests always default to Light.
    function getSavedAppearance() {
        return document.body.dataset.appearance === "Dark"
            ? "dark"
            : "light";
    }

    // A temporary theme lives only for the current session — it lets users
    // preview the alternate theme without saving it in Settings.
    function getTemporaryTheme() {
        try {
            const stored = sessionStorage.getItem(THEME_KEY);

            if (!stored) {
                return null;
            }

            const temporary = JSON.parse(stored);

            // If the saved appearance has changed since the temporary
            // choice was made, treat the temporary choice as stale.
            if (temporary.savedAppearance !== getSavedAppearance()) {
                sessionStorage.removeItem(THEME_KEY);
                return null;
            }

            return temporary.theme === "dark" || temporary.theme === "light"
                ? temporary.theme
                : null;
        } catch {
            return null;
        }
    }

    function saveTemporaryTheme(theme) {
        try {
            sessionStorage.setItem(
                THEME_KEY,
                JSON.stringify({
                    theme: theme,
                    savedAppearance: getSavedAppearance()
                })
            );
        } catch {
            // Silent fail — the toggle still works for the current page.
        }
    }

    // Clears the temporary theme whenever the user signs in or out.
    function prepareSessionTheme() {
        try {
            const currentAuthState = isAuthenticated()
                ? "signed-in"
                : "guest";

            const previousAuthState = sessionStorage.getItem(AUTH_STATE_KEY);

            if (previousAuthState !== currentAuthState) {
                sessionStorage.removeItem(THEME_KEY);
                sessionStorage.setItem(AUTH_STATE_KEY, currentAuthState);
            }
        } catch {
            // Silent fail — fall back to the saved appearance or Light.
        }
    }


    // =====================================================
    // THEME
    // =====================================================

    function applyTheme(theme) {
        const isDark = theme === "dark";

        document.body.classList.toggle("dark-mode", isDark);

        const button = document.getElementById("theme-toggle-button");
        const icon = document.getElementById("theme-toggle-icon");
        const label = document.getElementById("theme-toggle-label");

        if (icon) {
            icon.textContent = isDark ? "🌙" : "☀️";
        }

        if (label) {
            label.textContent = isDark ? "Night" : "Light";
        }

        if (button) {
            button.setAttribute("aria-pressed", String(isDark));
            button.setAttribute(
                "aria-label",
                isDark ? "Switch to Light mode" : "Switch to Night mode"
            );
            button.title = isDark
                ? "Switch to Light mode"
                : "Switch to Night mode";
        }
    }

    function initialiseTheme() {
        prepareSessionTheme();

        const button = document.getElementById("theme-toggle-button");

        // Priority: temporary session override → saved preference → light.
        const theme = getTemporaryTheme() ||
            (isAuthenticated() ? getSavedAppearance() : "light");

        applyTheme(theme);

        if (!button) {
            return;
        }

        button.addEventListener("click", function () {
            const isDark = document.body.classList.contains("dark-mode");
            const nextTheme = isDark ? "light" : "dark";

            saveTemporaryTheme(nextTheme);
            applyTheme(nextTheme);
        });
    }


    // =====================================================
    // SEARCH SHORTCUTS
    // =====================================================

    function initialiseSearchShortcuts() {
        const searchInput = document.querySelector(".search-input");

        if (!searchInput) {
            return;
        }

        document.addEventListener("keydown", function (event) {
            const isSearchShortcut =
                (event.ctrlKey || event.metaKey) &&
                event.key.toLowerCase() === "k";

            if (isSearchShortcut) {
                event.preventDefault();
                searchInput.focus();
                searchInput.select();
            }

            if (
                event.key === "Escape" &&
                document.activeElement === searchInput
            ) {
                searchInput.value = "";
                searchInput.blur();
            }
        });
    }


    // =====================================================
    // NOTIFICATION DROPDOWN
    // =====================================================

    function initialiseNotificationDropdown() {
        const menu = document.getElementById("notification-menu");
        const button = document.getElementById("notification-button");
        const dropdown = document.getElementById("notification-dropdown");

        // Only present when the user is signed in.
        if (!menu || !button || !dropdown) {
            return;
        }

        function setDropdownOpen(isOpen) {
            dropdown.hidden = !isOpen;
            button.setAttribute("aria-expanded", String(isOpen));
        }

        button.addEventListener("click", function (event) {
            event.stopPropagation();

            const isOpen =
                button.getAttribute("aria-expanded") === "true";

            setDropdownOpen(!isOpen);
        });

        // Close on outside click.
        document.addEventListener("click", function (event) {
            if (!menu.contains(event.target)) {
                setDropdownOpen(false);
            }
        });

        // Close on Escape, returning focus to the button.
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                const wasOpen =
                    button.getAttribute("aria-expanded") === "true";

                setDropdownOpen(false);

                if (wasOpen) {
                    button.focus();
                }
            }
        });
    }


    // =====================================================
    // BOOTSTRAP
    // =====================================================

    function initialiseLayout() {
        initialiseTheme();
        initialiseSearchShortcuts();
        initialiseNotificationDropdown();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialiseLayout);
    } else {
        initialiseLayout();
    }
})();