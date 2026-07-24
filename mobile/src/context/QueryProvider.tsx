/**
 * TanStack Query configuration tuned for mobile.
 *
 *  - `staleTime` keeps data warm so tab switches don't refetch constantly.
 *  - Retries only transient failures (network / timeout / 5xx), never 4xx.
 *  - `focusManager` is wired to React Native's AppState so returning to the
 *    foreground triggers a background refetch (the RN analog of window focus).
 *  - `refetchOnReconnect` refreshes after connectivity returns.
 *  - `onlineManager` extension point is prepared for Phase 11 NetInfo wiring.
 */
import React, { useEffect, useState } from "react";
import { AppState, type AppStateStatus, Platform } from "react-native";
import {
  focusManager,
  onlineManager,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { isRetryableError } from "@/api/errors";

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        refetchOnReconnect: true,
        refetchOnWindowFocus: true,
        networkMode: "online",
        retry: (failureCount, error) =>
          isRetryableError(error) && failureCount < 2,
        retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
      },
      mutations: {
        retry: 0,
        networkMode: "online",
      },
    },
  });
}

/**
 * Phase 11 extension point: wire `onlineManager` to NetInfo.
 *
 * Example (do not enable until offline mode ships):
 * ```
 * import NetInfo from "@react-native-community/netinfo";
 * onlineManager.setEventListener((setOnline) =>
 *   NetInfo.addEventListener((state) => {
 *     setOnline(Boolean(state.isConnected && state.isInternetReachable !== false));
 *   }),
 * );
 * ```
 */
function prepareOnlineManagerExtensionPoint() {
  // Keep Query aware that we manage online state ourselves later.
  // Until NetInfo is wired, the default (always online) remains.
  void onlineManager;
}

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(makeQueryClient);

  useEffect(() => {
    prepareOnlineManagerExtensionPoint();

    function onAppStateChange(state: AppStateStatus) {
      if (Platform.OS !== "web") {
        focusManager.setFocused(state === "active");
      }
    }
    const subscription = AppState.addEventListener("change", onAppStateChange);
    return () => subscription.remove();
  }, []);

  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}
