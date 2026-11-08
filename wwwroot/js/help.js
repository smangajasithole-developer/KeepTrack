// =====================================================
// KeepTrack — Help Script
// Handles: smooth scrolling when a support card is
// clicked, and automatic FAQ expand/collapse.
// =====================================================

document.addEventListener("DOMContentLoaded", function () {
    "use strict";


    // =====================================================
    // 1. SMOOTH SCROLL TO SECTIONS
    // When the user clicks a card in the "Explore KeepTrack"
    // grid, glide down to the matching section instead of
    // jumping. Also updates the URL hash without a scroll.
    // =====================================================

    const scrollLinks = document.querySelectorAll(".help-scroll-link");

    scrollLinks.forEach(function (link) {
        link.addEventListener("click", function (event) {
            const targetId = link.dataset.target;

            if (!targetId) {
                return;
            }

            const targetSection = document.getElementById(targetId);

            if (!targetSection) {
                return;
            }

            event.preventDefault();

            // Compute an offset so the sticky header doesn't
            // cover the section title.
            const headerOffset = 80;
            const elementPosition = targetSection.getBoundingClientRect().top;
            const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

            window.scrollTo({
                top: offsetPosition,
                behavior: "smooth"
            });

            // Update the URL hash without triggering a second jump
            if (history.replaceState) {
                history.replaceState(null, "", `#${targetId}`);
            }
        });
    });


    // =====================================================
    // 2. FAQ ACCORDION (OPTIONAL POLISH)
    // Ensures only one FAQ is open at a time inside the
    // Help Center section. Native <details> handles the
    // toggle — this just closes any other open one.
    // =====================================================

    const faqs = document.querySelectorAll(".help-faq");

    faqs.forEach(function (faq) {
        faq.addEventListener("toggle", function () {
            if (!faq.open) {
                return;
            }

            // Close every other open FAQ
            faqs.forEach(function (other) {
                if (other !== faq && other.open) {
                    other.open = false;
                }
            });
        });
    });


    // =====================================================
    // 3. SCROLL REVEAL (OPTIONAL)
    // Adds a subtle "fade in" as each section enters the
    // viewport for the first time.
    // =====================================================

    const revealTargets = document.querySelectorAll(
        ".help-section, .help-details, .help-footer"
    );

    if ("IntersectionObserver" in window) {
        const observer = new IntersectionObserver(
            function (entries) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("help-revealed");
                        observer.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.1 }
        );

        revealTargets.forEach(function (target) {
            target.classList.add("help-reveal-ready");
            observer.observe(target);
        });
    }

});