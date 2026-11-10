
document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    const form = document.getElementById("add-task-form");

    if (!form) {
        return;
    }

    // =====================================================
    // ELEMENTS
    // =====================================================

    const startImmediatelyInputs = document.querySelectorAll(
        'input[name="Input.StartImmediately"]'
    );

    const scheduledStartFields = document.getElementById(
        "scheduled-start-fields"
    );

    const startDateInput = document.getElementById(
        "scheduled-start-date"
    );

    const startDateDisplay = document.getElementById(
        "scheduled-start-date-display"
    );

    const startDateHelp = document.getElementById(
        "scheduled-start-date-help"
    );

    const startTimeInput = document.getElementById(
        "scheduled-start-time"
    );

    const dueDateInput = document.getElementById(
        "task-due-date"
    );

    const dueDateDisplay = document.getElementById(
        "task-due-date-display"
    );

    const dueDateHelp = document.getElementById(
        "task-due-date-help"
    );

    const dueTimeInput = document.getElementById(
        "task-due-time"
    );

    const dateFormatInput = document.getElementById(
        "user-date-format"
    );

    const dateFormat = dateFormatInput?.value || "yyyy/MM/dd";

    const reminderEnabled = document.getElementById(
        "reminder-enabled"
    );

    const reminderOptions = document.getElementById(
        "reminder-options"
    );

    const reminderRequirementMessage = document.getElementById(
        "reminder-requirement-message"
    );

    const reminderDurationMessage = document.getElementById(
        "reminder-duration-message"
    );

    const reminderValueField = document.getElementById(
        "reminder-value-field"
    );

    const reminderValueInput = document.getElementById(
        "reminder-value"
    );

    const reminderUnitField = document.getElementById(
        "reminder-unit-field"
    );

    const reminderUnitSelect = document.getElementById(
        "reminder-unit"
    );

    const reminderTimeField = document.getElementById(
        "reminder-time-field"
    );

    const reminderTimeInput = document.getElementById(
        "reminder-time"
    );

    const reminderIntervalField = document.getElementById(
        "reminder-interval-field"
    );

    const reminderIntervalInput = document.getElementById(
        "reminder-interval"
    );

    const reminderValueLabel = document.getElementById(
        "reminder-value-label"
    );

    // =====================================================
    // DATE FORMAT HELPERS
    // =====================================================

    const monthNames = [
        "January", "February", "March", "April",
        "May", "June", "July", "August",
        "September", "October", "November", "December"
    ];

    const shortMonthNames = [
        "Jan", "Feb", "Mar", "Apr",
        "May", "Jun", "Jul", "Aug",
        "Sep", "Oct", "Nov", "Dec"
    ];

    function padNumber(value) {
        return String(value).padStart(2, "0");
    }

    function getDateExample() {
        switch (dateFormat) {
            case "dd/MM/yyyy":
                return "24/12/2026";

            case "dd MMM yyyy":
                return "24 Dec 2026";

            case "yyyy MMM dd":
                return "2026 Dec 24";

            case "MMMM dd, yyyy":
                return "December 24, 2026";

            case "yyyy/MM/dd":
            default:
                return "2026/12/24";
        }
    }

    const dateExample = getDateExample();

    function getDateHelpText(isDueDate = false) {
        if (isDueDate) {
            return `e.g. ${dateExample}`;
        }

        return `Enter a date like the example shown. ${dateExample}`;
    }

    const dueDateExample = document.getElementById(
        "task-due-date-example"
    );

    if (dueDateExample) {
        dueDateExample.textContent = dateExample;
    }

    function updateDateInstructions(displayInput, helpElement) {
        if (displayInput) {
            displayInput.placeholder = dateExample;

            displayInput.setAttribute(
                "aria-label",
                displayInput === startDateDisplay
                    ? "Choose scheduled start date"
                    : "Choose due date"
            );
        }

        if (helpElement) {
            helpElement.textContent = getDateHelpText(
                helpElement === dueDateHelp
            );
        }
    }

    updateDateInstructions(startDateDisplay, startDateHelp);
    updateDateInstructions(dueDateDisplay, dueDateHelp);

    function formatDate(isoDate) {
        if (!isoDate) {
            return "";
        }

        const parts = isoDate.split("-").map(Number);

        if (
            parts.length !== 3 ||
            parts.some(Number.isNaN)
        ) {
            return "";
        }

        const [year, month, day] = parts;

        if (
            month < 1 ||
            month > 12 ||
            day < 1 ||
            day > new Date(Date.UTC(year, month, 0)).getUTCDate()
        ) {
            return "";
        }

        const dd = padNumber(day);
        const MM = padNumber(month);
        const MMM = shortMonthNames[month - 1];
        const MMMM = monthNames[month - 1];

        switch (dateFormat) {
            case "dd/MM/yyyy":
                return `${dd}/${MM}/${year}`;

            case "dd MMM yyyy":
                return `${dd} ${MMM} ${year}`;

            case "yyyy MMM dd":
                return `${year} ${MMM} ${dd}`;

            case "MMMM dd, yyyy":
                return `${MMMM} ${dd}, ${year}`;

            case "yyyy/MM/dd":
            default:
                return `${year}/${MM}/${dd}`;
        }
    }

    function parsePreferredDate(value) {
        if (!value || !value.trim()) {
            return null;
        }

        const text = value.trim();

        let year;
        let month;
        let day;
        let match;

        switch (dateFormat) {
            case "yyyy/MM/dd":
                match = text.match(
                    /^(\d{4})\/(\d{1,2})\/(\d{1,2})$/
                );

                if (match) {
                    [, year, month, day] = match;
                }
                break;

            case "dd/MM/yyyy":
                match = text.match(
                    /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/
                );

                if (match) {
                    [, day, month, year] = match;
                }
                break;

            case "dd MMM yyyy":
                match = text.match(
                    /^(\d{1,2})\s+([A-Za-z]{3})\s+(\d{4})$/i
                );

                if (match) {
                    day = match[1];

                    month = shortMonthNames.findIndex(
                        name =>
                            name.toLowerCase() ===
                            match[2].toLowerCase()
                    ) + 1;

                    year = match[3];
                }
                break;

            case "yyyy MMM dd":
                match = text.match(
                    /^(\d{4})\s+([A-Za-z]{3})\s+(\d{1,2})$/i
                );

                if (match) {
                    year = match[1];

                    month = shortMonthNames.findIndex(
                        name =>
                            name.toLowerCase() ===
                            match[2].toLowerCase()
                    ) + 1;

                    day = match[3];
                }
                break;

            case "MMMM dd, yyyy":
                match = text.match(
                    /^([A-Za-z]+)\s+(\d{1,2}),\s*(\d{4})$/i
                );

                if (match) {
                    month = monthNames.findIndex(
                        name =>
                            name.toLowerCase() ===
                            match[1].toLowerCase()
                    ) + 1;

                    day = match[2];
                    year = match[3];
                }
                break;
        }

        if (!year || !month || !day) {
            return null;
        }

        year = Number(year);
        month = Number(month);
        day = Number(day);

        const date = new Date(
            Date.UTC(year, month - 1, day)
        );

        if (
            date.getUTCFullYear() !== year ||
            date.getUTCMonth() !== month - 1 ||
            date.getUTCDate() !== day
        ) {
            return null;
        }

        return `${year}-${padNumber(month)}-${padNumber(day)}`;
    }

    function getInvalidDateMessage(displayInput) {
        return getDateHelpText(displayInput === dueDateDisplay);
    }

    function connectDateField(displayInput, nativeInput, helpElement) {
        if (!displayInput || !nativeInput) {
            return;
        }

        updateDateInstructions(displayInput, helpElement);

        displayInput.value = formatDate(nativeInput.value);

        nativeInput.addEventListener("change", () => {
            displayInput.value = formatDate(nativeInput.value);
            displayInput.setCustomValidity("");
            updateReminderAvailability();
        });

        displayInput.addEventListener("change", () => {
            const parsed = parsePreferredDate(displayInput.value);

            if (!displayInput.value.trim()) {
                nativeInput.value = "";
                displayInput.setCustomValidity("");
                updateReminderAvailability();
                return;
            }

            if (!parsed) {
                displayInput.setCustomValidity(
                    getInvalidDateMessage(displayInput)
                );

                displayInput.reportValidity();
                return;
            }

            displayInput.setCustomValidity("");
            nativeInput.value = parsed;
            displayInput.value = formatDate(parsed);

            nativeInput.dispatchEvent(
                new Event("change", { bubbles: true })
            );

            updateReminderAvailability();
        });

        displayInput.addEventListener("input", () => {
            displayInput.setCustomValidity("");
        });
    }

    // =====================================================
    // CALENDAR PICKER
    // =====================================================

    document.querySelectorAll("[data-date-picker-target]").forEach(button => {
        button.addEventListener("click", () => {
            const targetId = button.dataset.datePickerTarget;
            const nativeInput = document.getElementById(targetId);

            if (!nativeInput) {
                return;
            }

            if (typeof nativeInput.showPicker === "function") {
                nativeInput.showPicker();
            } else {
                nativeInput.focus();
                nativeInput.click();
            }
        });
    });

    // =====================================================
    // START OPTION
    // =====================================================

    function isStartImmediate() {
        const selected = document.querySelector(
            'input[name="Input.StartImmediately"]:checked'
        );

        return !selected || selected.value === "true";
    }

    function updateStartFields() {
        if (!scheduledStartFields) {
            return;
        }

        const scheduled = !isStartImmediate();

        scheduledStartFields.hidden = !scheduled;

        if (startDateDisplay) {
            startDateDisplay.required = scheduled;
        }

        if (startDateInput) {
            startDateInput.required = scheduled;
        }

        if (startTimeInput) {
            startTimeInput.required = scheduled;
        }

        updateReminderAvailability();
    }

    // =====================================================
    // DEVICE-LOCAL TIME HELPERS
    // =====================================================

    function getDeviceLocalNow() {
        const now = new Date();

        return new Date(Date.UTC(
            now.getFullYear(),
            now.getMonth(),
            now.getDate(),
            now.getHours(),
            now.getMinutes(),
            now.getSeconds(),
            now.getMilliseconds()
        ));
    }

    // =====================================================
    // PARSE TASK DATE AND TIME
    // =====================================================

    function parseLocalDateTime(dateValue, timeValue) {
        if (!dateValue || !timeValue) {
            return null;
        }

        const dateParts = dateValue.split("-").map(Number);
        const timeParts = timeValue.split(":").map(Number);

        if (
            dateParts.length !== 3 ||
            timeParts.length < 2 ||
            dateParts.some(Number.isNaN) ||
            timeParts.some(Number.isNaN)
        ) {
            return null;
        }

        const [year, month, day] = dateParts;
        const [hours, minutes] = timeParts;
        const seconds = timeParts[2] || 0;

        const result = new Date(Date.UTC(
            year,
            month - 1,
            day,
            hours,
            minutes,
            seconds
        ));

        if (
            result.getUTCFullYear() !== year ||
            result.getUTCMonth() !== month - 1 ||
            result.getUTCDate() !== day ||
            result.getUTCHours() !== hours ||
            result.getUTCMinutes() !== minutes
        ) {
            return null;
        }

        return result;
    }

    function getTaskDates() {
        const start = isStartImmediate()
            ? getDeviceLocalNow()
            : parseLocalDateTime(
                startDateInput?.value,
                startTimeInput?.value
            );

        const due = parseLocalDateTime(
            dueDateInput?.value,
            dueTimeInput?.value
        );

        return { start, due };
    }

    function getTaskDuration() {
        const { start, due } = getTaskDates();

        if (!start || !due) {
            return null;
        }

        const duration = due.getTime() - start.getTime();

        if (duration <= 0) {
            return null;
        }

        return {
            start,
            due,
            milliseconds: duration,
            hours: duration / (1000 * 60 * 60)
        };
    }

    // =====================================================
    // REMINDER UNIT RULES
    // =====================================================

    function getAllowedUnits(duration) {
        const units = ["Hour"];

        if (duration.hours <= 24) {
            return units;
        }

        units.push("Day");

        const oneMonthLater = new Date(duration.start);
        oneMonthLater.setUTCMonth(
            oneMonthLater.getUTCMonth() + 1
        );

        if (duration.due >= oneMonthLater) {
            units.push("Week", "Month");
        }

        const oneYearLater = new Date(duration.start);
        oneYearLater.setUTCFullYear(
            oneYearLater.getUTCFullYear() + 1
        );

        if (duration.due >= oneYearLater) {
            units.push("Year");
        }

        return units;
    }

    function getDurationMessage(duration, units) {
        if (!duration) {
            return "Add valid task start and due dates to see available reminder options.";
        }

        if (duration.hours <= 24) {
            return "This task lasts 24 hours or less. Hourly reminders are available.";
        }

        const unitLabels = {
            Hour: "Hours",
            Day: "Days",
            Week: "Weeks",
            Month: "Months",
            Year: "Years"
        };

        const availableLabels = units
            .map(unit => unitLabels[unit])
            .filter(Boolean)
            .join(", ");

        return `Available reminder frequencies: ${availableLabels}.`;
    }

    // =====================================================
    // UPDATE REMINDER UNIT OPTIONS
    // =====================================================

    function updateReminderUnits(allowedUnits) {
        if (!reminderUnitSelect) {
            return;
        }

        const previousValue = reminderUnitSelect.value;

        Array.from(reminderUnitSelect.options).forEach(option => {
            if (option.value === "") {
                option.hidden = false;
                option.disabled = false;
                option.textContent = "Choose frequency";
                return;
            }

            const allowed = allowedUnits.includes(option.value);

            option.hidden = !allowed;
            option.disabled = !allowed;
        });

        if (!allowedUnits.includes(previousValue)) {
            reminderUnitSelect.value = allowedUnits[0] || "";
        }
    }

    // =====================================================
    // SHOW THE CORRECT REMINDER INPUTS
    // =====================================================

    function updateReminderInputs() {
        if (!reminderEnabled || !reminderUnitSelect) {
            return;
        }

        const enabled = reminderEnabled.checked;
        const unit = reminderUnitSelect.value;

        if (reminderOptions) {
            reminderOptions.hidden = !enabled;
        }

        if (reminderRequirementMessage) {
            reminderRequirementMessage.hidden = enabled;
        }

        if (!enabled) {
            return;
        }

        const duration = getTaskDuration();
        const shortDuration = duration && duration.hours <= 24;

        if (shortDuration) {
            reminderUnitSelect.value = "Hour";
        }

        const effectiveUnit = reminderUnitSelect.value;

        const isValidUnit = [
            "Hour",
            "Day",
            "Week",
            "Month",
            "Year"
        ].includes(effectiveUnit);

        const showInterval = effectiveUnit === "Hour";
        const showCount = isValidUnit && !showInterval;
        const showUnit = !shortDuration;

        if (reminderUnitField) {
            reminderUnitField.hidden = !showUnit;
        }

        if (reminderValueField) {
            reminderValueField.hidden = !showCount;
        }

        if (reminderTimeField) {
            reminderTimeField.hidden = !showCount;
        }

        if (reminderIntervalField) {
            reminderIntervalField.hidden = !showInterval;
        }

        if (reminderValueInput) {
            reminderValueInput.required = showCount;
        }

        if (reminderTimeInput) {
            reminderTimeInput.required = showCount;
        }

        if (reminderIntervalInput) {
            reminderIntervalInput.required = showInterval;
        }

        if (reminderValueLabel) {
            reminderValueLabel.textContent = "Remind me every";
        }
    }

    // =====================================================
    // CHECK WHETHER REMINDERS CAN BE ENABLED
    // =====================================================

    function updateReminderAvailability() {
        if (
            !reminderEnabled ||
            !dueDateInput ||
            !dueTimeInput
        ) {
            return;
        }

        const duration = getTaskDuration();

        const hasDueDateTime =
            Boolean(dueDateInput.value) &&
            Boolean(dueTimeInput.value);

        const scheduledStartValid =
            isStartImmediate() ||
            Boolean(
                startDateInput?.value &&
                startTimeInput?.value
            );

        const canEnable =
            hasDueDateTime &&
            scheduledStartValid &&
            Boolean(duration);

        if (!canEnable) {
            reminderEnabled.checked = false;
        }

        reminderEnabled.disabled = !canEnable;

        if (reminderRequirementMessage) {
            reminderRequirementMessage.hidden = canEnable;

            if (!hasDueDateTime) {
                reminderRequirementMessage.textContent =
                    "Add a valid due date and due time before enabling reminders.";
            } else if (!scheduledStartValid) {
                reminderRequirementMessage.textContent =
                    "Add a valid scheduled start date and time before enabling reminders.";
            } else if (!duration) {
                reminderRequirementMessage.textContent =
                    "The due date and time must be after the task start.";
            }
        }

        if (!duration) {
            if (reminderOptions) {
                reminderOptions.hidden = true;
            }

            if (reminderDurationMessage) {
                reminderDurationMessage.textContent =
                    getDurationMessage(null, []);
            }

            updateReminderInputs();
            return;
        }

        const allowedUnits = getAllowedUnits(duration);

        updateReminderUnits(allowedUnits);

        if (reminderDurationMessage) {
            reminderDurationMessage.textContent =
                getDurationMessage(duration, allowedUnits);
        }

        updateReminderInputs();
    }

    // =====================================================
    // CONNECT DATE FIELDS
    // =====================================================

    connectDateField(
        startDateDisplay,
        startDateInput,
        startDateHelp
    );

    connectDateField(
        dueDateDisplay,
        dueDateInput,
        dueDateHelp
    );

    // =====================================================
    // EVENT LISTENERS
    // =====================================================

    startImmediatelyInputs.forEach(input => {
        input.addEventListener("change", updateStartFields);
    });

    [
        startDateInput,
        startTimeInput,
        dueDateInput,
        dueTimeInput
    ].forEach(input => {
        if (input) {
            input.addEventListener(
                "change",
                updateReminderAvailability
            );

            input.addEventListener(
                "input",
                updateReminderAvailability
            );
        }
    });

    if (reminderEnabled) {
        reminderEnabled.addEventListener("change", () => {
            updateReminderAvailability();
            updateReminderInputs();
        });
    }

    if (reminderUnitSelect) {
        reminderUnitSelect.addEventListener(
            "change",
            updateReminderInputs
        );
    }

    // =====================================================
    // VALIDATE DATES BEFORE SUBMITTING
    // =====================================================

    form.addEventListener("submit", event => {
        const dateFields = [
            {
                display: startDateDisplay,
                native: startDateInput
            },
            {
                display: dueDateDisplay,
                native: dueDateInput
            }
        ];

        for (const field of dateFields) {
            if (!field.display || !field.native) {
                continue;
            }

            if (!field.display.value.trim()) {
                field.native.value = "";
                field.display.setCustomValidity("");
                continue;
            }

            const parsed = parsePreferredDate(
                field.display.value
            );

            if (!parsed) {
                event.preventDefault();

                field.display.setCustomValidity(
                    getInvalidDateMessage(field.display)
                );

                field.display.reportValidity();
                field.display.focus();
                return;
            }

            field.display.setCustomValidity("");
            field.native.value = parsed;
        }
    });



    // =====================================================
    // REMINDER INTERVAL INPUT
    // =====================================================

    if (reminderIntervalInput) {
        const intervalSpinner = document.querySelector(
            "#reminder-interval-field .add-task-interval-spinner"
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

            // Move the caret past an automatically inserted colon.
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
    // INITIALIZE
    // =====================================================

    updateStartFields();
    updateReminderAvailability();
    updateReminderInputs();
});
