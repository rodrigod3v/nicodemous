import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

const NicodemouseContext = createContext();

export const NicodemouseProvider = ({ children }) => {
    const [connectionStatus, setConnectionStatus] = useState('Disconnected');
    const [discoveredDevices, setDiscoveredDevices] = useState([]);
    const [localIp, setLocalIp] = useState({ ip: '...', code: '...' });
    const [settings, setSettings] = useState(null);
    const [systemInfo, setSystemInfo] = useState({ machineName: '...', monitors: [] });
    const [connectedDevice, setConnectedDevice] = useState(null);
    const [sessionRole, setSessionRole] = useState(null); // 'controlling' or 'controlled'

    // Helper to send messages to Photino backend
    const sendMessage = useCallback((type, data = {}) => {
        const message = JSON.stringify({ type, ...data });
        console.log('[FRONTEND] Sending to backend:', type, data);
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(message);
        } else if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(message);
        }
    }, []);

    const connectDevice = useCallback((ip) => {
        sendMessage('connect_device', { ip });
    }, [sendMessage]);

    const toggleService = useCallback((service, enabled) => {
        sendMessage('service_toggle', { service, enabled });
    }, [sendMessage]);

    const updateSettings = useCallback((newSettings) => {
        sendMessage('update_settings', newSettings);
    }, [sendMessage]);

    const handleBackendMessage = useCallback((message) => {
        try {
            if (!message) return;
            const data = typeof message === 'string' ? JSON.parse(message) : message;
            if (!data) return;

            console.log('[FRONTEND] Context received message:', data.type, data);

            switch (data.type) {
                case 'discovery_result':
                    setDiscoveredDevices(data.devices || []);
                    break;
                case 'local_ip':
                    setLocalIp(data.detail);
                    break;
                case 'system_info':
                    setSystemInfo(data);
                    break;
                case 'connection_status':
                    const status = data.status;
                    const statusLower = status.toLowerCase();
                    setConnectionStatus(status);

                    if (statusLower.includes('disconnected') || status.includes('Error')) {
                        console.log('[FRONTEND] Disconnection detected, clearing session.');
                        setConnectedDevice(null);
                        setSessionRole(null);
                    } else if (status.includes('Controlled by')) {
                        const deviceName = status.replace('Controlled by', '').trim();
                        setConnectedDevice({ name: deviceName });
                        setSessionRole('controlled');
                    } else if (status.includes('Connected')) {
                        setSessionRole('controlling');
                    }
                    break;
                case 'settings_data':
                    setSettings(typeof data.settings === 'string' ? JSON.parse(data.settings) : data.settings);
                    break;
                default:
                    console.log('[FRONTEND] Unhandled message type:', data.type);
            }
        } catch (e) {
            console.error('[FRONTEND] Error processing backend message:', e);
        }
    }, []);

    useEffect(() => {
        // Initial request for settings
        sendMessage('get_settings');

        // Setup unified message listener
        const bridge = (e) => handleBackendMessage(e.data || e);

        if (window.external && window.external.receiveMessage) {
            window.external.receiveMessage(handleBackendMessage);
        } else if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', bridge);
        } else if (window.photino) {
            window.photino.receive && window.photino.receive(handleBackendMessage);
        }

        return () => {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.removeEventListener('message', bridge);
            }
        };
    }, [handleBackendMessage, sendMessage]);

    const value = {
        connectionStatus,
        discoveredDevices,
        localIp,
        settings,
        systemInfo,
        connectedDevice,
        sessionRole,
        sendMessage,
        connectDevice,
        toggleService,
        updateSettings
    };

    return (
        <NicodemouseContext.Provider value={value}>
            {children}
        </NicodemouseContext.Provider>
    );
};

export const usenicodemouse = () => {
    const context = useContext(NicodemouseContext);
    if (!context) {
        throw new Error('usenicodemouse must be used within a NicodemouseProvider');
    }
    return context;
};
