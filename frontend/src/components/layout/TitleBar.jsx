import React, { useState } from 'react';
import { Minus, X } from 'lucide-react';
import './TitleBar.css';

const TitleBar = () => {
    const [showConfirm, setShowConfirm] = useState(false);

    const handleMinimize = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "minimize_app" }));
        }
    };

    const handleCloseClick = () => {
        setShowConfirm(true);
    };

    const confirmHide = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "close_app" }));
        }
        setShowConfirm(false);
    };

    const confirmExit = () => {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "exit_app" }));
        }
        setShowConfirm(false);
    };

    const handleDrag = (e) => {
        if (e.buttons !== 1) return;
        if (e.target.closest('.title-bar-btn')) return;
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type: "drag_app" }));
        }
    };

    return (
        <>
            <div className="title-bar" onMouseDown={handleDrag}>
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
