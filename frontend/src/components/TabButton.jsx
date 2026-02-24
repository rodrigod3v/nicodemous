import React from 'react';

const TabButton = ({ active, onClick, label, icon }) => {
    // console.log(`[FRONTEND] TabButton Render: ${label}`);
    return (
        <button
            onClick={onClick}
            style={{
                padding: '12px 18px',
                borderRadius: '12px',
                display: 'flex',
                alignItems: 'center',
                gap: '12px',
                width: '100%',
                justifyContent: 'flex-start',
                backgroundColor: active ? 'rgba(99, 102, 241, 0.12)' : 'transparent',
                color: active ? 'var(--accent-primary)' : 'var(--text-dim)',
                fontSize: '15px',
                fontWeight: active ? '600' : '500',
                transition: 'all 0.2s',
                border: active ? '1px solid rgba(99, 102, 241, 0.2)' : '1px solid transparent'
            }}
        >
            <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
                <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
            </svg>
            {label}
        </button>
    );
};

export default TabButton;
