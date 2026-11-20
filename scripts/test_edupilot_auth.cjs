const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const script = fs.readFileSync(path.join(__dirname, '../web/wwwroot/js/edupilot-auth.js'), 'utf8');
function item(dataset = {}) {
    const classes = new Set();
    return { dataset, hidden: false, handlers: {}, attrs: {},
        classList: { toggle(name, enabled) { enabled ? classes.add(name) : classes.delete(name); }, contains(name) { return classes.has(name); } },
        addEventListener(name, callback) { this.handlers[name] = callback; },
        setAttribute(name, value) { this.attrs[name] = value; },
        getAttribute(name) { return this.attrs[name]; },
        removeAttribute(name) { delete this.attrs[name]; }, click() { this.handlers.click({ preventDefault() {} }); }
    };
}
test('Onboarding changes immediately and repeated clicks never wait for animations or Bootstrap', () => {
    const panels = [item(), item()];
    const dots = [item({ onboardSlide: '0' }), item({ onboardSlide: '1' })];
    const next = item(), start = item();
    const slides = { querySelectorAll: selector => selector === '.carousel-item' ? panels : dots };
    const document = { querySelectorAll: () => [], getElementById: id => ({ onboardCarousel: slides, onboardNext: next, onboardStart: start })[id] };
    vm.runInNewContext(script, { document });
    assert.equal(panels[0].hidden, false);
    assert.equal(panels[1].hidden, true);
    for (let i = 0; i < 25; i++) {
        next.click();
        assert.equal(panels[0].hidden, true);
        assert.equal(panels[1].hidden, false);
        assert.equal(start.hidden, false);
        assert.equal(dots[1].attrs['aria-current'], 'true');
        dots[0].click();
        assert.equal(next.hidden, false);
        assert.equal(start.hidden, true);
        assert.equal(panels[0].hidden, false);
    }
});
test('Password visibility changes its accessible label and restores masking', () => {
    const button = item({ target: 'Password' });
    const input = { type: 'password' };
    const icon = item(); icon.attrs.href = '/images/edupilot-icons.svg#eye';
    button.querySelector = () => icon;
    const document = { querySelectorAll: () => [button], getElementById: id => id === 'Password' ? input : null };
    vm.runInNewContext(script, { document });
    button.click();
    assert.equal(input.type, 'text');
    assert.equal(button.attrs['aria-label'], 'Hide password');
    assert.equal(icon.attrs.href, '/images/edupilot-icons.svg#eye-off');
    button.click();
    assert.equal(input.type, 'password');
    assert.equal(button.attrs['aria-label'], 'Show password');
    assert.equal(button.attrs['aria-pressed'], 'false');
});
