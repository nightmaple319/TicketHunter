const API = '';

// --- Config Load/Save ---
async function loadConfig() {
    try {
        const res = await fetch(`${API}/api/load`);
        const config = await res.json();
        applyConfigToUI(config);
    } catch (e) {
        console.error('載入設定失敗:', e);
    }
}

function applyConfigToUI(c) {
    setValue('homepage', c.homepage);
    setValue('ticketNumber', c.ticket_number);

    // Date auto-select
    setChecked('dateEnable', c.date_auto_select?.enable);
    setValue('dateKeyword', c.date_auto_select?.keyword);
    setValue('dateMode', c.date_auto_select?.mode);

    // Area auto-select
    setChecked('areaEnable', c.area_auto_select?.enable);
    setValue('areaKeyword', c.area_auto_select?.keyword);
    setValue('areaExclude', c.area_auto_select?.keyword_exclude);
    setValue('areaMode', c.area_auto_select?.mode);

    // OCR
    setChecked('ocrEnable', c.ocr?.enable);
    setChecked('ocrForceSubmit', c.ocr?.force_submit);
    setValue('ocrModelPath', c.ocr?.model_path);
    setValue('ocrImageSource', c.ocr?.image_source);

    // Accounts
    setValue('tixcraftSid', c.accounts?.tixcraft_sid);
    setValue('ticketmasterCookie', c.accounts?.ticketmaster_cookie);

    // Contact
    setValue('realName', c.contact?.real_name);
    setValue('phone', c.contact?.phone);
    setValue('email', c.contact?.email);
    setValue('creditCardPrefix', c.contact?.credit_card_prefix);

    // Advanced
    setChecked('headless', c.advanced?.headless);
    setChecked('playSound', c.advanced?.play_sound);
    setChecked('verbose', c.advanced?.verbose);
    setChecked('autoSubmitTicket', c.advanced?.auto_submit_ticket ?? true);
    setValue('autoReloadInterval', c.advanced?.auto_reload_interval);
    setValue('maxRetry', c.advanced?.max_retry);
    setValue('proxyServer', c.advanced?.proxy_server);
    setValue('webPort', c.advanced?.web_port);
    setValue('scheduleStart', c.advanced?.schedule_start);
    setValue('discordWebhookUrl', c.advanced?.discord_webhook_url);
    setValue('telegramBotToken', c.advanced?.telegram_bot_token);
    setValue('telegramChatId', c.advanced?.telegram_chat_id);
}

function buildConfigFromUI() {
    return {
        homepage: getValue('homepage'),
        ticket_number: parseInt(getValue('ticketNumber')) || 2,
        browser: 'chrome',
        date_auto_select: {
            enable: getChecked('dateEnable'),
            keyword: getValue('dateKeyword'),
            mode: getValue('dateMode')
        },
        area_auto_select: {
            enable: getChecked('areaEnable'),
            keyword: getValue('areaKeyword'),
            keyword_exclude: getValue('areaExclude'),
            mode: getValue('areaMode')
        },
        ocr: {
            enable: getChecked('ocrEnable'),
            force_submit: getChecked('ocrForceSubmit'),
            model_path: getValue('ocrModelPath'),
            image_source: getValue('ocrImageSource')
        },
        accounts: {
            tixcraft_sid: getValue('tixcraftSid'),
            ticketmaster_cookie: getValue('ticketmasterCookie')
        },
        contact: {
            real_name: getValue('realName'),
            phone: getValue('phone'),
            email: getValue('email'),
            credit_card_prefix: getValue('creditCardPrefix')
        },
        advanced: {
            headless: getChecked('headless'),
            play_sound: getChecked('playSound'),
            verbose: getChecked('verbose'),
            auto_submit_ticket: getChecked('autoSubmitTicket'),
            auto_reload_interval: parseInt(getValue('autoReloadInterval')) || 3,
            max_retry: parseInt(getValue('maxRetry')) || 3,
            proxy_server: getValue('proxyServer'),
            web_port: parseInt(getValue('webPort')) || 16888,
            schedule_start: getValue('scheduleStart'),
            discord_webhook_url: getValue('discordWebhookUrl'),
            telegram_bot_token: getValue('telegramBotToken'),
            telegram_chat_id: getValue('telegramChatId'),
            idle_keyword: '',
            resume_keyword: ''
        }
    };
}

async function saveConfig() {
    try {
        const config = buildConfigFromUI();
        const res = await fetch(`${API}/api/save`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });
        if (res.ok) {
            showSaveStatus('已儲存！');
        }
    } catch (e) {
        showSaveStatus('儲存失敗！');
    }
}

// --- Bot Control ---
async function botAction(action) {
    try {
        await fetch(`${API}/api/${action}`, { method: 'POST' });
    } catch (e) {
        console.error(`Bot ${action} 操作失敗:`, e);
    }
}

// --- Status Polling ---
async function pollStatus() {
    try {
        const res = await fetch(`${API}/api/status`);
        const status = await res.json();

        const badge = document.getElementById('statusBadge');
        const msg = document.getElementById('statusMessage');

        const stateMap = {
            0: ['閒置', 'bg-secondary'],
            1: ['執行中', 'bg-success'],
            2: ['已暫停', 'bg-warning'],
            3: ['等待驗證碼', 'bg-info'],
            4: ['訂單完成', 'bg-primary'],
            5: ['錯誤', 'bg-danger']
        };

        const [text, cls] = stateMap[status.state] || ['未知', 'bg-secondary'];
        badge.textContent = text;
        badge.className = `badge ${cls}`;
        msg.textContent = status.message || '';

        // Update captcha question
        if (status.captchaQuestion) {
            document.getElementById('captchaQuestion').textContent = status.captchaQuestion;
            const q = encodeURIComponent(status.captchaQuestion);
            document.getElementById('searchGoogle').href = `https://www.google.com/search?q=${q}`;
            document.getElementById('searchBing').href = `https://www.bing.com/search?q=${q}`;
        }
    } catch (e) { /* ignore polling errors */ }
}

// --- Tools ---
async function testDiscord() {
    await fetch(`${API}/api/test-discord`, { method: 'POST' });
    alert('Discord 測試訊息已送出！');
}

async function testTelegram() {
    await fetch(`${API}/api/test-telegram`, { method: 'POST' });
    alert('Telegram 測試訊息已送出！');
}

async function testOcr() {
    const file = document.getElementById('ocrTestFile').files[0];
    if (!file) return alert('請先選擇圖片');

    const buf = await file.arrayBuffer();
    const res = await fetch(`${API}/api/ocr`, {
        method: 'POST',
        body: buf
    });
    const data = await res.json();
    document.getElementById('ocrTestResult').textContent = `辨識結果：${data.result}`;
}

function setHomepage(url) {
    document.getElementById('homepage').value = url;
}

// --- Theme ---
function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute('data-bs-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-bs-theme', next);
    localStorage.setItem('theme', next);
}

// --- Helpers ---
function getValue(id) { return document.getElementById(id)?.value || ''; }
function setValue(id, val) { const el = document.getElementById(id); if (el && val != null) el.value = val; }
function getChecked(id) { return document.getElementById(id)?.checked || false; }
function setChecked(id, val) { const el = document.getElementById(id); if (el) el.checked = !!val; }

function showSaveStatus(text) {
    const el = document.getElementById('saveStatus');
    el.textContent = text;
    setTimeout(() => el.textContent = '', 2000);
}

// --- Version ---
async function loadVersion() {
    try {
        const res = await fetch(`${API}/api/version`);
        const data = await res.json();
        document.getElementById('versionLabel').textContent = `v${data.version}`;
    } catch (e) { /* ignore */ }
}

// --- Init ---
document.addEventListener('DOMContentLoaded', () => {
    const saved = localStorage.getItem('theme');
    if (saved) document.documentElement.setAttribute('data-bs-theme', saved);

    loadConfig();
    loadVersion();
    setInterval(pollStatus, 500);
});
