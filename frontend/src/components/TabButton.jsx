import React from 'react';

const TabButton = ({ active, onClick, label, icon }) => {
    // console.log(`[FRONTEND] TabButton Render: ${label}`);
    return (
        <button
            onClick={onClick}
            style={{
                position: 'relative',
                padding: '14px 20px',
                borderRadius: '12px',
                display: 'flex',
                alignItems: 'center',
                gap: '14px',
                width: '100%',
                justifyContent: 'flex-start',
                backgroundColor: active ? 'rgba(139, 92, 246, 0.1)' : 'transparent',
                color: active ? '#f8fafc' : 'var(--text-dim)',
                fontSize: '15px',
                fontWeight: active ? '600' : '500',
                transition: 'all 0.2s ease',
                border: 'none',
                overflow: 'hidden'
            }}
            onMouseEnter={(e) => {
                if (!active) {
                    e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.05)';
                    e.currentTarget.style.color = '#e2e8f0';
                }
            }}
            onMouseLeave={(e) => {
                if (!active) {
                    e.currentTarget.style.backgroundColor = 'transparent';
                    e.currentTarget.style.color = 'var(--text-dim)';
                }
            }}
        >
            {active && (
                <div style={{
                    position: 'absolute',
                    left: 0,
                    top: '50%',
                    transform: 'translateY(-50%)',
                    width: '4px',
                    height: '24px',
                    backgroundColor: 'var(--accent-primary)',
                    borderRadius: '0 4px 4px 0',
                    boxShadow: '0 0 10px var(--accent-primary)'
                }} />
            )}
            <svg width="22" height="22" fill="none" stroke={active ? 'var(--accent-primary)' : 'currentColor'} viewBox="0 0 24 24" strokeWidth="2">
                <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
            </svg>
            <span className="sidebar-label" style={{
                opacity: active ? 1 : 0.8,
                transition: 'opacity 0.2s'
            }}>{label}</span>
        </button>
    );
};

export default TabButton;
