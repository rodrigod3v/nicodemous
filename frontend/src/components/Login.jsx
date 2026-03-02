import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Shield, Lock, User, ArrowRight, Loader2, AlertCircle } from 'lucide-react';
import AnimatedLogo from './AnimatedLogo';
import './Login.css';

const Login = ({ onLogin }) => {
    const [isSetupMode, setIsSetupMode] = useState(false);
    const [isForgotMode, setIsForgotMode] = useState(false);
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

        const isPhotino = (window.external && window.external.sendMessage) || (window.chrome && window.chrome.webview);

        if (isPhotino) {
            const requestId = Math.random().toString(36).substring(7);

            const handleProxyResponse = (message) => {
                try {
                    const data = typeof message === 'string' ? JSON.parse(message) : message;
                    if (data.type === 'proxy_response' && data.requestId === requestId) {
                        if (data.success) {
                            if (isSetupMode) {
                                alert('System initialized! Please login.');
                                setIsSetupMode(false);
                                setPassword('');
                            } else {
                                const authData = JSON.parse(data.body);
                                localStorage.setItem('nicodemouse_token', authData.token);
                                onLogin(authData.token);
                            }
                        } else {
                            setError(data.error || 'Connection failed via Backend Proxy.');
                        }
                        setLoading(false);
                        window.removeEventListener('message', handleBridge);
                    }
                } catch (err) {
                    console.error('[AUTH] Proxy parse error:', err);
                    setError('Failed to process server response.');
                    setLoading(false);
                }
            };

            const handleBridge = (e) => handleProxyResponse(e.data || e);
            window.addEventListener('message', handleBridge);

            const message = JSON.stringify({
                type: 'proxy_request',
                requestId,
                url,
                method: 'POST',
                body: JSON.stringify({ username, password })
            });

            if (window.external && window.external.sendMessage) {
                window.external.sendMessage(message);
            } else if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(message);
            }
            return;
        }

        try {
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
            } else {
                const text = await response.text();
                setError(text || 'Authentication failed.');
            }
        } catch (err) {
            setError('Could not connect to Signaling Server. Check your connection.');
        } finally {
            setLoading(false);
        }
    };

    const containerVariants = {
        hidden: { opacity: 0 },
        visible: {
            opacity: 1,
            transition: { staggerChildren: 0.1, delayChildren: 0.3 }
        }
    };

    const itemVariants = {
        hidden: { opacity: 0, y: 20 },
        visible: { opacity: 1, y: 0, transition: { duration: 0.5, ease: "easeOut" } }
    };

    return (
        <div className="login-container">
            <div className="login-background">
                <motion.div
                    animate={{
                        scale: [1, 1.2, 1],
                        x: [0, 50, 0],
                        y: [0, 30, 0]
                    }}
                    transition={{ duration: 20, repeat: Infinity, ease: "easeInOut" }}
                    className="orb orb-1"
                />
                <motion.div
                    animate={{
                        scale: [1, 1.1, 1],
                        x: [0, -40, 0],
                        y: [0, -20, 0]
                    }}
                    transition={{ duration: 25, repeat: Infinity, ease: "easeInOut", delay: 2 }}
                    className="orb orb-2"
                />
            </div>

            <motion.div
                initial={{ opacity: 0, scale: 0.9, y: 20 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                transition={{ duration: 0.8, ease: [0.16, 1, 0.3, 1] }}
                className="login-card"
            >
                <motion.div
                    variants={containerVariants}
                    initial="hidden"
                    animate="visible"
                >
                    <motion.div variants={itemVariants} className="login-header">
                        <div className="logo-wrapper">
                            <AnimatedLogo size={100} />
                        </div>
                        <h1 className="gradient-text">nicodemouse</h1>
                        <p>
                            {isForgotMode ? 'Protocolo de Recuperação' :
                                isSetupMode ? 'System Initialization' : 'Digital Fortress Access'}
                        </p>
                    </motion.div>

                    <AnimatePresence mode="wait">
                        {isForgotMode ? (
                            <motion.div
                                key="forgot"
                                initial={{ opacity: 0, x: 20 }}
                                animate={{ opacity: 1, x: 0 }}
                                exit={{ opacity: 0, x: -20 }}
                                className="forgot-view"
                            >
                                <div className="info-box">
                                    <Shield className="info-icon" size={24} />
                                    <p>Para sua segurança, a recuperação de senha é realizada através de um administrador físico ou via chave de segurança mestre.</p>
                                </div>
                                <div className="support-actions">
                                    <p className="support-text">Siga as instruções do seu manual de implantação ou contate o suporte interno.</p>
                                    <motion.button
                                        whileHover={{ scale: 1.05 }}
                                        whileTap={{ scale: 0.95 }}
                                        onClick={() => setIsForgotMode(false)}
                                        className="secondary-button"
                                        style={{ width: '100%', marginTop: '20px' }}
                                    >
                                        Voltar ao Login
                                    </motion.button>
                                </div>
                            </motion.div>
                        ) : (
                            <motion.div
                                key="login-form"
                                initial={{ opacity: 0, x: -20 }}
                                animate={{ opacity: 1, x: 0 }}
                                exit={{ opacity: 0, x: 20 }}
                            >
                                {error && (
                                    <motion.div
                                        initial={{ opacity: 0, height: 0 }}
                                        animate={{ opacity: 1, height: 'auto' }}
                                        exit={{ opacity: 0, height: 0 }}
                                        className="login-error"
                                    >
                                        <AlertCircle size={18} />
                                        <span>{error}</span>
                                    </motion.div>
                                )}

                                <form onSubmit={handleSubmit}>
                                    <motion.div variants={itemVariants} className="form-group">
                                        <label>Identificador</label>
                                        <div className="input-wrapper">
                                            <User size={20} />
                                            <input
                                                type="text"
                                                className="login-input"
                                                placeholder="Nome de usuário"
                                                value={username}
                                                onChange={(e) => setUsername(e.target.value)}
                                                required
                                                autoComplete="username"
                                            />
                                        </div>
                                    </motion.div>

                                    <motion.div variants={itemVariants} className="form-group">
                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                            <label>Senha de Acesso</label>
                                            <span
                                                className="forgot-link"
                                                onClick={() => setIsForgotMode(true)}
                                            >
                                                Esqueceu?
                                            </span>
                                        </div>
                                        <div className="input-wrapper">
                                            <Lock size={20} />
                                            <input
                                                type="password"
                                                className="login-input"
                                                placeholder="••••••••••••"
                                                value={password}
                                                onChange={(e) => setPassword(e.target.value)}
                                                required
                                                autoComplete="current-password"
                                            />
                                        </div>
                                    </motion.div>

                                    <motion.button
                                        variants={itemVariants}
                                        whileHover={{ scale: 1.02, y: -2 }}
                                        whileTap={{ scale: 0.98 }}
                                        type="submit"
                                        disabled={loading}
                                        className="glow-button login-button"
                                    >
                                        {loading ? (
                                            <>
                                                <Loader2 className="animate-spin" size={20} />
                                                <span>Autenticando...</span>
                                            </>
                                        ) : (
                                            <>
                                                <span>{isSetupMode ? 'Criar Administrador' : 'Entrar no Sistema'}</span>
                                                <ArrowRight size={20} />
                                            </>
                                        )}
                                    </motion.button>
                                </form>
                            </motion.div>
                        )}
                    </AnimatePresence>

                    <motion.div variants={itemVariants} className="login-footer">
                        {!isForgotMode && (
                            <span
                                onClick={() => {
                                    setIsSetupMode(!isSetupMode);
                                    setError('');
                                }}
                                className="setup-link"
                            >
                                {isSetupMode ? '← Voltar para o Login' : 'Primeiro acesso? Configurar sistema'}
                            </span>
                        )}
                    </motion.div>
                </motion.div>
            </motion.div>
        </div>
    );
};

export default Login;
