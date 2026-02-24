import React, { useState, useEffect, useRef } from 'react';

const Settings = () => {
    const [config, setConfig] = useState({
        primaryMonitor: 'Monitor 1 (Primary)',
        borderSide: 'Right',
        sensitivity: 0.7,
        autoConnect: true,
        lockInput: true,
        delay: 150,
        cornerSize: 50
    });

    const isInitialLoad = useRef(true);

    // Load initial settings from backend
    useEffect(() => {
        const handleSettings = (e) => {
            console.log('[FRONTEND] Received settings:', e.detail);
            const s = JSON.parse(e.detail);
            setConfig({
                borderSide: s.ActiveEdge,
                sensitivity: s.MouseSensitivity,
                lockInput: config.lockInput, // Backend might not store lockInput state yet as per current logic
                delay: s.SwitchingDelayMs,
                cornerSize: s.DeadCornerSize,
                autoConnect: true
            });
            setTimeout(() => { isInitialLoad.current = false; }, 100);
        };

        window.addEventListener('nicodemous_settings', handleSettings);

        // Request settings
        const requestMessage = JSON.stringify({ type: 'get_settings' });
        if (window.external && window.external.sendMessage) window.external.sendMessage(requestMessage);
        else if (window.photino && window.photino.send) window.photino.send(requestMessage);
        else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) window.chrome.webview.postMessage(requestMessage);

        return () => window.removeEventListener('nicodemous_settings', handleSettings);
    }, []);

    // Sync settings with backend whenever they change
    useEffect(() => {
        if (isInitialLoad.current) return;

        const message = JSON.stringify({
            type: 'update_settings',
            edge: config.borderSide,
            lockInput: config.lockInput,
            delay: parseInt(config.delay),
            cornerSize: parseInt(config.cornerSize),
            sensitivity: parseFloat(config.sensitivity)
        });

        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(message);
        } else if (window.photino && window.photino.send) {
            window.photino.send(message);
        } else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
            window.chrome.webview.postMessage(message);
        }
    }, [config.borderSide, config.lockInput]);

    const toggleLock = () => {
        setConfig(prev => ({ ...prev, lockInput: !prev.lockInput }));
    };

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
            <div className="glass" style={{ padding: '40px' }}>
                <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                    Display & Border Logic
                </h2>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '40px' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                        <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                            <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Active Monitor</span>
                            <select
                                className="glass-input"
                                value={config.primaryMonitor}
                                onChange={(e) => setConfig({ ...config, primaryMonitor: e.target.value })}
                                style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', padding: '12px', borderRadius: '10px', color: 'white' }}
                            >
                                <option>Monitor 1 (Primary)</option>
                                <option>Monitor 2 (Secondary)</option>
                            </select>
                        </label>

                        <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                            <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Crossing Edge</span>
                            <p style={{ fontSize: '12px', color: 'var(--text-dim)', margin: 0 }}>Which edge leads to the secondary computer?</p>
                            <div style={{ display: 'flex', gap: '10px', marginTop: '5px' }}>
                                {['Left', 'Right'].map(edge => (
                                    <button
                                        key={edge}
                                        onClick={() => setConfig({ ...config, borderSide: edge })}
                                        style={{
                                            flex: 1,
                                            padding: '10px',
                                            borderRadius: '8px',
                                            border: '1px solid',
                                            borderColor: config.borderSide === edge ? 'var(--accent-primary)' : 'rgba(255,255,255,0.1)',
                                            background: config.borderSide === edge ? 'rgba(99, 102, 241, 0.1)' : 'transparent',
                                            color: config.borderSide === edge ? 'var(--accent-primary)' : 'white',
                                            cursor: 'pointer'
                                        }}
                                    >
                                        {edge}
                                    </button>
                                ))}
                            </div>
                        </label>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <div style={{ position: 'relative', width: '200px', height: '120px', border: '2px solid rgba(255,255,255,0.1)', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.2)' }}>
                            <span style={{ fontSize: '12px', color: 'var(--text-dim)' }}>PC 1 (Local)</span>
                            <div style={{
                                position: 'absolute',
                                right: config.borderSide === 'Right' ? '-20px' : 'auto',
                                left: config.borderSide === 'Left' ? '-20px' : 'auto',
                                width: '4px',
                                height: '60%',
                                background: 'var(--accent-primary)',
                                borderRadius: '2px',
                                boxShadow: '0 0 10px var(--accent-primary)'
                            }} />
                        </div>
                    </div>
                </div>
            </div>

            <div className="glass" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '25px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                        <h3 style={{ margin: 0 }}>Advanced Input Locking</h3>
                        <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Prevent local mouse movement while controlling remote.</p>
                    </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                    <Switch checked={config.lockInput} onChange={toggleLock} />
                    <span style={{ fontSize: '14px', fontWeight: '600' }}>{config.lockInput ? 'Enabled' : 'Disabled'}</span>
                </div>

                <div style={{ padding: '20px', background: 'rgba(255,255,255,0.02)', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.05)', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '30px' }}>
                    <div>
                        <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Switching Delay</span>
                                <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{config.delay}ms</span>
                            </div>
                            <input
                                type="range" min="0" max="1000" step="50"
                                value={config.delay}
                                onChange={(e) => setConfig({ ...config, delay: e.target.value })}
                                style={{ width: '100%', accentColor: 'var(--accent-primary)' }}
                            />
                            <p style={{ margin: 0, fontSize: '11px', color: 'rgba(255,255,255,0.3)' }}>Hold mouse at edge for this long to cross.</p>
                        </label>
                    </div>

                    <div>
                        <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Dead Corner Size</span>
                                <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{config.cornerSize}px</span>
                            </div>
                            <input
                                type="range" min="0" max="200" step="10"
                                value={config.cornerSize}
                                onChange={(e) => setConfig({ ...config, cornerSize: e.target.value })}
                                style={{ width: '100%', accentColor: 'var(--accent-primary)' }}
                            />
                            <p style={{ margin: 0, fontSize: '11px', color: 'rgba(255,255,255,0.3)' }}>Ignore edge activation near screen corners.</p>
                        </label>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default Settings;
