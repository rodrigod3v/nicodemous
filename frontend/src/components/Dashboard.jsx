import React, { useState, useEffect } from 'react';
import Settings from './Settings';

const Dashboard = () => {
    const [activeTab, setActiveTab] = useState('overview');
    const [connectionStatus, setConnectionStatus] = useState('Disconnected'); // Connected, Disconnected, Pairing
    const [services, setServices] = useState({
        input: true,
        audio: false,
        clipboard: true
    });
    const [discoveredDevices, setDiscoveredDevices] = useState([]);
    const [manualIp, setManualIp] = useState('');
    const [localIp, setLocalIp] = useState('......');

    useEffect(() => {
        const handleDiscovery = (e) => setDiscoveredDevices(e.detail);
        const handleIp = (e) => setLocalIp(e.detail);
        const handleStatus = (e) => setConnectionStatus(e.detail);

        window.addEventListener('nicodemous_discovery', handleDiscovery);
        window.addEventListener('nicodemous_ip', handleIp);
        window.addEventListener('nicodemous_status', handleStatus);

        return () => {
            window.removeEventListener('nicodemous_discovery', handleDiscovery);
            window.removeEventListener('nicodemous_ip', handleIp);
            window.removeEventListener('nicodemous_status', handleStatus);
        };
    }, []);

    const toggleService = (name) => {
        const newState = !services[name];
        setServices(prev => ({ ...prev, [name]: newState }));

        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({
                type: 'service_toggle',
                service: name,
                enabled: newState
            }));
        }
    };

    const startDiscovery = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: 'start_discovery' }));
        }
    };

    const connectToDevice = (ip) => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: 'connect_device', ip }));
        }
    };

    return (
        <div className="dashboard-container" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            {/* Sidebar */}
            <aside className="glass" style={{ width: 'var(--sidebar-width)', margin: '20px', display: 'flex', flexDirection: 'column', padding: '30px' }}>
                <div className="brand" style={{ fontSize: '24px', fontWeight: '800', marginBottom: '10px', letterSpacing: '-0.5px' }}>
                    NICODEMOUS<span style={{ color: 'var(--accent-primary)' }}>.</span>
                </div>

                <div style={{ padding: '8px 12px', background: 'rgba(99, 102, 241, 0.1)', borderRadius: '8px', border: '1px solid rgba(99, 102, 241, 0.2)', marginBottom: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontSize: '11px', fontWeight: '700', color: 'var(--accent-primary)', textTransform: 'uppercase' }}>Pairing Code</span>
                    <span style={{ fontSize: '13px', fontWeight: '600', color: 'white' }}>{localIp}</span>
                </div>

                <a
                    href="https://github.com/rodrigod3v/nicodemous/releases/latest"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="glass"
                    style={{
                        padding: '12px',
                        borderRadius: '10px',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '10px',
                        marginBottom: '30px',
                        textDecoration: 'none',
                        color: 'white',
                        fontSize: '13px',
                        border: '1px solid rgba(255,255,255,0.05)',
                        transition: 'all 0.2s'
                    }}
                    onMouseOver={(e) => e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.05)'}
                    onMouseOut={(e) => e.currentTarget.style.backgroundColor = 'transparent'}
                >
                    <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4 16v1a2 2 0 002 2h12a2 2 0 002-2v-1M7 10l5 5m0 0l5-5m-5 5V3" />
                    </svg>
                    Download Latest App
                </a>

                <nav style={{ display: 'flex', flexDirection: 'column', gap: '8px', flexGrow: 1 }}>
                    <TabButton active={activeTab === 'overview'} onClick={() => setActiveTab('overview')} label="Overview" icon="M3 9.5L12 4l9 5.5M19 9v10a2 2 0 01-2 2H7a2 2 0 01-2-2V9" />
                    <TabButton active={activeTab === 'devices'} onClick={() => setActiveTab('devices')} label="Devices" icon="M9 3H5a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2V5a2 2 0 00-2-2zM19 3h-4a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2V5a2 2 0 00-2-2zM9 13H5a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2v-4a2 2 0 00-2-2zM19 13h-4a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2v-4a2 2 0 00-2-2z" />
                    <TabButton active={activeTab === 'settings'} onClick={() => setActiveTab('settings')} label="Settings" icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                </nav>

                <div className="status glass" style={{ padding: '15px', display: 'flex', flexDirection: 'column', gap: '8px', marginTop: 'auto' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                        <div className="status-pulse" style={{ backgroundColor: connectionStatus === 'Connected' ? '#22c55e' : (connectionStatus === 'Pairing' ? '#f59e0b' : '#ef4444') }}></div>
                        <span style={{ fontSize: '14px', color: 'var(--text-dim)' }}>{connectionStatus}</span>
                    </div>
                </div>
            </aside>

            {/* Main Content */}
            <main style={{ flexGrow: 1, padding: '40px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div className="animate-fade">
                        <h1 style={{ fontSize: '32px', marginBottom: '8px' }}>Control Center</h1>
                        <p style={{ color: 'var(--text-dim)' }}>Welcome back to Nicodemous Dashboard</p>
                    </div>
                    <button className="glow-button" onClick={startDiscovery}>Find New Devices</button>
                </header>

                {activeTab === 'settings' && (
                    <Settings />
                )}

                {activeTab === 'overview' && (
                    <div className="grid-layout" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '24px' }}>
                        <ServiceCard
                            title="Universal Input"
                            description="Share mouse and keyboard across desktops."
                            enabled={services.input}
                            onToggle={() => toggleService('input')}
                            icon="M13 10V3L4 14h7v7l9-11h-7z"
                        />
                        <ServiceCard
                            title="High-Fidelity Audio"
                            description="Real-time lossless audio streaming."
                            enabled={services.audio}
                            onToggle={() => toggleService('audio')}
                            icon="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2z"
                        />
                        <ServiceCard
                            title="Clipboard & Sync"
                            description="Automatic text and file clipboard sharing."
                            enabled={services.clipboard}
                            onToggle={() => toggleService('clipboard')}
                            icon="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"
                        />
                    </div>
                )}

                {activeTab === 'devices' && (
                    <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <div className="glass" style={{ padding: '30px', display: 'flex', gap: '15px' }}>
                            <input
                                className="glass-input"
                                placeholder="Enter Remote IP or Pairing Code..."
                                value={manualIp}
                                onChange={(e) => setManualIp(e.target.value)}
                                style={{ flexGrow: 1, background: 'rgba(255,255,255,0.05)', border: 'none', padding: '15px', borderRadius: '12px', color: 'white' }}
                            />
                            <button className="glow-button" onClick={() => connectToDevice(manualIp)}>Connect</button>
                        </div>

                        <div className="grid-layout" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '20px' }}>
                            {discoveredDevices.length > 0 ? (
                                discoveredDevices.map((dev, i) => (
                                    <DeviceCard key={i} name={dev.Name} ip={dev.IPAddress} code={dev.Code} onConnect={() => connectToDevice(dev.Code)} />
                                ))
                            ) : (
                                <div className="glass" style={{ padding: '40px', textAlign: 'center', gridColumn: '1 / -1' }}>
                                    <p style={{ color: 'var(--text-dim)' }}>No active devices found. Ensure other devices are on the same Wi-Fi.</p>
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
};

const TabButton = ({ active, onClick, label, icon }) => (
    <button
        onClick={onClick}
        style={{
            padding: '12px 16px',
            borderRadius: '12px',
            display: 'flex',
            alignItems: 'center',
            gap: '12px',
            width: '100%',
            justifyContent: 'flex-start',
            backgroundColor: active ? 'rgba(99, 102, 241, 0.1)' : 'transparent',
            color: active ? 'var(--accent-primary)' : 'var(--text-dim)',
            fontSize: '14px',
            fontWeight: active ? '600' : '400',
            border: 'none',
            cursor: 'pointer',
            transition: 'all 0.2s'
        }}
    >
        <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
            <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
        </svg>
        {label}
    </button>
);

const ServiceCard = ({ title, description, enabled, onToggle, icon }) => (
    <div className="glass animate-fade" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ width: '48px', height: '48px', borderRadius: '14px', backgroundColor: 'rgba(99, 102, 241, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--accent-primary)' }}>
                <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
                </svg>
            </div>
            <Switch checked={enabled} onChange={onToggle} />
        </div>
        <div>
            <h3 style={{ fontSize: '20px', marginBottom: '8px' }}>{title}</h3>
            <p style={{ color: 'var(--text-dim)', fontSize: '14px', lineHeight: '1.5' }}>{description}</p>
        </div>
    </div>
);

const DeviceCard = ({ name, ip, code, onConnect }) => (
    <div className="glass animate-fade" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '15px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
            <div style={{ width: '40px', height: '40px', borderRadius: '50%', backgroundColor: 'rgba(34, 197, 94, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#22c55e' }}>
                <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                </svg>
            </div>
            <div>
                <h4 style={{ fontSize: '16px', margin: 0 }}>{name}</h4>
                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <code style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: 'bold' }}>{code}</code>
                    <span style={{ color: 'rgba(255,255,255,0.2)' }}>•</span>
                    <code style={{ fontSize: '10px', color: 'var(--text-dim)' }}>{ip}</code>
                </div>
            </div>
        </div>
        <button className="glow-button" style={{ padding: '8px', fontSize: '13px' }} onClick={onConnect}>Request Access</button>
    </div>
);

const Switch = ({ checked, onChange }) => (
    <div
        onClick={onChange}
        style={{
            width: '52px',
            height: '28px',
            borderRadius: '30px',
            backgroundColor: checked ? 'var(--accent-primary)' : 'rgba(255, 255, 255, 0.1)',
            position: 'relative',
            cursor: 'pointer',
            transition: 'background-color 0.3s'
        }}
    >
        <div style={{
            width: '20px',
            height: '20px',
            borderRadius: '50%',
            backgroundColor: 'white',
            position: 'absolute',
            top: '4px',
            left: checked ? '28px' : '4px',
            transition: 'left 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275)'
        }} />
    </div>
);

export default Dashboard;
