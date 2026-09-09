const editors = new WeakMap();
const equal = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const normalize = value => ({ title: value.title, markdown: value.markdown, date: value.date, status: value.status });
function write(state) {
    try { sessionStorage.setItem(state.key, JSON.stringify(state.draft)); }
    catch { state.available = false; }
}
export function attach(element, key, saved) {
    saved = normalize(saved);
    const state = { key, draft: saved, saved, dirty: false, available: true };
    let pending = null;
    try {
        const text = sessionStorage.getItem(key);
        if (text) {
            const candidate = JSON.parse(text);
            if (candidate && ['title', 'markdown', 'date', 'status'].every(k => typeof candidate[k] === 'string')) {
                if (!equal(normalize(candidate), saved)) pending = normalize(candidate);
                else sessionStorage.removeItem(key);
            }
        }
        const probe = `${key}:probe`;
        sessionStorage.setItem(probe, '1'); sessionStorage.removeItem(probe);
    } catch { state.available = false; }
    if (pending) { state.draft = pending; state.dirty = true; }
    state.input = event => {
        if (!event.target.dataset.notebookField) return;
        const draft = {};
        for (const field of element.querySelectorAll('[data-notebook-field]')) draft[field.dataset.notebookField] = field.value;
        state.draft = normalize(draft); state.dirty = !equal(state.draft, state.saved);
        write(state);
    };
    state.leave = event => { if (state.dirty) { event.preventDefault(); event.returnValue = ''; } };
    element.addEventListener('input', state.input);
    element.addEventListener('change', state.input);
    window.addEventListener('beforeunload', state.leave);
    editors.set(element, state);
    return { available: state.available, pending };
}
export function remember(element, draft) {
    const state = editors.get(element); if (!state) return;
    state.draft = normalize(draft); state.dirty = !equal(state.draft, state.saved); write(state);
}
export function acknowledge(element, snapshot) {
    const state = editors.get(element); if (!state) return;
    state.saved = normalize(snapshot);
    state.dirty = !equal(state.saved, state.draft);
    try {
        if (!state.dirty) sessionStorage.removeItem(state.key);
        else write(state);
    } catch { state.available = false; }
    return state.available;
}
export function discard(element, saved) {
    const state = editors.get(element); if (!state) return;
    state.draft = normalize(saved); acknowledge(element, saved);
}
export function detach(element) {
    const state = editors.get(element); if (!state) return;
    element.removeEventListener('input', state.input);
    element.removeEventListener('change', state.input);
    window.removeEventListener('beforeunload', state.leave);
    editors.delete(element);
}

export function selectedText(textarea) {
    return textarea.value.slice(textarea.selectionStart, textarea.selectionEnd);
}
