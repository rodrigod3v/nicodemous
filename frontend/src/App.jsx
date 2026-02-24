import React, { useEffect, useState } from 'react';
import Dashboard from './components/Dashboard';
import './App.css';

console.log('[FRONTEND] App.jsx module loading...');

window.onerror = (msg, url, line, col, error) => {
  console.error("[CRITICAL] Global JS Error (Module Scope):", { msg, url, line, col, error });
};

window.onunhandledrejection = (event) => {
  console.error("[CRITICAL] Unhandled Rejection (Module Scope):", event.reason);
};

class SimpleErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }
  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }
  componentDidCatch(error, errorInfo) {
    console.error("[CRITICAL] Frontend Error Caught:", error, errorInfo);
  }
  render() {
    if (this.state.hasError) {
      return (
        <div style={{ padding: '50px', background: '#1a1a1a', color: '#ff4444' }}>
          <h1>Something went wrong.</h1>
          <pre>{this.state.error.toString()}</pre>
        </div>
      );
    }
    return this.props.children;
  }
}

function App() {
  console.log('[FRONTEND] App component initializing...');
  useEffect(() => {
    console.log('[FRONTEND] App component mounted');

    const handleBackendMessage = (message) => {
      try {
        if (!message) return;
        const data = JSON.parse(message);
        if (!data) return;
        console.log('[FRONTEND] Parsed backend message:', data.type, data);
        if (data.type === 'discovery_result') {
          const event = new CustomEvent('nicodemous_discovery', { detail: data.devices });
          window.dispatchEvent(event);
        }
        if (data.type === 'local_ip') {
          const event = new CustomEvent('nicodemous_ip', { detail: data.detail });
          window.dispatchEvent(event);
        }
        if (data.type === 'system_info') {
          const event = new CustomEvent('nicodemous_system_info', { detail: data });
          window.dispatchEvent(event);
        }
        if (data.type === 'connection_status') {
          const event = new CustomEvent('nicodemous_status', { detail: data.status });
          window.dispatchEvent(event);
        }
      } catch (e) {
        console.error('[FRONTEND] Critical handling error:', e);
      }
    };

    // Unified message handler
    if (window.external && window.external.receiveMessage) {
      window.external.receiveMessage(handleBackendMessage);
    } else if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', (e) => handleBackendMessage(e.data));
    } else if (window.photino) {
      // Fallback for older Photino versions
      window.photino.receive && window.photino.receive(handleBackendMessage);
    }
  }, []);

  return (
    <div className="app">
      <SimpleErrorBoundary>
        <Dashboard />
      </SimpleErrorBoundary>
    </div>
  );
}

export default App;
