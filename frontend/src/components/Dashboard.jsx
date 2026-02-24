import React, { useState, useEffect } from 'react';
import Sidebar from './layout/Sidebar';
import OverviewTab from './tabs/OverviewTab';
import DiscoveryTab from './tabs/DiscoveryTab';
import ControlTab from './tabs/ControlTab';
import SettingsTab from './tabs/SettingsTab';
import { useNicodemous } from '../context/NicodemousContext';

const Dashboard = () => {
    const [activeTab, setActiveTab] = useState('overview');
    const { connectionStatus, sendMessage, sessionRole } = useNicodemous();
    const [isScanning, setIsScanning] = useState(false);

    // Auto-navigate to Active Device tab on connection and redirect back on disconnection
    useEffect(() => {
        if (connectionStatus.includes('Connected')) {
            setActiveTab('device');
        } else if (activeTab === 'device') {
            setActiveTab('overview');
        }
    }, [connectionStatus, activeTab]);

    const handleStartDiscovery = () => {
        setIsScanning(true);
        sendMessage('start_discovery');
        setActiveTab('devices');
        setTimeout(() => setIsScanning(false), 10000);
    };

    const renderTabContent = () => {
        switch (activeTab) {
            case 'overview':
                return <OverviewTab onNavigate={setActiveTab} />;
            case 'devices':
                return <DiscoveryTab />;
            case 'device':
                return <ControlTab />;
            case 'settings':
                return <SettingsTab />;
            default:
                return <OverviewTab onNavigate={setActiveTab} />;
        }
    };

    return (
        <div className="dashboard-container" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            <Sidebar activeTab={activeTab} onTabChange={setActiveTab} />

            <main style={{ flexGrow: 1, padding: '40px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '30px' }}>
                <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                        <h1 style={{ fontSize: '32px', marginBottom: '8px' }}>Control Center</h1>
                        <p style={{ color: 'var(--text-dim)' }}>Manage and discover devices on your local network</p>
                    </div>
                    <button
                        className={`glow-button ${isScanning ? 'scanning' : ''}`}
                        onClick={handleStartDiscovery}
                        disabled={isScanning}
                    >
                        {isScanning ? (
                            <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><div className="spinner"></div>Scanning...</div>
                        ) : 'Find New Devices'}
                    </button>
                </header>

                {renderTabContent()}
            </main>

            <style dangerouslySetInnerHTML={{
                __html: `
                .spinner { width: 16px; height: 16px; border: 2px solid rgba(255,255,255,0.3); border-radius: 50%; border-top-color: white; animation: spin 0.8s linear infinite; }
                @keyframes spin { to { transform: rotate(360deg); } }
                .glass-input:focus { outline: none; border-color: var(--accent-primary) !important; background: rgba(255,255,255,0.05) !important; }
                .glass-btn-small { background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); color: var(--text-dim); padding: 6px 12px; borderRadius: 8px; fontSize: 11px; fontWeight: bold; cursor: pointer; transition: all 0.2s; }
                .glass-btn-small:hover { background: rgba(255,255,255,0.1); color: white; }
            `}} />
        </div>
    );
};

export default Dashboard;
