import React, { useState, useEffect, useRef } from 'react';
import Switch from './Switch';

const Settings = () => {
    const [config, setConfig] = useState({
        primaryMonitor: 'Monitor 1 (Primary)',
        borderSide: 'Right',
        sensitivity: 0.7,
        autoConnect: true,
        lockInput: true,
        delay: 150,
        cornerSize: 50,
        gestureThreshold: 1000
    });

    const [services, setServices] = useState({
        input: true,
        audio: false,
        clipboard: true
    });

    const [systemInfo, setSystemInfo] = useState({
        machineName: 'This PC',
        monitors: [{ name: 'Monitor 1 (Primary)', isPrimary: true }]
    });

    const [connectionStatus, setConnectionStatus] = useState('Disconnected');
    const [remoteDeviceName, setRemoteDeviceName] = useState('');

    const isInitialLoad = useRef(true);

    // Load initial settings from backend
    useEffect(() => {
        const handleSettings = (e) => {
            if (!e.detail) return;
            try {
                const s = JSON.parse(e.detail);
                setConfig(prev => ({
                    ...prev,
                    borderSide: s.ActiveEdge || prev.borderSide,
                    sensitivity: s.MouseSensitivity || prev.sensitivity,
                    delay: s.SwitchingDelayMs ?? prev.delay,
                    cornerSize: s.DeadCornerSize ?? prev.cornerSize,
                    gestureThreshold: s.GestureThreshold ?? prev.gestureThreshold
                }));
                setServices({
                    input: s.EnableInput ?? true,
                    audio: s.EnableAudio ?? false,
                    clipboard: s.EnableClipboard ?? true
                });
                setTimeout(() => { isInitialLoad.current = false; }, 100);
            } catch (err) {
                console.error('[SETTINGS] Failed to parse settings:', err, e.detail);
            }
        };

        window.addEventListener('nicodemouse_settings', handleSettings);

        const handleSystemInfo = (e) => {
            if (e.detail) setSystemInfo(e.detail);
        };
        window.addEventListener('nicodemouse_system_info', handleSystemInfo);

        const handleStatus = (e) => {
            const status = e.detail?.status || '';
            setConnectionStatus(status);
            if (status.includes('Controlled by')) {
                setRemoteDeviceName(status.replace('Controlled by', '').trim());
            } else if (status.includes('Connected')) {
                // We are the controller, but the status might not immediately have the name here
                // We'll rely on the discovery list if needed, or wait for another update
            } else if (status === 'Disconnected') {
                setRemoteDeviceName('');
            }
        };
        window.addEventListener('nicodemouse_status', handleStatus);

        // Request settings
        const requestMessage = JSON.stringify({ type: 'get_settings' });
        if (window.external && window.external.sendMessage) window.external.sendMessage(requestMessage);
        else if (window.photino && window.photino.send) window.photino.send(requestMessage);
        else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) window.chrome.webview.postMessage(requestMessage);

        return () => {
            window.removeEventListener('nicodemouse_settings', handleSettings);
            window.removeEventListener('nicodemouse_system_info', handleSystemInfo);
            window.removeEventListener('nicodemouse_status', handleStatus);
        };
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
            sensitivity: parseFloat(config.sensitivity),
            gestureThreshold: parseInt(config.gestureThreshold)
        });

        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(message);
        } else if (window.photino && window.photino.send) {
            window.photino.send(message);
        } else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
            window.chrome.webview.postMessage(message);
        }
    }, [config.borderSide, config.lockInput, config.delay, config.cornerSize, config.sensitivity, config.gestureThreshold]);

    console.log('[FRONTEND] Settings Render State:', config);

    const toggleLock = () => {
        setConfig(prev => ({ ...prev, lockInput: !prev.lockInput }));
    };

    const restoreDefaults = () => {
        if (confirm('Restore all settings to factory defaults? This will also reset your active services.')) {
            const message = JSON.stringify({ type: 'reset_settings' });
            if (window.external && window.external.sendMessage) window.external.sendMessage(message);
            else if (window.photino && window.photino.send) window.photino.send(message);
            else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) window.chrome.webview.postMessage(message);
        }
    };

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
            <div className="glass" style={{ padding: '30px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderLeft: '4px solid var(--accent-primary)' }}>
                <div>
                    <h2 style={{ margin: 0, fontSize: '20px' }}>General Settings</h2>
                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Manage core application behavior and defaults.</p>
                </div>
                <button
                    onClick={restoreDefaults}
                    style={{
                        background: 'rgba(239, 68, 68, 0.1)',
                        border: '1px solid rgba(239, 68, 68, 0.2)',
                        color: '#ef4444',
                        padding: '10px 20px',
                        borderRadius: '10px',
                        fontSize: '13px',
                        fontWeight: '700',
                        cursor: 'pointer',
                        transition: 'all 0.2s',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px'
                    }}
                    onMouseOver={e => e.currentTarget.style.background = 'rgba(239, 68, 68, 0.2)'}
                    onMouseOut={e => e.currentTarget.style.background = 'rgba(239, 68, 68, 0.1)'}
                >
                    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    RESTORE SYSTEM DEFAULTS
                </button>
            </div>

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
                                style={{
                                    background: 'rgba(255,255,255,0.05)',
                                    border: '1px solid rgba(255,255,255,0.1)',
                                    padding: '12px',
                                    borderRadius: '10px',
                                    color: 'white',
                                    outline: 'none'
                                }}
                            >
                                {systemInfo.monitors.map((m, i) => (
                                    <option key={i} style={{ background: '#1e1e2e', color: 'white' }}>
                                        {m.name} {m.isPrimary ? '(Primary)' : ''}
                                    </option>
                                ))}
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

                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '20px' }}>
                        {/* Monitor Visualization */}
                        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                            <div style={{
                                position: 'relative',
                                width: '160px',
                                height: '90px',
                                border: '2px solid rgba(255,255,255,0.1)',
                                borderRadius: '8px',
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'center',
                                justifyContent: 'center',
                                background: 'rgba(255,255,255,0.02)',
                                boxShadow: '0 4px 15px rgba(0,0,0,0.3)'
                            }}>
                                <span style={{ fontSize: '11px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Local</span>
                                <span style={{ fontSize: '13px', color: 'white', fontWeight: '500', textAlign: 'center', padding: '0 10px' }}>{systemInfo.machineName}</span>

                                <div style={{
                                    position: 'absolute',
                                    right: config.borderSide === 'Right' ? '-2px' : 'auto',
                                    left: config.borderSide === 'Left' ? '-2px' : 'auto',
                                    width: '3px',
                                    height: '70%',
                                    background: 'var(--accent-primary)',
                                    borderRadius: '2px',
                                    boxShadow: '0 0 10px var(--accent-primary)',
                                    zIndex: 2
                                }} />

                                {/* Connection indicator line */}
                                <div style={{
                                    position: 'absolute',
                                    right: config.borderSide === 'Right' ? '-40px' : 'auto',
                                    left: config.borderSide === 'Left' ? '-40px' : 'auto',
                                    width: '40px',
                                    height: '2px',
                                    background: connectionStatus.includes('Connected') ? 'var(--accent-primary)' : 'rgba(255,255,255,0.1)',
                                    borderStyle: connectionStatus.includes('Connected') ? 'solid' : 'dashed',
                                    opacity: 0.5
                                }} />
                            </div>
                        </div>

                        {connectionStatus.includes('Connected') && (
                            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                                <div style={{
                                    position: 'relative',
                                    width: '160px',
                                    height: '90px',
                                    border: '2px solid var(--accent-primary)',
                                    borderRadius: '8px',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    background: 'rgba(99, 102, 241, 0.05)',
                                    boxShadow: '0 4px 20px rgba(99, 102, 241, 0.2)',
                                    animation: 'pulse 2s infinite'
                                }}>
                                    <span style={{ fontSize: '11px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Remote</span>
                                    <span style={{ fontSize: '13px', color: 'white', fontWeight: '500', textAlign: 'center', padding: '0 10px' }}>{remoteDeviceName || 'Remote Device'}</span>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

        </div>
    );
};

export default Settings;
