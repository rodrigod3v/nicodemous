import React, { useState, useEffect } from 'react';

const Dashboard = () => {
    const [activeTab, setActiveTab] = useState('overview');
    const [services, setServices] = useState({
        input: true,
        audio: false,
        clipboard: true
    });
    const [discoveredDevices, setDiscoveredDevices] = useState([]);

    // Integrate with Photino bridge
    const toggleService = (name) => {
        const newState = !services[name];
        setServices(prev => ({ ...prev, [name]: newState }));

        // Notify C# backend
        if (window.photino) {
            window.photino.send(JSON.stringify({
                type: 'service_toggle',
                service: name,
                enabled: newState
            }));
        }
    };

    const startDiscovery = () => {
        if (window.photino) {
            window.photino.send(JSON.stringify({ type: 'start_discovery' }));
        }
    };

    return (
        <div className="dashboard-container" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            {/* Sidebar */}
            <aside className="glass" style={{ width: 'var(--sidebar-width)', margin: '20px', display: 'flex', flexDirection: 'column', padding: '30px' }}>
                <div className="brand" style={{ fontSize: '24px', fontWeight: '800', marginBottom: '50px', letterSpacing: '-0.5px' }}>
                    NICODEMOUS<span style={{ color: 'var(--accent-primary)' }}>.</span>
                </div>

                <nav style={{ display: 'flex', flexDirection: 'column', gap: '8px', flexGrow: 1 }}>
                    <TabButton active={activeTab === 'overview'} onClick={() => setActiveTab('overview')} label="Overview" icon="M3 9.5L12 4l9 5.5M19 9v10a2 2 0 01-2 2H7a2 2 0 01-2-2V9" />
                    <TabButton active={activeTab === 'devices'} onClick={() => setActiveTab('devices')} label="Devices" icon="M9 3H5a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2V5a2 2 0 00-2-2zM19 3h-4a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2V5a2 2 0 00-2-2zM9 13H5a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2v-4a2 2 0 00-2-2zM19 13h-4a2 2 0 00-2 2v4a2 2 0 002 2h4a2 2 0 002-2v-4a2 2 0 00-2-2z" />
                    <TabButton active={activeTab === 'settings'} onClick={() => setActiveTab('settings')} label="Settings" icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                </nav>

                <div className="status glass" style={{ padding: '15px', display: 'flex', alignItems: 'center', gap: '10px', marginTop: 'auto' }}>
                    <div className="status-pulse"></div>
                    <span style={{ fontSize: '14px', color: 'var(--text-dim)' }}>System Online</span>
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
                    <div className="glass animate-fade" style={{ padding: '40px', textAlign: 'center' }}>
                        <h2 style={{ marginBottom: '15px' }}>Discovered Devices</h2>
                        <p style={{ color: 'var(--text-dim)' }}>Searching for other Nicodemous instances on your network...</p>
                        {/* List devices here */}
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
            fontWeight: active ? '600' : '400'
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
