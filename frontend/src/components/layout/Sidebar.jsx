import React from 'react';
import TabButton from '../TabButton';
import { useNicodemouse } from '../../context/nicodemouseContext';

const Sidebar = ({ activeTab, onTabChange, onLogout }) => {
    const { connectionStatus, localIp, sessionRole } = useNicodemouse();

    return (
        <div className="sidebar" style={{
            width: 'var(--sidebar-width)',
            height: '100%',
            backgroundColor: 'var(--bg-sidebar)',
            borderRight: '1px solid rgba(255,255,255,0.03)',
            padding: '32px 20px',
            display: 'flex',
            flexDirection: 'column',
            gap: '8px',
            transition: 'width 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
            flexShrink: 0,
            overflowX: 'hidden',
            overflowY: 'auto',
            zIndex: 10
        }}>
            <div style={{ padding: '0 8px 12px 8px', fontSize: '12px', fontWeight: '600', color: 'var(--text-dim)', textTransform: 'uppercase', letterSpacing: '1px' }}>
                <span className="sidebar-label">Menu</span>
            </div>

            <TabButton active={activeTab === 'overview'} onClick={() => onTabChange('overview')} label="Overview" icon="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            <TabButton active={activeTab === 'devices'} onClick={() => onTabChange('devices')} label="Discovery" icon="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />

            {connectionStatus.includes('Connected') && (
                <TabButton active={activeTab === 'device'} onClick={() => onTabChange('device')} label="Active Device" icon="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2" />
            )}

            <TabButton active={activeTab === 'settings'} onClick={() => onTabChange('settings')} label="Settings" icon="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />

            <div style={{ marginTop: 'auto', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div className="status" style={{
                    padding: '16px',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '12px',
                    backgroundColor: 'rgba(255,255,255,0.02)',
                    borderRadius: '12px',
                    border: '1px solid rgba(255,255,255,0.04)'
                }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                        <div className="status-pulse" style={{ backgroundColor: connectionStatus.includes('Connected') ? '#22c55e' : (connectionStatus.includes('Connecting') ? '#f59e0b' : '#ef4444') }}></div>
                        <span style={{ fontSize: '13px', color: '#e2e8f0', fontWeight: '600' }}>
                            {sessionRole ? `${sessionRole.charAt(0).toUpperCase() + sessionRole.slice(1)}` : (connectionStatus === 'Disconnected' ? 'Disconnected' : connectionStatus)}
                        </span>
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', fontSize: '12px', color: 'var(--text-dim)' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <span>IP</span>
                            <span style={{ fontFamily: 'monospace' }}>{localIp?.ip || '0.0.0.0'}</span>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <span>PIN</span>
                            <span style={{ color: 'var(--accent-primary)', fontWeight: '700', fontFamily: 'monospace', letterSpacing: '1px' }}>{localIp?.code || '......'}</span>
                        </div>
                    </div>
                </div>

                <button
                    onClick={onLogout}
                    style={{
                        width: '100%',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: '8px',
                        padding: '12px',
                        color: '#ef4444',
                        backgroundColor: 'rgba(239, 68, 68, 0.05)',
                        border: '1px solid rgba(239, 68, 68, 0.1)',
                        borderRadius: '12px',
                        fontSize: '14px',
                        fontWeight: '600',
                        transition: 'all 0.2s ease'
                    }}
                    onMouseEnter={(e) => {
                        e.currentTarget.style.backgroundColor = 'rgba(239, 68, 68, 0.1)';
                    }}
                    onMouseLeave={(e) => {
                        e.currentTarget.style.backgroundColor = 'rgba(239, 68, 68, 0.05)';
                    }}
                >
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"></path><polyline points="16 17 21 12 16 7"></polyline><line x1="21" y1="12" x2="9" y2="12"></line></svg>
                    Logout
                </button>
            </div>
        </div>
    );
};

export default Sidebar;
