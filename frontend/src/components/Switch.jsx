import React from 'react';

const Switch = ({ checked, onChange }) => (
    <div
        onClick={onChange}
        style={{
            width: '52px',
            height: '28px',
            borderRadius: '30px',
            backgroundColor: checked ? 'var(--accent-primary)' : 'rgba(255, 255, 255, 0.1)',
            position: 'relative',
            cursor: 'pointer',
            transition: 'background-color 0.3s'
        }}
    >
        <div style={{
            width: '20px',
            height: '20px',
            borderRadius: '50%',
            backgroundColor: 'white',
            position: 'absolute',
            top: '4px',
            left: checked ? '28px' : '4px',
            transition: 'left 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275)'
        }} />
    </div>
);

export default Switch;
