////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// EDIT BUTTONS
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CANCEL BUTTONS
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

cancelButtons.forEach(cBtn => {
    cBtn.addEventListener('click', function (event) {
        cBtn.form.reset();
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

            const displayElements = Array.from(subForm.querySelectorAll('[data-subform-display]'));
            displayElements.forEach(displayElement => {
                const subInput = displayElement.parentNode.querySelector('[data-subform-input]');

                if (subInput.tagName.toLowerCase() === 'select')
                    displayElement.textContent = subInput.options[subInput.selectedIndex].text;
                else
                    displayElement.textContent = subInput.value;
            });

            if (subForm.dataset.subform === 'added') {
                subForm.remove();
                return;
            }

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

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// SUBFORMS
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
            subInput.dataset.savedValue = subInput.value;

            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                if (subInput.tagName.toLowerCase() === 'select')
                    displayElement.textContent = subInput.options[subInput.selectedIndex].text;
                else
                    displayElement.textContent = subInput.value;

                displayElement.classList.remove('d-none');
                subInput.classList.add('d-none');
            }
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

            const displayElement = subInput.parentNode.querySelector('[data-subform-display]');

            if (displayElement) {
                if (subInput.tagName.toLowerCase() === 'select')
                    displayElement.textContent = subInput.options[subInput.selectedIndex].text;
                else
                    displayElement.textContent = subInput.value;
            }
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

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// ADD NEW QUOTE
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const addQuoteBtn = document.querySelector('[data-btn-add-quote]');
const quoteCountField = document.querySelector('#quote-count');
const newQuoteStatusField = addQuoteBtn.form.querySelector('#new-quote-status');
const newQuoteIdField = addQuoteBtn.form.querySelector('#new-quote-id');
const newQuoteTextField = addQuoteBtn.form.querySelector('#new-quote-text');
const newQuoteTextInvalid = newQuoteTextField.parentNode.querySelector('.invalid-feedback');
const newQuoteAuthorField = addQuoteBtn.form.querySelector('#new-quote-author');
const newQuoteAuthorInvalid = newQuoteAuthorField.parentNode.querySelector('.invalid-feedback');
const quoteTemplate = document.querySelector('#template-quote');
const quoteSelector = addQuoteBtn.dataset.addTarget;

addQuoteBtn.addEventListener('click', function (event) {
    const text = newQuoteTextField.value.trim();
    const author = newQuoteAuthorField.value.trim();
    
    if (!text && newQuoteTextInvalid) {
        newQuoteTextField.classList.add('is-invalid');
        newQuoteTextInvalid.classList.remove('field-validation-valid');
        newQuoteTextInvalid.classList.add('field-validation-error');
        newQuoteTextInvalid.innerHTML = '<span id="new-quote-text-error" class="is-invalid">Please input a quote.</span>';
    }
    else if (newQuoteTextField.classList.contains('is-invalid') && newQuoteTextInvalid) {
        newQuoteTextField.classList.remove('is-invalid');
        newQuoteTextInvalid.classList.add('field-validation-valid');
        newQuoteTextInvalid.classList.remove('field-validation-error');
        newQuoteTextInvalid.innerHTML = '';
    }

    if (!author && newQuoteAuthorInvalid) {
        newQuoteAuthorField.classList.add('is-invalid');
        newQuoteAuthorInvalid.classList.remove('field-validation-valid');
        newQuoteAuthorInvalid.classList.add('field-validation-error');
        newQuoteAuthorInvalid.innerHTML = '<span id="new-quote-author-error" class="is-invalid">Please input an author name.</span>';
    }
    else if (newQuoteAuthorField.classList.contains('is-invalid') && newQuoteAuthorInvalid) {
        newQuoteAuthorField.classList.remove('is-invalid');
        newQuoteAuthorInvalid.classList.add('field-validation-valid');
        newQuoteAuthorInvalid.classList.remove('field-validation-error');
        newQuoteAuthorInvalid.innerHTML = '';
    }

    if (!text || !author)
        return false;

    const quoteCount = quoteCountField.value;
    if (!(quoteCount >= 0) || !quoteTemplate || !quoteSelector || quoteSelector === '')
        return false;

    const target = document.querySelector(quoteSelector);

    if (!target)
        return false;

    const templateCopy = quoteTemplate.cloneNode(true);

    templateCopy.removeAttribute('id');
    templateCopy.classList.remove('d-none');
    templateCopy.setAttribute('data-subform', 'added');

    const qStatus = templateCopy.querySelector('#template-quote-status');
    qStatus.id = `Quotes_${quoteCount}__Status`;
    qStatus.name = `Quotes[${quoteCount}].Status`;
    qStatus.value = newQuoteStatusField.value;

    const qId = templateCopy.querySelector('#template-quote-id');
    qId.id = `Quotes_${quoteCount}__QuoteId`;
    qId.name = `Quotes[${quoteCount}].QuoteId`;
    qId.value = newQuoteIdField.value;

    const qText = templateCopy.querySelector('#template-quote-text');
    qText.id = `Quotes_${quoteCount}__Quote`;
    qText.name = `Quotes[${quoteCount}].Quote`;
    qText.value = text;
    qText.dataset.savedValue = text;
    qText.dataset.originalValue = text;

    const qTextDisplay = qText.parentNode.querySelector('[data-subform-display]');
    qTextDisplay.textContent = text;

    const qAuthor = templateCopy.querySelector('#template-quote-author');
    qAuthor.id = `Quotes_${quoteCount}__Author`;
    qAuthor.name = `Quotes[${quoteCount}].Author`;
    qAuthor.value = author;
    qAuthor.dataset.savedValue = author;
    qAuthor.dataset.originalValue = author;

    const qAuthorDisplay = qAuthor.parentNode.querySelector('[data-subform-display]');
    qAuthorDisplay.textContent = author;

    target.appendChild(templateCopy);
    
    const removeBtn = templateCopy.querySelector('[data-subform-remove]');

    removeBtn.addEventListener('click', function (event) {
        templateCopy.remove();
    });

    addQuoteBtn.form.reset();
    newQuoteTextField.disabled = false;
    newQuoteAuthorField.disabled = false;
    newQuoteStatusField.value = newQuoteStatusField.dataset.originalValue;

    if (newQuoteIdField.value !== newQuoteIdField.dataset.originalValue) {
        newQuoteIdField.form.querySelector(`[data-quote-id="${newQuoteIdField.value}"]`).parentNode.classList.add('d-none');

        newQuoteIdField.value = newQuoteIdField.dataset.originalValue;
    }

    quoteCountField.value++;
    $('#quoteModal').modal('hide');
});


////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CANCEL NEW QUOTE
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const cancelQuoteBtn = document.querySelector('[data-btn-cancel-quote]');

cancelQuoteBtn.addEventListener('click', function (event) {
    if (newQuoteTextField.classList.contains('is-invalid') && newQuoteTextInvalid) {
        newQuoteTextField.classList.remove('is-invalid');

        newQuoteTextInvalid.classList.add('field-validation-valid');
        newQuoteTextInvalid.classList.remove('field-validation-error');
        newQuoteTextInvalid.innerHTML = '';
    }

    if (newQuoteAuthorField.classList.contains('is-invalid') && newQuoteAuthorInvalid) {
        newQuoteAuthorField.classList.remove('is-invalid');
        newQuoteAuthorInvalid.classList.add('field-validation-valid');
        newQuoteAuthorInvalid.classList.remove('field-validation-error');
        newQuoteAuthorInvalid.innerHTML = '';
    }

    cancelQuoteBtn.form.reset();
    newQuoteTextField.disabled = false;
    newQuoteAuthorField.disabled = false;
    newQuoteStatusField.value = newQuoteStatusField.dataset.originalValue;
    newQuoteIdField.value = newQuoteIdField.dataset.originalValue;
});

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// ADD EXISTING QUOTE
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const quoteButtons = Array.from(document.querySelectorAll('[data-quote-id]'));
const selectQuoteToggle = document.querySelector('#select-quote');

quoteButtons.forEach(quoteBtn => {
    quoteBtn.addEventListener('click', function (event) {
        if (newQuoteTextField.classList.contains('is-invalid') && newQuoteTextInvalid) {
            newQuoteTextField.classList.remove('is-invalid');

            newQuoteTextInvalid.classList.add('field-validation-valid');
            newQuoteTextInvalid.classList.remove('field-validation-error');
            newQuoteTextInvalid.innerHTML = '';
        }

        if (newQuoteAuthorField.classList.contains('is-invalid') && newQuoteAuthorInvalid) {
            newQuoteAuthorField.classList.remove('is-invalid');
            newQuoteAuthorInvalid.classList.add('field-validation-valid');
            newQuoteAuthorInvalid.classList.remove('field-validation-error');
            newQuoteAuthorInvalid.innerHTML = '';
        }
        
        newQuoteStatusField.value = 'Unchanged';
        newQuoteIdField.value = quoteBtn.dataset.quoteId;
        newQuoteTextField.value = quoteBtn.dataset.quoteText;
        newQuoteTextField.disabled = true;
        newQuoteAuthorField.value = quoteBtn.dataset.quoteAuthor;
        newQuoteAuthorField.disabled = true;

        selectQuoteToggle.checked = false;
    });
});

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CANCEL/UNDO HIDDEN AVAILABLE QUOTES
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const quotesCancelBtn = document.querySelector('#quotes-form').querySelector('[data-btn-cancel]');

quotesCancelBtn.addEventListener('click', function (event) {
    quoteButtons.forEach(quoteBtn => {
        quoteBtn.parentNode.classList.remove('d-none');
    });
});

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// ADD NEW PET
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const addPetBtn = document.querySelector('[data-btn-add-pet]');
const petCountField = document.querySelector('#pet-count');
const newPetStatusField = addPetBtn.form.querySelector('#new-pet-status');
const newPetIdField = addPetBtn.form.querySelector('#new-pet-id');
const newPetNameField = addPetBtn.form.querySelector('#new-pet-name');
const newPetNameInvalid = newPetNameField.parentNode.querySelector('.invalid-feedback');
const newPetMoodField = addPetBtn.form.querySelector('#new-pet-mood');
const newPetMoodInvalid = newPetMoodField.parentNode.querySelector('.invalid-feedback');
const newPetKindField = addPetBtn.form.querySelector('#new-pet-kind');
const newPetKindInvalid = newPetKindField.parentNode.querySelector('.invalid-feedback');
const petTemplate = document.querySelector('#template-pet');
const petSelector = addPetBtn.dataset.addTarget;

addPetBtn.addEventListener('click', function (event) {
    const name = newPetNameField.value.trim();
    const mood = newPetMoodField.options[newPetMoodField.selectedIndex];
    const kind = newPetKindField.options[newPetKindField.selectedIndex];
    
    if (!name && newPetNameInvalid) {
        newPetNameField.classList.add('is-invalid');

        newPetNameInvalid.classList.remove('field-validation-valid');
        newPetNameInvalid.classList.add('field-validation-error');
        newPetNameInvalid.innerHTML = '<span id="new-quote-text-error" class="is-invalid">Please input a name.</span>';
    }
    else if (newPetNameField.classList.contains('is-invalid') && newPetNameInvalid) {
        newPetNameField.classList.remove('is-invalid');

        newPetNameInvalid.classList.add('field-validation-valid');
        newPetNameInvalid.classList.remove('field-validation-error');
        newPetNameInvalid.innerHTML = '';
    }
    
    if (!mood.value && newPetMoodInvalid) {
        newPetMoodField.classList.add('is-invalid');

        newPetMoodInvalid.classList.remove('field-validation-valid');
        newPetMoodInvalid.classList.add('field-validation-error');
        newPetMoodInvalid.innerHTML = '<span id="new-quote-author-error" class="is-invalid">Please select a mood.</span>';
    }
    else if (newPetMoodField.classList.contains('is-invalid') && newPetMoodInvalid) {
        newPetMoodField.classList.remove('is-invalid');

        newPetMoodInvalid.classList.add('field-validation-valid');
        newPetMoodInvalid.classList.remove('field-validation-error');
        newPetMoodInvalid.innerHTML = '';
    }

    if (!kind.value && newPetKindInvalid) {
        newPetKindField.classList.add('is-invalid');

        newPetKindInvalid.classList.remove('field-validation-valid');
        newPetKindInvalid.classList.add('field-validation-error');
        newPetKindInvalid.innerHTML = '<span id="new-quote-author-error" class="is-invalid">Please select a mood.</span>';
    }
    else if (newPetKindField.classList.contains('is-invalid') && newPetKindInvalid) {
        newPetKindField.classList.remove('is-invalid');

        newPetKindInvalid.classList.add('field-validation-valid');
        newPetKindInvalid.classList.remove('field-validation-error');
        newPetKindInvalid.innerHTML = '';
    }

    if (!name || !mood.value || !kind.value)
        return false;

    const petCount = petCountField.value;
    if (!(petCount >= 0) || !petTemplate || !petSelector || petSelector === '')
        return false;

    const target = document.querySelector(petSelector);

    if (!target)
        return false;

    const templateCopy = petTemplate.cloneNode(true);

    templateCopy.removeAttribute('id');
    templateCopy.classList.remove('d-none');
    templateCopy.setAttribute('data-subform', 'added');

    const pStatus = templateCopy.querySelector('#template-pet-status');
    pStatus.id = `Pets_${petCount}__Status`;
    pStatus.name = `Pets[${petCount}].Status`;
    pStatus.value = newPetStatusField.value;

    const pId = templateCopy.querySelector('#template-pet-id');
    pId.id = `Pets_${petCount}__PetId`;
    pId.name = `Pets[${petCount}].PetId`;
    pId.value = newPetIdField.value;

    const pName = templateCopy.querySelector('#template-pet-name');
    pName.id = `Pets_${petCount}__Name`;
    pName.name = `Pets[${petCount}].Name`;
    pName.value = name;
    pName.dataset.savedValue = name;
    pName.dataset.originalValue = name;

    const pNameDisplay = pName.parentNode.querySelector('[data-subform-display]');
    pNameDisplay.textContent = name;

    const pMood = templateCopy.querySelector('#template-pet-mood');
    pMood.id = `Pets_${petCount}__Mood`;
    pMood.name = `Pets[${petCount}].Mood`;
    pMood.value = mood.value;
    pMood.dataset.savedValue = mood.value;
    pMood.dataset.originalValue = mood.value;

    const pMoodDisplay = pMood.parentNode.querySelector('[data-subform-display');
    pMoodDisplay.textContent = mood.text;

    const pKind = templateCopy.querySelector('#template-pet-kind');
    pKind.id = `Pets_${petCount}__Kind`;
    pKind.name = `Pets[${petCount}].Kind`;
    pKind.value = kind.value;
    pKind.dataset.savedValue = kind.value;
    pKind.dataset.originalValue = kind.value;

    const pKindDisplay = pKind.parentNode.querySelector('[data-subform-display');
    pKindDisplay.textContent = kind.text;
    

    target.appendChild(templateCopy);

    const removeBtn = templateCopy.querySelector('[data-subform-remove]');

    removeBtn.addEventListener('click', function (event) {
        templateCopy.remove();
    });

    addPetBtn.form.reset();
    
    petCountField.value++;
    $('#petModal').modal('hide');
});

////////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// CANCEL NEW PET
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////

const cancelPetBtn = document.querySelector('[data-btn-cancel-pet]');

cancelPetBtn.addEventListener('click', function (event) {
    if (newPetNameField.classList.contains('is-invalid') && newPetNameInvalid) {
        newPetNameField.classList.remove('is-invalid');

        newPetNameInvalid.classList.add('field-validation-valid');
        newPetNameInvalid.classList.remove('field-validation-error');
        newPetNameInvalid.innerHTML = '';
    }

    if (newPetMoodField.classList.contains('is-invalid') && newPetMoodInvalid) {
        newPetMoodField.classList.remove('is-invalid');

        newPetMoodInvalid.classList.add('field-validation-valid');
        newPetMoodInvalid.classList.remove('field-validation-error');
        newPetMoodInvalid.innerHTML = '';
    }

    if (newPetKindField.classList.contains('is-invalid') && newPetKindInvalid) {
        newPetKindField.classList.remove('is-invalid');

        newPetKindInvalid.classList.add('field-validation-valid');
        newPetKindInvalid.classList.remove('field-validation-error');
        newPetKindInvalid.innerHTML = '';
    }

    cancelPetBtn.form.reset();
});