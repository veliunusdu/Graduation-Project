(() => {
  const button = document.getElementById('student-menu');
  const sidebar = document.getElementById('student-sidebar');
  function closeMenu() { sidebar.classList.remove('is-open'); button.setAttribute('aria-expanded', 'false'); }
  button.addEventListener('click', () => { const open = sidebar.classList.toggle('is-open'); button.setAttribute('aria-expanded', String(open)); });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') closeMenu(); });
  document.addEventListener('click', event => { if (!sidebar.contains(event.target) && !button.contains(event.target)) closeMenu(); });
})();
