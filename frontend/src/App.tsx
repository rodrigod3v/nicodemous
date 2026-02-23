import React, { useState, useEffect } from 'react';
import { Monitor, MousePointer2, Clipboard, Mic, FileText, Settings, Shield, Activity } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';

function App() {
  const [devices, setDevices] = useState([
    { name: 'MacBook Pro', ip: '192.168.1.15', status: 'Connected', id: '1' },
    { name: 'Windows-Gaming', ip: '192.168.1.22', status: 'Discovered', id: '2' },
  ]);

  const [features, setFeatures] = useState({
    mouse: true,
    clipboard: true,
    audio: false,
    files: true
  });

  const toggleFeature = (feature: keyof typeof features) => {
    setFeatures(prev => ({ ...prev, [feature]: !prev[feature] }));
    // @ts-ignore
    window.external.sendMessage(JSON.stringify({ 
      command: 'toggle_feature', 
      feature, 
      enabled: !features[feature] 
    }));
  };

  return (
    <div className="p-8 h-screen w-screen flex gap-8">
      {/* Sidebar - Control Panel */}
      <motion.div 
        initial={{ x: -50, opacity: 0 }}
        animate={{ x: 0, opacity: 1 }}
        className="w-80 glass-card p-6 flex flex-col gap-6"
      >
        <div className="flex items-center gap-3 mb-4">
          <div className="p-3 bg-indigo-500 rounded-xl shadow-lg shadow-indigo-500/20">
            <Shield className="text-white w-6 h-6" />
          </div>
          <h1 className="text-xl font-bold tracking-tight">Nicodemous</h1>
        </div>

        <div className="space-y-4">
          <p className="text-xs font-semibold text-dim uppercase tracking-wider">Features</p>
          
          <FeatureToggle 
            icon={<MousePointer2 size={20}/>} 
            label="Mouse & Keys" 
            enabled={features.mouse} 
            onClick={() => toggleFeature('mouse')} 
          />
          <FeatureToggle 
            icon={<Clipboard size={20}/>} 
            label="Clipboard Sync" 
            enabled={features.clipboard} 
            onClick={() => toggleFeature('clipboard')} 
          />
          <FeatureToggle 
            icon={<Mic size={20}/>} 
            label="Audio Sharing" 
            enabled={features.audio} 
            onClick={() => toggleFeature('audio')} 
          />
          <FeatureToggle 
            icon={<FileText size={20}/>} 
            label="File Transfer" 
            enabled={features.files} 
            onClick={() => toggleFeature('files')} 
          />
        </div>

        <div className="mt-auto p-4 bg-white/5 rounded-2xl border border-white/5">
          <div className="flex items-center gap-3">
            <Activity className="text-green-400 w-4 h-4" />
            <span className="text-sm font-medium">System Optimized</span>
          </div>
          <p className="text-xs text-dim mt-1">Latency: 2ms (UDP)</p>
        </div>
      </motion.div>

      {/* Main Content - Device List */}
      <div className="flex-1 flex flex-col gap-8">
        <header className="flex justify-between items-center">
          <div>
            <h2 className="text-3xl font-bold tracking-tight">Nearby Desktops</h2>
            <p className="text-dim mt-1">Select a device to start sharing control.</p>
          </div>
          <button className="p-3 glass-card hover:bg-white/10">
            <Settings className="text-dim" size={20} />
          </button>
        </header>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <AnimatePresence>
            {devices.map((device, idx) => (
              <DeviceCard key={device.id} device={device} index={idx} />
            ))}
          </AnimatePresence>
        </div>
      </div>
    </div>
  );
}

function FeatureToggle({ icon, label, enabled, onClick }: any) {
  return (
    <button 
      onClick={onClick}
      className={`w-full flex items-center justify-between p-4 rounded-2xl border transition-all ${
        enabled ? 'bg-indigo-500/10 border-indigo-500/30' : 'bg-white/5 border-white/5 grayscale'
      }`}
    >
      <div className="flex items-center gap-3">
        <div className={enabled ? 'text-indigo-400' : 'text-dim'}>{icon}</div>
        <span className={`text-sm font-medium ${enabled ? 'text-white' : 'text-dim'}`}>{label}</span>
      </div>
      <div className={`w-8 h-4 rounded-full relative transition-colors ${enabled ? 'bg-indigo-500' : 'bg-white/20'}`}>
        <motion.div 
          animate={{ x: enabled ? 18 : 2 }}
          className="absolute top-1 left-0 w-2 h-2 bg-white rounded-full"
        />
      </div>
    </button>
  );
}

function DeviceCard({ device, index }: any) {
  return (
    <motion.div
      initial={{ y: 20, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ delay: index * 0.1 }}
      whileHover={{ scale: 1.02 }}
      className="glass-card p-6 group relative overflow-hidden"
    >
      <div className="absolute top-0 right-0 p-4">
        {device.status === 'Connected' ? (
          <div className="status-pulse" />
        ) : (
          <div className="w-2 h-2 rounded-full bg-white/20" />
        )}
      </div>
      
      <div className="p-4 bg-white/5 rounded-2xl w-fit mb-4">
        <Monitor className="text-indigo-400" size={24} />
      </div>

      <h3 className="text-lg font-bold">{device.name}</h3>
      <p className="text-dim text-sm">{device.ip}</p>

      <div className="mt-6 pt-6 border-t border-white/5 flex items-center justify-between">
        <span className="text-xs font-semibold uppercase tracking-widest text-dim">
          {device.status}
        </span>
        <button className="text-xs font-bold text-indigo-400 hover:text-indigo-300">
          Settings
        </button>
      </div>

      {/* Decorative gradient */}
      <div className="absolute -bottom-10 -right-10 w-24 h-24 bg-indigo-500/10 blur-3xl rounded-full" />
    </motion.div>
  );
}

export default App;
