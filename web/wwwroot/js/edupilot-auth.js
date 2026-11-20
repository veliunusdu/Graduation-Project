(() => {
    document.querySelectorAll('.ep-pw-toggle').forEach(button => {
        button.addEventListener('click', () => {
            const input = document.getElementById(button.dataset.target);
            if (!input) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.setAttribute('aria-pressed', String(show));
            const label = button.dataset.target === 'ConfirmPassword' ? 'confirm password' : 'password';
            button.setAttribute('aria-label', `${show ? 'Hide' : 'Show'} ${label}`);
            const icon = button.querySelector('use');
            if (icon) icon.setAttribute('href', icon.getAttribute('href').split('#')[0] + (show ? '#eye-off' : '#eye'));
        });
    });
    const slides = document.getElementById('onboardCarousel');
    const next = document.getElementById('onboardNext');
    const start = document.getElementById('onboardStart');
    if (slides && next && start) {
        const panels = Array.from(slides.querySelectorAll('.carousel-item'));
        const indicators = Array.from(slides.querySelectorAll('[data-onboard-slide]'));
        function showSlide(index) {
            panels.forEach((panel, position) => {
                const active = position === index;
                panel.hidden = !active;
                panel.classList.toggle('active', active);
            });
            indicators.forEach((button, position) => {
                const active = position === index;
                button.classList.toggle('active', active);
                if (active) button.setAttribute('aria-current', 'true');
                else button.removeAttribute('aria-current');
            });
            const last = index === panels.length - 1;
            next.hidden = last;
            start.hidden = !last;
        }
        next.addEventListener('click', event => {
            event.preventDefault();
            showSlide(panels.length - 1);
        });
        indicators.forEach(button => button.addEventListener('click', () => showSlide(Number(button.dataset.onboardSlide))));
        showSlide(0);
    }
})();
