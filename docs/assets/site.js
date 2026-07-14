(() => {
  const body = document.body;
  const header = document.querySelector("[data-header]");
  const toggle = document.querySelector(".nav-toggle");
  const nav = document.querySelector(".main-nav");
  const lightbox = document.querySelector("[data-lightbox]");
  const lightboxImage = lightbox?.querySelector("img");

  const updateHeader = () => header?.classList.toggle("scrolled", window.scrollY > 18);
  updateHeader();
  window.addEventListener("scroll", updateHeader, { passive: true });

  toggle?.addEventListener("click", () => {
    const open = !body.classList.contains("nav-open");
    body.classList.toggle("nav-open", open);
    toggle.setAttribute("aria-expanded", String(open));
  });

  nav?.addEventListener("click", event => {
    if (event.target.closest("a")) {
      body.classList.remove("nav-open");
      toggle?.setAttribute("aria-expanded", "false");
    }
  });

  document.querySelectorAll("[data-year]").forEach(node => {
    node.textContent = String(new Date().getFullYear());
  });

  const revealNodes = document.querySelectorAll(".reveal");
  if ("IntersectionObserver" in window && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
    try {
      const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add("visible");
            observer.unobserve(entry.target);
          }
        });
      }, { threshold: 0.14 });
      document.documentElement.classList.add("reveal-enhanced");
      revealNodes.forEach(node => observer.observe(node));
    } catch {
      document.documentElement.classList.remove("reveal-enhanced");
      revealNodes.forEach(node => node.classList.add("visible"));
    }
  } else {
    revealNodes.forEach(node => node.classList.add("visible"));
  }

  document.querySelectorAll("[data-image]").forEach(button => {
    button.addEventListener("click", () => {
      if (!lightbox || !lightboxImage) return;
      lightboxImage.src = button.dataset.image;
      lightboxImage.alt = button.dataset.alt || "";
      lightbox.showModal();
    });
  });

  lightbox?.querySelector(".lightbox-close")?.addEventListener("click", () => lightbox.close());
  lightbox?.addEventListener("click", event => {
    if (event.target === lightbox) lightbox.close();
  });
})();
