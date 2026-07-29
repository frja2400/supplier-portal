// Rensa serverside-genererade valideringsfel när användaren börjar skriva om i fältet
document.addEventListener('input', function (event) {
    const field = event.target;
    if (!field.name) return;

    const errorSpan = document.querySelector(`[data-valmsg-for="${field.name}"]`);
    if (errorSpan && errorSpan.textContent.trim() !== '') {
        errorSpan.textContent = '';
        errorSpan.classList.remove('field-validation-error');
        errorSpan.classList.add('field-validation-valid');
    }
});