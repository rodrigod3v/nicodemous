import React, { useState, useEffect, useRef } from 'react';
import Settings from './Settings';
import Switch from './Switch';
import TabButton from './TabButton';
import ServiceCard from './ServiceCard';
import DeviceCard from './DeviceCard';

const Dashboard = () => {
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
    const [isConnecting, setIsConnecting] = useState(null);
    const [connectedDevice, setConnectedDevice] = useState(null);
    const [sessionRole, setSessionRole] = useState(null); // 'controller' or 'client'

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

    const [systemInfo, setSystemInfo] = useState({
        machineName: 'This PC',
        monitors: [{ name: 'Monitor 1 (Primary)', isPrimary: true }]
    });

    const isInitialLoad = useRef(true);

    const sendToBackend = (type, data = {}) => {
        const message = JSON.stringify({ type, ...data });
        try {
            if (window.external && window.external.sendMessage) {
                window.external.sendMessage(message);
            } else if (window.photino && window.photino.send) {
                window.photino.send(message);
            } else if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {
                window.chrome.webview.postMessage(message);
            }
        } catch (err) {
            console.error('[FRONTEND] Failed to send message:', err);
        }
    };

    useEffect(() => {
        const handleDiscovery = (e) => setDiscoveredDevices(e.detail || []);
        const handleLocalDetails = (e) => {
            if (e.detail?.code) setLocalCode(e.detail.code);
            if (e.detail?.ip) setLocalIp(e.detail.ip);
        };

        const handleSettings = (e) => {
            if (!e.detail) return;
            try {
                const s = typeof e.detail === 'string' ? JSON.parse(e.detail) : e.detail;
                setConfig(prev => ({
                    ...prev,
                    borderSide: s.ActiveEdge || prev.borderSide,
                    sensitivity: s.MouseSensitivity || prev.sensitivity,
                    delay: s.SwitchingDelayMs ?? prev.delay,
                    cornerSize: s.DeadCornerSize ?? prev.cornerSize,
                    lockInput: s.LockInput ?? prev.lockInput,
                    gestureThreshold: s.GestureThreshold ?? prev.gestureThreshold
                }));
                setServices({
                    input: s.EnableInput ?? true,
                    audio: s.EnableAudio ?? false,
                    clipboard: s.EnableClipboard ?? true
                });
                setTimeout(() => { isInitialLoad.current = false; }, 100);
            } catch (err) {
                console.error('[FRONTEND] Failed to parse settings:', err);
            }
        };

        const handleSystemInfo = (e) => {
            if (e.detail) setSystemInfo(e.detail);
        };

        const handleStatus = (e) => {
            const status = typeof e.detail === 'string' ? e.detail : (e.detail?.status || '');
            console.log('[FRONTEND] Connection status update:', status);
            setConnectionStatus(status);

            const statusLower = status.toLowerCase();

            if (statusLower.includes('disconnected') || status.includes('Error')) {
                console.log('[FRONTEND] Disconnection/Error detected, clearing session state.');
                setConnectedDevice(null);
                setSessionRole(null);
                setIsConnecting(null);
            } else if (status.includes('Controlled by')) {
                const deviceName = status.replace('Controlled by', '').trim();
                setConnectedDevice({ name: deviceName });
                setSessionRole('controlled');
                setIsConnecting(null);
            } else if (status.includes('Connected')) {
                setSessionRole('controlling');

                // Track which device we just connected to
                if (isConnecting) {
                    const dev = discoveredDevices.find(d => d.ip === isConnecting);
                    if (dev) setConnectedDevice(dev);
                    else setConnectedDevice({ name: 'Remote Device', ip: isConnecting });
                }

                setIsConnecting(null);
                setActiveTab('device'); // Auto-navigate on successful connection
            }
        };

        window.addEventListener('nicodemous_discovery', handleDiscovery);
        window.addEventListener('nicodemous_ip', handleLocalDetails);
        window.addEventListener('nicodemous_status', handleStatus);
        window.addEventListener('nicodemous_settings', handleSettings);
        window.addEventListener('nicodemous_system_info', handleSystemInfo);

        // Initial request for settings
        sendToBackend('get_settings');

        return () => {
            window.removeEventListener('nicodemous_discovery', handleDiscovery);
            window.removeEventListener('nicodemous_ip', handleLocalDetails);
            window.removeEventListener('nicodemous_status', handleStatus);
            window.removeEventListener('nicodemous_settings', handleSettings);
            window.removeEventListener('nicodemous_system_info', handleSystemInfo);
        };
    }, []);

    // Settings Sync Effect
    useEffect(() => {
        if (isInitialLoad.current) return;

        sendToBackend('update_settings', {
            edge: config.borderSide,
            lockInput: config.lockInput,
            delay: parseInt(config.delay),
            cornerSize: parseInt(config.cornerSize),
            sensitivity: parseFloat(config.sensitivity),
            gestureThreshold: parseInt(config.gestureThreshold)
        });
    }, [config.borderSide, config.lockInput, config.delay, config.cornerSize, config.sensitivity, config.gestureThreshold]);

    const toggleService = (name) => {
        const newState = !services[name];
        setServices(prev => ({ ...prev, [name]: newState }));
        sendToBackend('service_toggle', { service: name, enabled: newState });
    };

    const startDiscovery = () => {
        setIsScanning(true);
        setDiscoveredDevices([]);
        setActiveTab('devices'); // Navigate to Discovery tab
        sendToBackend('start_discovery');
        setTimeout(() => setIsScanning(false), 10000);
    };

    const connectToDevice = (target) => {
        if (!target?.trim()) return;
        setConnectionStatus('Connecting...');
        setIsConnecting(target.trim());
        sendToBackend('connect_device', { ip: target.trim() });
    };

    const restoreDefaults = () => {
        if (window.confirm('Restore all session settings to factory defaults?')) {
            sendToBackend('reset_settings');
        }
    };

    return (
        <div className="dashboard-container" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            {/* Sidebar */}
            <div className="glass" style={{ width: 'var(--sidebar-width)', padding: '30px 20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                <div className="brand animate-fade" style={{ fontSize: '26px', fontWeight: '800', marginBottom: '30px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <div className="glow-button" style={{ width: '42px', height: '42px', borderRadius: '12px', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 0 }}>
                        <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M12 11c0 3.517-1.009 6.799-2.753 9.571m-3.44-20.4L2.032 4.415m1.889-1.342c.447.83 1.107 1.567 1.886 2.149M3.5 11a7.5 7.5 0 1115 0 7.5 7.5 0 01-15 0z" />
                        </svg>
                    </div>
                    <span className="gradient-text">Nicodemous</span>
                </div>

                <TabButton active={activeTab === 'overview'} onClick={() => setActiveTab('overview')} label="Overview" icon="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                <TabButton active={activeTab === 'devices'} onClick={() => setActiveTab('devices')} label="Discovery" icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                <TabButton active={activeTab === 'device'} onClick={() => setActiveTab('device')} label="Active Device" icon="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
                <TabButton active={activeTab === 'settings'} onClick={() => setActiveTab('settings')} label="Settings" icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />

                <div className="status glass" style={{ padding: '15px', display: 'flex', flexDirection: 'column', gap: '8px', marginTop: 'auto' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                        <div className="status-pulse" style={{ backgroundColor: connectionStatus.includes('Connected') ? '#22c55e' : (connectionStatus.includes('Connecting') ? '#f59e0b' : '#ef4444') }}></div>
                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '500' }}>
                            {sessionRole ? `${sessionRole.charAt(0).toUpperCase() + sessionRole.slice(1)}` : (connectionStatus === 'Disconnected' ? 'Disconnected' : connectionStatus)}
                        </span>
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '11px', color: 'rgba(255,255,255,0.4)' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>IP</span><span>{localIp}</span></div>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>PIN</span><span style={{ color: 'var(--accent-primary)', fontWeight: 'bold' }}>{localCode}</span></div>
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <main style={{ flexGrow: 1, padding: '40px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                        <h1 style={{ fontSize: '32px', marginBottom: '8px' }}>Control Center</h1>
                        <p style={{ color: 'var(--text-dim)' }}>Manage and discover devices on your local network</p>
                    </div>
                    <button className={`glow-button ${isScanning ? 'scanning' : ''}`} onClick={startDiscovery} disabled={isScanning}>
                        {isScanning ? (
                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><div className="spinner"></div>Scanning...</div>
                        ) : 'Find New Devices'}
                    </button>
                </header>

                {activeTab === 'overview' && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '20px' }}>
                            <div className="glass" style={{ padding: '20px', borderLeft: '4px solid var(--accent-primary)' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Active Connection</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{connectionStatus.includes('Connected') ? (connectedDevice?.name || 'Remote Device') : 'None'}</h3>
                            </div>
                            <div className="glass" style={{ padding: '20px', borderLeft: '4px solid #22c55e' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Service Health</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{Object.values(services).filter(s => s).length} Active</h3>
                            </div>
                            <div className="glass" style={{ padding: '20px', borderLeft: '4px solid #a855f7' }}>
                                <span style={{ fontSize: '12px', color: 'var(--text-dim)', textTransform: 'uppercase', fontWeight: 'bold' }}>Discovery</span>
                                <h3 style={{ fontSize: '18px', marginTop: '5px' }}>{discoveredDevices.length} Nodes Found</h3>
                            </div>
                        </div>

                        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '30px' }}>
                            {/* Nearby Devices Preview */}
                            <div className="glass" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <h2 style={{ fontSize: '20px', margin: 0 }}>Nearby Devices</h2>
                                    <button className="glass" onClick={() => setActiveTab('devices')} style={{ fontSize: '12px', padding: '5px 12px', cursor: 'pointer', borderRadius: '8px', background: 'rgba(255,255,255,0.05)', color: 'var(--text-dim)', border: '1px solid rgba(255,255,255,0.1)' }}>View All</button>
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
                                            <button className="glow-button" onClick={() => connectToDevice(dev.ip)} style={{ padding: '6px 15px', fontSize: '12px' }}>Connect</button>
                                        </div>
                                    )) : (
                                        <div style={{ padding: '40px', textAlign: 'center', color: 'var(--text-dim)', fontSize: '14px', border: '1px dashed rgba(255,255,255,0.1)', borderRadius: '12px' }}>
                                            {isScanning ? 'Scanning...' : 'No devices found yet.'}
                                        </div>
                                    )}
                                </div>
                            </div>

                            {/* Quick Join Panel */}
                            <div className="glass" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '20px', background: 'linear-gradient(135deg, rgba(99, 102, 241, 0.1) 0%, rgba(255,255,255,0.01) 100%)' }}>
                                <h2 style={{ fontSize: '20px', margin: 0 }}>Direct Connection</h2>
                                <p style={{ fontSize: '13px', color: 'var(--text-dim)', margin: 0 }}>Enter an IP or PIN to join a session instantly.</p>
                                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginTop: '10px' }}>
                                    <input className="glass-input" placeholder="IP Address / PIN" value={manualIp} onChange={(e) => setManualIp(e.target.value)} style={{ padding: '12px', borderRadius: '10px', fontSize: '14px' }} />
                                    <button className="glow-button" onClick={() => connectToDevice(manualIp)} style={{ width: '100%' }}>Connect Now</button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}

                {activeTab === 'device' && (
                    <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                        {connectionStatus.includes('Connected') ? (
                            <div className="glass" style={{ padding: '40px' }}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px' }}>
                                    <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                                        <div className="glow-button" style={{ width: '60px', height: '60px', borderRadius: '20px' }}><svg width="28" height="28" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg></div>
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
                                    <button onClick={() => sendToBackend('service_toggle', { service: 'disconnect', enabled: true })} className="glass" style={{ background: 'rgba(239, 68, 68, 0.1)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.2)', padding: '12px 25px', borderRadius: '12px', fontSize: '14px', fontWeight: 'bold' }}>
                                        {sessionRole === 'controlling' ? 'Terminate Session' : 'Stop Sharing'}
                                    </button>
                                </div>
                                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '25px' }}>
                                    {[
                                        { id: 'input', label: sessionRole === 'controlling' ? 'Remote Input' : 'Allow Remote Input', description: sessionRole === 'controlling' ? 'Cross monitors to take control.' : 'Allow the remote user to move your mouse.', icon: 'M15 15l-2 5L9 9l11 4-5 2z', color: 'var(--accent-primary)' },
                                        { id: 'clipboard', label: 'Universal Clipboard', description: 'Share text and files seamlessly.', icon: 'M9 5a2 2 0 002 2h2a2 2 0 002-2', color: '#22c55e' },
                                        { id: 'audio', label: sessionRole === 'controlling' ? 'Audio Stream' : 'Output Local Audio', description: sessionRole === 'controlling' ? 'Redirect audio output.' : 'Send system audio to remote.', icon: 'M15.536 8.464a5 5 0 010 7.072', color: '#a855f7' },
                                        { id: 'file', label: 'File Transfer', description: 'Drag and drop files.', icon: 'M9 12h6m-6 4h6', color: '#64748b', disabled: true }
                                    ].map(slot => (
                                        <div key={slot.id} className="glass" style={{ padding: '25px', display: 'flex', flexDirection: 'column', gap: '15px', opacity: slot.disabled ? 0.5 : 1 }}>
                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                <div className="glow-button" style={{ width: '40px', height: '40px', background: 'rgba(255,255,255,0.03)', color: slot.color, padding: 0 }}><svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2"><path strokeLinecap="round" strokeLinejoin="round" d={slot.icon} /></svg></div>
                                                {!slot.disabled && <Switch checked={services[slot.id]} onChange={() => toggleService(slot.id)} />}
                                                {slot.disabled && <span style={{ fontSize: '11px', color: 'var(--text-dim)', background: 'rgba(255,255,255,0.08)', padding: '4px 8px', borderRadius: '6px' }}>Soon</span>}
                                            </div>
                                            <div><h3 style={{ margin: 0, fontSize: '16px' }}>{slot.label}</h3><p style={{ margin: '5px 0 0 0', fontSize: '12px', color: 'var(--text-dim)', lineHeight: '1.5' }}>{slot.description}</p></div>
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
                                                <button onClick={restoreDefaults} style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: 'var(--text-dim)', padding: '6px 12px', borderRadius: '8px', fontSize: '11px', fontWeight: 'bold', cursor: 'pointer', transition: 'all 0.2s' }}>
                                                    RESTORE DEFAULTS
                                                </button>
                                            </h2>
                                            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '40px' }}>
                                                <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
                                                    <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Active Monitor</span>
                                                        <select
                                                            className="glass-input"
                                                            value={config.primaryMonitor}
                                                            onChange={(e) => setConfig({ ...config, primaryMonitor: e.target.value })}
                                                            style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', padding: '12px', borderRadius: '10px', color: 'white', outline: 'none' }}
                                                        >
                                                            {systemInfo.monitors.map((m, i) => (
                                                                <option key={i} style={{ background: '#1e1e2e', color: 'white' }}>{m.name} {m.isPrimary ? '(Primary)' : ''}</option>
                                                            ))}
                                                        </select>
                                                    </label>
                                                    <label style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Crossing Edge</span>
                                                        <div style={{ display: 'flex', gap: '10px', marginTop: '5px' }}>
                                                            {['Left', 'Right'].map(edge => (
                                                                <button key={edge} onClick={() => setConfig({ ...config, borderSide: edge })} style={{ flex: 1, padding: '10px', borderRadius: '8px', border: '1px solid', borderColor: config.borderSide === edge ? 'var(--accent-primary)' : 'rgba(255,255,255,0.1)', background: config.borderSide === edge ? 'rgba(99, 102, 241, 0.1)' : 'transparent', color: config.borderSide === edge ? 'var(--accent-primary)' : 'white' }}>{edge}</button>
                                                            ))}
                                                        </div>
                                                    </label>
                                                </div>
                                                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '20px' }}>
                                                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' }}>
                                                        <div style={{ position: 'relative', width: '140px', height: '80px', border: '2px solid rgba(255,255,255,0.1)', borderRadius: '8px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', background: 'rgba(255,255,255,0.02)' }}>
                                                            <span style={{ fontSize: '10px', color: 'var(--accent-primary)', fontWeight: '700', textTransform: 'uppercase', marginBottom: '4px' }}>Local</span>
                                                            <span style={{ fontSize: '12px', color: 'white', textAlign: 'center', padding: '0 10px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', width: '100%' }}>{systemInfo.machineName}</span>
                                                            <div style={{ position: 'absolute', right: config.borderSide === 'Right' ? '-2px' : 'auto', left: config.borderSide === 'Left' ? '-2px' : 'auto', width: '3px', height: '70%', background: 'var(--accent-primary)', borderRadius: '2px', boxShadow: '0 0 10px var(--accent-primary)' }} />
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

                                        <div className="glass" style={{ padding: '30px' }}>
                                            <h2 style={{ marginBottom: '25px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: '20px' }}>
                                                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                                                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                                                        <path strokeLinecap="round" strokeLinejoin="round" d="M15 15l-2 5L9 9l11 4-5 2z" />
                                                    </svg>
                                                    Mouse Dynamics & Precision
                                                </div>
                                                <button onClick={restoreDefaults} style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: 'var(--text-dim)', padding: '6px 12px', borderRadius: '8px', fontSize: '11px', fontWeight: 'bold', cursor: 'pointer', transition: 'all 0.2s' }}>
                                                    RESTORE DEFAULTS
                                                </button>
                                            </h2>
                                            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '30px' }}>
                                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Sensitivity <span title="Controls the speed of the remote cursor relative to Local movement." style={{ cursor: 'help', opacity: 0.5, fontSize: '12px' }}>ⓘ</span></span>
                                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{Math.round(config.sensitivity * 100)}%</span>
                                                    </div>
                                                    <input type="range" min="0.1" max="3.0" step="0.1" value={config.sensitivity} onChange={(e) => setConfig({ ...config, sensitivity: e.target.value })} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                                </label>
                                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Return Force <span title="How hard you must 'push' the screen edge to escape back to Local control." style={{ cursor: 'help', opacity: 0.5, fontSize: '12px' }}>ⓘ</span></span>
                                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{config.gestureThreshold}px</span>
                                                    </div>
                                                    <input type="range" min="500" max="5000" step="100" value={config.gestureThreshold} onChange={(e) => setConfig({ ...config, gestureThreshold: e.target.value })} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                                </label>
                                            </div>
                                        </div>

                                        <div className="glass" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '25px' }}>
                                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                                                <div>
                                                    <h3 style={{ margin: 0 }}>Input Locking & Crossing</h3>
                                                    <p style={{ margin: '5px 0 0 0', fontSize: '14px', color: 'var(--text-dim)' }}>Behavior when cursor is at the edge or on remote.</p>
                                                </div>
                                                <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                                                    <button onClick={restoreDefaults} style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: 'var(--text-dim)', padding: '6px 12px', borderRadius: '8px', fontSize: '11px', fontWeight: 'bold', cursor: 'pointer', transition: 'all 0.2s' }}>
                                                        RESTORE DEFAULTS
                                                    </button>
                                                    <div style={{ height: '30px', width: '1px', background: 'rgba(255,255,255,0.1)' }} />
                                                    <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                                                        <Switch checked={config.lockInput} onChange={() => setConfig(prev => ({ ...prev, lockInput: !prev.lockInput }))} />
                                                        <span style={{ fontSize: '14px', fontWeight: '600', minWidth: '45px' }}>{config.lockInput ? 'Locked' : 'Free'}</span>
                                                    </div>
                                                </div>
                                            </div>

                                            <div style={{ padding: '20px', background: 'rgba(255,255,255,0.02)', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.05)', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '30px' }}>
                                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Switching Delay</span>
                                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{config.delay}ms</span>
                                                    </div>
                                                    <input type="range" min="0" max="1000" step="50" value={config.delay} onChange={(e) => setConfig({ ...config, delay: e.target.value })} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
                                                </label>
                                                <label style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                                                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                                        <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '600' }}>Dead Corner</span>
                                                        <span style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: '700' }}>{config.cornerSize}px</span>
                                                    </div>
                                                    <input type="range" min="0" max="200" step="10" value={config.cornerSize} onChange={(e) => setConfig({ ...config, cornerSize: e.target.value })} style={{ width: '100%', accentColor: 'var(--accent-primary)' }} />
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
                        ) : (
                            <div className="glass" style={{ padding: '80px 40px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '20px' }}>
                                <div style={{ width: '80px', height: '80px', borderRadius: '30px', background: 'rgba(255,255,255,0.02)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-dim)' }}><svg width="40" height="40" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5"><path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg></div>
                                <h2 style={{ fontSize: '24px', marginBottom: '10px' }}>No Active Connection</h2>
                                <p style={{ color: 'var(--text-dim)', maxWidth: '400px' }}>Connect to a device from the Discovery tab to start sharing.</p>
                                <button className="glow-button" onClick={() => setActiveTab('devices')}>Go to Discovery</button>
                            </div>
                        )}
                    </div>
                )
                }

                {
                    activeTab === 'devices' && (
                        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
                            <div className="glass" style={{ padding: '30px', display: 'flex', gap: '15px', alignItems: 'center' }}>
                                <input className="glass-input" placeholder="Direct IP or Pairing Code..." value={manualIp} onChange={(e) => setManualIp(e.target.value)} style={{ flexGrow: 1, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)', padding: '15px 20px', borderRadius: '12px', color: 'white' }} />
                                <button className={`glow-button ${isConnecting === manualIp ? 'loading' : ''}`} onClick={() => connectToDevice(manualIp)} disabled={isConnecting === manualIp}>{isConnecting === manualIp ? <div className="spinner"></div> : 'Quick Connect'}</button>
                            </div>
                            <div className="grid-layout" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '20px' }}>
                                {discoveredDevices.length > 0 ? discoveredDevices.map((dev, i) => (
                                    <DeviceCard key={dev.ip || i} name={dev.name || dev.hostname || 'Unknown Device'} ip={dev.ip || '0.0.0.0'} code={dev.code || '000000'} status={connectionStatus.includes('Connected') && connectedDevice?.name === dev.name ? 'Connected' : 'Available'} isConnecting={isConnecting === dev.ip} onConnect={() => connectToDevice(dev.ip)} />
                                )) : (
                                    <div className="glass" style={{ padding: '60px 40px', textAlign: 'center', gridColumn: '1 / -1' }}><p style={{ color: 'var(--text-dim)' }}>{isScanning ? 'Scanning for devices...' : 'No active devices found.'}</p></div>
                                )}
                            </div>
                        </div>
                    )
                }

                {activeTab === 'settings' && <Settings />}
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
