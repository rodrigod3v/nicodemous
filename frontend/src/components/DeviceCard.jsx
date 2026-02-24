import React from 'react';

const DeviceCard = ({ name, ip, code, status, isConnecting, onConnect }) => {
    const isConnected = status === 'Connected';

    return (
        <div className="glass animate-fade device-card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px', transition: 'all 0.3s' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                <div style={{
                    width: '44px', height: '44px', borderRadius: '12px',
                    backgroundColor: isConnected ? 'rgba(34, 197, 94, 0.12)' : 'rgba(255, 255, 255, 0.05)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center',
                    color: isConnected ? '#22c55e' : 'white'
                }}>
                    <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                    </svg>
                </div>
                <div style={{ flexGrow: 1 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <h4 style={{ fontSize: '17px', margin: 0, fontWeight: '600' }}>{name}</h4>
                        <span style={{ fontSize: '11px', color: isConnected ? '#22c55e' : '#94a3b8', fontWeight: 'bold', textTransform: 'uppercase' }}>{status}</span>
                    </div>
                    <div style={{ display: 'flex', gap: '8px', alignItems: 'center', marginTop: '4px' }}>
                        <code style={{ fontSize: '12px', color: 'var(--accent-primary)', fontWeight: 'bold' }}>{code}</code>
                        <span style={{ color: 'rgba(255,255,255,0.1)' }}>|</span>
                        <code style={{ fontSize: '11px', color: 'var(--text-dim)' }}>{ip}</code>
                    </div>
                </div>
            </div>
            <button
                className={isConnected ? "secondary-button" : `glow-button ${isConnecting ? 'loading' : ''}`}
                style={{ width: '100%', padding: '10px', fontSize: '14px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
                onClick={onConnect}
                disabled={isConnecting}
            >
                {isConnecting ? (
                    <>
                        <div className="spinner" style={{ width: '16px', height: '16px', border: '2px solid rgba(255,255,255,0.3)', borderRadius: '50%', borderTopColor: 'white', animation: 'spin 0.8s linear infinite' }}></div>
                        Connecting...
                    </>
                ) : (isConnected ? 'Disconnect' : 'Request Access')}
            </button>
            <style dangerouslySetInnerHTML={{
                __html: `
                @keyframes spin { to { transform: rotate(360deg); } }
            `}} />
        </div>
    );
};

export default DeviceCard;
