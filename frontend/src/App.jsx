import React, { useState, useEffect } from 'react';
import Dashboard from './components/Dashboard';
import Login from './components/Login';
import TitleBar from './components/layout/TitleBar';
import { NicodemouseProvider, useNicodemouse } from './context/nicodemouseContext';
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

  const handleLogin = (token) => {
    setAuthToken(token);
  };

  const handleLogout = () => {
    localStorage.removeItem('nicodemouse_token');
    setAuthToken(null);
  };

  return (
    <div className="app">
      <TitleBar />
      <div className="app-content-wrapper">
        <SimpleErrorBoundary>
          <NicodemouseProvider>
            {!authToken ? (
              <LoginWithContext onLogin={handleLogin} />
            ) : (
              <Dashboard onLogout={handleLogout} />
            )}
          </NicodemouseProvider>
        </SimpleErrorBoundary>
      </div>
    </div>
  );
}

// Wrapper to consume context inside the same file if needed, or just let Login handle its own context
const LoginWithContext = ({ onLogin }) => {
  const { localIp } = useNicodemouse();
  return <Login onLogin={onLogin} backendIp={localIp?.ip} />;
};
export default App;
