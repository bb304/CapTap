import { StatusBar } from 'expo-status-bar';
import { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';

const API_URL = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5001';

type HealthResponse = {
  status: string;
  database?: string;
};

export default function App() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const checkBackend = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_URL}/health`);
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = (await response.json()) as HealthResponse;
      setHealth(data);
    } catch (err) {
      setHealth(null);
      setError(err instanceof Error ? err.message : 'Unable to reach API');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void checkBackend();
  }, [checkBackend]);

  return (
    <View style={styles.container}>
      <Text style={styles.brand}>CapTap</Text>
      <Text style={styles.tagline}>Tap. Confirm. Peace of mind.</Text>

      <View style={styles.statusCard}>
        <Text style={styles.statusLabel}>Backend connection</Text>
        {loading ? (
          <ActivityIndicator size="large" color="#2B6CB0" />
        ) : error ? (
          <Text style={styles.errorText}>Offline — {error}</Text>
        ) : (
          <>
            <Text style={styles.okText}>Connected ({health?.status})</Text>
            {health?.database ? (
              <Text style={styles.metaText}>Database: {health.database}</Text>
            ) : null}
          </>
        )}
        <Text style={styles.metaText}>API: {API_URL}</Text>
      </View>

      <Pressable style={styles.button} onPress={() => void checkBackend()}>
        <Text style={styles.buttonText}>Retry health check</Text>
      </Pressable>

      <StatusBar style="dark" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F7FAFC',
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 24,
  },
  brand: {
    fontSize: 40,
    fontWeight: '700',
    color: '#1A365D',
    marginBottom: 8,
  },
  tagline: {
    fontSize: 16,
    color: '#4A5568',
    marginBottom: 32,
  },
  statusCard: {
    width: '100%',
    backgroundColor: '#FFFFFF',
    borderRadius: 16,
    padding: 20,
    gap: 8,
    marginBottom: 20,
    borderWidth: 1,
    borderColor: '#E2E8F0',
  },
  statusLabel: {
    fontSize: 14,
    fontWeight: '600',
    color: '#2D3748',
    marginBottom: 4,
  },
  okText: {
    fontSize: 18,
    color: '#276749',
    fontWeight: '600',
  },
  errorText: {
    fontSize: 16,
    color: '#C53030',
    fontWeight: '600',
  },
  metaText: {
    fontSize: 13,
    color: '#718096',
  },
  button: {
    backgroundColor: '#2B6CB0',
    paddingVertical: 14,
    paddingHorizontal: 24,
    borderRadius: 12,
    minWidth: 220,
    alignItems: 'center',
  },
  buttonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
});
