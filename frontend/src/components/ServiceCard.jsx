import React from 'react';
import Switch from './Switch';

const ServiceCard = ({ title, description, enabled, onToggle, icon }) => (
    <div className="glass animate-fade" style={{ padding: '30px', display: 'flex', flexDirection: 'column', gap: '20px', transition: 'transform 0.3s' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ width: '52px', height: '52px', borderRadius: '16px', backgroundColor: 'rgba(99, 102, 241, 0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--accent-primary)' }}>
                <svg width="26" height="26" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2.5">
                    <path strokeLinecap="round" strokeLinejoin="round" d={icon} />
                </svg>
            </div>
            <Switch checked={enabled} onChange={onToggle} />
        </div>
        <div>
            <h3 style={{ fontSize: '20px', marginBottom: '8px', color: 'white' }}>{title}</h3>
            <p style={{ color: 'var(--text-dim)', fontSize: '14px', lineHeight: '1.6' }}>{description}</p>
        </div>
    </div>
);

export default ServiceCard;
