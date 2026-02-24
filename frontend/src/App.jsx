import React, { useEffect } from 'react';
import Dashboard from './components/Dashboard';
import './App.css';

function App() {
  useEffect(() => {
    const handleBackendMessage = (message) => {
      console.log('Message from Backend:', message);
      try {
        const data = JSON.parse(message);
        if (data.type === 'discovery_result') {
          const event = new CustomEvent('nicodemous_discovery', { detail: data.devices });
          window.dispatchEvent(event);
        }
        if (data.type === 'local_ip') {
          const event = new CustomEvent('nicodemous_ip', { detail: data.ip });
          window.dispatchEvent(event);
        }
        if (data.type === 'settings_data') {
          const event = new CustomEvent('nicodemous_settings', { detail: data.settings });
          window.dispatchEvent(event);
        }
        if (data.type === 'connection_status') {
          const event = new CustomEvent('nicodemous_status', { detail: data.status });
          window.dispatchEvent(event);
        }
      } catch (e) {
        console.error('Failed to parse backend message:', e);
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
      <Dashboard />
    </div>
  );
}

export default App;
