// Run with: node tests/browser/sessionNotebook.test.mjs
import fs from 'node:fs';
import assert from 'node:assert/strict';
const source = fs.readFileSync(new URL('../../src/AethericGm.Web/wwwroot/sessionNotebook.js', import.meta.url), 'utf8');
const storage = new Map();
const windowListeners = new Map();
const sessionStorage = {
    getItem: key => storage.get(key) ?? null,
    setItem: (key, value) => storage.set(key, value),
    removeItem: key => storage.delete(key)
};
const window = {
    addEventListener: (name, callback) => windowListeners.set(name, callback),
    removeEventListener: (name, callback) => { if (windowListeners.get(name) === callback) windowListeners.delete(name); }
};
const api = new Function('sessionStorage', 'window', source.replaceAll('export function', 'function') + '\nreturn { attach, remember, acknowledge, discard, detach, selectedText };')(sessionStorage, window);
const saved = { title: 'Notebook', markdown: 'Prep', date: '', status: 'Draft' };
function editor() {
    const listeners = new Map();
    const fields = Object.entries(saved).map(([name, value]) => ({ dataset: { notebookField: name }, value }));
    return {
        addEventListener: (name, callback) => listeners.set(name, callback),
        removeEventListener: name => listeners.delete(name),
        querySelectorAll: () => fields,
        type(text) { fields.find(f => f.dataset.notebookField === 'markdown').value = text; listeners.get('input')({ target: fields[1] }); }
    };
}
const element = editor();
assert.equal(api.attach(element, 'user:session', saved).pending, null);
element.type('During play');
assert.equal(JSON.parse(storage.get('user:session')).markdown, 'During play');
element.type('Newer unsaved prose');
api.acknowledge(element, { ...saved, markdown: 'During play' });
assert.equal(JSON.parse(storage.get('user:session')).markdown, 'Newer unsaved prose', 'an older save must not clear newer browser edits');
api.detach(element);
assert.equal(windowListeners.size, 0);
const refreshed = editor();
assert.equal(api.attach(refreshed, 'user:session', saved).pending.markdown, 'Newer unsaved prose');
api.discard(refreshed, saved);
assert.equal(storage.has('user:session'), false);
api.remember(refreshed, { ...saved, markdown: 'Restored revision' });
api.acknowledge(refreshed, { ...saved, markdown: 'Restored revision' });
assert.equal(storage.has('user:session'), false);
api.detach(refreshed);
assert.equal(api.attach(editor(), 'different-user:session', saved).pending, null);
const originalSet = sessionStorage.setItem;
sessionStorage.setItem = () => { throw new Error('Storage disabled'); };
assert.equal(api.attach(editor(), 'blocked-storage', saved).available, false);
sessionStorage.setItem = originalSet;
console.log('Session notebook draft recovery checks passed.');

assert.equal(api.selectedText({ value: 'Done.\nUnresolved: bell\nLater.', selectionStart: 6, selectionEnd: 22 }), 'Unresolved: bell');
assert.equal(api.selectedText({ value: 'Notebook', selectionStart: 2, selectionEnd: 2 }), '');
console.log('Session continuity text selection checks passed.');
