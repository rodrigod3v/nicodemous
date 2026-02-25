import React, { useState, useEffect } from 'react';
import Dashboard from './components/Dashboard';
import Login from './components/Login';
import { NicodemouseProvider } from './context/nicodemouseContext';
import './App.css';

console.log('[FRONTEND] App.jsx module loading...');

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
          <pre>{this.state.error ? this.state.error.toString() : ''}</pre>
        </div>
      );
    }
    return this.props.children;
  }
}

function App() {
  console.log('[FRONTEND] App component initializing...');
  const [authToken, setAuthToken] = useState(localStorage.getItem('nicodemouse_token'));
  const [localIp, setLocalIp] = useState(null);

  useEffect(() => {
    const handleMessage = (message) => {
      try {
        const data = typeof message === 'string' ? JSON.parse(message) : message;
        console.log('[FRONTEND] App received message:', data.type);
        if (data.type === 'local_ip') {
          setLocalIp(data.detail.ip);
        }
      } catch (e) {
        console.error('[FRONTEND] Error in App message listener:', e);
      }
    };

    // Setup unified message listener
    if (window.external && window.external.receiveMessage) {
      window.external.receiveMessage(handleMessage);
    } else if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', (e) => handleMessage(e.data || e));
    } else if (window.photino) {
      window.photino.receive && window.photino.receive(handleMessage);
    }
  }, []);

  const handleLogin = (token) => {
    setAuthToken(token);
  };

  const handleLogout = () => {
    localStorage.removeItem('nicodemouse_token');
    setAuthToken(null);
  };

  return (
    <div className="app">
      <SimpleErrorBoundary>
        {!authToken ? (
          <Login onLogin={handleLogin} backendIp={localIp} />
        ) : (
          <NicodemouseProvider>
            <Dashboard onLogout={handleLogout} />
          </NicodemouseProvider>
        )}
      </SimpleErrorBoundary>
    </div>
  );
}

export default App;
