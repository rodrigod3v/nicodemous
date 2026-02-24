import React, { useState } from 'react';
import { usenicodemouse } from '../../context/nicodemouseContext';

const OverviewTab = ({ onNavigate }) => {
    const {
        connectionStatus,
        discoveredDevices,
        connectedDevice,
        settings,
        connectDevice,
        sendMessage
    } = usenicodemouse();

    const [manualIp, setManualIp] = useState('');
    const [isScanning, setIsScanning] = useState(false);

    const activeServicesCount = settings ?
        Object.entries(settings).filter(([key, val]) =>
            (key === 'EnableInput' || key === 'EnableAudio' || key === 'EnableClipboard') && val === true
        ).length : 0;

    const startDiscovery = () => {
        setIsScanning(true);
        sendMessage('start_discovery');
        onNavigate('devices');
        setTimeout(() => setIsScanning(false), 10000);
    };

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {/* Stats Grid */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '20px' }}>
                <div className="glass" style={{ padding: '20px', borderLeft: '4px solid var(--accent-primary)' }}>
                    <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Active Connection</span>
                    <h3 style={{ fontSize: '18px', marginTop: '5px' }}>
                        {connectionStatus.includes('Connected') ? (connectedDevice?.name || 'Remote Device') : 'None'}
                    </h3>
                </div>
                <div className="glass" style={{ padding: '20px', borderLeft: '4px solid #22c55e' }}>
                    <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Service Health</span>
                    <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{activeServicesCount} Active</h3>
                </div>
                <div className="glass" style={{ padding: '20px', borderLeft: '4px solid #a855f7' }}>
                    <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Discovery</span>
                    <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{discoveredDevices.length} Nodes Found</h3>
                </div>
            </div>

            <div className="responsive-split-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '30px' }}>
                {/* Nearby Devices Preview */}
                <div className="glass" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <h2 style={{ fontSize: '20px', margin: 0 }}>Nearby Devices</h2>
                        <button className="glass" onClick={() => onNavigate('devices')} style={{ fontSize: '12px', padding: '5px 12px', cursor: 'pointer', borderRadius: '8px', background: 'rgba(255,255,255,0.05)', color: 'var(--text-dim)', border: '1px solid rgba(255,255,255,0.1)' }}>View All</button>
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                        {discoveredDevices.length > 0 ? discoveredDevices.slice(0, 3).map((dev, i) => (
                            <div key={i} className="glass" style={{ padding: '15px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'rgba(255,255,255,0.02)' }}>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                                    <div style={{ color: 'var(--accent-primary)' }}>
                                        <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                                    </div>
                                    <div>
                                        <div style={{ fontSize: '14px', fontWeight: '600' }}>{dev.name || dev.hostname || 'Unknown Device'}</div>
                                        <div style={{ fontSize: '11px', color: 'var(--text-dim)' }}>{dev.ip}</div>
                                    </div>
                                </div>
                                <button className="glow-button" onClick={() => connectDevice(dev.ip)} style={{ padding: '6px 15px', fontSize: '12px' }}>Connect</button>
                            </div>
                        )) : (
                            <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-dim)', fontSize: '14px', border: '1px dashed rgba(255,255,255,0.1)', borderRadius: '12px' }}>
                                No devices found yet.
                            </div>
                        )}
                    </div>
                </div>

                {/* Quick Join Panel */}
                <div className="glass" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px', background: 'linear-gradient(135deg, rgba(99, 102, 241, 0.1) 0%, rgba(255,255,255,0.01) 100%)' }}>
                    <h2 style={{ fontSize: '20px', margin: 0 }}>Direct Connection</h2>
                    <p style={{ fontSize: '13px', color: 'var(--text-dim)', margin: 0 }}>Enter an IP or PIN to join a session instantly.</p>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '10px' }}>
                        <input className="glass-input" placeholder="IP Address / PIN" value={manualIp} onChange={(e) => setManualIp(e.target.value)} style={{ padding: '12px', borderRadius: '10px', fontSize: '14px' }} />
                        <button className="glow-button" onClick={() => connectDevice(manualIp)} style={{ width: '100%' }}>Connect Now</button>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default OverviewTab;
