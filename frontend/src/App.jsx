import React, { useEffect } from 'react';
import Dashboard from './components/Dashboard';
import './App.css';

function App() {
  useEffect(() => {
    // Listener for messages from Photino .NET backend
    if (window.photino) {
      window.photino.onMessage((message) => {
        console.log('Message from Backend:', message);
        try {
          const data = JSON.parse(message);
          // Handle incoming data like discovered devices, clipboard sync, etc.
          if (data.type === 'discovery_result') {
            const event = new CustomEvent('nicodemous_discovery', { detail: data.devices });
            window.dispatchEvent(event);
          }
          if (data.type === 'local_ip') {
            const event = new CustomEvent('nicodemous_ip', { detail: data.ip });
            window.dispatchEvent(event);
          }
        } catch (e) {
          console.error('Failed to parse backend message:', e);
        }
      });
    }
  }, []);

  return (
    <div className="app">
      <Dashboard />
    </div>
  );
}

export default App;
