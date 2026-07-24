/**
 * TanStack Query + offline persistence + NetInfo onlineManager.
 */
import React, { useEffect, useState } from "react";
import { AppState, type AppStateStatus, Platform } from "react-native";
import NetInfo from "@react-native-community/netinfo";
import { focusManager, onlineManager, QueryClient } from "@tanstack/react-query";
import { PersistQueryClientProvider } from "@tanstack/react-query-persist-client";
import { isRetryableError } from "@/api/errors";
import { LocalDatabase } from "@/services/localDatabase";
import { createSqlitePersister, shouldDehydrateQuery } from "@/services/queryPersister";
import { attachSyncEngine } from "@/services/syncEngine";

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 24 * 60 * 60_000, // keep cached slices for a day offline
        refetchOnReconnect: true,
        refetchOnWindowFocus: true,
        networkMode: "offlineFirst",
        retry: (failureCount, error) => isRetryableError(error) && failureCount < 2,
        retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
      },
      mutations: {
        retry: 0,
        networkMode: "offlineFirst",
      },
    },
  });
}

function wireOnlineManager() {
  return onlineManager.setEventListener((setOnline) => {
    let debounce: ReturnType<typeof setTimeout> | undefined;
    const unsubscribe = NetInfo.addEventListener((state) => {
      // Simulator NetInfo often flaps isInternetReachable null↔true; debounce
      // so reconnect refetches don't thrash the UI.
      const online = Boolean(state.isConnected && state.isInternetReachable !== false);
      clearTimeout(debounce);
      debounce = setTimeout(() => setOnline(online), 400);
    });
    return () => {
      clearTimeout(debounce);
      unsubscribe();
    };
  });
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(makeQueryClient);
  const [persister] = useState(createSqlitePersister);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      await LocalDatabase.ready();
      if (!cancelled) setReady(true);
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!ready) return;
    const unsubscribeNetInfo = wireOnlineManager();
    const detachSync = attachSyncEngine(queryClient);

    function onAppStateChange(state: AppStateStatus) {
      if (Platform.OS !== "web") {
        focusManager.setFocused(state === "active");
      }
    }
    const subscription = AppState.addEventListener("change", onAppStateChange);
    return () => {
      unsubscribeNetInfo();
      detachSync();
      subscription.remove();
    };
  }, [ready, queryClient]);

  if (!ready) {
    return null;
  }

  return (
    <PersistQueryClientProvider
      client={queryClient}
      persistOptions={{
        persister,
        maxAge: 1000 * 60 * 60 * 24 * 7,
        dehydrateOptions: {
          shouldDehydrateQuery: (query) =>
            query.state.status === "success" && shouldDehydrateQuery(query),
        },
      }}
    >
      {children}
    </PersistQueryClientProvider>
  );
}
