(() => {
  let submitting = false;
  const forms = document.querySelectorAll('[data-material-processing-form]');
  forms.forEach(form => form.addEventListener('submit', () => {
    if (!form.checkValidity()) return;
    submitting = true;
    form.querySelectorAll('button[type=submit]').forEach(button => {
      button.dataset.originalLabel = button.textContent;
      button.disabled = true;
      button.textContent = 'Processing...';
    });
    const progress = document.getElementById('material-progress');
    if (progress) progress.hidden = false;
  }));
  window.addEventListener('pageshow', event => {
    if (!event.persisted) return;
    submitting = false;
    forms.forEach(form => form.querySelectorAll('[data-original-label]').forEach(button => {
      button.textContent = button.dataset.originalLabel;
      button.disabled = false;
    }));
    const progress = document.getElementById('material-progress');
    if (progress) progress.hidden = true;
  });
  const live = document.querySelector('[data-material-auto-refresh="true"]');
  if (live) setTimeout(() => {
    const upload = document.getElementById('upload');
    if (!submitting && !upload?.files?.length && document.visibilityState === 'visible') location.reload();
  }, 5000);
})();
