import React, { useState } from 'react';
import { useNicodemous } from '../../context/NicodemousContext';
import Switch from '../Switch';

const ControlTab = () => {
    const {
        connectionStatus,
        connectedDevice,
        sessionRole,
        settings,
        toggleService,
        updateSettings,
        sendMessage,
        systemInfo
    } = useNicodemous();

    const restoreDefaults = () => {
        if (window.confirm('Restore all session settings to factory defaults?')) {
            sendMessage('reset_settings');
        }
    };

    const handleConfigChange = (key, value) => {
        const newSettings = { ...settings };
        // Map UI keys to backend keys if necessary
        const map = {
            borderSide: 'ActiveEdge',
            lockInput: 'LockInput',
            delay: 'SwitchingDelayMs',
            cornerSize: 'DeadCornerSize',
            sensitivity: 'MouseSensitivity',
            gestureThreshold: 'GestureThreshold'
        };

        const backendKey = map[key] || key;
        newSettings[backendKey] = value;

        // Sync to backend
        updateSettings({
            edge: key === 'borderSide' ? value : settings.ActiveEdge,
            lockInput: key === 'lockInput' ? value : settings.LockInput,
            delay: key === 'delay' ? parseInt(value) : settings.SwitchingDelayMs,
            cornerSize: key === 'cornerSize' ? parseInt(value) : settings.DeadCornerSize,
            sensitivity: key === 'sensitivity' ? parseFloat(value) : settings.MouseSensitivity,
            gestureThreshold: key === 'gestureThreshold' ? parseInt(value) : settings.GestureThreshold
        });
    };

    if (!connectionStatus.includes('Connected')) {
        return (
            <div className="glass" style={{ padding: '80px 40px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '20px' }}>
                <div style={{ width: '80px', height: '80px', borderRadius: '30px', background: 'rgba(255,255,255,0.02)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-dim)' }}>
                    <svg width="40" height="40" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                </div>
                <h2 style={{ fontSize: '24px', marginBottom: '10px' }}>No Active Connection</h2>
                <p style={{ color: 'var(--text-dim)', maxWidth: '400px' }}>Connect to a device from the Discovery tab to start sharing.</p>
            </div>
        );
    }

    const services = [
        { id: 'input', label: sessionRole === 'controlling' ? 'Remote Input' : 'Allow Remote Input', description: sessionRole === 'controlling' ? 'Cross monitors to take control.' : 'Allow the remote user to move your mouse.', icon: 'M15 15l-2 5L9 9l11 4-5 2z', color: 'var(--accent-primary)', enabled: settings?.EnableInput },
        { id: 'clipboard', label: 'Universal Clipboard', description: 'Share text and files seamlessly.', icon: 'M9 5a2 2 0 002 2h2a2 2 0 002-2', color: '#22c55e', enabled: settings?.EnableClipboard },
        { id: 'audio', label: sessionRole === 'controlling' ? 'Audio Stream' : 'Output Local Audio', description: sessionRole === 'controlling' ? 'Redirect audio output.' : 'Send system audio to remote.', icon: 'M15.536 8.464a5 5 0 010 7.072', color: '#a855f7', enabled: settings?.EnableAudio },
        { id: 'file', label: 'File Transfer', description: 'Drag and drop files.', icon: 'M9 12h6m-6 4h6', color: '#64748b', disabled: true }
    ];

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
            <div className="glass" style={{ padding: '40px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                        <div className="glow-button" style={{ width: '60px', height: '60px', borderRadius: '20px' }}>
                            <svg width="28" height="28" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                        </div>
                        <div>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                                <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', letterSpacing: '0.15em' }}>
                                    {sessionRole === 'controlling' ? 'Controlling Target' : 'Controlled by Peer'}
                                </span>
                                <div className="status-pulse" style={{ width: '8px', height: '8px', backgroundColor: '#22c55e' }}></div>
                            </div>
                            <h1 style={{ margin: '5px 0 0 0', fontSize: '32px' }}>{connectedDevice?.name || 'Remote Device'}</h1>
                        </div>
                    </div>
                    <button onClick={() => toggleService('disconnect', true)} className="glass" style={{ background: 'rgba(239, 68, 68, 0.1)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.2)', padding: '12px 25px', borderRadius: '12px', fontSize: '14px', fontWeight: 'bold' }}>
                        {sessionRole === 'controlling' ? 'Terminate Session' : 'Stop Sharing'}
                    </button>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '25px' }}>
                    {services.map(slot => (
                        <div key={slot.id} className="glass" style={{ padding: '25px', display: 'flex', flexDirection: 'column', gap: '15px', opacity: slot.disabled ? 0.5 : 1 }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                <div className="glow-button" style={{ width: '40px', height: '40px', background: 'rgba(255,255,255,0.03)', color: slot.color, padding: 0 }}>
                                    <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d={slot.icon} /></svg>
                                </div>
                                {!slot.disabled && <Switch checked={slot.enabled} onChange={() => toggleService(slot.id, !slot.enabled)} />}
                                {slot.disabled && <span style={{ fontSize: '11px', color: 'var(--text-dim)', background: 'rgba(255,255,255,0.08)', padding: '4px 8px', borderRadius: '6px' }}>Soon</span>}
                            </div>
                            <div>
                                <h3 style={{ margin: 0, fontSize: '16px' }}>{slot.label}</h3>
                                <p style={{ margin: '5px 0 0 0', fontSize: '12px', color: 'var(--text-dim)', lineHeight: '1.5' }}>{slot.description}</p>
                            </div>
                        </div>
                    ))}
                </div>

                {sessionRole === 'controlling' ? (
                    <div style={{ marginTop: '40px', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <div className="glass" style={{ padding: '30px' }}>
                            <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: '20px' }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                                        <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                                    </svg>
                                    Display & Border Logic
                                </div>
                                <button onClick={restoreDefaults} className="glass-btn-small">RESTORE DEFAULTS</button>
                            </h2>
                            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '40px' }}>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                                    <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Active Monitor</span>
                                        <select
                                            className="glass-input"
                                            value={settings?.ActiveMonitor || ''}
                                            onChange={(e) => handleConfigChange('ActiveMonitor', e.target.value)}
                                            style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', padding: '12px', borderRadius: '10px', color: 'white' }}
                                        >
                                            {systemInfo.monitors?.map((m, i) => (
                                                <option key={i} value={m.name} style={{ background: '#1e1e2e', color: 'white' }}>{m.name} {m.isPrimary ? '(Primary)' : ''}</option>
                                            ))}
                                        </select>
                                    </label>
                                    <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Crossing Edge</span>
                                        <div style={{ display: 'flex', gap: '10px', marginTop: '5px' }}>
                                            {['Left', 'Right'].map(edge => (
                                                <button key={edge} onClick={() => handleConfigChange('borderSide', edge)} style={{ flex: 1, padding: '10px', borderRadius: '8px', border: '1px solid', borderColor: settings?.ActiveEdge === edge ? 'var(--accent-primary)' : 'rgba(255,255,255,0.1)', background: settings?.ActiveEdge === edge ? 'rgba(99, 102, 241, 0.1)' : 'transparent', color: settings?.ActiveEdge === edge ? 'var(--accent-primary)' : 'white' }}>{edge}</button>
                                            ))}
                                        </div>
                                    </label>
                                </div>
                                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '20px' }}>
                                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                                        <div style={{ position: 'relative', width: '140px', height: '80px', border: '2px solid rgba(255,255,255,0.1)', borderRadius: '8px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: 'rgba(255,255,255,0.02)' }}>
                                            <span style={{ fontSize: '10px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', marginBottom: '4px' }}>Local</span>
                                            <span style={{ fontSize: '12px', color: 'white', textAlign: 'center', padding: '0 10px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', width: '100%' }}>{systemInfo.machineName}</span>
                                            <div style={{ position: 'absolute', right: settings?.ActiveEdge === 'Right' ? '-2px' : 'auto', left: settings?.ActiveEdge === 'Left' ? '-2px' : 'auto', width: '3px', height: '70%', background: 'var(--accent-primary)', borderRadius: '2px', boxShadow: '0 0 10px var(--accent-primary)' }} />
                                        </div>
                                    </div>
                                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                                        <div style={{ position: 'relative', width: '140px', height: '80px', border: '2px solid var(--accent-primary)', borderRadius: '8px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: 'rgba(99, 102, 241, 0.05)' }}>
                                            <span style={{ fontSize: '10px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', marginBottom: '4px' }}>Remote</span>
                                            <span style={{ fontSize: '12px', color: 'white', textAlign: 'center', padding: '0 10px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', width: '100%' }}>{connectedDevice?.name || 'Remote Device'}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Dynamics Section */}
                        <div className="glass" style={{ padding: '30px' }}>
                            <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: '20px' }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M15 15l-2 5L9 9l11 4-5 2z" /></svg>
                                    Mouse Dynamics & Precision
                                </div>
                                <button onClick={restoreDefaults} className="glass-btn-small">RESTORE DEFAULTS</button>
                            </h2>
                            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '30px' }}>
                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Sensitivity</span>
                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{Math.round((settings?.MouseSensitivity || 0.7) * 100)}%</span>
                                    </div>
                                    <input type="range" min="0.1" max="3.0" step="0.1" value={settings?.MouseSensitivity || 0.7} onChange={(e) => handleConfigChange('sensitivity', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                </label>
                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Return Force</span>
                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{settings?.GestureThreshold || 1000}px</span>
                                    </div>
                                    <input type="range" min="500" max="5000" step="100" value={settings?.GestureThreshold || 1000} onChange={(e) => handleConfigChange('gestureThreshold', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                </label>
                            </div>
                        </div>

                        {/* Locking Section */}
                        <div className="glass" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '25px' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                <div>
                                    <h3 style={{ margin: 0 }}>Input Locking & Crossing</h3>
                                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Behavior when cursor is at the edge or on remote.</p>
                                </div>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                                    <button onClick={restoreDefaults} className="glass-btn-small">RESTORE DEFAULTS</button>
                                    <div style={{ height: '30px', width: '1px', background: 'rgba(255,255,255,0.1)' }} />
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                                        <Switch checked={settings?.LockInput} onChange={() => handleConfigChange('lockInput', !settings?.LockInput)} />
                                        <span style={{ fontSize: '14px', fontWeight: '600', minWidth: '45px' }}>{settings?.LockInput ? 'Locked' : 'Free'}</span>
                                    </div>
                                </div>
                            </div>
                            <div style={{ padding: '20px', background: 'rgba(255,255,255,0.02)', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.05)', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '30px' }}>
                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Switching Delay</span>
                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{settings?.SwitchingDelayMs || 150}ms</span>
                                    </div>
                                    <input type="range" min="0" max="1000" step="50" value={settings?.SwitchingDelayMs || 150} onChange={(e) => handleConfigChange('delay', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                </label>
                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Dead Corner</span>
                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{settings?.DeadCornerSize || 50}px</span>
                                    </div>
                                    <input type="range" min="0" max="200" step="10" value={settings?.DeadCornerSize || 50} onChange={(e) => handleConfigChange('cornerSize', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                </label>
                            </div>
                        </div>
                    </div>
                ) : (
                    <div className="glass" style={{ marginTop: '40px', padding: '30px', textAlign: 'center', border: '1px dashed rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.01)' }}>
                        <p style={{ color: 'var(--text-dim)', fontSize: '14px', margin: 0 }}>
                            <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2" style={{ marginRight: '8px', verticalAlign: 'middle' }}>
                                <path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                            Session settings are managed by the controlling device.
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default ControlTab;
