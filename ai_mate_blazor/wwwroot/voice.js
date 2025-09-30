(function(){
  const storeKey = 'aimate_voice_prompts_v1';
  const w = window;
  w.voice = w.voice || {};

  // Simple localStorage JSON helpers
  w.voice.getPrompts = function(){
    try { return JSON.parse(localStorage.getItem(storeKey) || '[]'); } catch { return []; }
  }

  // Helpers to open/close menu and focus simulate input
  w.voice.openMenu = function(){
    try {
      const btn = document.querySelector('.floating-mic-menu');
      if (btn) btn.click();
    } catch {}
  }
  w.voice.closeMenu = function(){
    try {
      const isOpen = !!document.querySelector('.voice-history');
      if (isOpen){
        const btn = document.querySelector('.floating-mic-menu');
        if (btn) btn.click();
      }
    } catch {}
  }
  w.voice.focusSimInput = function(){
    try {
      const input = document.getElementById('menu-sim-input');
      if (input){ input.focus(); input.select && input.select(); }
    } catch {}
  }
  w.voice.dismissChip = function(){
    try {
      const btn = document.querySelector('.voice-chip .clear');
      if (btn) btn.click();
    } catch {}
  }

  // Record audio for N seconds and return a base64 data URL (webm)
  w.voice.recordAudio = function(seconds){
    return new Promise(async (resolve) => {
      try {
        const dur = Math.max(1, Math.min(15, Number(seconds)||3));
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const chunks = [];
        const rec = new MediaRecorder(stream, { mimeType: 'audio/webm' });
        rec.ondataavailable = e => { if (e.data && e.data.size > 0) chunks.push(e.data); };
        rec.onstop = async () => {
          try {
            const blob = new Blob(chunks, { type: 'audio/webm' });
            const reader = new FileReader();
            reader.onloadend = () => { resolve(String(reader.result||'')); };
            reader.readAsDataURL(blob);
          } catch { resolve(''); }
          try { stream.getTracks().forEach(t => t.stop()); } catch {}
        };
        rec.start();
        setTimeout(() => { try { rec.stop(); } catch {} }, dur * 1000);
      } catch {
        resolve('');
      }
    });
  }

  // Bind global hotkeys
  w.voice.bindHotkeys = function(){
    try {
      // Ensure chip animation styles exist
      try {
        if (!document.getElementById('voice-chip-anim-style')){
          const st = document.createElement('style');
          st.id = 'voice-chip-anim-style';
          st.textContent = `
            .voice-chip { animation: chipIn 200ms ease-out; }
            @keyframes chipIn { from { transform: translateY(8px); opacity: .01; } to { transform: translateY(0); opacity: 1; } }
          `;
          document.head.appendChild(st);
        }
      } catch {}
      document.addEventListener('keydown', (e) => {
        const tag = (e.target && (e.target.tagName || '')).toLowerCase();
        if (tag === 'input' || tag === 'textarea' || e.metaKey || e.ctrlKey || e.altKey) return;
        if (e.key === '/'){
          e.preventDefault();
          if (!document.querySelector('.voice-history')) w.voice.openMenu();
          // Small delay to ensure menu rendered
          setTimeout(() => w.voice.focusSimInput(), 0);
        } else if (e.key === 'Escape'){
          if (document.querySelector('.voice-chip')) { w.voice.dismissChip(); return; }
          if (document.querySelector('.voice-history')) { w.voice.closeMenu(); return; }
        }
      }, true);
    } catch {}
  }

  // Simple global toast (no dependencies)
  let toastEl = null;
  w.voice.toast = function(message){
    try {
      if (!toastEl){
        toastEl = document.createElement('div');
        toastEl.style.position = 'fixed';
        toastEl.style.left = '50%';
        toastEl.style.transform = 'translateX(-50%)';
        toastEl.style.bottom = 'calc(84px + env(safe-area-inset-bottom))';
        toastEl.style.background = 'rgba(20,24,29,0.96)';
        toastEl.style.border = '1px solid rgba(200,121,62,0.28)';
        toastEl.style.borderRadius = '10px';
        toastEl.style.padding = '8px 12px';
        toastEl.style.color = '#e8edf2';
        toastEl.style.fontSize = '12px';
        toastEl.style.zIndex = '1000';
        toastEl.style.boxShadow = '0 10px 24px rgba(0,0,0,0.35)';
        document.body.appendChild(toastEl);
      }
      toastEl.textContent = String(message || '');
      toastEl.style.opacity = '1';
      clearTimeout(toastEl._timer);
      toastEl._timer = setTimeout(() => { toastEl.style.opacity = '0'; }, 1800);
    } catch {}
  }

  // Prefill simulate phrase in the floating menu and open it
  w.voice.prefillMenuSimulate = function(phrase){
    try {
      localStorage.setItem('aimate_voice_history_simulate', phrase || '');
      localStorage.setItem('aimate_voice_history_open', '1');
      const btn = document.querySelector('.floating-mic-menu');
      if (btn) btn.click();
    } catch (e) { console.warn('prefillMenuSimulate error', e); }
  }

  // Copy text to clipboard
  w.voice.copyText = async function(text){
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(text || '');
        return true;
      }
    } catch {}
    try {
      const ta = document.createElement('textarea');
      ta.value = text || '';
      ta.style.position = 'fixed';
      ta.style.top = '-9999px';
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      const ok = document.execCommand('copy');
      document.body.removeChild(ta);
      return !!ok;
    } catch (e) { console.warn('copyText error', e); return false; }
  }
  w.voice.setPrompts = function(list){
    try { localStorage.setItem(storeKey, JSON.stringify(list || [])); } catch {}
  }

  // Web Speech Recognition wrapper (prefixed across browsers)
  const SpeechRecognition = w.SpeechRecognition || w.webkitSpeechRecognition;

  function createRecognizer(){
    if (!SpeechRecognition) return null;
    const rec = new SpeechRecognition();
    rec.lang = 'en-US';
    rec.interimResults = false;
    rec.maxAlternatives = 1;
    return rec;
  }

  // Record once: returns a Promise<string> transcript
  w.voice.recordOnce = function(){
    return new Promise((resolve, reject) => {
      const rec = createRecognizer();
      if (!rec) { reject(new Error('SpeechRecognition not supported')); return; }
      let finished = false;
      rec.onresult = (e) => {
        if (finished) return;
        finished = true;
        const t = e.results && e.results[0] && e.results[0][0] ? e.results[0][0].transcript : '';
        rec.stop();
        resolve(t);
      };
      rec.onerror = (e) => { if (!finished) { finished = true; reject(e.error || e); } };
      rec.onend = () => { if (!finished) { finished = true; resolve(''); } };
      try { rec.start(); } catch (err) { reject(err); }
    });
  }

  // Continuous listening with callback into .NET via DotNetObjectReference
  let continuous = { rec: null, dotnet: null };

  w.voice.startListening = function(dotnet){
    if (continuous.rec) return true;
    const rec = createRecognizer();
    if (!rec) return false;
    continuous.rec = rec;
    continuous.dotnet = dotnet;
    rec.continuous = true;
    rec.onresult = (e) => {
      try {
        const idx = e.resultIndex;
        const t = e.results && e.results[idx] && e.results[idx][0] ? e.results[idx][0].transcript : '';
        if (t && dotnet && dotnet.invokeMethodAsync) {
          dotnet.invokeMethodAsync('OnRecognized', t);
        }
      } catch {}
    };
    rec.onerror = (e) => { console.warn('voice error', e); };
    rec.onend = () => {
      // attempt auto-restart if still enabled
      if (continuous.rec === rec) {
        try { rec.start(); } catch {}
      }
    };
    try { rec.start(); } catch (err) { console.warn(err); return false; }
    return true;
  }

  w.voice.stopListening = function(){
    if (continuous.rec) {
      try { continuous.rec.onend = null; continuous.rec.stop(); } catch {}
      continuous.rec = null;
      continuous.dotnet = null;
    }
  }

  // Trigger a client-side download
  w.voice.downloadFile = function(filename, content, mimeType){
    try {
      const blob = new Blob([content || ''], { type: mimeType || 'text/plain;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename || 'download.txt';
      document.body.appendChild(a);
      a.click();
      setTimeout(() => { document.body.removeChild(a); URL.revokeObjectURL(url); }, 0);
    } catch (e) { console.warn('downloadFile error', e); }
  }

  // Open a file picker and return the text content of the first selected file
  w.voice.pickFileText = function(accept){
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      if (accept) input.accept = accept;
      input.style.display = 'none';
      document.body.appendChild(input);
      input.onchange = () => {
        const file = input.files && input.files[0];
        if (!file) { document.body.removeChild(input); resolve(''); return; }
        const reader = new FileReader();
        reader.onload = () => { document.body.removeChild(input); resolve(String(reader.result || '')); };
        reader.onerror = () => { document.body.removeChild(input); resolve(''); };
        reader.readAsText(file);
      };
      input.click();
    });
  }
})();
