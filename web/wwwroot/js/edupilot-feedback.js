(() => {
  const button = document.getElementById('save-review');
  if (!button) return;
  const grade = document.getElementById('Grade');
  const feedback = document.getElementById('Feedback');
  const persisted = button.dataset.saved === 'true';
  const originalGrade = grade.value;
  const originalFeedback = feedback.value.trim();
  function refresh() {
    const saved = persisted && grade.value === originalGrade && feedback.value.trim() === originalFeedback;
    button.textContent = saved ? 'Saved' : 'Save grade and feedback';
    button.classList.toggle('ep-button-saved', saved);
    button.classList.toggle('btn-primary', !saved);
    button.dataset.saved = String(saved);
  }
  grade.addEventListener('input', refresh);
  feedback.addEventListener('input', refresh);
})();
