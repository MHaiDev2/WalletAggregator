import { useState } from 'react';
import { ethers } from 'ethers';
import './app.css';

const SUPPORTED_WALLETS = [
  {
    id: 'metamask',
    name: 'MetaMask',
    check: () => typeof window.ethereum !== 'undefined' && window.ethereum.isMetaMask,
    connect: async () => {
      const provider = new ethers.BrowserProvider(window.ethereum);
      return provider;
    }
  },
  {
    id: 'rabby',
    name: 'Rabby Wallet',
    check: () => typeof window.rabby !== 'undefined',
    connect: async () => {
      const provider = new ethers.BrowserProvider(window.rabby);
      return provider;
    }
  }
];

function App() {
  // IMPORTANT: Instead of null, better use an empty string ('') or undefined
  // for the 'select' element to avoid warnings.
  const [selectedWalletId, setSelectedWalletId] = useState('');
  const [wallets, setWallets] = useState([]);
  const [totalBalance, setTotalBalance] = useState(null);
  const [loading, setLoading] = useState(false);
  const [uiMessage, setUiMessage] = useState('');

  /**
   * Filter all supported wallets and return only those available in the current browser.
   */
  const checkAvailableWallets = () => {
    return SUPPORTED_WALLETS.filter(wallet => wallet.check());
  };

  /**
   * Connect to the selected wallet.
   */
  const connectWallet = async () => {
    if (!selectedWalletId) {
      setUiMessage('📱 Please select a wallet first.');
      return;
    }

    const wallet = SUPPORTED_WALLETS.find(w => w.id === selectedWalletId);
    if (!wallet) return;

    try {
      const provider = await wallet.connect();
      const accounts = await provider.send('eth_requestAccounts', []);
      const address = accounts[0];

      // Prevent duplicate wallets
      if (wallets.includes(address)) {
        setUiMessage('⚠️ This wallet is already connected.');
        return;
      }

      // Limit to two wallets
      if (wallets.length >= 2) {
        setUiMessage('❌ Only two wallets are allowed.');
        return;
      }

      // Example: Listen to account changes (for MetaMask only)
      if (window.ethereum?.on && selectedWalletId === 'metamask') {
        window.ethereum.on('accountsChanged', (newAccounts) => {
          if (newAccounts.length > 0) {
            const newAddress = newAccounts[0];
            // Remove old address, add the new address
            setWallets(prev => [
              ...prev.filter(addr => addr !== address),
              newAddress
            ]);
          }
        });
      }

      setWallets(prev => [...prev, address]);
      setUiMessage('');
    } catch (err) {
      console.error('Connection error:', err);
      setUiMessage('❌ Wallet connection failed.');
    }
  };

  /**
   * Retrieve the total POL balance for the connected wallets.
   */
  const getTotalBalance = async () => {
    setLoading(true);
    setTotalBalance(null);
    setUiMessage('');
    try {
      const response = await fetch('https://localhost:7299/api/wallet/aggregate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ addresses: wallets })
      });
      const result = await response.json();
      if (response.ok) {
        setTotalBalance(result.totalBalance);
      } else {
        setUiMessage(`❌ Error: ${JSON.stringify(result)}`);
      }
    } catch (err) {
      console.error('Network error:', err);
      setUiMessage('❌ Network error when retrieving the balance.');
    }
    setLoading(false);
  };

  /**
   * Reset all states (wallet addresses, balance, messages, and selected wallet).
   */
  const reset = () => {
    setWallets([]);
    setTotalBalance(null);
    setUiMessage('');
    setSelectedWalletId('');
  };

  return (
    <div className="container">
      <h1>Wallet POL Aggregator</h1>

      <div>
        <select 
          value={selectedWalletId}
          onChange={(e) => setSelectedWalletId(e.target.value)}
        >
          <option value="">📱 Select a wallet...</option>
          {checkAvailableWallets().map((wallet) => (
            <option key={wallet.id} value={wallet.id}>
              {wallet.name}
            </option>
          ))}
        </select>

        <button onClick={connectWallet}>
          🔌 Connect Wallet
        </button>

        <button onClick={reset}>
          🔄 Reset
        </button>
      </div>

      <ul className="wallet-list">
        {wallets.map((addr, idx) => (
          <li key={addr}>
            Wallet #{idx + 1}: {addr}
          </li>
        ))}
      </ul>

      {wallets.length === 1 && (
        <p>
          <em>⚠️ Please switch to a different wallet address and reconnect.</em>
        </p>
      )}

      {wallets.length === 2 && (
        <button onClick={getTotalBalance}>
          💰 Check Total Balance
        </button>
      )}

      {loading && <p className="loading">⏳ Loading...</p>}

      {totalBalance !== null && (
        <p style={{ fontSize: '1.5rem', marginTop: '1rem' }}>
          ✅ You own <strong>{totalBalance}</strong> POL
        </p>
      )}

      {uiMessage && (
        <p className="message">{uiMessage}</p>
      )}
    </div>
  );
}

export default App;
