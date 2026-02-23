import React, { useState } from 'react';

const Settings = () => {
    const [config, setConfig] = useState({
        primaryMonitor: 'Monitor 1 (1920x1080)',
        borderSide: 'Right',
        sensitivity: 'High',
        autoConnect: true
    });

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

            <div className="glass" style={{ padding: '30px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                    <h3 style={{ margin: 0 }}>Advanced Input Locking</h3>
                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Prevent local mouse movement while controlling remote.</p>
                </div>
                <div style={{
                    width: '52px', height: '28px', borderRadius: '30px', backgroundColor: 'var(--accent-primary)', position: 'relative', cursor: 'pointer'
                }}>
                    <div style={{ width: '20px', height: '20px', borderRadius: '50%', backgroundColor: 'white', position: 'absolute', top: '4px', right: '4px' }} />
                </div>
            </div>
        </div>
    );
};

export default Settings;
