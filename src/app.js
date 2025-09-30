// AI Mate - Enhanced UI Features

// Toast Notification System
class ToastManager {
  constructor() {
    this.container = null;
    this.init();
  }

  init() {
    if (!this.container) {
      this.container = document.createElement('div');
      this.container.className = 'toast-container';
      document.body.appendChild(this.container);
    }
  }

  show(message, type = 'info', duration = 3000) {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    const icons = {
      success: '✓',
      error: '✕',
      info: 'ℹ'
    };
    
    toast.innerHTML = `
      <span class="toast-icon">${icons[type] || 'ℹ'}</span>
      <span class="toast-message">${message}</span>
      <button class="toast-close" aria-label="Close">×</button>
    `;
    
    this.container.appendChild(toast);
    
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => this.remove(toast));
    
    if (duration > 0) {
      setTimeout(() => this.remove(toast), duration);
    }
    
    return toast;
  }

  remove(toast) {
    toast.style.animation = 'toastSlide 0.3s reverse';
    setTimeout(() => {
      if (toast.parentNode) {
        toast.parentNode.removeChild(toast);
      }
    }, 300);
  }

  success(message, duration) { return this.show(message, 'success', duration); }
  error(message, duration) { return this.show(message, 'error', duration); }
  info(message, duration) { return this.show(message, 'info', duration); }
}

const toast = new ToastManager();

// Offline Detection
class OfflineManager {
  constructor() {
    this.banner = null;
    this.init();
  }

  init() {
    window.addEventListener('online', () => this.handleOnline());
    window.addEventListener('offline', () => this.handleOffline());
    
    if (!navigator.onLine) {
      this.handleOffline();
    }
  }

  handleOffline() {
    if (!this.banner) {
      this.banner = document.createElement('div');
      this.banner.className = 'offline-banner';
      this.banner.textContent = '📡 You are offline. Some features may be limited.';
      document.body.insertBefore(this.banner, document.body.firstChild);
    }
    toast.info('You are offline', 0);
  }

  handleOnline() {
    if (this.banner) {
      this.banner.style.animation = 'slideDown 0.3s reverse';
      setTimeout(() => {
        if (this.banner && this.banner.parentNode) {
          this.banner.parentNode.removeChild(this.banner);
          this.banner = null;
        }
      }, 300);
    }
    toast.success('Back online!', 2000);
  }
}

const offlineManager = new OfflineManager();

// Voice Assistant with Web Speech API
class VoiceAssistant {
  constructor() {
    this.recognition = null;
    this.isRecording = false;
    this.transcript = [];
    this.init();
  }

  init() {
    if ('webkitSpeechRecognition' in window || 'SpeechRecognition' in window) {
      const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
      this.recognition = new SpeechRecognition();
      this.recognition.continuous = false;
      this.recognition.interimResults = true;
      this.recognition.lang = 'en-US';
      
      this.recognition.onresult = (event) => this.handleResult(event);
      this.recognition.onerror = (event) => this.handleError(event);
      this.recognition.onend = () => this.handleEnd();
    }
  }

  start() {
    if (!this.recognition) {
      toast.error('Speech recognition not supported in this browser');
      return;
    }
    
    if (this.isRecording) return;
    
    try {
      this.recognition.start();
      this.isRecording = true;
      this.updateUI(true);
    } catch (e) {
      console.error('Speech recognition error:', e);
      toast.error('Could not start speech recognition');
    }
  }

  stop() {
    if (this.recognition && this.isRecording) {
      this.recognition.stop();
    }
  }

  handleResult(event) {
    const result = event.results[event.results.length - 1];
    const transcriptText = result[0].transcript;
    
    const transcriptEl = document.querySelector('.transcript');
    if (transcriptEl) {
      transcriptEl.textContent = transcriptText;
    }
    
    if (result.isFinal) {
      this.addToHistory(transcriptText);
      this.processCommand(transcriptText);
    }
  }

  handleError(event) {
    console.error('Speech recognition error:', event.error);
    if (event.error === 'no-speech') {
      toast.info('No speech detected. Please try again.');
    } else if (event.error === 'not-allowed') {
      toast.error('Microphone permission denied');
    } else {
      toast.error('Speech recognition error: ' + event.error);
    }
    this.isRecording = false;
    this.updateUI(false);
  }

  handleEnd() {
    this.isRecording = false;
    this.updateUI(false);
  }

  updateUI(recording) {
    const micButton = document.querySelector('.mic-button');
    if (micButton) {
      if (recording) {
        micButton.classList.add('pulse');
        micButton.innerHTML = '🔴 Recording...';
      } else {
        micButton.classList.remove('pulse');
        micButton.innerHTML = '🎙️ Hold to Speak';
      }
    }
  }

  addToHistory(text) {
    const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    this.transcript.unshift({ time, text });
    
    // Keep only last 10
    if (this.transcript.length > 10) {
      this.transcript = this.transcript.slice(0, 10);
    }
    
    this.renderHistory();
  }

  renderHistory() {
    let historyEl = document.querySelector('.transcript-history');
    if (!historyEl) {
      historyEl = document.createElement('div');
      historyEl.className = 'transcript-history';
      const assistantEl = document.querySelector('.assistant');
      if (assistantEl) {
        assistantEl.appendChild(historyEl);
      }
    }
    
    historyEl.innerHTML = this.transcript.map(item => `
      <div class="transcript-item">
        <div class="transcript-time">${item.time}</div>
        <div class="transcript-text">${item.text}</div>
      </div>
    `).join('');
  }

  processCommand(text) {
    const lower = text.toLowerCase();
    
    // Simple command processing
    if (lower.includes('show') || lower.includes('go to') || lower.includes('open')) {
      if (lower.includes('job')) {
        this.navigateTo('/pages/jobs.html', 'Jobs');
      } else if (lower.includes('client')) {
        this.navigateTo('/pages/clients.html', 'Clients');
      } else if (lower.includes('invoice')) {
        this.navigateTo('/pages/invoices.html', 'Invoices');
      } else if (lower.includes('home')) {
        this.navigateTo('/pages/home.html', 'Home');
      } else if (lower.includes('setting')) {
        this.navigateTo('/pages/settings.html', 'Settings');
      }
    } else {
      toast.info('Command recognized: ' + text);
    }
  }

  navigateTo(url, title) {
    htmx.ajax('GET', url, { target: '#content', swap: 'innerHTML' });
    document.getElementById('title').textContent = title;
    history.pushState({}, title, url);
    toast.success(`Opening ${title}`);
  }
}

const voiceAssistant = new VoiceAssistant();

// Search functionality
class SearchManager {
  constructor(selector) {
    this.searchInput = document.querySelector(selector);
    if (this.searchInput) {
      this.init();
    }
  }

  init() {
    this.searchInput.addEventListener('input', (e) => this.handleSearch(e));
    
    const clearBtn = this.searchInput.parentElement.querySelector('.search-clear');
    if (clearBtn) {
      clearBtn.addEventListener('click', () => {
        this.searchInput.value = '';
        this.handleSearch({ target: this.searchInput });
      });
    }
  }

  handleSearch(event) {
    const query = event.target.value.toLowerCase();
    const items = document.querySelectorAll('.list-item, .card');
    
    items.forEach(item => {
      const text = item.textContent.toLowerCase();
      if (text.includes(query)) {
        item.style.display = '';
      } else {
        item.style.display = 'none';
      }
    });
  }
}

// Local Storage Manager for persistence
class StorageManager {
  static save(key, data) {
    try {
      localStorage.setItem(`aiMate_${key}`, JSON.stringify(data));
      return true;
    } catch (e) {
      console.error('Storage error:', e);
      return false;
    }
  }

  static load(key) {
    try {
      const data = localStorage.getItem(`aiMate_${key}`);
      return data ? JSON.parse(data) : null;
    } catch (e) {
      console.error('Storage error:', e);
      return null;
    }
  }

  static remove(key) {
    localStorage.removeItem(`aiMate_${key}`);
  }
}

// Theme manager
class ThemeManager {
  constructor() {
    this.currentTheme = StorageManager.load('theme') || 'auto';
    this.apply();
  }

  toggle() {
    const themes = ['auto', 'light', 'dark'];
    const currentIndex = themes.indexOf(this.currentTheme);
    this.currentTheme = themes[(currentIndex + 1) % themes.length];
    this.apply();
    StorageManager.save('theme', this.currentTheme);
    toast.success(`Theme: ${this.currentTheme}`);
  }

  apply() {
    const root = document.documentElement;
    if (this.currentTheme === 'light') {
      root.style.colorScheme = 'light';
    } else if (this.currentTheme === 'dark') {
      root.style.colorScheme = 'dark';
    } else {
      root.style.colorScheme = '';
    }
  }
}

const themeManager = new ThemeManager();

// Enhanced HTMX event handlers
document.body.addEventListener('htmx:afterSwap', (evt) => {
  if (evt.target.id === 'content') {
    // Initialize search on new pages
    const searchBar = evt.target.querySelector('.search-input');
    if (searchBar) {
      new SearchManager('.search-input');
    }
    
    // Initialize voice assistant on assistant page
    const micButton = evt.target.querySelector('.mic-button');
    if (micButton && !micButton.dataset.initialized) {
      micButton.dataset.initialized = 'true';
      micButton.addEventListener('click', () => voiceAssistant.start());
    }
    
    // Initialize theme toggle on settings page
    const themeToggle = evt.target.querySelector('[data-action="toggle-theme"]');
    if (themeToggle && !themeToggle.dataset.initialized) {
      themeToggle.dataset.initialized = 'true';
      themeToggle.addEventListener('click', () => themeManager.toggle());
    }
  }
});

// Show loading toast for long requests
document.body.addEventListener('htmx:beforeRequest', (evt) => {
  if (evt.detail.elt.dataset.showLoading !== 'false') {
    evt.detail._loadingToast = toast.info('Loading...', 0);
  }
});

document.body.addEventListener('htmx:afterRequest', (evt) => {
  if (evt.detail._loadingToast) {
    toast.remove(evt.detail._loadingToast);
  }
  
  if (evt.detail.successful) {
    // Optional success feedback
    if (evt.detail.elt.dataset.successMessage) {
      toast.success(evt.detail.elt.dataset.successMessage);
    }
  } else {
    toast.error('Request failed. Please try again.');
  }
});

// Export for global access
window.aiMate = {
  toast,
  voiceAssistant,
  themeManager,
  StorageManager
};
