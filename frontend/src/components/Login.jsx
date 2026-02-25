import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { Shield, Lock, User } from 'lucide-react';
import './Login.css';

const Login = ({ onLogin, backendIp }) => {
    const [isSetupMode, setIsSetupMode] = useState(false);
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [loading, setLoading] = useState(false);

    // Exclusive production signaling server (VM)
    const SIGNALING_BASE = 'http://144.22.254.132:8080';

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        setLoading(true);

        const endpoint = isSetupMode ? '/api/auth/signup' : '/api/auth/login';
        const url = `${SIGNALING_BASE}${endpoint}`;

        try {
            console.log(`[AUTH] Attempting ${endpoint} at ${SIGNALING_BASE}`);
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password }),
                signal: AbortSignal.timeout(10000)
            });

            if (response.ok) {
                if (isSetupMode) {
                    alert('System initialized! Please login.');
                    setIsSetupMode(false);
                    setPassword('');
                } else {
                    const data = await response.json();
                    localStorage.setItem('nicodemouse_token', data.token);
                    onLogin(data.token);
                }
                setLoading(false);
                return;
            } else {
                const text = await response.text();
                setError(text || 'Action failed.');
            }
        } catch (err) {
            console.error(`[AUTH] Failed to connect to ${SIGNALING_BASE}:`, err);
            setError(`Could not connect to VM Signaling Server. Check your internet connection or firewall.`);
        }

        setLoading(false);
    };

    return (
        <div className="login-container">
            <div className="login-background">
                <div className="orb orb-1"></div>
                <div className="orb orb-2"></div>
            </div>

            <motion.div
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8, ease: "easeOut" }}
                className="login-card glass"
            >
                <div className="login-header">
                    <div className="logo-container">
                        <Shield size={32} />
                    </div>
                    <h1 className="gradient-text">nicodemouse</h1>
                    <p>{isSetupMode ? 'Create initial administrator' : 'Secure Access System'}</p>
                </div>

                {error && (
                    <motion.div
                        initial={{ opacity: 0, scale: 0.95 }}
                        animate={{ opacity: 1, scale: 1 }}
                        className="login-error"
                    >
                        {error}
                    </motion.div>
                )}

                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label>Username</label>
                        <div className="input-wrapper">
                            <User size={18} />
                            <input
                                type="text"
                                className="login-input"
                                placeholder="Enter username"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    <div className="form-group">
                        <label>Password</label>
                        <div className="input-wrapper">
                            <Lock size={18} />
                            <input
                                type="password"
                                className="login-input"
                                placeholder="••••••••"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                required
                            />
                        </div>
                    </div>

                    <button
                        type="submit"
                        disabled={loading}
                        className="glow-button login-button"
                    >
                        {loading ? 'Processing...' : (isSetupMode ? 'Initialize System' : 'Sign In')}
                    </button>
                </form>

                <div className="login-footer" style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                    <span
                        onClick={() => setIsSetupMode(!isSetupMode)}
                        className="setup-link"
                    >
                        {isSetupMode ? '← Back to Login' : 'Setup First Time Account?'}
                    </span>
                </div>
            </motion.div>
        </div>
    );
};

export default Login;
