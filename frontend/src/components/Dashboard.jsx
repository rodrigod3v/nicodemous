import React, { useState, useEffect } from 'react';
import Settings from './Settings';
import Switch from './Switch';
import TabButton from './TabButton';
import ServiceCard from './ServiceCard';
import DeviceCard from './DeviceCard';

const Dashboard = () => {
    console.log('[FRONTEND] Dashboard component rendering...');
    const [activeTab, setActiveTab] = useState('overview');
    const [connectionStatus, setConnectionStatus] = useState('Disconnected');
    const [isScanning, setIsScanning] = useState(false);
    const [services, setServices] = useState({
        input: true,
        audio: false,
        clipboard: true
    });
    const [discoveredDevices, setDiscoveredDevices] = useState([]);
    const [manualIp, setManualIp] = useState('');
    const [localCode, setLocalCode] = useState('......');
    const [localIp, setLocalIp] = useState('0.0.0.0');
    const [isConnecting, setIsConnecting] = useState(null); // stores the IP/Code of the device being connected

    const sendToBackend = (type, data = {}) => {
        const message = JSON.stringify({ type, ...data });
        console.log(`[FRONTEND] Sending to Backend:`, message);

        try {
            if (window.external && window.external.sendMessage) {
                window.external.sendMessage(message);
            } else if (window.photino && window.photino.send) {
                window.photino.send(message);
            } else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                window.chrome.webview.postMessage(message);
            } else {
                console.warn('[FRONTEND] No backend interop found. Running in browser?');
            }
        } catch (err) {
            console.error('[FRONTEND] Failed to send message:', err);
        }
    };

    useEffect(() => {
        const handleDiscovery = (e) => {
            setDiscoveredDevices(e.detail || []);
        };
        const handleLocalDetails = (e) => {
            if (e.detail && typeof e.detail === 'object') {
                setLocalCode(e.detail.code || '......');
                setLocalIp(e.detail.ip || '0.0.0.0');
            } else if (e.detail) {
                setLocalCode(e.detail);
            }
        };
        const handleStatus = (e) => {
            setConnectionStatus(e.detail);
            if (e.detail === 'Connected' || e.detail === 'Disconnected') {
                setIsConnecting(null);
            }
        };

        window.addEventListener('nicodemous_discovery', handleDiscovery);
        window.addEventListener('nicodemous_ip', handleLocalDetails);
        window.addEventListener('nicodemous_status', handleStatus);

        return () => {
            window.removeEventListener('nicodemous_discovery', handleDiscovery);
            window.removeEventListener('nicodemous_ip', handleLocalDetails);
            window.removeEventListener('nicodemous_status', handleStatus);
        };
    }, []);

    const toggleService = (name) => {
        const newState = !services[name];
        setServices(prev => ({ ...prev, [name]: newState }));
        sendToBackend('service_toggle', { service: name, enabled: newState });
    };

    const startDiscovery = () => {
        setIsScanning(true);
        setDiscoveredDevices([]);
        setActiveTab('devices'); // Auto-navigate to discovery tab
        sendToBackend('start_discovery');
        setTimeout(() => setIsScanning(false), 10000);
    };

    const connectToDevice = (target) => {
        if (!target || target.trim() === '') return;
        setConnectionStatus('Connecting...');
        setIsConnecting(target);
        sendToBackend('connect_device', { ip: target.trim() });
    };

    console.log('[FRONTEND] Dashboard Rendering with state:', { activeTab, localIp, localCode });

    return (
        <div className="dashboard-container" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            {/* Sidebar */}
            <div className="glass" style={{
                width: 'var(--sidebar-width)',
                padding: '30px 20px',
                display: 'flex',
                flexDirection: 'column',
                gap: '10px'
            }}>
                <div className="brand animate-fade" style={{ fontSize: '26px', fontWeight: '800', marginBottom: '30px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <div className="glow-button" style={{ width: '42px', height: '42px', borderRadius: '12px', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 0 }}>
                        <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 11c0 3.517-1.009 6.799-2.753 9.571m-3.44-20.4L2.032 4.415m1.889-1.342c.447.83 1.107 1.567 1.886 2.149M3.5 11a7.5 7.5 0 1115 0 7.5 7.5 0 01-15 0z" />
                        </svg>
                    </div>
                    <span className="gradient-text">Nicodemous</span>
                </div>

                <TabButton
                    active={activeTab === 'overview'}
                    onClick={() => setActiveTab('overview')}
                    label="Overview"
                    icon="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                />
                <TabButton
                    active={activeTab === 'devices'}
                    onClick={() => setActiveTab('devices')}
                    label="Discovery"
                    icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
                <TabButton
                    active={activeTab === 'settings'}
                    onClick={() => setActiveTab('settings')}
                    label="Settings"
                    icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
                />

                <div className="status glass" style={{ padding: '15px', display: 'flex', flexDirection: 'column', gap: '8px', marginTop: 'auto' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                        <div className="status-pulse" style={{
                            backgroundColor: connectionStatus === 'Connected' ? '#22c55e' :
                                (String(connectionStatus || '').includes('Connecting') ? '#f59e0b' : '#ef4444')
                        }}></div>
                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '500' }}>{connectionStatus || 'Disconnected'}</span>
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '11px', color: 'rgba(255,255,255,0.4)' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <span>IP</span>
                            <span>{localIp}</span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <span>PIN</span>
                            <span style={{ color: 'var(--accent-primary)', fontWeight: 'bold' }}>{localCode}</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <main style={{ flexGrow: 1, padding: '40px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div className="animate-fade">
                        <h1 style={{ fontSize: '32px', marginBottom: '8px' }}>Control Center</h1>
                        <p style={{ color: 'var(--text-dim)' }}>Manage and discover devices on your local network</p>
                    </div>
                    <button
                        className={`glow-button ${isScanning ? 'scanning' : ''}`}
                        onClick={startDiscovery}
                        disabled={isScanning}
                        style={{ display: 'flex', alignItems: 'center', gap: '10px' }}
                    >
                        {isScanning ? (
                            <>
                                <div className="spinner"></div>
                                Scanning...
                            </>
                        ) : 'Find New Devices'}
                    </button>
                </header>

                {activeTab === 'settings' && <Settings />}

                {activeTab === 'overview' && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        {/* Status Dashboard Section */}
                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '20px' }}>
                            <div className="glass animate-fade" style={{ padding: '20px', borderLeft: '4px solid var(--accent-primary)' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Active Connection</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{connectionStatus === 'Connected' ? 'Remote Desktop' : 'None'}</h3>
                            </div>
                            <div className="glass animate-fade" style={{ padding: '20px', borderLeft: '4px solid #22c55e' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Service Health</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{Object.values(services).filter(s => s).length} Active</h3>
                            </div>
                            <div className="glass animate-fade" style={{ padding: '20px', borderLeft: '4px solid #a855f7' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Discovery</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{discoveredDevices.length} Nodes Found</h3>
                            </div>
                        </div>

                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '30px' }}>
                            <ServiceCard
                                title="Remote Input"
                                description="Share your mouse and keyboard across devices seamlessly."
                                enabled={services.input}
                                onToggle={() => toggleService('input')}
                                icon="M15 15l-2 5L9 9l11 4-5 2zm0 0l5 5M7.188 2.239l.777 2.897M5.136 7.965l-2.898-.777M13.95 4.05l-2.122 2.122m-5.657 5.656l-2.12 2.122"
                            />
                            <ServiceCard
                                title="Shared Clipboard"
                                description="Copy values and files on one computer and paste them on another."
                                enabled={services.clipboard}
                                onToggle={() => toggleService('clipboard')}
                                icon="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
                            />
                            <ServiceCard
                                title="Sync Audio"
                                description="Stream system audio from connected devices to your main speakers."
                                enabled={services.audio}
                                onToggle={() => toggleService('audio')}
                                icon="M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z"
                            />
                        </div>
                    </div>
                )}

                {activeTab === 'devices' && (
                    <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <div className="glass" style={{ padding: '30px', display: 'flex', gap: '15px', alignItems: 'center' }}>
                            <div style={{ flexGrow: 1, position: 'relative' }}>
                                <input
                                    className="glass-input"
                                    placeholder="Direct IP or Pairing Code..."
                                    value={manualIp}
                                    onChange={(e) => setManualIp(e.target.value)}
                                    style={{ width: '100%', background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)', padding: '15px 20px', borderRadius: '12px', color: 'white', fontSize: '15px' }}
                                />
                            </div>
                            <button
                                className={`glow-button ${isConnecting === manualIp ? 'loading' : ''}`}
                                onClick={() => connectToDevice(manualIp)}
                                disabled={isConnecting === manualIp}
                            >
                                {isConnecting === manualIp ? <div className="spinner"></div> : 'Quick Connect'}
                            </button>
                        </div>

                        <div className="grid-layout" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '20px' }}>
                            {discoveredDevices.length > 0 ? (
                                discoveredDevices.map((dev, i) => (
                                    <DeviceCard
                                        key={dev.ip || i}
                                        name={dev.hostname || 'Unknown Device'}
                                        ip={dev.ip || '0.0.0.0'}
                                        code={dev.code || '000000'}
                                        status={connectionStatus === 'Connected' ? 'Connected' : 'Available'}
                                        isConnecting={isConnecting === dev.ip}
                                        onConnect={() => connectToDevice(dev.ip)}
                                    />
                                ))
                            ) : (
                                <div className="glass" style={{ padding: '60px 40px', textAlign: 'center', gridColumn: '1 / -1' }}>
                                    <p style={{ color: 'var(--text-dim)' }}>{isScanning ? 'Scanning for devices...' : 'No active devices found. Click "Find New Devices" above to scan.'}</p>
                                </div>
                            )}
                        </div>
                    </div>
                )}
            </main>

            <style dangerouslySetInnerHTML={{
                __html: `
                .spinner { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.3); border-radius: 50%; border-top-color: white; animation: spin 0.8s linear infinite; }
                @keyframes spin { to { transform: rotate(360deg); } }
                .glass-input:focus { outline: none; border-color: var(--accent-primary) !important; background: rgba(255,255,255,0.05) !important; }
            `}} />
        </div>
    );
};

export default Dashboard;
