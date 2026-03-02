import React from 'react';
import { motion } from 'framer-motion';

const AnimatedLogo = ({ size = 80 }) => {
    return (
        <motion.div
            style={{ width: size, height: size, position: 'relative' }}
            initial={{ scale: 0.5, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            transition={{ duration: 1, ease: [0.16, 1, 0.3, 1] }}
        >
            {/* Outer Rotating Ring */}
            <motion.svg
                viewBox="0 0 100 100"
                style={{ position: 'absolute', inset: 0 }}
                animate={{ rotate: 360 }}
                transition={{ duration: 15, repeat: Infinity, ease: "linear" }}
            >
                <circle
                    cx="50" cy="50" r="45"
                    fill="none"
                    stroke="url(#logo-gradient)"
                    strokeWidth="1"
                    strokeDasharray="10 15"
                    opacity="0.5"
                />
            </motion.svg>

            {/* Pulsing Hexagon Background */}
            <motion.svg
                viewBox="0 0 100 100"
                style={{ position: 'absolute', inset: 0 }}
                animate={{
                    scale: [1, 1.05, 1],
                    opacity: [0.3, 0.6, 0.3]
                }}
                transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }}
            >
                <path
                    d="M50 5 L90 25 L90 75 L50 95 L10 75 L10 25 Z"
                    fill="var(--accent-primary)"
                    opacity="0.1"
                />
            </motion.svg>

            {/* Main Shield Icon */}
            <motion.div
                style={{
                    position: 'absolute',
                    inset: 0,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'white',
                    filter: 'drop-shadow(0 0 15px rgba(139, 92, 246, 0.6))'
                }}
                animate={{ y: [0, -4, 0] }}
                transition={{ duration: 3, repeat: Infinity, ease: "easeInOut" }}
            >
                <svg width={size * 0.5} height={size * 0.5} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
                    <motion.path
                        d="M12 8v4"
                        initial={{ pathLength: 0 }}
                        animate={{ pathLength: 1 }}
                        transition={{ duration: 1, delay: 1 }}
                    />
                    <motion.path
                        d="M12 16h.01"
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        transition={{ duration: 0.5, delay: 1.5 }}
                    />
                </svg>
            </motion.div>

            {/* Definitions for Gradient */}
            <svg style={{ width: 0, height: 0, position: 'absolute' }}>
                <defs>
                    <linearGradient id="logo-gradient" x1="0%" y1="0%" x2="100%" y2="100%">
                        <stop offset="0%" stopColor="var(--accent-primary)" />
                        <stop offset="100%" stopColor="var(--accent-secondary)" />
                    </linearGradient>
                </defs>
            </svg>
        </motion.div>
    );
};

export default AnimatedLogo;
