// =====================================================
// KeepTrack — Edit Task Script
// Handles: start-time toggle, duration-based reminder
// unit filtering, and form validation before submit.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. ELEMENT CACHE
    // =====================================================

    const startImmediatelyRadios = document.querySelectorAll(
        'input[name="Input.StartImmediately"]'
    );

    const scheduledStartFields = document.getElementById("edit-scheduled-start-fields");
    const startDateInput = document.getElementById("edit-scheduled-start-date");
    const startTimeInput = document.getElementById("edit-scheduled-start-time");
    const dueDateInput = document.getElementById("edit-task-due-date");
    const dueTimeInput = document.getElementById("edit-task-due-time");

    const reminderEnabledInput = document.getElementById("edit-reminder-enabled");
    const reminderOptions = document.getElementById("edit-task-reminder-options");
    const reminderUnitInput = document.getElementById("edit-reminder-unit");
    const reminderHourFields = document.getElementById("edit-reminder-hour-fields");
    const reminderCalendarFields = document.getElementById("edit-reminder-calendar-fields");
    const reminderDurationMessage = document.getElementById("edit-reminder-duration-message");
    const reminderValueInput = document.getElementById("edit-reminder-value");
    const reminderTimeInput = document.getElementById("edit-reminder-time");
    const reminderIntervalInput = document.getElementById("edit-reminder-interval");

    const taskForm = document.getElementById("edit-task-form");


    // =====================================================
    // 2. DATE / TIME HELPERS
    // =====================================================

    function isStartImmediate() {
        const selected = document.querySelector(
            'input[name="Input.StartImmediately"]:checked'
        );

        return selected ? selected.value === "true" : true;
    }

    function parseLocalDateTime(dateValue, timeValue) {
        if (!dateValue || !timeValue) {
            return null;
        }

        const dateTime = new Date(`${dateValue}T${timeValue}:00`);

        return Number.isNaN(dateTime.getTime()) ? null : dateTime;
    }

    function getStartDateTime() {
        if (isStartImmediate()) {
            return new Date();
        }

        return parseLocalDateTime(startDateInput.value, startTimeInput.value);
    }

    function getDueDateTime() {
        return parseLocalDateTime(dueDateInput.value, dueTimeInput.value);
    }

    function addCalendarMonths(date, months) {
        const result = new Date(date);
        const originalDay = result.getDate();

        result.setDate(1);
        result.setMonth(result.getMonth() + months);

        const lastDayOfMonth = new Date(
            result.getFullYear(),
            result.getMonth() + 1,
            0
        ).getDate();

        result.setDate(Math.min(originalDay, lastDayOfMonth));

        return result;
    }


    // =====================================================
    // 3. DURATION-BASED REMINDER UNITS
    // =====================================================

    function getAllowedReminderUnits(start, due) {
        if (!start || !due || due <= start) {
            return [];
        }

        const durationMs = due.getTime() - start.getTime();
        const twentyFourHours = 24 * 60 * 60 * 1000;

        // Up to 24 hours: Hour only
        if (durationMs <= twentyFourHours) {
            return ["Hour"];
        }

        // More than 24 hours: Hour and Day
        const units = ["Hour", "Day"];

        // After one calendar month: Month and Week
        if (due >= addCalendarMonths(start, 1)) {
            units.push("Month", "Week");
        }

        // After one calendar year: Year
        if (due >= addCalendarMonths(start, 12)) {
            units.push("Year");
        }

        return units;
    }

    function updateReminderUnitOptions(allowedUnits) {
        const options = Array.from(reminderUnitInput.options);

        options.forEach(function (option) {
            if (!option.value) {
                option.disabled = false;
                option.hidden = false;
                return;
            }

            const allowed = allowedUnits.includes(option.value);
            option.disabled = !allowed;
            option.hidden = !allowed;
        });

        // If the current unit is no longer allowed, select the first available
        if (!allowedUnits.includes(reminderUnitInput.value)) {
            reminderUnitInput.value = allowedUnits.length ? allowedUnits[0] : "";
        }
    }


    // =====================================================
    // 4. START FIELDS
    // =====================================================

    function updateStartFields() {
        const immediate = isStartImmediate();

        scheduledStartFields.hidden = immediate;

        startDateInput.required = !immediate;
        startTimeInput.required = !immediate;
    }


    // =====================================================
    // 5. REMINDER FIELDS
    // =====================================================

    function updateReminderFields() {
        const enabled = reminderEnabledInput.checked;

        reminderOptions.hidden = !enabled;

        if (!enabled) {
            reminderHourFields.hidden = true;
            reminderCalendarFields.hidden = true;
            reminderDurationMessage.hidden = true;

            reminderIntervalInput.required = false;
            reminderValueInput.required = false;
            reminderTimeInput.required = false;

            return;
        }

        const start = getStartDateTime();
        const due = getDueDateTime();
        const hasValidDuration = start !== null && due !== null && due > start;

        if (!hasValidDuration) {
            reminderHourFields.hidden = true;
            reminderCalendarFields.hidden = true;
            reminderDurationMessage.hidden = false;

            reminderIntervalInput.required = false;
            reminderValueInput.required = false;
            reminderTimeInput.required = false;

            updateReminderUnitOptions([]);

            return;
        }

        reminderDurationMessage.hidden = true;

        const allowedUnits = getAllowedReminderUnits(start, due);
        updateReminderUnitOptions(allowedUnits);

        const selectedUnit = reminderUnitInput.value;

        if (selectedUnit === "Hour") {
            reminderHourFields.hidden = false;
            reminderCalendarFields.hidden = true;

            reminderIntervalInput.required = true;
            reminderValueInput.required = false;
            reminderTimeInput.required = false;
        } else if (["Day", "Week", "Month", "Year"].includes(selectedUnit)) {
            reminderHourFields.hidden = true;
            reminderCalendarFields.hidden = false;

            reminderIntervalInput.required = false;
            reminderValueInput.required = true;
            reminderTimeInput.required = true;
        } else {
            reminderHourFields.hidden = true;
            reminderCalendarFields.hidden = true;

            reminderIntervalInput.required = false;
            reminderValueInput.required = false;
            reminderTimeInput.required = false;
        }
    }

    function refreshDateAndReminderFields() {
        updateStartFields();
        updateReminderFields();
    }


    // =====================================================
    // 6. EVENT LISTENERS
    // =====================================================

    startImmediatelyRadios.forEach(function (radio) {
        radio.addEventListener("change", refreshDateAndReminderFields);
    });

    [startDateInput, startTimeInput, dueDateInput, dueTimeInput].forEach(function (input) {
        input.addEventListener("change", refreshDateAndReminderFields);
        input.addEventListener("input", refreshDateAndReminderFields);
    });

    reminderEnabledInput.addEventListener("change", function () {
        if (reminderEnabledInput.checked) {
            const start = getStartDateTime();
            const due = getDueDateTime();

            if (!start || !due || due <= start) {
                reminderEnabledInput.checked = false;

                reminderOptions.hidden = true;
                reminderDurationMessage.hidden = false;

                alert(
                    "Please set a valid due date and time after the task start before enabling reminders."
                );

                refreshDateAndReminderFields();
                return;
            }
        }

        refreshDateAndReminderFields();
    });

    reminderUnitInput.addEventListener("change", updateReminderFields);


    // =====================================================
    // REMINDER INTERVAL INPUT
    // =====================================================

    if (reminderIntervalInput) {
        const intervalSpinner = document.querySelector(
            "#edit-reminder-hour-fields .edit-task-interval-spinner"
        );

        let selectedSegment = 0;

        function getIntervalParts() {
            const parts = reminderIntervalInput.value.split(":");

            return [
                parts[0] || "00",
                parts[1] || "00",
                parts[2] || "00"
            ];
        }

        function selectSegment(segment) {
            const positions = [
                [0, 2],
                [3, 5],
                [6, 8]
            ];

            selectedSegment = segment;
            reminderIntervalInput.focus();

            reminderIntervalInput.setSelectionRange(
                positions[segment][0],
                positions[segment][1]
            );
        }

        function updateSelectedSegment() {
            const position =
                reminderIntervalInput.selectionStart ?? 0;

            if (position <= 2) {
                selectedSegment = 0;
            } else if (position <= 5) {
                selectedSegment = 1;
            } else {
                selectedSegment = 2;
            }
        }

        function formatIntervalDigits() {
            const oldPosition =
                reminderIntervalInput.selectionStart ?? 0;

            const digitsBeforeCursor =
                reminderIntervalInput.value
                    .slice(0, oldPosition)
                    .replace(/\D/g, "").length;

            const digits = reminderIntervalInput.value
                .replace(/\D/g, "")
                .slice(0, 6);

            let formatted = "";

            if (digits.length <= 2) {
                formatted = digits;

                if (digits.length === 2) {
                    formatted += ":";
                }
            } else if (digits.length <= 4) {
                formatted =
                    digits.slice(0, 2) + ":" +
                    digits.slice(2);

                if (digits.length === 4) {
                    formatted += ":";
                }
            } else {
                formatted =
                    digits.slice(0, 2) + ":" +
                    digits.slice(2, 4) + ":" +
                    digits.slice(4, 6);
            }

            reminderIntervalInput.value = formatted;

            let newPosition = 0;
            let digitCount = 0;

            while (
                newPosition < formatted.length &&
                digitCount < digitsBeforeCursor
            ) {
                if (/\d/.test(formatted[newPosition])) {
                    digitCount++;
                }

                newPosition++;
            }

            if (
                (digitsBeforeCursor === 2 ||
                    digitsBeforeCursor === 4) &&
                formatted[newPosition] === ":"
            ) {
                newPosition++;
            }

            reminderIntervalInput.setSelectionRange(
                newPosition,
                newPosition
            );

            updateSelectedSegment();
        }

        function adjustIntervalSegment(direction) {
            const parts = getIntervalParts();
            const limits = [24, 60, 60];

            let value =
                parseInt(parts[selectedSegment], 10) || 0;

            value =
                (value + direction + limits[selectedSegment]) %
                limits[selectedSegment];

            parts[selectedSegment] =
                String(value).padStart(2, "0");

            reminderIntervalInput.value = parts.join(":");

            selectSegment(selectedSegment);
        }

        reminderIntervalInput.addEventListener(
            "input",
            formatIntervalDigits
        );

        reminderIntervalInput.addEventListener(
            "click",
            updateSelectedSegment
        );

        reminderIntervalInput.addEventListener(
            "keyup",
            updateSelectedSegment
        );

        reminderIntervalInput.addEventListener(
            "select",
            updateSelectedSegment
        );

        if (intervalSpinner) {
            intervalSpinner.addEventListener(
                "pointerdown",
                event => event.preventDefault()
            );

            intervalSpinner.addEventListener(
                "click",
                event => {
                    const button = event.target.closest(
                        "[data-interval-action]"
                    );

                    if (!button) return;

                    const direction =
                        button.dataset.intervalAction === "increase"
                            ? 1
                            : -1;

                    adjustIntervalSegment(direction);
                }
            );
        }
    }


    // =====================================================
    // 7. FORM SUBMISSION VALIDATION
    // =====================================================

    if (taskForm) {
        taskForm.addEventListener("submit", function (event) {
            // If reminders are off, let the server handle validation
            if (!reminderEnabledInput.checked) {
                return;
            }

            const start = getStartDateTime();
            const due = getDueDateTime();

            // Must have a valid start → due duration
            if (!start || !due || due <= start) {
                event.preventDefault();

                reminderDurationMessage.hidden = false;

                alert("Reminders require a due date and time after the task start.");
                return;
            }

            // Must use a unit that the duration allows
            const allowedUnits = getAllowedReminderUnits(start, due);

            if (!allowedUnits.includes(reminderUnitInput.value)) {
                event.preventDefault();
                alert("Please choose a reminder unit allowed by the task duration.");
                return;
            }

            // Hour-based: validate HH:mm:ss format and interval < duration
            if (reminderUnitInput.value === "Hour") {
                const interval = reminderIntervalInput.value.trim();
                const intervalPattern = /^\d+:[0-5]\d:[0-5]\d$/;

                if (!intervalPattern.test(interval)) {
                    event.preventDefault();
                    alert("Enter the Hour reminder interval in HH:mm:ss format, for example 01:30:00.");
                    reminderIntervalInput.focus();
                    return;
                }

                const parts = interval.split(":").map(Number);

                const intervalMs = (parts[0] * 3600 + parts[1] * 60 + parts[2]) * 1000;
                const durationMs = due.getTime() - start.getTime();

                if (intervalMs <= 0 || intervalMs >= durationMs) {
                    event.preventDefault();
                    alert("The reminder interval must be positive and shorter than the task duration.");
                    reminderIntervalInput.focus();
                }

                return;
            }

            // Calendar-based: require value >= 1 and a reminder time
            if (!reminderValueInput.value || Number(reminderValueInput.value) < 1) {
                event.preventDefault();
                alert("Enter a reminder interval of at least 1.");
                reminderValueInput.focus();
                return;
            }

            if (!reminderTimeInput.value) {
                event.preventDefault();
                alert("Please select a reminder time.");
                reminderTimeInput.focus();
            }
        });
    }


    // =====================================================
    // 8. SUBMIT BUTTON LOADING STATE
    // Locks the Save button while the form is posting.
    // =====================================================

    if (taskForm) {
        taskForm.addEventListener("submit", function () {
            const submitBtn = taskForm.querySelector("button[type='submit'][data-submitting]");

            if (!submitBtn) {
                return;
            }

            const message = submitBtn.dataset.submitting;

            if (message) {
                submitBtn.textContent = message;
            }

            submitBtn.disabled = true;
        });
    }


    // =====================================================
    // 9. INITIALISE
    // =====================================================

    refreshDateAndReminderFields();
});
