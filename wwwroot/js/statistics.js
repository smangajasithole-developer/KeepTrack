// =====================================================
// KeepTrack — Statistics Script
// Handles: doughnut chart, activity line chart, and
// status comparison bar chart. All data comes from
// hidden feeds in Statistics.cshtml.
// =====================================================

document.addEventListener("DOMContentLoaded", () => {
    "use strict";

    // =====================================================
    // DATA FEEDERS
    // =====================================================

    const statisticsData = document.getElementById("statistics-data");

    if (!statisticsData) {
        return;
    }

    // ---------- Overview ----------
    const totalTasks = Number(statisticsData.dataset.total) || 0;
    const completedTasks = Number(statisticsData.dataset.completed) || 0;
    const pendingTasks = Number(statisticsData.dataset.pending) || 0;
    const overdueTasks = Number(statisticsData.dataset.overdue) || 0;
    const completionPercentage = Number(statisticsData.dataset.completion) || 0;

    // ---------- Scheduling ----------
    const scheduledTasks = Number(statisticsData.dataset.scheduled) || 0;
    const startImmediatelyTasks = Number(statisticsData.dataset.startImmediately) || 0;
    const tasksWithDueDate = Number(statisticsData.dataset.withDueDate) || 0;
    const tasksWithoutDueDate = Number(statisticsData.dataset.withoutDueDate) || 0;


    // =====================================================
    // CHART.JS GUARD
    // =====================================================

    if (typeof Chart === "undefined") {
        console.error("KeepTrack Statistics: Chart.js could not be loaded.");
        return;
    }

    Chart.defaults.font.family =
        "Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

    Chart.defaults.font.size = 12;
    Chart.defaults.plugins.legend.labels.usePointStyle = true;


    // =====================================================
    // SHARED STATUS DATA
    // =====================================================

    const statusLabels = ["Completed", "Pending", "Overdue"];
    const statusValues = [completedTasks, pendingTasks, overdueTasks];
    const statusColors = ["#16a34a", "#64748b", "#dc2626"];


    // =====================================================
    // 1. TASK STATUS DOUGHNUT
    // =====================================================

    const taskStatusCanvas = document.getElementById("taskStatusChart");

    if (taskStatusCanvas) {
        new Chart(taskStatusCanvas, {
            type: "doughnut",

            data: {
                labels: statusLabels,
                datasets: [{
                    data: statusValues,
                    backgroundColor: statusColors,
                    borderWidth: 0,
                    hoverOffset: 7,
                    spacing: 3,
                    borderRadius: 6
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "72%",

                plugins: {
                    legend: { display: false },

                    tooltip: {
                        callbacks: {
                            label(context) {
                                const value = Number(context.raw) || 0;

                                const percentage = totalTasks > 0
                                    ? Math.round((value / totalTasks) * 100)
                                    : 0;

                                return `${context.label}: ${value} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    }


    // =====================================================
    // 2. TASK ACTIVITY DATA (last 7 days)
    // =====================================================

    const activityContainer = document.getElementById("statistics-activity-data");

    const activityItems = activityContainer
        ? Array.from(activityContainer.querySelectorAll(".statistics-activity-item"))
        : [];

    const activityLabels = activityItems.map(item => item.dataset.label || "");

    const activityFullDates = activityItems.map(
        item => item.dataset.fullDate || item.dataset.label || ""
    );

    const activityValues = activityItems.map(
        item => Number(item.dataset.count) || 0
    );


    // =====================================================
    // 3. TASK ACTIVITY LINE CHART
    // =====================================================

    const taskActivityCanvas = document.getElementById("taskActivityChart");

    if (taskActivityCanvas) {
        new Chart(taskActivityCanvas, {
            type: "line",

            data: {
                labels: activityLabels,

                datasets: [{
                    label: "Tasks Created",
                    data: activityValues,
                    borderColor: "#6366f1",
                    backgroundColor: "rgba(99, 102, 241, 0.12)",
                    borderWidth: 2,
                    fill: true,
                    tension: 0.35,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    pointBackgroundColor: "#6366f1"
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                interaction: {
                    intersect: false,
                    mode: "index"
                },

                plugins: {
                    legend: { display: false },

                    tooltip: {
                        callbacks: {
                            title(items) {
                                if (!items.length) return "";

                                return activityFullDates[items[0].dataIndex] || "";
                            },

                            label(context) {
                                const count = Number(context.raw) || 0;
                                return `Tasks created: ${count}`;
                            }
                        }
                    }
                },

                scales: {
                    x: { grid: { display: false } },
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }


    // =====================================================
    // 4. STATUS COMPARISON BAR CHART
    // =====================================================

    const statusComparisonCanvas = document.getElementById("statusComparisonChart");

    if (statusComparisonCanvas) {
        new Chart(statusComparisonCanvas, {
            type: "bar",

            data: {
                labels: statusLabels,

                datasets: [{
                    label: "Number of Tasks",
                    data: statusValues,
                    backgroundColor: statusColors,
                    borderRadius: 6,
                    borderSkipped: false,
                    maxBarThickness: 65
                }]
            },

            options: {
                responsive: true,
                maintainAspectRatio: false,

                plugins: {
                    legend: { display: false },

                    tooltip: {
                        callbacks: {
                            label(context) {
                                return `Tasks: ${Number(context.raw) || 0}`;
                            }
                        }
                    }
                },

                scales: {
                    x: { grid: { display: false } },
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }


    // =====================================================
    // DEBUG
    // =====================================================

    console.debug("KeepTrack Statistics loaded.", {
        totalTasks,
        completedTasks,
        pendingTasks,
        overdueTasks,
        completionPercentage,
        scheduledTasks,
        startImmediatelyTasks,
        tasksWithDueDate,
        tasksWithoutDueDate
    });
});