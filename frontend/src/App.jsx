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

  const handleLogin = (token) => {
    setAuthToken(token);
  };

  return (
    <div className="app">
      <SimpleErrorBoundary>
        {!authToken ? (
          <Login onLogin={handleLogin} />
        ) : (
          <NicodemouseProvider>
            <Dashboard />
          </NicodemouseProvider>
        )}
      </SimpleErrorBoundary>
    </div>
  );
}

export default App;
