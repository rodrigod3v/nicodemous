import React from 'react';
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

    const handleConfigChange = (key, value) => {
        const map = {
            borderSide: 'ActiveEdge',
            lockInput: 'LockInput',
            delay: 'SwitchingDelayMs',
            cornerSize: 'DeadCornerSize',
            sensitivity: 'MouseSensitivity',
            gestureThreshold: 'GestureThreshold'
        };

        const backendKey = map[key] || key;
        const newSettings = { ...settings, [backendKey]: value };

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
            <div className="glass animate-fade" style={{ padding: '80px 40px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '20px' }}>
                <div style={{ width: '80px', height: '80px', borderRadius: '30px', background: 'rgba(255,255,255,0.02)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-dim)', border: '1px solid rgba(255,255,255,0.05)' }}>
                    <svg width="40" height="40" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                </div>
                <h2 style={{ fontSize: '24px', fontWeight: '700' }}>No Active Session</h2>
                <p style={{ color: 'var(--text-dim)', maxWidth: '400px', lineHeight: '1.6' }}>Establish a connection with a device in the Discovery tab to begin controlling or sharing resources.</p>
            </div>
        );
    }

    const services = [
        { id: 'input', label: 'Inputs', icon: 'M15 15l-2 5L9 9l11 4-5 2z', color: 'var(--accent-primary)', enabled: settings?.EnableInput },
        { id: 'clipboard', label: 'Clipboard', icon: 'M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3', color: '#22c55e', enabled: settings?.EnableClipboard },
        { id: 'audio', label: 'Audio', icon: 'M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z', color: '#a855f7', enabled: settings?.EnableAudio },
        { id: 'file', label: 'Transfer', icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z', color: '#64748b', disabled: true }
    ];

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '25px' }}>
            {/* Control Center Header */}
            <div className="glass" style={{ padding: '30px', position: 'relative', overflow: 'hidden' }}>
                <div style={{ position: 'absolute', top: '-50px', right: '-50px', width: '200px', height: '200px', background: 'radial-gradient(circle, var(--accent-primary) 0%, transparent 70%)', opacity: '0.05', pointerEvents: 'none' }}></div>

                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '25px' }}>
                        <div style={{ position: 'relative' }}>
                            <div className="glow-button" style={{ width: '70px', height: '70px', borderRadius: '24px', background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)' }}>
                                <svg width="32" height="32" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                            </div>
                            <div style={{ position: 'absolute', bottom: '-5px', right: '-5px', width: '24px', height: '24px', borderRadius: '50%', background: '#1e1e2e', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '2px solid rgba(255,255,255,0.1)' }}>
                                <div className="status-pulse" style={{ width: '10px', height: '10px', backgroundColor: '#22c55e' }}></div>
                            </div>
                        </div>
                        <div>
                            <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '800', textTransform: 'uppercase', letterSpacing: '0.2em' }}>
                                {sessionRole === 'controlling' ? 'CONTROLLING SESSION' : 'EXTERNAL ACCESS'}
                            </span>
                            <h1 style={{ margin: '5px 0 0 0', fontSize: '36px', fontWeight: '800', letterSpacing: '-0.02em' }}>{connectedDevice?.name || 'Remote Host'}</h1>
                        </div>
                    </div>
                    <button onClick={() => toggleService('disconnect', true)} className="glass-btn" style={{ background: 'rgba(239, 68, 68, 0.08)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.2)', padding: '14px 28px', borderRadius: '14px', fontSize: '14px', fontWeight: '800', transition: 'all 0.2s ease' }}>
                        {sessionRole === 'controlling' ? 'CLOSE SESSION' : 'STOP SHARING'}
                    </button>
                </div>
            </div>

            {/* Service Intelligence Grid */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '20px' }}>
                {services.map(slot => (
                    <div
                        key={slot.id}
                        className="glass animate-fade"
                        style={{
                            padding: '24px',
                            display: 'flex',
                            flexDirection: 'column',
                            gap: '18px',
                            opacity: slot.disabled ? 0.4 : 1,
                            background: !slot.disabled && slot.enabled ? `linear-gradient(135deg, rgba(255,255,255,0.03) 0%, ${slot.color}08 100%)` : 'rgba(255,255,255,0.02)',
                            border: !slot.disabled && slot.enabled ? `1px solid ${slot.color}33` : '1px solid rgba(255,255,255,0.05)',
                            transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
                        }}
                    >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <div style={{
                                width: '44px',
                                height: '44px',
                                borderRadius: '12px',
                                background: !slot.disabled && slot.enabled ? `${slot.color}15` : 'rgba(255,255,255,0.03)',
                                color: !slot.disabled && slot.enabled ? slot.color : 'var(--text-dim)',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                transition: 'all 0.3s ease'
                            }}>
                                <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d={slot.icon} /></svg>
                            </div>
                            {!slot.disabled && <Switch checked={slot.enabled} onChange={() => toggleService(slot.id, !slot.enabled)} />}
                            {slot.disabled && <span style={{ fontSize: '10px', color: 'var(--text-dim)', background: 'rgba(255,255,255,0.08)', padding: '4px 8px', borderRadius: '6px', fontWeight: 'bold' }}>TBD</span>}
                        </div>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                            <span style={{ fontSize: '15px', fontWeight: '800' }}>{slot.label}</span>
                            <span style={{ fontSize: '12px', color: 'var(--text-dim)', fontWeight: '500' }}>
                                {!slot.disabled && slot.enabled ? 'Enabled' : (slot.disabled ? 'Coming Soon' : 'Inactive')}
                            </span>
                        </div>
                    </div>
                ))}
            </div>

            {sessionRole === 'controlling' ? (
                <div style={{ display: 'grid', gridTemplateColumns: '1.2fr 1fr', gap: '25px' }}>
                    {/* Monitor Crossing Setup */}
                    <div className="glass" style={{ padding: '35px', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <h2 style={{ fontSize: '20px', fontWeight: '800', margin: 0, display: 'flex', alignItems: 'center', gap: '12px' }}>
                                <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" /></svg>
                                Setup Spatial
                            </h2>
                            <div style={{ display: 'flex', background: 'rgba(255,255,255,0.03)', padding: '4px', borderRadius: '10px', border: '1px solid rgba(255,255,255,0.05)' }}>
                                {['Left', 'Right'].map(edge => (
                                    <button
                                        key={edge}
                                        onClick={() => handleConfigChange('borderSide', edge)}
                                        style={{
                                            padding: '8px 20px',
                                            borderRadius: '8px',
                                            fontSize: '12px',
                                            fontWeight: '800',
                                            background: settings?.ActiveEdge === edge ? 'var(--accent-primary)' : 'transparent',
                                            color: settings?.ActiveEdge === edge ? 'white' : 'var(--text-dim)',
                                            border: 'none',
                                            cursor: 'pointer',
                                            transition: 'all 0.2s ease'
                                        }}
                                    >
                                        {edge.toUpperCase()}
                                    </button>
                                ))}
                            </div>
                        </div>

                        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '15px' }}>
                            <div className="glass" style={{ width: '200px', height: '120px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', position: 'relative', border: '2px solid rgba(255,255,255,0.05)', background: 'rgba(255,255,255,0.01)' }}>
                                <span style={{ fontSize: '10px', color: 'var(--text-dim)', fontWeight: '800', textTransform: 'uppercase', marginBottom: '4px' }}>YOU</span>
                                <span style={{ fontSize: '13px', fontWeight: '700', padding: '0 15px', textAlign: 'center', opacity: 0.8 }}>{systemInfo.machineName}</span>
                                <div style={{
                                    position: 'absolute',
                                    right: settings?.ActiveEdge === 'Right' ? '-2px' : 'auto',
                                    left: settings?.ActiveEdge === 'Left' ? '-2px' : 'auto',
                                    width: '4px',
                                    height: '80%',
                                    background: 'var(--accent-primary)',
                                    borderRadius: '50px',
                                    boxShadow: '0 0 15px var(--accent-primary)'
                                }} />
                            </div>
                            <div style={{ color: 'var(--accent-primary)', fontSize: '24px', fontWeight: 'bold' }}>→</div>
                            <div className="glass" style={{
                                width: '200px',
                                height: '120px',
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: 'center',
                                justifyContent: 'center',
                                border: '2px solid var(--accent-primary)',
                                background: 'rgba(99, 102, 241, 0.05)',
                                color: 'var(--accent-primary)'
                            }}>
                                <span style={{ fontSize: '10px', fontWeight: '800', textTransform: 'uppercase', marginBottom: '4px' }}>REMOTE</span>
                                <span style={{ fontSize: '14px', fontWeight: '800', padding: '0 15px', textAlign: 'center' }}>{connectedDevice?.name}</span>
                            </div>
                        </div>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                            <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '700' }}>Active Monitor</span>
                            <select
                                className="glass-input"
                                value={settings?.ActiveMonitor || ''}
                                onChange={(e) => handleConfigChange('ActiveMonitor', e.target.value)}
                                style={{ width: '100%', padding: '12px', background: 'rgba(255,255,255,0.02)', fontWeight: '600' }}
                            >
                                {systemInfo.monitors?.map((m, i) => (
                                    <option key={i} value={m.name} style={{ background: '#1e1e2e' }}>{m.name} {m.isPrimary ? '(Main)' : ''}</option>
                                ))}
                            </select>
                        </div>
                    </div>

                    {/* Session Intelligence */}
                    <div className="glass" style={{ padding: '35px', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <h2 style={{ fontSize: '20px', fontWeight: '800', margin: 0, display: 'flex', alignItems: 'center', gap: '12px' }}>
                            <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M13 10V3L4 14h7v7l9-11h-7z" /></svg>
                            Session Settings
                        </h2>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '25px' }}>
                            {/* Sensitivity */}
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <span style={{ fontSize: '14px', color: 'var(--text-dim)', fontWeight: '700' }}>Mouse Sensitivity</span>
                                    <span style={{ fontSize: '14px', color: 'var(--accent-primary)', fontWeight: '800' }}>{Math.round((settings?.MouseSensitivity || 1.0) * 100)}%</span>
                                </div>
                                <input type="range" min="0.1" max="3.0" step="0.1" value={settings?.MouseSensitivity || 1.0} onChange={(e) => handleConfigChange('sensitivity', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                            </div>

                            {/* Return Force */}
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <span style={{ fontSize: '14px', color: 'var(--text-dim)', fontWeight: '700' }}>Snap-back Force</span>
                                    <span style={{ fontSize: '14px', color: 'var(--accent-primary)', fontWeight: '800' }}>{settings?.GestureThreshold || 1000}px</span>
                                </div>
                                <input type="range" min="500" max="5000" step="100" value={settings?.GestureThreshold || 1000} onChange={(e) => handleConfigChange('gestureThreshold', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                            </div>

                            {/* Switching Latency */}
                            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <span style={{ fontSize: '14px', color: 'var(--text-dim)', fontWeight: '700' }}>Switching Latency</span>
                                    <span style={{ fontSize: '14px', color: 'var(--accent-primary)', fontWeight: '800' }}>{settings?.SwitchingDelayMs || 150}ms</span>
                                </div>
                                <input type="range" min="0" max="1000" step="50" value={settings?.SwitchingDelayMs || 150} onChange={(e) => handleConfigChange('delay', e.target.value)} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                            </div>

                            {/* Input Locking Toggle */}
                            <div className="glass" style={{ padding: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: settings?.LockInput ? 'rgba(99, 102, 241, 0.05)' : 'transparent', border: '1px solid rgba(255,255,255,0.05)' }}>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
                                    <span style={{ fontSize: '14px', fontWeight: '800' }}>Lock Local Input</span>
                                    <span style={{ fontSize: '11px', color: 'var(--text-dim)' }}>Block local movement on remote control.</span>
                                </div>
                                <Switch checked={settings?.LockInput} onChange={() => handleConfigChange('lockInput', !settings?.LockInput)} />
                            </div>
                        </div>
                    </div>
                </div>
            ) : (
                <div className="glass" style={{ padding: '40px', textAlign: 'center', border: '2px dashed rgba(255,255,255,0.05)', background: 'rgba(255,255,255,0.01)' }}>
                    <div style={{ width: '50px', height: '50px', background: 'rgba(255,255,255,0.03)', borderRadius: '15px', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 15px auto', color: 'var(--accent-primary)' }}>
                        <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
                    </div>
                    <p style={{ color: 'var(--text-dim)', fontSize: '15px', fontWeight: '600', maxWidth: '400px', margin: '0 auto', lineHeight: '1.6' }}>
                        This session is currently being managed by <span style={{ color: 'white' }}>{connectedDevice?.name}</span>. Technical settings are inherited from the controller.
                    </p>
                </div>
            )}
        </div>
    );
};

export default ControlTab;
