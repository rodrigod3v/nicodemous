import React, { useState } from 'react';
import { usenicodemouse } from '../../context/nicodemouseContext';
import DeviceCard from '../DeviceCard';

const DiscoveryTab = () => {
    const {
        discoveredDevices,
        connectionStatus,
        connectedDevice,
        connectDevice,
        sendMessage
    } = usenicodemouse();

    const [manualIp, setManualIp] = useState('');
    const [isScanning, setIsScanning] = useState(false);
    const [isConnecting, setIsConnecting] = useState(null);

    const startDiscovery = () => {
        setIsScanning(true);
        sendMessage('start_discovery');
        setTimeout(() => setIsScanning(false), 10000);
    };

    const handleConnect = (ip) => {
        setIsConnecting(ip);
        connectDevice(ip);
        // Context will clear connecting state on status change
    };

    return (
        <div className="animate-fade" style={{ display: 'flex', flexDirection: 'column', gap: '30px' }}>
            <div className="glass" style={{ padding: '30px', display: 'flex', gap: '15px', alignItems: 'center' }}>
                <input
                    className="glass-input"
                    placeholder="Direct IP or Pairing Code..."
                    value={manualIp}
                    onChange={(e) => setManualIp(e.target.value)}
                    style={{ flexGrow: 1, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.08)', padding: '15px 20px', borderRadius: '12px', color: 'white' }}
                />
                <button
                    className={`glow-button ${isConnecting === manualIp ? 'loading' : ''}`}
                    onClick={() => handleConnect(manualIp)}
                    disabled={isConnecting === manualIp}
                >
                    {isConnecting === manualIp ? <div className="spinner"></div> : 'Quick Connect'}
                </button>
            </div>

            <div className="grid-layout" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '20px' }}>
                {discoveredDevices.length > 0 ? discoveredDevices.map((dev, i) => (
                    <DeviceCard
                        key={dev.ip || i}
                        name={dev.name || dev.hostname || 'Unknown Device'}
                        ip={dev.ip || '0.0.0.0'}
                        code={dev.code || '000000'}
                        status={connectionStatus.includes('Connected') && connectedDevice?.name === dev.name ? 'Connected' : 'Available'}
                        isConnecting={isConnecting === dev.ip}
                        onConnect={() => handleConnect(dev.ip)}
                    />
                )) : (
                    <div className="glass" style={{ padding: '60px 40px', textAlign: 'center', gridColumn: '1 / -1' }}>
                        <p style={{ color: 'var(--text-dim)' }}>
                            {isScanning ? 'Scanning for devices...' : 'No active devices found.'}
                        </p>
                    </div>
                )}
            </div>
        </div>
    );
};

export default DiscoveryTab;
