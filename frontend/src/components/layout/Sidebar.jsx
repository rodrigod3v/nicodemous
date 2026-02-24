import React from 'react';
import TabButton from '../TabButton';
import { useNicodemous } from '../../context/NicodemousContext';

const Sidebar = ({ activeTab, onTabChange }) => {
    const { connectionStatus, localIp, sessionRole } = useNicodemous();

    return (
        <div className="glass" style={{ width: 'var(--sidebar-width)', padding: '30px 20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <div className="brand animate-fade" style={{ fontSize: '26px', fontWeight: '800', marginBottom: '30px', display: 'flex', alignItems: 'center', gap: '12px' }}>
                <div className="glow-button" style={{ width: '42px', height: '42px', borderRadius: '12px', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 0 }}>
                    <svg width="24" height="24" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 11c0 3.517-1.009 6.799-2.753 9.571m-3.44-20.4L2.032 4.415m1.889-1.342c.447.83 1.107 1.567 1.886 2.149M3.5 11a7.5 7.5 0 1115 0 7.5 7.5 0 01-15 0z" />
                    </svg>
                </div>
                <span className="gradient-text">Nicodemous</span>
            </div>

            <TabButton active={activeTab === 'overview'} onClick={() => onTabChange('overview')} label="Overview" icon="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            <TabButton active={activeTab === 'devices'} onClick={() => onTabChange('devices')} label="Discovery" icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />

            {connectionStatus.includes('Connected') && (
                <TabButton active={activeTab === 'device'} onClick={() => onTabChange('device')} label="Active Device" icon="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
            )}

            <TabButton active={activeTab === 'settings'} onClick={() => onTabChange('settings')} label="Settings" icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />

            <div className="status glass" style={{ padding: '15px', display: 'flex', flexDirection: 'column', gap: '8px', marginTop: 'auto' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <div className="status-pulse" style={{ backgroundColor: connectionStatus.includes('Connected') ? '#22c55e' : (connectionStatus.includes('Connecting') ? '#f59e0b' : '#ef4444') }}></div>
                    <span style={{ fontSize: '13px', color: 'var(--text-dim)', fontWeight: '500' }}>
                        {sessionRole ? `${sessionRole.charAt(0).toUpperCase() + sessionRole.slice(1)}` : (connectionStatus === 'Disconnected' ? 'Disconnected' : connectionStatus)}
                    </span>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', fontSize: '11px', color: 'rgba(255,255,255,0.4)' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>IP</span><span>{localIp?.ip || '0.0.0.0'}</span></div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}><span>PIN</span><span style={{ color: 'var(--accent-primary)', fontWeight: 'bold' }}>{localIp?.code || '......'}</span></div>
                </div>
            </div>
        </div>
    );
};

export default Sidebar;
