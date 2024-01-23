const editButtons = Array.from(document.querySelectorAll('[data-btn-edit]'));
const assignButtons = Array.from(document.querySelectorAll('[data-btn-assign]'));

editButtons.forEach(editBtn => {
    editBtn.addEventListener('click', function (event) {
        editBtn.form.getElementsByTagName('fieldset')[0].disabled = null;

        const formButtons = Array.from(editBtn.form.getElementsByTagName('button'));
        formButtons.forEach(formBtn => formBtn.classList.toggle('d-none'));

        const hiddenFields = Array.from(editBtn.form.querySelectorAll('[data-form-hidden]'));
        hiddenFields.forEach(field => field.classList.remove('d-none'));

        editButtons.forEach(eBtn => { if (eBtn.form !== editBtn.form) eBtn.classList.add('d-none'); });
        assignButtons.forEach(aBtn => aBtn.classList.add('d-none'));
    });
});

const cancelButtons = Array.from(document.querySelectorAll('[data-btn-cancel]'));

cancelButtons.forEach(cBtn => {
    cBtn.addEventListener('click', function (event) {
        cBtn.form.getElementsByTagName('fieldset')[0].disabled = 'disabled';

        const formButtons = Array.from(cBtn.form.getElementsByTagName('button'));
        formButtons.forEach(formBtn => formBtn.classList.toggle('d-none'));

        const hiddenFields = Array.from(cBtn.form.querySelectorAll('[data-form-hidden]'));
        hiddenFields.forEach(field => field.classList.add('d-none'));

        editButtons.forEach(eBtn => { if (eBtn.form !== cBtn.form) eBtn.classList.remove('d-none'); });
        assignButtons.forEach(aBtn => aBtn.classList.remove('d-none'));


        const counter = cBtn.form.querySelector('[data-counter-value]');
        if (counter)
            counter.value = counter.dataset.counterValue;

        const subForms = Array.from(cBtn.form.querySelectorAll('[data-subform]'));
        subForms.forEach(subForm => {
            const status = subForm.querySelector('[data-subform-status]');

            switch (status.value) {
                case 'Inserted':
                case 'Unknown':
                    subForm.remove();
                    break;
                case 'Deleted':
                    subForm.classList.remove('d-none');
                default:
                    status.value = 'Unchanged';
                    break;
            }
        });
    });
});

const subForms = Array.from(document.querySelectorAll('[data-subform]'));
const subEditButtons = Array.from(document.querySelectorAll('[data-subform-edit]'));

subForms.forEach(subForm => {
    const statusField = subForm.querySelector('[data-subform-status]');
    const editBtn = subForm.querySelector('[data-subform-edit]');
    const cancelBtn = subForm.querySelector('[data-subform-cancel]');
    const resetBtn = subForm.querySelector('[data-subform-reset]');
    const removeBtn = subForm.querySelector('[data-subform-remove]');
    const confirmBtn = subForm.querySelector('[data-subform-confirm]');
    const subInputs = Array.from(subForm.querySelectorAll('[data-subform-input]'));

    editBtn.addEventListener('click', function (event) {
        editBtn.form.querySelector('[data-btn-cancel]').classList.add('d-none');
        editBtn.form.querySelector('button[type=submit]').classList.add('d-none');

        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.add('d-none');
                subInput.classList.remove('d-none');
            }

            subInput.readOnly = false;
        });

        subEditButtons.forEach(subEdit => {
            subEdit.classList.add('d-none');
        });

        cancelBtn.classList.remove('d-none');
        removeBtn.classList.add('d-none');
        confirmBtn.classList.remove('d-none');
    });

    cancelBtn.addEventListener('click', function (event) {
        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.remove('d-none');
                subInput.classList.add('d-none');
            }

            subInput.readOnly = true;
            subInput.value = subInput.dataset.savedValue;
        });

        cancelBtn.form.querySelector('[data-btn-cancel]').classList.remove('d-none');
        cancelBtn.form.querySelector('button[type=submit]').classList.remove('d-none');

        subEditButtons.forEach(subEdit => {
            subEdit.classList.remove('d-none');
        });

        cancelBtn.classList.add('d-none');
        removeBtn.classList.remove('d-none');
        confirmBtn.classList.add('d-none');
    });

    confirmBtn.addEventListener('click', function (event) {
        if (statusField.value === 'Unchanged')
            statusField.value = 'Modified';

        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.remove('d-none');
                subInput.classList.add('d-none');
            }

            subInput.readOnly = true;
            subInput.dataset.savedValue = subInput.value;
        });

        confirmBtn.form.querySelector('[data-btn-cancel]').classList.remove('d-none');
        confirmBtn.form.querySelector('button[type=submit]').classList.remove('d-none');

        subEditButtons.forEach(subEdit => {
            subEdit.classList.remove('d-none');
        });

        cancelBtn.classList.add('d-none');
        removeBtn.classList.remove('d-none');
        confirmBtn.classList.add('d-none');
    });

    resetBtn.addEventListener('click', function (event) {
        statusField.value = 'Unchanged';

        subInputs.forEach(subInput => {
            subInput.value = subInput.dataset.originalValue;
        });
    });

    removeBtn.addEventListener('click', function (event) {
        if (statusField.value === 'Inserted') {
            //statusField.value = 'Unknown';
            //subForm.classList.add('d-none');
            subForm.remove();
            return;
        }

        statusField.value = 'Deleted';
        subForm.classList.add('d-none');
    });
});

const addQuoteBtn = document.querySelector('[data-btn-add-quote]');
const quoteCountField = document.querySelector('#quote-count');
const newQuoteTextField = addQuoteBtn.form.querySelector('#new-quote-text');
const newQuoteAuthorField = addQuoteBtn.form.querySelector('#new-quote-author');
const quoteTemplate = document.querySelector('#template-quote');
const quoteSelector = addQuoteBtn.dataset.addTarget;

addQuoteBtn.addEventListener('click', function (event) {
    const quoteCount = quoteCountField.value;
    if (!(quoteCount >= 0) || !quoteTemplate || !quoteSelector || quoteSelector === '')
        return false;

    const target = document.querySelector(quoteSelector);

    if (!target)
        return false;

    const templateCopy = quoteTemplate.cloneNode(true);

    templateCopy.removeAttribute('id');
    templateCopy.classList.remove('d-none');

    const qStatus = templateCopy.querySelector('#template-quote-status');
    qStatus.id = `Quotes_${quoteCount}__Status`;
    qStatus.name = `Quotes[${quoteCount}].Status`;
    qStatus.value = 'Inserted';

    const qId = templateCopy.querySelector('#template-quote-id');
    qId.id = `Quotes_${quoteCount}__QuoteId`;
    qId.name = `Quotes[${quoteCount}].QuoteId`;
    qId.value = '00000000-0000-0000-0000-000000000000';

    const qText = templateCopy.querySelector('#template-quote-text');
    qText.id = `Quotes_${quoteCount}__Quote`;
    qText.name = `Quotes[${quoteCount}].Quote`;
    qText.value = newQuoteTextField.value;
    qText.dataset.savedValue = newQuoteTextField.value;
    qText.dataset.originalValue = newQuoteTextField.value;

    const qAuthor = templateCopy.querySelector('#template-quote-author');
    qAuthor.id = `Quotes_${quoteCount}__Author`;
    qAuthor.name = `Quotes[${quoteCount}].Author`;
    qAuthor.value = newQuoteAuthorField.value;
    qAuthor.dataset.savedValue = newQuoteAuthorField.value;
    qAuthor.dataset.originalValue = newQuoteAuthorField.value;

    target.appendChild(templateCopy);

    const editBtn = templateCopy.querySelector('[data-subform-edit]');
    const cancelBtn = templateCopy.querySelector('[data-subform-cancel]');
    const resetBtn = templateCopy.querySelector('[data-subform-reset]');
    const removeBtn = templateCopy.querySelector('[data-subform-remove]');
    const confirmBtn = templateCopy.querySelector('[data-subform-confirm]');
    const subInputs = Array.from(templateCopy.querySelectorAll('[data-subform-input]'));
    subEditButtons.push(editBtn);

    editBtn.addEventListener('click', function (event) {
        editBtn.form.querySelector('[data-btn-cancel]').classList.add('d-none');
        editBtn.form.querySelector('button[type=submit]').classList.add('d-none');

        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.add('d-none');
                subInput.classList.remove('d-none');
            }

            subInput.readOnly = false;
        });

        subEditButtons.forEach(subEdit => {
            subEdit.classList.add('d-none');
        });

        cancelBtn.classList.remove('d-none');
        removeBtn.classList.add('d-none');
        confirmBtn.classList.remove('d-none');
    });

    cancelBtn.addEventListener('click', function (event) {
        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.remove('d-none');
                subInput.classList.add('d-none');
            }

            subInput.readOnly = true;
            subInput.value = subInput.dataset.savedValue;
        });

        cancelBtn.form.querySelector('[data-btn-cancel]').classList.remove('d-none');
        cancelBtn.form.querySelector('button[type=submit]').classList.remove('d-none');

        subEditButtons.forEach(subEdit => {
            subEdit.classList.remove('d-none');
        });

        cancelBtn.classList.add('d-none');
        removeBtn.classList.remove('d-none');
        confirmBtn.classList.add('d-none');
    });

    confirmBtn.addEventListener('click', function (event) {
        if (qStatus.value === 'Unchanged')
            qStatus.value = 'Modified';

        subInputs.forEach(subInput => {
            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                displayElement.classList.remove('d-none');
                subInput.classList.add('d-none');
            }

            subInput.readOnly = true;
            subInput.dataset.savedValue = subInput.value;
        });

        confirmBtn.form.querySelector('[data-btn-cancel]').classList.remove('d-none');
        confirmBtn.form.querySelector('button[type=submit]').classList.remove('d-none');

        subEditButtons.forEach(subEdit => {
            subEdit.classList.remove('d-none');
        });

        cancelBtn.classList.add('d-none');
        removeBtn.classList.remove('d-none');
        confirmBtn.classList.add('d-none');
    });

    resetBtn.addEventListener('click', function (event) {
        qStatus.value = 'Unchanged';

        subInputs.forEach(subInput => {
            subInput.value = subInput.dataset.originalValue;
        });
    });

    removeBtn.addEventListener('click', function (event) {
        console.log(qStatus.value);
        if (qStatus.value === 'Inserted') {
            //qStatus.value = 'Unknown';
            //templateCopy.classList.add('d-none');
            templateCopy.remove();
            return;
        }

        qStatus.value = 'Deleted';
        templateCopy.classList.add('d-none');
    });

    addQuoteBtn.form.reset();
    quoteCountField.value++;
});