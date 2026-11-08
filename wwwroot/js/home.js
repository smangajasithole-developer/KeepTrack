// =====================================================
// KeepTrack — Home Script
// Handles: smooth scrolling for internal anchor links.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. SMOOTH SCROLL FOR ANCHOR LINKS
    // Adds a sticky-header offset so the section title
    // isn't hidden behind the app header.
    // =====================================================

    const scrollLinks = document.querySelectorAll(".home-scroll-link");

    scrollLinks.forEach(function (link) {
        link.addEventListener("click", function (event) {
            const href = link.getAttribute("href");

            if (!href || !href.startsWith("#")) {
                return;
            }

            const targetSection = document.getElementById(href.slice(1));

            if (!targetSection) {
                return;
            }

            event.preventDefault();

            const headerOffset = 80;
            const elementPosition = targetSection.getBoundingClientRect().top;
            const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

            window.scrollTo({
                top: offsetPosition,
                behavior: "smooth"
            });
        });
    });

});