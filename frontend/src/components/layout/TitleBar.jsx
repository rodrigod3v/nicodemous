import React, { useState, useEffect } from 'react';
import { Minus, X } from 'lucide-react';
import './TitleBar.css';

const TitleBar = () => {
    const [showConfirm, setShowConfirm] = useState(false);

    useEffect(() => {
        let isDragging = false;
        let startX = 0;
        let startY = 0;
        const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;

        const handleMouseDown = (e) => {
            if (e.target.closest('.title-bar-btn')) return;
            if (e.button !== 0) return;
            if (e.target.closest('.title-bar-drag-area') || e.target.closest('.title-bar')) {
                if (isMac) {
                    isDragging = true;
                    startX = e.screenX;
                    startY = e.screenY;
                } else {
                    if (window.external && window.external.sendMessage) {
                        window.external.sendMessage(JSON.stringify({ type: "drag_app" }));
                    }
                }
            }
        };

        const handleMouseMove = (e) => {
            if (!isDragging) return;
            const dx = e.screenX - startX;
            const dy = e.screenY - startY;
            startX = e.screenX;
            startY = e.screenY;

            if (window.external && window.external.sendMessage) {
                window.external.sendMessage(JSON.stringify({ type: "move_app", dx, dy }));
            }
        };

        const handleMouseUp = () => {
            isDragging = false;
        };

        window.addEventListener('mousedown', handleMouseDown);
        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);

        return () => {
            window.removeEventListener('mousedown', handleMouseDown);
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, []);

    const handleMinimize = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "hide_app" }));
        }
    };

    const handleCloseClick = () => {
        setShowConfirm(true);
    };

    const confirmHide = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "hide_app" }));
        }
        setShowConfirm(false);
    };

    const confirmExit = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "exit_app" }));
        }
        setShowConfirm(false);
    };

    return (
        <>
            <div className="title-bar">
                <div className="title-bar-drag-area"></div>

                <div className="title-bar-content">
                    <div className="title-bar-left">
                        <div className="animated-n-logo">
                            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                <path d="M5 20V4L19 20V4" stroke="url(#n-gradient)" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" className="n-path" />
                                <defs>
                                    <linearGradient id="n-gradient" x1="5" y1="4" x2="19" y2="20" gradientUnits="userSpaceOnUse">
                                        <stop stopColor="#8b5cf6" />
                                        <stop offset="1" stopColor="#d946ef" />
                                    </linearGradient>
                                </defs>
                            </svg>
                        </div>
                        <span className="title-bar-title">nicodemouse</span>
                    </div>

                    <div className="title-bar-controls">
                        <button className="title-bar-btn minimize-btn" onClick={handleMinimize} title="Minimize">
                            <Minus size={16} />
                        </button>

                        <button className="title-bar-btn close-btn" onClick={handleCloseClick} title="Close">
                            <X size={16} />
                        </button>
                    </div>
                </div>
            </div>

            {showConfirm && (
                <div className="close-confirm-overlay">
                    <div className="close-confirm-modal glass">
                        <h3>Exit nicodemouse</h3>
                        <p>Would you like to fully exit the application, or keep it running in the system tray?</p>
                        <div className="close-confirm-actions">
                            <button className="secondary-button" onClick={() => setShowConfirm(false)}>Cancel</button>
                            <button className="secondary-button" onClick={confirmHide}>Minimize to Tray</button>
                            <button className="glow-button" style={{ background: 'linear-gradient(135deg, #ef4444, #b91c1c)', boxShadow: '0 10px 15px -3px rgba(239, 68, 68, 0.3)' }} onClick={confirmExit}>Exit Application</button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
};

export default TitleBar;
