
 // =====================================================
// KeepTrack — Dashboard Script
// Handles: task calendar rendering + task-status doughnut.
// All logic reads from hidden data feeds in Dashboard.cshtml.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    // =====================================================
    // USER SETTINGS
    // =====================================================

    const dateFormatElement = document.getElementById("user-date-format");
    const userDateFormat = dateFormatElement?.value || "yyyy/MM/dd";

    // =====================================================
    // LOCAL DATE + TIME
    // =====================================================

    function getUserWallClockNow() {
        const now = new Date();

        return {
            year: now.getFullYear(),
            month: now.getMonth() + 1,
            day: now.getDate(),
            hour: now.getHours(),
            minute: now.getMinutes(),
            second: now.getSeconds()
        };
    }

    function padNumber(value) {
        return String(value).padStart(2, "0");
    }

    function parseWallClock(value) {
        if (!value) {
            return null;
        }

        const match = value.match(
            /^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2})(?::(\d{2}))?)?$/
        );

        if (!match) {
            return null;
        }

        return {
            year: Number(match[1]),
            month: Number(match[2]),
            day: Number(match[3]),
            hour: Number(match[4] || 0),
            minute: Number(match[5] || 0),
            second: Number(match[6] || 0)
        };
    }

    // =====================================================
    // DATE FORMATTING
    // =====================================================

    function formatWallClockDate(value) {
        const date = typeof value === "string"
            ? parseWallClock(value)
            : value;

        if (!date) {
            return "";
        }

        const year = String(date.year);
        const month = padNumber(date.month);
        const day = padNumber(date.day);

        const monthNames = [
            "January", "February", "March", "April",
            "May", "June", "July", "August",
            "September", "October", "November", "December"
        ];

        const monthShortNames = [
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        ];

        switch (userDateFormat) {
            case "dd/MM/yyyy":
                return `${day}/${month}/${year}`;

            case "dd MMM yyyy":
                return `${day} ${monthShortNames[date.month - 1]} ${year}`;

            case "yyyy MMM dd":
                return `${year} ${monthShortNames[date.month - 1]} ${day}`;

            case "MMMM dd, yyyy":
                return `${monthNames[date.month - 1]} ${day}, ${year}`;

            case "yyyy/MM/dd":
            default:
                return `${year}/${month}/${day}`;
        }
    }

    function formatWallClockTime(value) {
        const date = typeof value === "string"
            ? parseWallClock(value)
            : value;

        if (!date) {
            return "";
        }

        return [
            padNumber(date.hour),
            padNumber(date.minute),
            padNumber(date.second)
        ].join(":");
    }

    function formatWallClockDateTime(value) {
        if (!value) {
            return "Not scheduled";
        }

        const date = parseWallClock(value);

        if (!date) {
            return "Not scheduled";
        }

        return `${formatWallClockDate(date)} at ${formatWallClockTime(date)}`;
    }

    function compareWallClockValues(first, second) {
        const a = typeof first === "string" ? parseWallClock(first) : first;
        const b = typeof second === "string" ? parseWallClock(second) : second;

        if (!a || !b) {
            return 0;
        }

        const firstValue = [
            a.year, a.month, a.day, a.hour, a.minute, a.second
        ];

        const secondValue = [
            b.year, b.month, b.day, b.hour, b.minute, b.second
        ];

        for (let i = 0; i < firstValue.length; i++) {
            if (firstValue[i] < secondValue[i]) return -1;
            if (firstValue[i] > secondValue[i]) return 1;
        }

        return 0;
    }

    function getDateKey(date) {
        return [
            date.year,
            padNumber(date.month),
            padNumber(date.day)
        ].join("-");
    }

    function getMonthLabel(year, month) {
        const date = new Date(year, month - 1, 1);

        return new Intl.DateTimeFormat(undefined, {
            month: "long",
            year: "numeric"
        }).format(date);
    }

    // =====================================================
    // TASK DATA
    // =====================================================

    const calendarElement = document.getElementById("task-calendar");
    const monthLabel = document.getElementById("calendar-month-label");
    const previousButton = document.getElementById("calendar-previous");
    const nextButton = document.getElementById("calendar-next");
    const todayButton = document.getElementById("calendar-today");
    const emptyMessage = document.getElementById("calendar-empty-message");

    const taskDataContainer = document.getElementById("calendar-task-data");

    const tasks = [];

    if (taskDataContainer) {
        taskDataContainer.querySelectorAll("[data-calendar-task]").forEach(function (element) {
            tasks.push({
                id: element.dataset.taskId || "",
                title: element.dataset.title || "Untitled task",
                description: element.dataset.description || "",
                start: element.dataset.start || "",
                due: element.dataset.due || "",
                startImmediately: element.dataset.startImmediately === "true",
                completed: element.dataset.completed === "true"
            });
        });
    }

    // =====================================================
    // CALENDAR STATE
    // =====================================================

    const initialNow = getUserWallClockNow();

    let displayedYear = initialNow.year;
    let displayedMonth = initialNow.month;
    let selectedDateKey = null;

    // Retain the selected day's events for the Back button.
    let currentDayDateKey = null;
    let currentDayEvents = [];

    // =====================================================
    // CALENDAR EVENT BUILDERS
    // =====================================================

    function getTaskEvents() {
        const events = [];

        tasks.forEach(function (task) {
            // Start indicator.
            if (task.start) {
                const startDate = parseWallClock(task.start);

                if (startDate) {
                    events.push({
                        task: task,
                        type: "start",
                        date: startDate,
                        dateKey: getDateKey(startDate)
                    });
                }
            } else if (task.startImmediately) {
                const now = getUserWallClockNow();

                events.push({
                    task: task,
                    type: "start",
                    date: now,
                    dateKey: getDateKey(now)
                });
            }

            // The second indicator represents the deadline state:
            // Due, Overdue, or Completed.
            if (task.due) {
                const dueDate = parseWallClock(task.due);

                if (dueDate) {
                    events.push({
                        task: task,
                        type: "due",
                        date: dueDate,
                        dateKey: getDateKey(dueDate)
                    });
                }
            }
        });

        return events;
    }

    function isTaskOverdue(task) {
        if (task.completed || !task.due) {
            return false;
        }

        const dueDate = parseWallClock(task.due);

        if (!dueDate) {
            return false;
        }

        // A task becomes overdue when its deadline is reached
        // or passed and it remains incomplete.
        return compareWallClockValues(
            dueDate,
            getUserWallClockNow()
        ) <= 0;
    }

function getEventClass(event) {
    // A Start indicator always represents the task's start.
    if (event.type === "start") {
        return "calendar-start-event";
    }

    // Only the due/outcome indicator changes with the task's status.
    if (event.task.completed) {
        return "calendar-completed-event";
    }

    if (isTaskOverdue(event.task)) {
        return "calendar-overdue-event";
    }

    return "calendar-due-event";
}

    // Determine the text displayed on each indicator.
    function getEventLabel(event) {
        const time = formatWallClockTime(event.date);

        let label;

        if (event.type === "start") {
            label = "Starts";
        } else if (event.task.completed) {
            label = "Completed";
        } else if (isTaskOverdue(event.task)) {
            label = "Overdue";
        } else {
            label = "Due";
        }

        return `${time} · ${label}: ${event.task.title}`;
    }

    // =====================================================
    // CALENDAR RENDERING
    // =====================================================

    function renderCalendar() {
        if (!calendarElement || !monthLabel) {
            return;
        }

        calendarElement.innerHTML = "";
        monthLabel.textContent = getMonthLabel(
            displayedYear,
            displayedMonth
        );

        const firstDay = new Date(
            displayedYear,
            displayedMonth - 1,
            1
        ).getDay();

        const daysInMonth = new Date(
            displayedYear,
            displayedMonth,
            0
        ).getDate();

        const todayKey = getDateKey(getUserWallClockNow());
        const eventsByDate = new Map();

        getTaskEvents().forEach(function (event) {
            if (!eventsByDate.has(event.dateKey)) {
                eventsByDate.set(event.dateKey, []);
            }

            eventsByDate.get(event.dateKey).push(event);
        });

        eventsByDate.forEach(function (dayEvents) {
            dayEvents.sort(function (a, b) {
                const timeComparison = compareWallClockValues(
                    a.date,
                    b.date
                );

                if (timeComparison !== 0) {
                    return timeComparison;
                }

                if (a.type === b.type) {
                    return a.task.title.localeCompare(b.task.title);
                }

                return a.type === "start" ? -1 : 1;
            });
        });

        // Leading blank cells.
        for (let i = 0; i < firstDay; i++) {
            const blank = document.createElement("div");

            blank.className = "calendar-day calendar-day-empty";
            blank.setAttribute("aria-hidden", "true");

            calendarElement.appendChild(blank);
        }

        // Day cells.
        for (let day = 1; day <= daysInMonth; day++) {
            const dateKey = [
                displayedYear,
                padNumber(displayedMonth),
                padNumber(day)
            ].join("-");

            const cell = document.createElement("div");
            cell.className = "calendar-day";

            if (dateKey === todayKey) {
                cell.classList.add("calendar-day-today");
            }

            if (dateKey === selectedDateKey) {
                cell.classList.add("calendar-day-selected");
            }

            const dayNumber = document.createElement("span");
            dayNumber.className = "calendar-day-number";
            dayNumber.textContent = String(day);

            cell.appendChild(dayNumber);

            const dayEvents = eventsByDate.get(dateKey) || [];

            if (dayEvents.length > 0) {
                const eventContainer = document.createElement("div");
                eventContainer.className = "calendar-day-events";

                // Show no more than two events directly in the calendar.
                dayEvents.slice(0, 2).forEach(function (event) {
                    const eventButton = document.createElement("button");

                    eventButton.type = "button";
                    eventButton.className =
                        `calendar-event ${getEventClass(event)}`;

                    eventButton.textContent = getEventLabel(event);
                    eventButton.title = getEventLabel(event);

                    eventButton.addEventListener("click", function (clickEvent) {
                        clickEvent.stopPropagation();

                        // Direct event clicks do not show the Back button.
                        openTaskModal(event.task);
                    });

                    eventContainer.appendChild(eventButton);
                });

                // Show a link to the remaining events, if necessary.
                if (dayEvents.length > 2) {
                    const moreButton = document.createElement("button");

                    moreButton.type = "button";
                    moreButton.className = "calendar-event-more";
                    moreButton.textContent = `+${dayEvents.length - 2} more`;

                    moreButton.setAttribute(
                        "aria-label",
                        `Show ${dayEvents.length - 2} more events on ${formatWallClockDate(dateKey)}`
                    );

                    moreButton.addEventListener("click", function (clickEvent) {
                        clickEvent.stopPropagation();
                        showMoreEvents(dateKey, dayEvents);
                    });

                    eventContainer.appendChild(moreButton);
                }

                cell.appendChild(eventContainer);
            }

            cell.addEventListener("click", function () {
                selectedDateKey = dateKey;
                renderCalendar();

                if (dayEvents.length > 0) {
                    showMoreEvents(dateKey, dayEvents);
                }
            });

            calendarElement.appendChild(cell);
        }

        if (emptyMessage) {
            const monthHasEvents = eventsByDate.size > 0
                && Array.from(eventsByDate.keys()).some(function (key) {
                    return key.startsWith(
                        `${displayedYear}-${padNumber(displayedMonth)}-`
                    );
                });

            emptyMessage.hidden = monthHasEvents;
        }
    }

    // =====================================================
    // SHOW ALL EVENTS FOR A SELECTED DAY
    // =====================================================

    function showMoreEvents(dateKey, dayEvents) {
        const modal = document.getElementById("calendar-task-modal");

        if (!modal) {
            return;
        }

        const title = document.getElementById("calendar-modal-title");
        const description = document.getElementById("calendar-modal-description");
        const editLink = document.getElementById("calendar-modal-edit");
        const eventList = document.getElementById("calendar-modal-event-list");

        const detailRowIds = [
            "calendar-modal-status-row",
            "calendar-modal-start-row",
            "calendar-modal-due-row"
        ];

        currentDayDateKey = dateKey;
        currentDayEvents = dayEvents;

        if (title) {
            title.textContent = `Tasks on ${formatWallClockDate(dateKey)}`;
        }

        if (description) {
            description.textContent =
                `Select an event below to view its task details. ${dayEvents.length} scheduled event(s).`;
        }

        detailRowIds.forEach(function (id) {
            const row = document.getElementById(id);

            if (row) {
                row.hidden = true;
            }
        });

        if (editLink) {
            editLink.hidden = true;
        }

        if (eventList) {
            eventList.replaceChildren();
            eventList.hidden = false;

            // Create the Back button once.
            let backButton = document.getElementById(
                "calendar-modal-back-to-events"
            );

            if (!backButton) {
                backButton = document.createElement("button");
                backButton.id = "calendar-modal-back-to-events";
                backButton.type = "button";
                backButton.className = "calendar-today-button";
                backButton.textContent = "← Back to all events";
                backButton.setAttribute(
                    "aria-label",
                    "Back to all events"
                );

                backButton.addEventListener("click", function () {
                    if (currentDayDateKey && currentDayEvents.length > 0) {
                        showMoreEvents(
                            currentDayDateKey,
                            currentDayEvents
                        );
                    }
                });
            }

            // Hide Back while the full event list is displayed.
            backButton.hidden = true;

            eventList.parentNode.insertBefore(backButton, eventList);

            dayEvents.forEach(function (event) {
                const eventButton = document.createElement("button");

                eventButton.type = "button";
                eventButton.className =
                    `calendar-modal-event ${getEventClass(event)}`;

                const eventTime = document.createElement("span");
                eventTime.className = "calendar-modal-event-time";
                eventTime.textContent = formatWallClockTime(event.date);

                const eventInfo = document.createElement("span");
                eventInfo.className = "calendar-modal-event-info";

                const taskTitle = document.createElement("strong");
                taskTitle.className = "calendar-modal-event-title";
                taskTitle.textContent = event.task.title;

                const eventType = document.createElement("span");
                eventType.className = "calendar-modal-event-type";

                if (event.type === "start") {
                    eventType.textContent = "Starts";
                } else if (event.task.completed) {
                    eventType.textContent = "Completed";
                } else if (isTaskOverdue(event.task)) {
                    eventType.textContent = "Overdue";
                } else {
                    eventType.textContent = "Due";
                }

                eventInfo.append(taskTitle, eventType);
                eventButton.append(eventTime, eventInfo);

                eventButton.addEventListener("click", function () {
                    // Show Back when opening details from this list.
                    openTaskModal(event.task, true);
                });

                eventList.appendChild(eventButton);
            });
        }

        openModal(modal);
    }

    // =====================================================
    // TASK DETAILS MODAL
    // =====================================================

    function openTaskModal(task, openedFromEventList = false) {
        const modal = document.getElementById("calendar-task-modal");

        if (!modal) {
            return;
        }

        const title = document.getElementById("calendar-modal-title");
        const description = document.getElementById("calendar-modal-description");
        const status = document.getElementById("calendar-modal-status");
        const start = document.getElementById("calendar-modal-start");
        const due = document.getElementById("calendar-modal-due");
        const editLink = document.getElementById("calendar-modal-edit");
        const eventList = document.getElementById("calendar-modal-event-list");
        const backButton = document.getElementById(
            "calendar-modal-back-to-events"
        );

        // Hide the event list while viewing task details.
        if (eventList) {
            eventList.hidden = true;
            eventList.replaceChildren();
        }

        // Show Back only when details were opened from the event list.
        if (backButton) {
            backButton.hidden = !openedFromEventList;
        }

        [
            "calendar-modal-status-row",
            "calendar-modal-start-row",
            "calendar-modal-due-row"
        ].forEach(function (id) {
            const row = document.getElementById(id);

            if (row) {
                row.hidden = false;
            }
        });

        if (title) {
            title.textContent = task.title;
        }

        if (description) {
            description.textContent =
                task.description || "No description provided.";
        }

        // Task status and matching popup colour.
        if (status) {
            status.classList.remove(
                "calendar-status-start",
                "calendar-status-due",
                "calendar-status-overdue",
                "calendar-status-completed"
            );

            if (task.completed) {
                status.textContent = "Completed";
                status.classList.add("calendar-status-completed");
            } else if (isTaskOverdue(task)) {
                status.textContent = "Overdue";
                status.classList.add("calendar-status-overdue");
            } else {
                status.textContent = "Pending";
                status.classList.add("calendar-status-due");
            }
        }

        if (start) {
            if (task.start) {
                start.textContent = formatWallClockDateTime(task.start);
            } else if (task.startImmediately) {
                start.textContent = "Right away";
            } else {
                start.textContent = "Not scheduled";
            }
        }

        if (due) {
            due.textContent = task.due
                ? formatWallClockDateTime(task.due)
                : "No due date";
        }

        if (editLink) {
            editLink.hidden = false;
            editLink.href =
                `/Tasks/EditTask?id=${encodeURIComponent(task.id)}`;
        }

        openModal(modal);
    }

    // =====================================================
    // MODAL OPEN / CLOSE
    // =====================================================

    function openModal(modal) {
        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("modal-open");
    }

    function closeModal() {
        const modal = document.getElementById("calendar-task-modal");

        if (!modal) {
            return;
        }

        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.classList.remove("modal-open");

        const editLink = document.getElementById("calendar-modal-edit");

        if (editLink) {
            editLink.hidden = false;
        }
    }

    const closeModalButton = document.getElementById("calendar-modal-close");
    const closeModalFooterButton = document.getElementById(
        "calendar-modal-close-button"
    );
    const modalOverlay = document.getElementById("calendar-modal-overlay");

    closeModalButton?.addEventListener("click", closeModal);
    closeModalFooterButton?.addEventListener("click", closeModal);
    modalOverlay?.addEventListener("click", closeModal);

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeModal();
        }
    });

    // =====================================================
    // CALENDAR NAVIGATION
    // =====================================================

    function moveMonth(amount) {
        displayedMonth += amount;

        if (displayedMonth < 1) {
            displayedMonth = 12;
            displayedYear--;
        } else if (displayedMonth > 12) {
            displayedMonth = 1;
            displayedYear++;
        }

        selectedDateKey = null;
        renderCalendar();
    }

    previousButton?.addEventListener("click", function () {
        moveMonth(-1);
    });

    nextButton?.addEventListener("click", function () {
        moveMonth(1);
    });

    todayButton?.addEventListener("click", function () {
        const now = getUserWallClockNow();

        displayedYear = now.year;
        displayedMonth = now.month;
        selectedDateKey = getDateKey(now);

        renderCalendar();
    });

    // =====================================================
    // TASK STATUS DOUGHNUT CHART
    // =====================================================

    const chartCanvas = document.getElementById("dashboardStatusChart");
    const chartDataElement = document.getElementById("dashboard-status-data");

    if (chartCanvas && chartDataElement && typeof Chart !== "undefined") {
        const completed = Number(chartDataElement.dataset.completed || 0);
        const pending = Number(chartDataElement.dataset.pending || 0);
        const overdue = Number(chartDataElement.dataset.overdue || 0);

        const existingChart = Chart.getChart(chartCanvas);
        existingChart?.destroy();

        new Chart(chartCanvas, {
            type: "doughnut",
            data: {
                labels: ["Completed", "Pending", "Overdue"],
                datasets: [{
                    data: [completed, pending, overdue],
                    backgroundColor: ["#22c55e", "#3b82f6", "#ef4444"],
                    borderWidth: 0,
                    hoverOffset: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "72%",
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: true }
                }
            }
        });
    }

    // =====================================================
    // INITIAL RENDER
    // =====================================================

    renderCalendar();
});







function getEventClass(event) {
    // A Start indicator always represents the task's start.
    if (event.type === "start") {
        return "calendar-start-event";
    }

    // Only the due/outcome indicator changes with the task's status.
    if (event.task.completed) {
        return "calendar-completed-event";
    }

    if (isTaskOverdue(event.task)) {
        return "calendar-overdue-event";
    }

    return "calendar-due-event";
}
