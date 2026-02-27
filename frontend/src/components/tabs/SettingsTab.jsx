import React, { useState, useEffect } from 'react';
import { useNicodemouse } from '../../context/nicodemouseContext';

const SettingsTab = () => {
    const {
        settings,
        systemInfo,
        updateSettings,
        sendMessage
    } = useNicodemouse();

    const [pin, setPin] = useState('');
    const [pinError, setPinError] = useState('');
    const [showToast, setShowToast] = useState(false);
    const [showConfirmModal, setShowConfirmModal] = useState(false);

    useEffect(() => {
        if (settings?.PairingCode) {
            setPin(settings.PairingCode);
        }
    }, [settings?.PairingCode]);

    const requestRestoreDefaults = () => {
        setShowConfirmModal(true);
    };

    const confirmRestoreDefaults = () => {
        sendMessage('reset_settings');
        setShowConfirmModal(false);
        setShowToast(true);
        setTimeout(() => setShowToast(false), 3000);
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
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            <div className="glass" style={{ padding: '24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderLeft: '4px solid var(--accent-primary)' }}>
                <div>
                    <h2 style={{ margin: 0, fontSize: '20px' }}>General Settings</h2>
                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Manage core application behavior and defaults.</p>
                </div>
                <button onClick={requestRestoreDefaults} className="glass" style={{ background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.2)', color: '#ef4444', padding: '10px 20px', borderRadius: '10px', fontSize: '13px', fontWeight: '700', display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5"><path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
                    RESTORE SYSTEM DEFAULTS
                </button>
            </div>

            {/* Custom PIN Section */}
            <div className="glass" style={{ padding: '24px' }}>
                <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
                    Pairing PIN Security
                </h2>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '40px', alignItems: 'center' }}>
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

            <div className="glass" style={{ padding: '24px' }}>
                <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                    System Information
                </h2>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '40px' }}>
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

            {/* Success Toast */}
            {showToast && (
                <div style={{
                    position: 'fixed',
                    bottom: '40px',
                    left: '50%',
                    transform: 'translateX(-50%)',
                    background: 'rgba(34, 197, 94, 0.15)',
                    border: '1px solid rgba(34, 197, 94, 0.3)',
                    color: '#4ade80',
                    padding: '12px 24px',
                    borderRadius: '12px',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '12px',
                    boxShadow: '0 8px 32px rgba(0, 0, 0, 0.3)',
                    backdropFilter: 'blur(10px)',
                    zIndex: 1000
                }}>
                    <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                    <span style={{ fontWeight: '600', fontSize: '14px' }}>System defaults restored successfully!</span>
                </div>
            )}

            {/* Custom Confirm Modal */}
            {showConfirmModal && (
                <div style={{
                    position: 'fixed',
                    top: 0, left: 0, right: 0, bottom: 0,
                    background: 'rgba(0,0,0,0.6)',
                    backdropFilter: 'blur(4px)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    zIndex: 2000
                }}>
                    <div className="glass animate-fade" style={{ padding: '30px', maxWidth: '400px', width: '90%', display: 'flex', flexDirection: 'column', gap: '20px', borderTop: '4px solid #ef4444', boxShadow: '0 20px 40px rgba(0,0,0,0.4)', borderRadius: '16px' }}>
                        <div>
                            <h3 style={{ margin: '0 0 10px 0', fontSize: '18px', display: 'flex', alignItems: 'center', gap: '10px' }}>
                                <svg width="24" height="24" fill="none" stroke="#ef4444" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                                Restore Defaults?
                            </h3>
                            <p style={{ margin: 0, color: 'var(--text-dim)', fontSize: '14px', lineHeight: '1.5' }}>
                                This will reset all your settings to their factory defaults. This action cannot be undone. Are you sure you want to proceed?
                            </p>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '10px' }}>
                            <button onClick={() => setShowConfirmModal(false)} style={{ padding: '10px 20px', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.05)', color: 'white', cursor: 'pointer', fontSize: '13px', fontWeight: '600', transition: 'all 0.2s' }} onMouseOver={e => e.currentTarget.style.background = 'rgba(255,255,255,0.1)'} onMouseOut={e => e.currentTarget.style.background = 'rgba(255,255,255,0.05)'}>Cancel</button>
                            <button onClick={confirmRestoreDefaults} style={{ padding: '10px 20px', borderRadius: '8px', border: 'none', background: '#ef4444', color: 'white', cursor: 'pointer', fontSize: '13px', fontWeight: '600', transition: 'all 0.2s', boxShadow: '0 4px 12px rgba(239, 68, 68, 0.4)' }} onMouseOver={e => e.currentTarget.style.transform = 'translateY(-1px)'} onMouseOut={e => e.currentTarget.style.transform = 'none'}>Confirm Reset</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default SettingsTab;
