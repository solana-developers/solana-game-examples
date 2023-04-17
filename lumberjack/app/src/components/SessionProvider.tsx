// components/SessionProvider.tsx
// The SessionProvider component initializes the SessionKeyManager and provides it to its children via context.
// Wrap any component that needs access to the SessionKeyManager with this provider.

import { SessionWalletProvider, useSessionKeyManager } from '@gumhq/react-sdk';
import { AnchorWallet, useAnchorWallet, useConnection } from '@solana/wallet-adapter-react';

interface SessionProviderProps {
  children: React.ReactNode;
}

const SessionProvider: React.FC<SessionProviderProps> = ({ children }) => {
  const { connection } = useConnection();
  const anchorWallet = useAnchorWallet() as AnchorWallet;
  const cluster = "devnet"; // or "mainnet-beta", "testnet", "localnet"
  const sessionWallet = useSessionKeyManager(anchorWallet, connection, cluster);

  return (
    <SessionWalletProvider sessionWallet={sessionWallet}>
      {children}
    </SessionWalletProvider>
  );
};

export default SessionProvider;