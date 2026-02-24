import React, { useState, useEffect } from 'react';
import { usenicodemouse } from '../../context/nicodemouseContext';

const SettingsTab = () => {
    const {
        settings,
        systemInfo,
        updateSettings,
        sendMessage
    } = usenicodemouse();

    const [pin, setPin] = useState('');
    const [pinError, setPinError] = useState('');

    useEffect(() => {
        if (settings?.PairingCode) {
            setPin(settings.PairingCode);
        }
    }, [settings?.PairingCode]);

    const restoreDefaults = () => {
        if (window.confirm('Restore all settings to factory defaults?')) {
            sendMessage('reset_settings');
        }
    };

    const handlePinChange = (e) => {
        let val = e.target.value;
        // Basic emoji check (crude but usually effective for common emojis)
        const emojiRegex = /[\u{1F600}-\u{1F64F}\u{1F300}-\u{1F5FF}\u{1F680}-\u{1F6FF}\u{1F1E6}-\u{1F1FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]/u;

        if (emojiRegex.test(val)) return;

        if (val.length > 6) val = val.substring(0, 6);
        setPin(val);

        if (val.length === 6) {
            setPinError('');
            updateSettings({
                edge: settings?.ActiveEdge,
                lockInput: settings?.LockInput,
                delay: settings?.SwitchingDelayMs,
                cornerSize: settings?.DeadCornerSize,
                sensitivity: settings?.MouseSensitivity,
                gestureThreshold: settings?.GestureThreshold,
                pairingCode: val
            });
        } else {
            setPinError('PIN must be exactly 6 characters.');
        }
    };

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
            <div className="glass" style={{ padding: '30px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderLeft: '4px solid var(--accent-primary)' }}>
                <div>
                    <h2 style={{ margin: 0, fontSize: '20px' }}>General Settings</h2>
                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Manage core application behavior and defaults.</p>
                </div>
                <button onClick={restoreDefaults} className="glass" style={{ background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.2)', color: '#ef4444', padding: '10px 20px', borderRadius: '10px', fontSize: '13px', fontWeight: '700', display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
                    RESTORE SYSTEM DEFAULTS
                </button>
            </div>

            {/* Custom PIN Section */}
            <div className="glass" style={{ padding: '40px' }}>
                <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
                    Pairing PIN Security
                </h2>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '40px', alignItems: 'center' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                        <p style={{ color: 'var(--text-dim)', fontSize: '14px', lineHeight: '1.6' }}>
                            Customize your 6-character security PIN. This code is required by other devices to establish a connection with your computer.
                        </p>
                        <div style={{ position: 'relative' }}>
                            <input
                                type="text"
                                className="glass-input"
                                value={pin}
                                onChange={handlePinChange}
                                placeholder="6-digit PIN"
                                style={{
                                    fontSize: '24px',
                                    letterSpacing: '0.5em',
                                    textAlign: 'center',
                                    fontWeight: 'bold',
                                    width: '100%',
                                    background: 'rgba(255,255,255,0.05)',
                                    textTransform: 'uppercase'
                                }}
                                maxLength={6}
                            />
                            {pinError && <p style={{ color: '#ef4444', fontSize: '12px', marginTop: '8px', position: 'absolute' }}>{pinError}</p>}
                        </div>
                    </div>
                    <div className="glass" style={{ padding: '25px', background: 'rgba(99, 102, 241, 0.05)', border: '1px solid rgba(99, 102, 241, 0.2)' }}>
                        <h4 style={{ margin: '0 0 10px 0', color: 'var(--accent-primary)' }}>Security Requirements</h4>
                        <ul style={{ margin: 0, paddingLeft: '20px', fontSize: '13px', color: 'var(--text-dim)', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                            <li>Must be exactly 6 characters long</li>
                            <li>Supports numbers, letters and symbols</li>
                            <li>Emojis are not permitted</li>
                            <li>Changes take effect immediately</li>
                        </ul>
                    </div>
                </div>
            </div>

            <div className="glass" style={{ padding: '40px' }}>
                <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    System Information
                </h2>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '40px' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '12px', background: 'rgba(255,255,255,0.02)', borderRadius: '8px' }}>
                            <span style={{ color: 'var(--text-dim)' }}>Machine Name</span>
                            <span style={{ fontWeight: '600' }}>{systemInfo.machineName}</span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', padding: '12px', background: 'rgba(255,255,255,0.02)', borderRadius: '8px' }}>
                            <span style={{ color: 'var(--text-dim)' }}>Monitors Detected</span>
                            <span style={{ fontWeight: '600' }}>{systemInfo.monitors?.length || 0}</span>
                        </div>
                    </div>
                    <div className="glass" style={{ padding: '20px', background: 'rgba(255,255,255,0.01)' }}>
                        <p style={{ margin: 0, fontSize: '13px', color: 'var(--text-dim)', fontStyle: 'italic' }}>
                            nicodemouse uses your local network for encrypted discovery. Ensure both devices are on the same Wi-Fi or LAN.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default SettingsTab;
