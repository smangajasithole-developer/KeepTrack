// =====================================================
// KeepTrack — Tasks Script
// Handles: task count display, overdue indicators,
// and the "View Task" details modal.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. TASK COUNT DISPLAY
    // Reads data-task-count and formats "1 task" vs "N tasks".
    // =====================================================

    function updateTaskCount() {
        const countElement = document.querySelector("[data-task-count]");

        if (!countElement) {
            return;
        }

        const count = Number(countElement.dataset.taskCount);

        countElement.textContent =
            `${count} ${count === 1 ? "task" : "tasks"}`;
    }

    updateTaskCount();


    // =====================================================
    // 2. OVERDUE INDICATORS
    // Adds "Overdue by X days/hours" to any task card with
    // data-overdue-indicator where the due date has passed.
    // =====================================================

    function updateOverdueIndicators() {
        const now = new Date();

        document.querySelectorAll(".task-card").forEach(function (card) {
            const dueValue = card.dataset.dueDate;
            const completed = card.dataset.taskCompleted === "true";

            const indicator = card.querySelector("[data-overdue-indicator]");

            if (!indicator) {
                return;
            }

            if (!dueValue || completed) {
                indicator.hidden = true;
                return;
            }

            const dueDate = new Date(dueValue);

            if (Number.isNaN(dueDate.getTime()) || dueDate > now) {
                indicator.hidden = true;
                return;
            }

            const difference = now.getTime() - dueDate.getTime();

            const totalMinutes = Math.floor(difference / 60000);
            const days = Math.floor(totalMinutes / 1440);
            const hours = Math.floor((totalMinutes % 1440) / 60);
            const minutes = totalMinutes % 60;

            let amount;
            let unit;

            if (days > 0) {
                amount = days;
                unit = days === 1 ? "day" : "days";
            } else if (hours > 0) {
                amount = hours;
                unit = hours === 1 ? "hour" : "hours";
            } else {
                amount = minutes;
                unit = minutes === 1 ? "minute" : "minutes";
            }

            indicator.textContent = `Overdue by ${amount} ${unit}`;
            indicator.hidden = false;
        });
    }

    updateOverdueIndicators();


    // =====================================================
    // 3. VIEW TASK MODAL
    // Populates the modal with data-* attributes from the
    // clicked "View" button and manages open/close + focus.
    // =====================================================

    const modal = document.getElementById("task-view-modal");

    if (!modal) {
        return;
    }

    const overlay = document.getElementById("task-view-modal-overlay");
    const closeButton = document.getElementById("task-view-modal-close");
    const footerCloseButton = document.getElementById("task-view-modal-close-button");

    const titleElement = document.getElementById("task-view-modal-title");
    const descriptionElement = document.getElementById("task-view-modal-description");
    const statusElement = document.getElementById("task-view-modal-status");
    const createdElement = document.getElementById("task-view-modal-created");
    const dueElement = document.getElementById("task-view-modal-due");
    const startElement = document.getElementById("task-view-modal-start");
    const reminderElement = document.getElementById("task-view-modal-reminder");

    let previouslyFocusedElement = null;

    function openModal(button) {
        previouslyFocusedElement = document.activeElement;

        titleElement.textContent =
            button.dataset.taskTitle || "Task";

        descriptionElement.textContent =
            button.dataset.taskDescription || "No description";

        statusElement.textContent =
            button.dataset.taskStatus || "Pending";

        createdElement.textContent =
            button.dataset.taskCreated || "Not available";

        dueElement.textContent =
            button.dataset.taskDue || "No due date";

        startElement.textContent =
            button.dataset.taskStart || "Not scheduled";

        reminderElement.textContent =
            button.dataset.taskReminder || "Off";

        modal.classList.add("open");
        modal.setAttribute("aria-hidden", "false");

        document.body.classList.add("task-view-modal-open");

        if (closeButton) {
            closeButton.focus();
        }
    }

    function closeModal() {
        if (!modal.classList.contains("open")) {
            return;
        }

        modal.classList.remove("open");
        modal.setAttribute("aria-hidden", "true");

        document.body.classList.remove("task-view-modal-open");

        if (
            previouslyFocusedElement &&
            typeof previouslyFocusedElement.focus === "function"
        ) {
            previouslyFocusedElement.focus();
        }
    }

    // Attach "View" buttons
    document.querySelectorAll(".task-view-button").forEach(function (button) {
        button.addEventListener("click", function () {
            openModal(button);
        });
    });

    // Attach close handlers
    closeButton?.addEventListener("click", closeModal);
    footerCloseButton?.addEventListener("click", closeModal);
    overlay?.addEventListener("click", closeModal);

    // Close on Escape key
    document.addEventListener("keydown", function (event) {
        if (
            event.key === "Escape" &&
            modal.classList.contains("open")
        ) {
            closeModal();
        }
    });
});


    // =====================================================
    // 4. BUTTON LOADING STATES
    // Prevents double-clicking Complete/Delete by showing
    // the data-submitting text and disabling the button.
    // (Kept here so it works even without site.js loaded.)
    // =====================================================

    document.addEventListener("click", function (event) {
        const button = event.target.closest("button[data-submitting]");

        if (!button) {
            return;
        }

        // If a confirm dialog cancels, don't change the label.
        if (button.getAttribute("onclick") &&
            button.getAttribute("onclick").includes("confirm") &&
            !confirm("Are you sure?")) {
            return;
        }

        const message = button.dataset.submitting;

        if (message) {
            button.textContent = message;
        }

        button.style.pointerEvents = "none";
        button.style.opacity = "0.7";
    });